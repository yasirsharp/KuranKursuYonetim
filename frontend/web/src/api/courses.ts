import apiClient from './client';
import type { IApiResponse, IPaginatedResponse } from '../types/common';
import type { ITokenResponse, TenantStatus } from '../types/auth';
import type {
  ICourseApplication,
  ICourseApplicationInfo,
  ICourseApplicationStatus,
  ICourseInfo,
  ITenant,
  ITenantSettings,
} from '../types/course';

// ── Kurs başvurusu (herkese açık) ───────────────────────────────────

export async function sendApplicationCode(phoneNumber: string): Promise<void> {
  await apiClient.post('/api/course-applications/send-code', { phoneNumber });
}

export async function submitApplication(data: ICourseApplication): Promise<ITokenResponse> {
  const response = await apiClient.post<IApiResponse<ITokenResponse>>('/api/course-applications', data);
  return response.data.data;
}

export async function getMyApplication(): Promise<ICourseApplicationStatus> {
  const response = await apiClient.get<IApiResponse<ICourseApplicationStatus>>('/api/course-applications/me');
  return response.data.data;
}

export async function resubmitApplication(data: ICourseApplicationInfo): Promise<void> {
  await apiClient.put('/api/course-applications/me', data);
}

// ── Platform yöneticisi ─────────────────────────────────────────────

export async function getTenants(params: {
  status?: TenantStatus;
  search?: string;
  page?: number;
  pageSize?: number;
}): Promise<IPaginatedResponse<ITenant>> {
  const response = await apiClient.get<IApiResponse<IPaginatedResponse<ITenant>>>('/api/tenants', {
    params: { pageSize: 50, ...params },
  });
  return response.data.data;
}

export async function approveTenant(id: string): Promise<string> {
  const response = await apiClient.post<IApiResponse<null>>(`/api/tenants/${id}/approve`);
  return response.data.message;
}

export async function rejectTenant(id: string, reason: string): Promise<string> {
  const response = await apiClient.post<IApiResponse<null>>(`/api/tenants/${id}/reject`, { reason });
  return response.data.message;
}

// ── Kurs ayarları ───────────────────────────────────────────────────

export async function getSettings(): Promise<ITenantSettings> {
  const response = await apiClient.get<IApiResponse<ITenantSettings>>('/api/settings');
  return response.data.data;
}

export async function updateSettings(settings: ITenantSettings): Promise<void> {
  await apiClient.put('/api/settings', settings);
}

export async function getCourseInfo(): Promise<ICourseInfo> {
  const response = await apiClient.get<IApiResponse<ICourseInfo>>('/api/settings/course');
  return response.data.data;
}

export async function updateCourseInfo(data: { name: string; contactEmail?: string; contactPhone?: string }): Promise<void> {
  await apiClient.put('/api/settings/course', data);
}
