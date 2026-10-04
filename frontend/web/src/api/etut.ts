import apiClient from './client';
import type { IDailyAssignment, ICompleteEtutAssignment, ICreateDailyAssignment } from '../types/etut';
import type { IApiResponse, IPaginatedResponse } from '../types/common';

export async function getEtutAssignments(date: string): Promise<IPaginatedResponse<IDailyAssignment>> {
  const response = await apiClient.get<IApiResponse<IPaginatedResponse<IDailyAssignment>>>(
    `/api/dailyassignments/etut?date=${date}`
  );
  return response.data.data;
}

export async function completeEtutAssignment(data: ICompleteEtutAssignment): Promise<void> {
  await apiClient.post('/api/dailyassignments/complete-etut', data);
}

export async function createDailyAssignment(data: ICreateDailyAssignment): Promise<void> {
  await apiClient.post('/api/dailyassignments', data);
}
