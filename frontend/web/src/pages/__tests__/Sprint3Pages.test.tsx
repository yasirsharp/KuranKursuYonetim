import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import type React from 'react';
import { AuthProvider } from '../../context/AuthContext';
import OtpVerifyPage from '../auth/OtpVerifyPage';
import ApplicationStatusPage from '../auth/ApplicationStatusPage';
import PlatformTenantsPage from '../platform/PlatformTenantsPage';
import { verifyOtp } from '../../api/auth';
import { approveTenant, getMyApplication, getTenants, resubmitApplication } from '../../api/courses';
import { readSession, writeSession } from '../../services/session';

vi.mock('../../api/auth', async (importOriginal) => ({
  ...(await importOriginal<typeof import('../../api/auth')>()),
  verifyOtp: vi.fn(),
}));

vi.mock('../../api/courses', () => ({
  getMyApplication: vi.fn(),
  resubmitApplication: vi.fn(),
  getTenants: vi.fn(),
  approveTenant: vi.fn(),
  rejectTenant: vi.fn(),
}));

const tokenResponse = (overrides: Record<string, unknown> = {}) => ({
  token: 'jwt',
  expiration: new Date(Date.now() + 3_600_000).toISOString(),
  refreshToken: 'r',
  refreshTokenExpiration: new Date(Date.now() + 86_400_000).toISOString(),
  userId: 'u1',
  tenantId: 'k2',
  tenantStatus: 'active',
  role: 'head_teacher',
  firstName: 'Hasan',
  lastName: 'Demir',
  requiresTenantSelection: false,
  tenants: [],
  ...overrides,
});

const renderAt = (path: string, element: React.ReactElement, state?: unknown) =>
  render(
    <AuthProvider>
      <MemoryRouter initialEntries={[{ pathname: path, state }]}>
        <Routes>
          <Route path={path} element={element} />
          <Route path="/" element={<div>Ana sayfa</div>} />
          <Route path="/basvuru-durumu" element={<div>Başvuru durumu</div>} />
          <Route path="/login" element={<div>Giriş</div>} />
        </Routes>
      </MemoryRouter>
    </AuthProvider>
  );

describe('Kurs seçimi (OtpVerifyPage)', () => {
  beforeEach(() => vi.clearAllMocks());

  it('numara iki kursta kayıtlıysa kurs listesi gösterir ve seçilen kursla aynı kodu tekrar doğrular', async () => {
    vi.mocked(verifyOtp)
      .mockResolvedValueOnce(tokenResponse({
        token: '',
        requiresTenantSelection: true,
        tenants: [
          { tenantId: 'k1', tenantName: 'Merkez Kursu', tenantStatus: 'active', role: 'parent' },
          { tenantId: 'k2', tenantName: 'Yeşilvadi Kursu', tenantStatus: 'pending', role: 'tenant_admin' },
        ],
      }) as never)
      .mockResolvedValueOnce(tokenResponse({ tenantId: 'k1', role: 'parent' }) as never);

    renderAt('/verify-otp', <OtpVerifyPage />, { phoneNumber: '5001112235' });
    const user = userEvent.setup();
    const inputs = screen.getAllByRole('textbox');
    for (let i = 0; i < 6; i++) await user.type(inputs[i]!, String(i + 1));
    await user.click(screen.getByRole('button', { name: 'Giriş Yap' }));

    expect(await screen.findByText('Kurs Seçin')).toBeInTheDocument();
    expect(screen.getByText('Onay bekliyor')).toBeInTheDocument();

    await user.click(screen.getByText('Merkez Kursu'));

    await waitFor(() => expect(screen.getByText('Ana sayfa')).toBeInTheDocument());
    expect(verifyOtp).toHaveBeenLastCalledWith({ phoneNumber: '5001112235', code: '123456', tenantId: 'k1' });
    expect(readSession()?.tenantId).toBe('k1');
  });

  it('onay bekleyen kursun müdürü giriş yapınca başvuru durumu ekranına gider', async () => {
    vi.mocked(verifyOtp).mockResolvedValueOnce(tokenResponse({ tenantStatus: 'pending', role: 'tenant_admin' }) as never);

    renderAt('/verify-otp', <OtpVerifyPage />, { phoneNumber: '5001112240' });
    const user = userEvent.setup();
    const inputs = screen.getAllByRole('textbox');
    for (let i = 0; i < 6; i++) await user.type(inputs[i]!, '1');
    await user.click(screen.getByRole('button', { name: 'Giriş Yap' }));

    expect(await screen.findByText('Başvuru durumu')).toBeInTheDocument();
  });
});

