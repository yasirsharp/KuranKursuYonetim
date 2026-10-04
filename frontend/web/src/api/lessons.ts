import apiClient from './client';
import type { ILesson, ICreateLesson, IUpdateLesson } from '../types/lesson';
import type { IApiResponse } from '../types/common';

export async function getLessons(): Promise<ILesson[]> {
  const response = await apiClient.get<IApiResponse<ILesson[]>>('/api/lessons');
  return response.data.data;
}

export async function getLessonById(id: string): Promise<ILesson> {
  const response = await apiClient.get<IApiResponse<ILesson>>(`/api/lessons/${id}`);
  return response.data.data;
}

export async function getStudentLessons(studentId: string): Promise<ILesson[]> {
  const response = await apiClient.get<IApiResponse<ILesson[]>>(`/api/students/${studentId}/lessons`);
  return response.data.data;
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
