import { describe, it, expect, vi, beforeEach } from 'vitest';
import apiClient from '../client';
import { getLessons, getStudentLessons, getLessonById, createLesson, updateLesson, deleteLesson } from '../lessons';

vi.mock('../client', () => ({
  default: {
    get: vi.fn(),
    post: vi.fn(),
    put: vi.fn(),
    delete: vi.fn(),
  },
}));

describe('lessons api', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('getLessons', () => {
    it('backend PagedResult yanıtını diziye açmalı', async () => {
      const mockLessons = [
        { id: 'l1', cuzNo: 1, pageNo: 5, performance: 'iyi' },
      ];

      vi.mocked(apiClient.get).mockResolvedValueOnce({
        data: {
          success: true,
          data: {
            data: mockLessons,
            totalCount: 1,
            page: 1,
            pageSize: 100,
          },
        },
      });

      const result = await getLessons();

      expect(apiClient.get).toHaveBeenCalledWith('/api/lessons?page=1&pageSize=100');
      expect(Array.isArray(result)).toBe(true);
      expect(result).toHaveLength(1);
      expect(result[0]?.pageNo).toBe(5);
    });
  });

  describe('getStudentLessons', () => {
    it('öğrenci derslerini backend rotasıyla (/api/lessons/student/{id}) çağırmalı ve dizi dönmeli', async () => {
      const studentId = 'student-123';
      const mockLessons = [
        { id: 'l2', studentId, cuzNo: 2, pageNo: 10 },
      ];

      vi.mocked(apiClient.get).mockResolvedValueOnce({
        data: {
          success: true,
          data: {
            data: mockLessons,
            totalCount: 1,
            page: 1,
            pageSize: 100,
          },
        },
      });

      const result = await getStudentLessons(studentId);

      expect(apiClient.get).toHaveBeenCalledWith(
        `/api/lessons/student/${studentId}?page=1&pageSize=100`
      );
      expect(Array.isArray(result)).toBe(true);
      expect(result).toHaveLength(1);
      expect(result[0]?.cuzNo).toBe(2);
    });
  });

  describe('getLessonById', () => {
    it('tekil ders nesnesini getirmeli', async () => {
      const lesson = { id: 'l1', pageNo: 12 };
      vi.mocked(apiClient.get).mockResolvedValueOnce({
        data: { success: true, data: lesson },
      });

      const result = await getLessonById('l1');

      expect(apiClient.get).toHaveBeenCalledWith('/api/lessons/l1');
      expect(result).toEqual(lesson);
    });
  });

  describe('createLesson & updateLesson', () => {
    it('createLesson POST isteği göndermeli', async () => {
      vi.mocked(apiClient.post).mockResolvedValueOnce({ data: { success: true } });

      const newLesson = { studentId: 's1', pageNo: 15 };
      await createLesson(newLesson as any);

      expect(apiClient.post).toHaveBeenCalledWith('/api/lessons', newLesson);
    });

    it('updateLesson PUT isteği göndermeli', async () => {
      vi.mocked(apiClient.put).mockResolvedValueOnce({ data: { success: true } });

      const updateData = { id: 'l1', performance: 'pekiyi' };
      await updateLesson(updateData as any);

      expect(apiClient.put).toHaveBeenCalledWith('/api/lessons', updateData);
    });

    it('deleteLesson DELETE isteği göndermeli', async () => {
      vi.mocked(apiClient.delete).mockResolvedValueOnce({ data: { success: true } });

      await deleteLesson('l1');

      expect(apiClient.delete).toHaveBeenCalledWith('/api/lessons/l1');
    });
  });
});
