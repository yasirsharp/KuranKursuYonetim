import { describe, it, expect, vi, beforeEach } from 'vitest';
import apiClient from '../client';
import { getAttendances, getStudentAttendances, createAttendance, createBulkAttendance } from '../attendance';

vi.mock('../client', () => ({
  default: {
    get: vi.fn(),
    post: vi.fn(),
  },
}));

describe('attendance api', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('getAttendances', () => {
    it('1-tabanlı sayfalama parametreleri ile çağırmalı ve data ile items alanlarını normalize etmeli', async () => {
      const mockList = [
        { id: 'a1', studentId: 's1', status: 'present', date: '2026-10-12' },
      ];

      vi.mocked(apiClient.get).mockResolvedValueOnce({
        data: {
          success: true,
          data: {
            data: mockList,
            totalCount: 1,
            page: 1,
            pageSize: 100,
          },
        },
      });

      const result = await getAttendances(1, 100);

      expect(apiClient.get).toHaveBeenCalledWith('/api/attendance?page=1&pageSize=100');
      expect(result.data).toEqual(mockList);
      expect(result.items).toEqual(mockList);
      expect(result.totalCount).toBe(1);
    });
  });

  describe('getStudentAttendances', () => {
    it('öğrenciye özel rotayı çağırmalı ve normalize etmeli', async () => {
      const studentId = 'st-99';
      const mockList = [
        { id: 'a2', studentId, status: 'late', date: '2026-10-12' },
      ];

      vi.mocked(apiClient.get).mockResolvedValueOnce({
        data: {
          success: true,
          data: {
            data: mockList,
            totalCount: 1,
            page: 1,
            pageSize: 30,
          },
        },
      });

      const result = await getStudentAttendances(studentId, 1, 30);

      expect(apiClient.get).toHaveBeenCalledWith(
        `/api/attendance/student/${studentId}?page=1&pageSize=30`
      );
      expect(result.data).toEqual(mockList);
      expect(result.items).toEqual(mockList);
    });
  });

  describe('createAttendance & createBulkAttendance', () => {
    it('createAttendance tekil yoklama göndermeli', async () => {
      vi.mocked(apiClient.post).mockResolvedValueOnce({ data: { success: true } });

      const dto = { studentId: 's1', date: '2026-10-12', status: 'present' };
      await createAttendance(dto as any);

      expect(apiClient.post).toHaveBeenCalledWith('/api/attendance', dto);
    });

    it('createBulkAttendance toplu yoklama rotasına göndermeli', async () => {
      vi.mocked(apiClient.post).mockResolvedValueOnce({ data: { success: true } });

      const bulkDto = {
        date: '2026-10-12',
        attendances: [{ studentId: 's1', status: 'present' }],
      };
      await createBulkAttendance(bulkDto as any);

      expect(apiClient.post).toHaveBeenCalledWith('/api/attendance/bulk', bulkDto);
    });
  });
});
