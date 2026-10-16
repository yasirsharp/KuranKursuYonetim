import axios, { type AxiosRequestConfig } from 'axios';
import { Logger } from '../services/logger';
import { API_BASE_URL, STORAGE_KEYS } from '../utils/constants';
import { clearSession, readSession, writeSession } from '../services/session';
import type { IApiResponse } from '../types/common';
import type { ITokenResponse, TenantStatus } from '../types/auth';

const log = Logger.create('ApiClient');

/** Kurs durumu ekranı (onay bekliyor / reddedildi) */
export const APPLICATION_STATUS_PATH = '/basvuru-durumu';

/**
 * Axios instance — JWT interceptor + otomatik loglama
 */
const apiClient = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
  timeout: 15000,
});

// Giriş/başvuru uçlarındaki 401 "kod hatalı" demektir; oturum düşürülmez, sayfa kendisi gösterir
const ANONYMOUS_ENDPOINTS = ['/api/auth/', '/api/platform/auth/', '/api/course-applications/send-code'];
const isAnonymousEndpoint = (url = '') =>
  ANONYMOUS_ENDPOINTS.some((p) => url.startsWith(p)) || url === '/api/course-applications';

type RetriableConfig = AxiosRequestConfig & { _retried?: boolean; _startTime?: number };

// ── Request Interceptor ──────────────────────────────────────────────
apiClient.interceptors.request.use(
  (config) => {
    // JWT token ekle
    const token = localStorage.getItem(STORAGE_KEYS.TOKEN);
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }

    log.info(
      'request',
      `→ ${config.method?.toUpperCase()} ${config.url}`,
      config.data ? { body: config.data } : undefined
    );

    // Zamanlama için başlangıç zamanını metadata'ya ekle
    (config as RetriableConfig)._startTime = Date.now();

    return config;
  },
  (error: unknown) => {
    log.error('request', 'İstek oluşturulamadı', error);
    return Promise.reject(error);
  }
);

// ── Oturum yenileme (aynı anda gelen 401'ler tek refresh isteği paylaşır) ──
let refreshInFlight: Promise<boolean> | null = null;

async function refreshSession(): Promise<boolean> {
  const user = readSession();
  if (!user?.refreshToken) return false;

  try {
    // apiClient değil: interceptor döngüsüne girmesin
    const response = await axios.post<IApiResponse<ITokenResponse>>(
      `${API_BASE_URL}/api/auth/refresh`,
      { refreshToken: user.refreshToken },
      { headers: { 'Content-Type': 'application/json' }, timeout: 15000 }
    );
    const data = response.data.data;
    writeSession({
      ...user,
      token: data.token,
      expiration: data.expiration,
      refreshToken: data.refreshToken,
      refreshTokenExpiration: data.refreshTokenExpiration,
      tenantStatus: data.tenantStatus,
    });
    log.info('refresh', 'Oturum yenilendi');
    return true;
  } catch (err) {
    log.warn('refresh', 'Oturum yenilenemedi', err);
    return false;
  }
}

function redirectTo(path: string) {
  if (window.location.pathname !== path) window.location.assign(path);
}

function endSession(reason?: string) {
  const wasPlatform = readSession()?.kind === 'platform';
  clearSession();
  const target = wasPlatform ? '/platform/giris' : '/login';
  redirectTo(reason ? `${target}?neden=${reason}` : target);
}

// ── Response Interceptor ─────────────────────────────────────────────
apiClient.interceptors.response.use(
  (response) => {
    const duration = Date.now() - ((response.config as RetriableConfig)._startTime || Date.now());
    log.info(
      'response',
      `← ${response.status} ${response.config.url} (${duration}ms)`
    );
    return response;
  },
  async (error: unknown) => {
    if (!axios.isAxiosError(error)) {
      log.error('response', 'Bilinmeyen hata', error);
      return Promise.reject(error);
    }

    const status = error.response?.status;
    const config = error.config as RetriableConfig | undefined;
    const url = config?.url;
    const duration = Date.now() - (config?._startTime || Date.now());
    const body = error.response?.data as { code?: string } | undefined;

    log.error('response', `← ${status || 'NETWORK_ERROR'} ${url} (${duration}ms)`, error.response?.data);

    if (status === 401 && config && !isAnonymousEndpoint(url)) {
      // Access token süresi dolmuş olabilir: bir kez yenileyip isteği tekrarla
      if (!config._retried && readSession()?.refreshToken) {
        config._retried = true;
        refreshInFlight ??= refreshSession().finally(() => { refreshInFlight = null; });
        // Tekrarlanan istek yeni token'ı request interceptor'dan alır
        if (await refreshInFlight) return apiClient(config);
      }
      log.warn('response', 'Oturum geçersiz — çıkış yapılıyor');
      endSession();
    }

    if (status === 403 && (body?.code === 'tenant_pending' || body?.code === 'tenant_rejected')) {
      // Kurs onay bekliyor / reddedildi: oturumu güncelle, durum ekranına götür
      const user = readSession();
      const tenantStatus: TenantStatus = body.code === 'tenant_pending' ? 'pending' : 'rejected';
      if (user && user.tenantStatus !== tenantStatus) writeSession({ ...user, tenantStatus });
      redirectTo(APPLICATION_STATUS_PATH);
    }

    if (status === 403 && body?.code === 'tenant_inactive') {
      endSession('kurs-pasif');
    }

    return Promise.reject(error);
  }
);

/** Backend hata yanıtından kullanıcıya gösterilecek mesaj (doğrulama hataları dahil) */
export function getErrorMessage(error: unknown, fallback = 'Bir hata oluştu. Lütfen tekrar deneyin.'): string {
  if (axios.isAxiosError(error)) {
    const data = error.response?.data as
      | { message?: string; errors?: { propertyName: string; errorMessage: string }[] }
      | undefined;
    if (data?.errors?.length) return data.errors.map((e) => e.errorMessage).join(' ');
    if (data?.message) return data.message;
    if (!error.response) return 'Sunucuya ulaşılamadı. İnternet bağlantınızı kontrol edin.';
  }
  return fallback;
}

/** Doğrulama hatalarını alan adına göre gruplar (form alanlarının altında göstermek için) */
export function getFieldErrors(error: unknown): Record<string, string> {
  const result: Record<string, string> = {};
  if (axios.isAxiosError(error)) {
    const data = error.response?.data as { errors?: { propertyName: string; errorMessage: string }[] } | undefined;
    for (const e of data?.errors ?? []) {
      const key = e.propertyName.charAt(0).toLowerCase() + e.propertyName.slice(1);
      result[key] ??= e.errorMessage;
    }
  }
  return result;
}

export default apiClient;