describe('Başvuru durumu ekranı', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    writeSession({
      kind: 'tenant', token: 't', expiration: new Date(Date.now() + 3_600_000).toISOString(),
      tenantId: 'k2', tenantStatus: 'pending', role: 'tenant_admin', firstName: 'Kemal', lastName: 'Arslan',
    });
  });

  it('onay bekleyen başvuruda "Kursunuz onay bekliyor" yazar', async () => {
    vi.mocked(getMyApplication).mockResolvedValue({
      tenantId: 'k2', courseName: 'Yeşilvadi Kursu', status: 'pending', city: 'Konya', district: 'Selçuklu',
      submittedAt: new Date().toISOString(),
    });

    renderAt('/basvuru-durumu', <ApplicationStatusPage />);

    expect(await screen.findByText('Kursunuz onay bekliyor')).toBeInTheDocument();
    expect(screen.getByText('Konya / Selçuklu')).toBeInTheDocument();
  });

  it('reddedilen başvuruda gerekçeyi gösterir ve düzeltilip yeniden gönderilebilir', async () => {
    vi.mocked(getMyApplication)
      .mockResolvedValueOnce({
        tenantId: 'k2', courseName: 'Yeşilvadi Kursu', status: 'rejected', city: 'Konya', district: 'Selçuklu',
        rejectionReason: 'Adres bilgisi eksik.',
      })
      .mockResolvedValueOnce({ tenantId: 'k2', courseName: 'Yeşilvadi Kursu', status: 'pending', city: 'Konya', district: 'Selçuklu' });
    vi.mocked(resubmitApplication).mockResolvedValueOnce();

    renderAt('/basvuru-durumu', <ApplicationStatusPage />);
    const user = userEvent.setup();

    expect(await screen.findByText('Adres bilgisi eksik.')).toBeInTheDocument();
    await user.click(screen.getByRole('button', { name: /Bilgileri Düzenle/ }));
    await user.type(screen.getByLabelText(/Adres/), 'Cami Sok. No: 3');
    await user.click(screen.getByRole('button', { name: 'Yeniden Gönder' }));

    expect(await screen.findByText('Kursunuz onay bekliyor')).toBeInTheDocument();
    expect(resubmitApplication).toHaveBeenCalledWith(expect.objectContaining({ address: 'Cami Sok. No: 3', city: 'Konya' }));
    expect(readSession()?.tenantStatus).toBe('pending');
  });
});

describe('Platform onay paneli', () => {
  beforeEach(() => vi.clearAllMocks());

  it('bekleyen başvuruyu listeler ve onaylar', async () => {
    const pending = {
      id: 'k2', name: 'Yeşilvadi Kursu', slug: 'yesilvadi', status: 'pending' as const, isActive: true,
      city: 'Konya', district: 'Selçuklu', contactPhone: '5001112240', createdAt: new Date().toISOString(),
    };
    vi.mocked(getTenants)
      .mockResolvedValueOnce({ data: [pending], totalCount: 1, page: 1, pageSize: 50 })
      .mockResolvedValueOnce({ data: [], totalCount: 0, page: 1, pageSize: 50 });
    vi.mocked(approveTenant).mockResolvedValueOnce('Kurs başvurusu onaylandı.');
    vi.spyOn(window, 'confirm').mockReturnValue(true);

    render(<MemoryRouter><PlatformTenantsPage /></MemoryRouter>);
    const user = userEvent.setup();

    expect(await screen.findByText('Yeşilvadi Kursu')).toBeInTheDocument();
    expect(getTenants).toHaveBeenCalledWith({ status: 'pending', search: undefined });

    await user.click(screen.getByRole('button', { name: 'Onayla' }));

    expect(approveTenant).toHaveBeenCalledWith('k2');
    expect(await screen.findByText('"Yeşilvadi Kursu" onaylandı.')).toBeInTheDocument();
    expect(await screen.findByText('Onay bekleyen başvuru yok.')).toBeInTheDocument();
  });
});
