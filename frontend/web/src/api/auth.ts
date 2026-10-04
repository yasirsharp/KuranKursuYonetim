import apiClient from './client';
import type { ISendOtpRequest, IVerifyOtpRequest, ITokenResponse } from '../types/auth';
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
