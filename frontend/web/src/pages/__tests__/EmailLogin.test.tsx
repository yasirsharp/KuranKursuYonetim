import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { AuthProvider } from '../../context/AuthContext';
import LoginPage from '../auth/LoginPage';
import OtpVerifyPage from '../auth/OtpVerifyPage';
import { sendEmailCode, verifyEmailCode } from '../../api/auth';

vi.mock('../../api/auth', async (importOriginal) => ({
  ...(await importOriginal<typeof import('../../api/auth')>()),
  sendEmailCode: vi.fn(),
  verifyEmailCode: vi.fn(),
}));

describe('E-posta ile giriş', () => {
  beforeEach(() => vi.clearAllMocks());

  it('e-posta sekmesinden kod ister ve gelen kodla giriş yapar', async () => {
    vi.mocked(sendEmailCode).mockResolvedValue();
    vi.mocked(verifyEmailCode).mockResolvedValue({
      token: 'jwt', expiration: new Date(Date.now() + 3_600_000).toISOString(),
      refreshToken: 'r', refreshTokenExpiration: new Date(Date.now() + 86_400_000).toISOString(),
      userId: 'u1', tenantId: 'k1', tenantStatus: 'active', role: 'head_teacher',
      firstName: 'Ahmet', lastName: 'Yılmaz', requiresTenantSelection: false, tenants: [],
    });
    const user = userEvent.setup();

    render(
      <AuthProvider>
        <MemoryRouter initialEntries={['/login']}>
          <Routes>
            <Route path="/login" element={<LoginPage />} />
            <Route path="/verify-otp" element={<OtpVerifyPage />} />
            <Route path="/" element={<div>Ana sayfa</div>} />
          </Routes>
        </MemoryRouter>
      </AuthProvider>
    );

    await user.click(screen.getByRole('tab', { name: 'E-posta' }));
    await user.type(screen.getByPlaceholderText('ornek@kurs.com'), 'ahmet.hoca@kurs.com');
    await user.click(screen.getByText('Doğrulama Kodu Gönder'));

    expect(sendEmailCode).toHaveBeenCalledWith('ahmet.hoca@kurs.com');
    expect(await screen.findByText(/adresine gönderilen 6 haneli kodu girin/)).toBeInTheDocument();

    await user.type(screen.getByLabelText('Kodun 1. hanesi'), '123456');
    await user.click(screen.getByRole('button', { name: /Doğrula|Giriş/ }));

    await waitFor(() => expect(verifyEmailCode).toHaveBeenCalledWith({ email: 'ahmet.hoca@kurs.com', code: '123456', tenantId: undefined }));
    expect(await screen.findByText('Ana sayfa')).toBeInTheDocument();
  });
});
