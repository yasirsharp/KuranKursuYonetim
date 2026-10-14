import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import React from 'react';
import DashboardPage from '../DashboardPage';
import { getDashboardSummary } from '../../../api/dashboard';
import { getStudents } from '../../../api/students';

vi.mock('../../../api/dashboard', () => ({
  getDashboardSummary: vi.fn(),
}));

vi.mock('../../../api/students', () => ({
  getStudents: vi.fn(),
}));

const renderWithRouter = (ui: React.ReactElement) => {
  return render(<MemoryRouter>{ui}</MemoryRouter>);
};

describe('DashboardPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('başlangıçta yüklenirken skeleton elemanları görüntülenmeli', () => {
    vi.mocked(getDashboardSummary).mockReturnValue(new Promise(() => {}));
    vi.mocked(getStudents).mockReturnValue(new Promise(() => {}));

    const { container } = renderWithRouter(<DashboardPage />);

    expect(container.querySelectorAll('.skeleton').length).toBeGreaterThan(0);
  });

  it('veriler başarıyla yüklendiğinde istatistik kartları ve öğrenci önizlemesi render edilmeli', async () => {
    vi.mocked(getDashboardSummary).mockResolvedValueOnce({
      totalStudents: 12,
      todayLessonCount: 8,
      todayAttendanceRate: 90,
      averageCompletedPages: 45,
    });

    vi.mocked(getStudents).mockResolvedValueOnce([
      {
        id: 's1',
        firstName: 'Ahmet',
        lastName: 'Yılmaz',
        currentPage: 120,
        status: 'active',
      } as any,
    ]);

    renderWithRouter(<DashboardPage />);

    // Başlık ve İstatistikler
    await waitFor(() => {
      expect(screen.getByText('Hafız Takip Paneli')).toBeInTheDocument();
      expect(screen.getByText('Toplam Öğrenci')).toBeInTheDocument();
      expect(screen.getByText('12')).toBeInTheDocument();
      expect(screen.getByText('Bugün İşlenen')).toBeInTheDocument();
      expect(screen.getByText('8')).toBeInTheDocument();
    });

    // Öğrenci Listesi
    expect(screen.getByText('Ahmet Yılmaz')).toBeInTheDocument();
    expect(screen.getByText(/Sayfa 120/)).toBeInTheDocument();
  });

  it('öğrenci listesi boş olduğunda "Henüz öğrenci kaydı bulunmuyor." mesajı gösterilmeli', async () => {
    vi.mocked(getDashboardSummary).mockResolvedValueOnce({
      totalStudents: 0,
      todayLessonCount: 0,
      todayAttendanceRate: 0,
      averageCompletedPages: 0,
    });

    vi.mocked(getStudents).mockResolvedValueOnce([]);

    renderWithRouter(<DashboardPage />);

    await waitFor(() => {
      expect(screen.getByText('Henüz öğrenci kaydı bulunmuyor.')).toBeInTheDocument();
    });
  });

  it('API çağrısı hata verdiğinde hata uyarısı görüntülenmeli', async () => {
    vi.mocked(getDashboardSummary).mockRejectedValueOnce(new Error('Network error'));
    vi.mocked(getStudents).mockRejectedValueOnce(new Error('Network error'));

    renderWithRouter(<DashboardPage />);

    await waitFor(() => {
      expect(screen.getByText('Veriler yüklenemedi. Lütfen tekrar deneyin.')).toBeInTheDocument();
    });
  });
});
