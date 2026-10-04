import apiClient from './client';
import type { IStudent, ICreateStudent, IUpdateStudent } from '../types/student';
import type { IApiResponse } from '../types/common';

export async function getStudents(): Promise<IStudent[]> {
  const response = await apiClient.get<IApiResponse<IStudent[]>>('/api/students');
  return response.data.data;
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
