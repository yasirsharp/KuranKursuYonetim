import apiClient from './client';
import type { IAttendance, ICreateAttendance, IBulkAttendance } from '../types/attendance';
import type { IApiResponse, IPaginatedResponse } from '../types/common';

export async function getAttendances(page: number = 1, pageSize: number = 100): Promise<IPaginatedResponse<IAttendance>> {
  const response = await apiClient.get<IApiResponse<IPaginatedResponse<IAttendance>>>(`/api/attendance?page=${page}&pageSize=${pageSize}`);
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

export async function getStudentAttendances(studentId: string, page: number = 1, pageSize: number = 30): Promise<IPaginatedResponse<IAttendance>> {
  const response = await apiClient.get<IApiResponse<IPaginatedResponse<IAttendance>>>(`/api/attendance/student/${studentId}?page=${page}&pageSize=${pageSize}`);
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

export async function createAttendance(data: ICreateAttendance): Promise<void> {
  await apiClient.post('/api/attendance', data);
}

export async function createBulkAttendance(data: IBulkAttendance): Promise<void> {
  await apiClient.post('/api/attendance/bulk', data);
}
