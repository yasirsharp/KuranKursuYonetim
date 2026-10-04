import apiClient from './client';
import type { IDashboardSummary, IStudentProgress, IGroupOverview } from '../types/dashboard';
import type { IApiResponse } from '../types/common';

export async function getDashboardSummary(): Promise<IDashboardSummary> {
  const response = await apiClient.get<IApiResponse<IDashboardSummary>>('/api/dashboard/summary');
  return response.data.data;
}

export async function getStudentProgress(studentId: string): Promise<IStudentProgress> {
  const response = await apiClient.get<IApiResponse<IStudentProgress>>(`/api/dashboard/progress/${studentId}`);
  return response.data.data;
}

export async function getGroupOverview(groupId: string): Promise<IGroupOverview> {
  const response = await apiClient.get<IApiResponse<IGroupOverview>>(
    `/api/dashboard/group/${groupId}/overview`
  );
  return response.data.data;
}
