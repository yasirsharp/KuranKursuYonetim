import apiClient from './client';
import type { IDailyAssignment, ICompleteEtutAssignment, ICreateDailyAssignment } from '../types/etut';
import type { IApiResponse, IPaginatedResponse } from '../types/common';

export async function getEtutAssignments(date: string, page: number = 1, pageSize: number = 100): Promise<IPaginatedResponse<IDailyAssignment>> {
  const response = await apiClient.get<IApiResponse<IPaginatedResponse<IDailyAssignment>>>(
    `/api/dailyassignments/etut?date=${date}&page=${page}&pageSize=${pageSize}`
  );
  const payload = response.data?.data;
  const list = payload?.data ?? payload?.items ?? [];
  return {
    ...payload,
    data: list,
    items: list,
    totalCount: payload?.totalCount ?? list.length,
    page: payload?.page ?? page,
    pageSize: payload?.pageSize ?? pageSize,
  };
}

export async function completeEtutAssignment(data: ICompleteEtutAssignment): Promise<void> {
  await apiClient.post('/api/dailyassignments/complete-etut', data);
}

export async function createDailyAssignment(data: ICreateDailyAssignment): Promise<void> {
  await apiClient.post('/api/dailyassignments', data);
}
