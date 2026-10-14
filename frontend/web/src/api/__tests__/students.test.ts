import { describe, it, expect, vi, beforeEach } from 'vitest';
import apiClient from '../client';
import { getStudents, getStudentById, createStudent, updateStudent, deleteStudent } from '../students';

vi.mock('../client', () => ({
  default: {
    get: vi.fn(),
    post: vi.fn(),
    put: vi.fn(),
    delete: vi.fn(),
  },
}));

describe('students api', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('getStudents', () => {
    it('backend PagedResult döndüğünde (response.data.data.data) dizi olarak unwrap etmeli', async () => {
      // Backend PagedResult formatı: data.data içinde liste nesnesi
      const mockStudents = [
        { id: '1', firstName: 'Ahmet', lastName: 'Yılmaz', currentPage: 15 },
        { id: '2', firstName: 'Mehmet', lastName: 'Kaya', currentPage: 20 },
      ];

      vi.mocked(apiClient.get).mockResolvedValueOnce({
        data: {
          success: true,
          message: 'Öğrenciler listelendi',
          data: {
            data: mockStudents,
            totalCount: 2,
            page: 1,
            pageSize: 100,
          },
        },
      });

      const result = await getStudents();

      expect(apiClient.get).toHaveBeenCalledWith('/api/students?page=1&pageSize=100');
      expect(Array.isArray(result)).toBe(true);
      expect(result).toHaveLength(2);
      expect(result[0]?.firstName).toBe('Ahmet');
      // students.slice veya .filter gibi dizi operasyonları sorunsuz çalışmalı
      expect(result.slice(0, 1)).toHaveLength(1);
    });

    it('backend doğrudan düz dizi döndüğünde (response.data.data = []) uyumlu çalışmalı', async () => {
      const mockStudents = [{ id: '3', firstName: 'Ali', lastName: 'Demir' }];

      vi.mocked(apiClient.get).mockResolvedValueOnce({
        data: {
          success: true,
          data: mockStudents,
        },
      });

      const result = await getStudents();

      expect(Array.isArray(result)).toBe(true);
      expect(result).toHaveLength(1);
      expect(result[0]?.firstName).toBe('Ali');
    });

    it('backend verisi null veya tanımsız olduğunda boş dizi dönmeli', async () => {
      vi.mocked(apiClient.get).mockResolvedValueOnce({
        data: {
          success: true,
          data: null,
        },
      });

      const result = await getStudents();

      expect(Array.isArray(result)).toBe(true);
      expect(result).toEqual([]);
    });

    it('özel page ve pageSize parametreleri verildiğinde URL sorgusuna doğru eklemeli', async () => {
      vi.mocked(apiClient.get).mockResolvedValueOnce({
        data: { success: true, data: { data: [] } },
      });

      await getStudents(2, 20);

      expect(apiClient.get).toHaveBeenCalledWith('/api/students?page=2&pageSize=20');
    });
  });

  describe('getStudentById', () => {
    it('öğrenci ID ile tekil öğrenci nesnesini döndürmeli', async () => {
      const student = { id: 's1', firstName: 'Yasir', lastName: 'Demirci' };
      vi.mocked(apiClient.get).mockResolvedValueOnce({
        data: {
          success: true,
          data: student,
        },
      });

      const result = await getStudentById('s1');

      expect(apiClient.get).toHaveBeenCalledWith('/api/students/s1');
      expect(result).toEqual(student);
    });
  });

  describe('CRUD işlemleri', () => {
    it('createStudent POST isteği atmalı', async () => {
      vi.mocked(apiClient.post).mockResolvedValueOnce({ data: { success: true } });

      const newStudent = { firstName: 'Ahmet', lastName: 'Can' };
      await createStudent(newStudent as any);

      expect(apiClient.post).toHaveBeenCalledWith('/api/students', newStudent);
    });

    it('updateStudent PUT isteği atmalı', async () => {
      vi.mocked(apiClient.put).mockResolvedValueOnce({ data: { success: true } });

      const updateData = { id: 's1', firstName: 'Ahmet Güncel' };
      await updateStudent(updateData as any);

      expect(apiClient.put).toHaveBeenCalledWith('/api/students', updateData);
    });

    it('deleteStudent DELETE isteği atmalı', async () => {
      vi.mocked(apiClient.delete).mockResolvedValueOnce({ data: { success: true } });

      await deleteStudent('s1');

      expect(apiClient.delete).toHaveBeenCalledWith('/api/students/s1');
    });
  });
});
