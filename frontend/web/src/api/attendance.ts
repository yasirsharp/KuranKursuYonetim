import apiClient from './client';
import type { IAttendance, ICreateAttendance, IBulkAttendance } from '../types/attendance';
import type { IApiResponse, IPaginatedResponse } from '../types/common';

export async function getAttendances(page: number = 0, size: number = 100): Promise<IPaginatedResponse<IAttendance>> {
  const response = await apiClient.get<IApiResponse<IPaginatedResponse<IAttendance>>>(`/api/attendance?page=${page}&size=${size}`);
  return response.data.data;
}

export async function getStudentAttendances(studentId: string, page: number = 0, size: number = 10): Promise<IPaginatedResponse<IAttendance>> {
  const response = await apiClient.get<IApiResponse<IPaginatedResponse<IAttendance>>>(`/api/attendance/student/${studentId}?page=${page}&size=${size}`);
  return response.data.data;
}

export async function createAttendance(data: ICreateAttendance): Promise<void> {
  await apiClient.post('/api/attendance', data);
}

export async function createBulkAttendance(data: IBulkAttendance): Promise<void> {
  await apiClient.post('/api/attendance/bulk', data);
}
