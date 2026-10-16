import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import axios, { type AxiosAdapter, type InternalAxiosRequestConfig } from 'axios';
import apiClient, { getErrorMessage, getFieldErrors } from '../client';
import { readSession, writeSession } from '../../services/session';
import type { IAuthUser } from '../../types/auth';

const user: IAuthUser = {
  kind: 'tenant',
  token: 'eski-token',
  expiration: new Date(Date.now() - 1000).toISOString(),
  refreshToken: 'refresh-1',
  refreshTokenExpiration: new Date(Date.now() + 86_400_000).toISOString(),
  tenantId: 'k1',
  tenantStatus: 'active',
  role: 'head_teacher',
  firstName: 'Ahmet',
  lastName: 'Yılmaz',
};

const respond = (config: InternalAxiosRequestConfig, status: number, data: unknown) => {
  const response = { data, status, statusText: '', headers: {}, config };
  if (status >= 400) {
    return Promise.reject(new axios.AxiosError('hata', String(status), config, null, response));
  }
  return Promise.resolve(response);
};

describe('apiClient', () => {
  const originalClientAdapter = apiClient.defaults.adapter;
  const originalGlobalAdapter = axios.defaults.adapter;
  const assign = vi.fn();

  beforeEach(() => {
    vi.stubGlobal('location', { ...window.location, pathname: '/students', assign });
    assign.mockReset();
  });

  afterEach(() => {
    apiClient.defaults.adapter = originalClientAdapter;
    axios.defaults.adapter = originalGlobalAdapter;
    vi.unstubAllGlobals();
  });

  it('401 alınca oturumu bir kez yenileyip isteği yeni token ile tekrarlar', async () => {
    writeSession(user);
    const seenTokens: string[] = [];

    apiClient.defaults.adapter = ((config) => {
      const auth = String(config.headers.Authorization);
      seenTokens.push(auth);
      return auth === 'Bearer yeni-token' ? respond(config, 200, { ok: true }) : respond(config, 401, {});
    }) as AxiosAdapter;
    axios.defaults.adapter = ((config) =>
      respond(config, 200, {
        success: true,
        data: {
          token: 'yeni-token',
          expiration: new Date(Date.now() + 3_600_000).toISOString(),
          refreshToken: 'refresh-2',
          refreshTokenExpiration: user.refreshTokenExpiration,
          tenantStatus: 'active',
        },
      })) as AxiosAdapter;

    const result = await apiClient.get('/api/students');

    expect(result.data).toEqual({ ok: true });
    expect(seenTokens).toEqual(['Bearer eski-token', 'Bearer yeni-token']);
    expect(readSession()?.refreshToken).toBe('refresh-2');
    expect(assign).not.toHaveBeenCalled();
  });

  it('yenileme başarısızsa oturumu kapatıp girişe yönlendirir', async () => {
    writeSession(user);
    apiClient.defaults.adapter = ((config) => respond(config, 401, {})) as AxiosAdapter;
    axios.defaults.adapter = ((config) => respond(config, 401, {})) as AxiosAdapter;

    await expect(apiClient.get('/api/students')).rejects.toBeTruthy();

    expect(readSession()).toBeNull();
    expect(assign).toHaveBeenCalledWith('/login');
  });

  it('giriş uçlarındaki 401 (hatalı kod) oturumu düşürmez', async () => {
    apiClient.defaults.adapter = ((config) => respond(config, 401, { message: 'Doğrulama kodu hatalı.' })) as AxiosAdapter;

    await expect(apiClient.post('/api/auth/verify-otp', {})).rejects.toBeTruthy();

    expect(assign).not.toHaveBeenCalled();
  });

  it('kurs onay bekliyor (403 tenant_pending) ise durumu kaydedip başvuru ekranına götürür', async () => {
    writeSession({ ...user, expiration: new Date(Date.now() + 3_600_000).toISOString() });
    apiClient.defaults.adapter = ((config) => respond(config, 403, { code: 'tenant_pending' })) as AxiosAdapter;

    await expect(apiClient.get('/api/students')).rejects.toBeTruthy();

    expect(readSession()?.tenantStatus).toBe('pending');
    expect(assign).toHaveBeenCalledWith('/basvuru-durumu');
  });

  it('doğrulama hatalarını mesaja ve alan adına çevirir', async () => {
    apiClient.defaults.adapter = ((config) =>
      respond(config, 400, {
        message: 'Doğrulama hatası.',
        errors: [{ propertyName: 'CourseName', errorMessage: 'Kurs adı boş olamaz.' }],
      })) as AxiosAdapter;

    const error = await apiClient.post('/api/course-applications', {}).catch((e: unknown) => e);

    expect(getErrorMessage(error)).toBe('Kurs adı boş olamaz.');
    expect(getFieldErrors(error)).toEqual({ courseName: 'Kurs adı boş olamaz.' });
  });
});
