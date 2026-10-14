import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import React from 'react';
import StudentListPage from '../StudentListPage';
import { getStudents } from '../../../api/students';

vi.mock('../../../api/students', () => ({
  getStudents: vi.fn(),
}));

const renderWithRouter = (ui: React.ReactElement) => {
  return render(<MemoryRouter>{ui}</MemoryRouter>);
};

describe('StudentListPage', () => {
  const mockStudents = [
    { id: '1', firstName: 'Ahmet', lastName: 'Yılmaz', currentPage: 50, status: 'active' },
    { id: '2', firstName: 'Mehmet', lastName: 'Kaya', currentPage: 120, status: 'active' },
    { id: '3', firstName: 'Ali', lastName: 'Demir', currentPage: 200, status: 'hafiz' },
  ];

  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('başlangıçta tüm öğrencileri listelemeli', async () => {
    vi.mocked(getStudents).mockResolvedValueOnce(mockStudents as any);

    renderWithRouter(<StudentListPage />);

    await waitFor(() => {
      expect(screen.getByText('Ahmet Yılmaz')).toBeInTheDocument();
      expect(screen.getByText('Mehmet Kaya')).toBeInTheDocument();
      expect(screen.getByText('Ali Demir')).toBeInTheDocument();
    });
  });

  it('arama kutusuna yazıldığında öğrencileri filtrelemeli', async () => {
    vi.mocked(getStudents).mockResolvedValueOnce(mockStudents as any);

    renderWithRouter(<StudentListPage />);

    await waitFor(() => {
      expect(screen.getByText('Ahmet Yılmaz')).toBeInTheDocument();
    });

    const searchInput = screen.getByPlaceholderText('Öğrenci ara...');
    await userEvent.type(searchInput, 'Mehmet');

    expect(screen.getByText('Mehmet Kaya')).toBeInTheDocument();
    expect(screen.queryByText('Ahmet Yılmaz')).not.toBeInTheDocument();
    expect(screen.queryByText('Ali Demir')).not.toBeInTheDocument();
  });

  it('eşleşen öğrenci bulunamadığında "Öğrenci bulunamadı." mesajı göstermeli', async () => {
    vi.mocked(getStudents).mockResolvedValueOnce(mockStudents as any);

    renderWithRouter(<StudentListPage />);

    await waitFor(() => {
      expect(screen.getByText('Ahmet Yılmaz')).toBeInTheDocument();
    });

    const searchInput = screen.getByPlaceholderText('Öğrenci ara...');
    await userEvent.type(searchInput, 'Olmayanİsim');

    expect(screen.getByText('Öğrenci bulunamadı.')).toBeInTheDocument();
  });

  it('API hata verdiğinde hata mesajını görüntülemeli', async () => {
    vi.mocked(getStudents).mockRejectedValueOnce(new Error('Sunucu hatası'));

    renderWithRouter(<StudentListPage />);

    await waitFor(() => {
      expect(screen.getByText('Öğrenciler yüklenirken bir hata oluştu.')).toBeInTheDocument();
    });
  });
});
