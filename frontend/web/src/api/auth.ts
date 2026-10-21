import apiClient from './client';
import type {
  ISendOtpRequest,
  IVerifyOtpRequest,
  IVerifyEmailCodeRequest,
  ITokenResponse,
  IPlatformTokenResponse,
  IAuthUser,
  UserRole,
} from '../types/auth';
import type { IApiResponse } from '../types/common';

export async function sendOtp(data: ISendOtpRequest): Promise<void> {
  await apiClient.post('/api/auth/send-otp', data);
}

export async function verifyOtp(data: IVerifyOtpRequest): Promise<ITokenResponse> {
  const response = await apiClient.post<IApiResponse<ITokenResponse>>(
    '/api/auth/verify-otp',
    data
  );
  return response.data.data;
}

export async function sendEmailCode(email: string): Promise<void> {
  await apiClient.post('/api/auth/send-email-code', { email });
}

export async function verifyEmailCode(data: IVerifyEmailCodeRequest): Promise<ITokenResponse> {
  const response = await apiClient.post<IApiResponse<ITokenResponse>>('/api/auth/verify-email-code', data);
  return response.data.data;
}

/** Çıkış: refresh token sunucuda iptal edilir (hata olsa da yerel oturum kapatılır) */
export async function logoutSession(refreshToken: string): Promise<void> {
  await apiClient.post('/api/auth/logout', { refreshToken });
}

export async function platformLogin(email: string, password: string): Promise<IPlatformTokenResponse> {
  const response = await apiClient.post<IApiResponse<IPlatformTokenResponse>>(
    '/api/platform/auth/login',
    { email, password }
  );
  return response.data.data;
}

/** Backend token yanıtını oturum kaydına çevirir */
export function toAuthUser(result: ITokenResponse): IAuthUser {
  return {
    kind: 'tenant',
    token: result.token,
    expiration: result.expiration,
    refreshToken: result.refreshToken,
    refreshTokenExpiration: result.refreshTokenExpiration,
    userId: result.userId,
    tenantId: result.tenantId,
    tenantStatus: result.tenantStatus,
    role: result.role as UserRole,
    firstName: result.firstName,
    lastName: result.lastName,
  };
}

export function platformToAuthUser(result: IPlatformTokenResponse): IAuthUser {
  return {
    kind: 'platform',
    token: result.token,
    expiration: result.expiration,
    userId: result.userId,
    tenantId: '',
    role: result.role as UserRole,
    firstName: 'Platform',
    lastName: 'Yöneticisi',
    email: result.email,
  };
}
