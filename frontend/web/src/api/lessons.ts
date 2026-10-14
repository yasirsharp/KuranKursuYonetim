import apiClient from './client';
import type { ILesson, ICreateLesson, IUpdateLesson } from '../types/lesson';
import type { IApiResponse } from '../types/common';

export async function getLessons(page: number = 1, pageSize: number = 100): Promise<ILesson[]> {
  const response = await apiClient.get<IApiResponse<any>>(`/api/lessons?page=${page}&pageSize=${pageSize}`);
  const payload = response.data?.data;
  if (Array.isArray(payload)) return payload;
  if (payload && Array.isArray(payload.data)) return payload.data;
  if (payload && Array.isArray(payload.items)) return payload.items;
  return [];
}

export async function getLessonById(id: string): Promise<ILesson> {
  const response = await apiClient.get<IApiResponse<ILesson>>(`/api/lessons/${id}`);
  return response.data.data;
}

export async function getStudentLessons(studentId: string, page: number = 1, pageSize: number = 100): Promise<ILesson[]> {
  const response = await apiClient.get<IApiResponse<any>>(`/api/lessons/student/${studentId}?page=${page}&pageSize=${pageSize}`);
  const payload = response.data?.data;
  if (Array.isArray(payload)) return payload;
  if (payload && Array.isArray(payload.data)) return payload.data;
  if (payload && Array.isArray(payload.items)) return payload.items;
  return [];
}

export async function createLesson(data: ICreateLesson): Promise<void> {
  await apiClient.post('/api/lessons', data);
}

export async function updateLesson(data: IUpdateLesson): Promise<void> {
  await apiClient.put('/api/lessons', data);
}

export async function deleteLesson(id: string): Promise<void> {
  await apiClient.delete(`/api/lessons/${id}`);
}
