import axios from 'axios';
import { Logger } from '../services/logger';
import { API_BASE_URL, STORAGE_KEYS } from '../utils/constants';

const log = Logger.create('ApiClient');

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
    (config as unknown as Record<string, unknown>)._startTime = Date.now();

    return config;
  },
  (error: unknown) => {
    log.error('request', 'İstek oluşturulamadı', error);
    return Promise.reject(error);
  }
);

// ── Response Interceptor ─────────────────────────────────────────────
apiClient.interceptors.response.use(
  (response) => {
    const duration = Date.now() - ((response.config as unknown as Record<string, unknown>)._startTime as number || Date.now());
    log.info(
      'response',
      `← ${response.status} ${response.config.url} (${duration}ms)`
    );
    return response;
  },
  (error: unknown) => {
    if (axios.isAxiosError(error)) {
      const status = error.response?.status;
      const url = error.config?.url;
      const duration = Date.now() - ((error.config as unknown as Record<string, unknown> | undefined)?._startTime as number || Date.now());

      log.error(
        'response',
        `← ${status || 'NETWORK_ERROR'} ${url} (${duration}ms)`,
        error.response?.data
      );

      // 401 → Token geçersiz, login'e yönlendir
      if (status === 401) {
        log.warn('response', 'Token geçersiz — oturum sonlandırılıyor');
        localStorage.removeItem(STORAGE_KEYS.TOKEN);
        localStorage.removeItem(STORAGE_KEYS.USER);
        window.location.href = '/login';
      }
    } else {
      log.error('response', 'Bilinmeyen hata', error);
    }

    return Promise.reject(error);
  }
);

export default apiClient;
