import apiClient from './client';
import type { IStudent, ICreateStudent, IUpdateStudent } from '../types/student';
import type { IApiResponse } from '../types/common';

export async function getStudents(page: number = 1, pageSize: number = 100): Promise<IStudent[]> {
  const response = await apiClient.get<IApiResponse<any>>(`/api/students?page=${page}&pageSize=${pageSize}`);
  const payload = response.data?.data;
  if (Array.isArray(payload)) {
    return payload;
  }
  if (payload && Array.isArray(payload.data)) {
    return payload.data;
  }
  if (payload && Array.isArray(payload.items)) {
    return payload.items;
  }
  return [];
}

export async function getStudentById(id: string): Promise<IStudent> {
  const response = await apiClient.get<IApiResponse<IStudent>>(`/api/students/${id}`);
  return response.data.data;
}

export async function createStudent(data: ICreateStudent): Promise<void> {
  await apiClient.post('/api/students', data);
}

export async function updateStudent(data: IUpdateStudent): Promise<void> {
  await apiClient.put('/api/students', data);
}

export async function deleteStudent(id: string): Promise<void> {
  await apiClient.delete(`/api/students/${id}`);
}
