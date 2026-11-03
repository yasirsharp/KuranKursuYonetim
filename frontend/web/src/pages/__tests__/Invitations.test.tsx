import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { AuthProvider } from '../../context/AuthContext';
import JoinPage from '../auth/JoinPage';
import StaffPage from '../teacher/StaffPage';
import { writeSession } from '../../services/session';
import {
  approveJoinRequest,
  createInvitation,
  getInvitations,
  getJoinRequests,
  getPublicInvitation,
  getStaff,
  joinWithInvitation,
  sendJoinCode,
} from '../../api/invitations';
import { whatsAppShareUrl } from '../../utils/invitation';

vi.mock('qrcode', () => ({ default: { toDataURL: vi.fn().mockResolvedValue('data:image/png;base64,AA==') } }));
vi.mock('../../api/invitations', async (importOriginal) => ({
  ...(await importOriginal<typeof import('../../api/invitations')>()),
  getPublicInvitation: vi.fn(),
  sendJoinCode: vi.fn(),
  joinWithInvitation: vi.fn(),
  getInvitations: vi.fn(),
  createInvitation: vi.fn(),
  revokeInvitation: vi.fn(),
  getJoinRequests: vi.fn(),
  approveJoinRequest: vi.fn(),
  rejectJoinRequest: vi.fn(),
  getStaff: vi.fn(),
}));

function renderAt(path: string, routePath: string, element: React.ReactElement) {
  return render(
    <AuthProvider>
      <MemoryRouter initialEntries={[path]}>
        <Routes>
          <Route path={routePath} element={element} />
        </Routes>
      </MemoryRouter>
    </AuthProvider>
  );
}

describe('Davet bağlantısıyla katılım (/katil/KOD)', () => {
  beforeEach(() => vi.clearAllMocks());

  it('kursu ve rolü gösterir; bilgiler + SMS koduyla katılım isteği gönderir', async () => {
    vi.mocked(getPublicInvitation).mockResolvedValue({
      code: 'AB7K2M9X', courseName: "Merkez Kur'an Kursu", city: 'Konya', district: 'Meram',
      role: 'assistant_teacher', expiresAt: new Date(Date.now() + 86_400_000).toISOString(),
    });
    vi.mocked(sendJoinCode).mockResolvedValue();
    vi.mocked(joinWithInvitation).mockResolvedValue('Katılım isteğiniz kurs yöneticisine iletildi.');
    const user = userEvent.setup();

    renderAt('/katil/AB7K2M9X', '/katil/:code', <JoinPage />);

    expect(await screen.findByText("Merkez Kur'an Kursu")).toBeInTheDocument();
    expect(screen.getByText('Yardımcı hoca olarak katılın')).toBeInTheDocument();
    expect(getPublicInvitation).toHaveBeenCalledWith('AB7K2M9X');

    await user.type(screen.getByLabelText('Ad'), 'Yusuf');
    await user.type(screen.getByLabelText('Soyad'), 'Kaya');
    await user.type(screen.getByLabelText('Cep telefonu'), '0532 111 22 33');
    await user.click(screen.getByText('Doğrulama Kodu Gönder'));

    expect(sendJoinCode).toHaveBeenCalledWith('AB7K2M9X', '05321112233');
    await user.type(await screen.findByLabelText('Doğrulama kodu'), '123456');

    await waitFor(() =>
      expect(joinWithInvitation).toHaveBeenCalledWith('AB7K2M9X', {
        firstName: 'Yusuf', lastName: 'Kaya', phoneNumber: '05321112233', email: undefined, code: '123456',
      })
    );
    expect(await screen.findByText('İsteğiniz gönderildi')).toBeInTheDocument();
    expect(screen.getByText('📱 Uygulamayı indir')).toBeInTheDocument();
    expect(joinWithInvitation).toHaveBeenCalledTimes(1);
  });

  it('geçersiz bağlantıda sunucunun mesajını gösterir', async () => {
    vi.mocked(getPublicInvitation).mockRejectedValue(new Error('yok'));

    renderAt('/katil/ESKIKOD1', '/katil/:code', <JoinPage />);

    expect(await screen.findByText('Davet açılamadı')).toBeInTheDocument();
  });
});

describe('Personel ekranı', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    writeSession({
      kind: 'tenant', token: 't', expiration: new Date(Date.now() + 3_600_000).toISOString(),
      tenantId: 'k1', tenantStatus: 'active', role: 'tenant_admin', firstName: 'Mehmet', lastName: 'Müdür',
    });
    vi.mocked(getStaff).mockResolvedValue([
      { id: 'u1', firstName: 'Mehmet', lastName: 'Müdür', phoneNumber: '5001112230', role: 'tenant_admin' },
    ]);
  });

  it('bekleyen isteği onaylar, davet bağlantısı oluşturur ve WhatsApp bağlantısını hazırlar', async () => {
    vi.mocked(getJoinRequests)
      .mockResolvedValueOnce([{
        id: 'r1', firstName: 'Yusuf', lastName: 'Kaya', phoneNumber: '5321112233', role: 'head_teacher',
        status: 'pending', createdAt: new Date().toISOString(),
      }])
      .mockResolvedValue([]);
    vi.mocked(getInvitations)
      .mockResolvedValueOnce([])
      .mockResolvedValueOnce([])
      .mockResolvedValue([{
        id: 'i1', code: 'AB7K2M9X', role: 'head_teacher',
        expiresAt: new Date(Date.now() + 7 * 86_400_000).toISOString(), createdAt: new Date().toISOString(),
      }]);
    vi.mocked(approveJoinRequest).mockResolvedValue('Yusuf Kaya personel olarak eklendi.');
    vi.mocked(createInvitation).mockResolvedValue({} as never);
    const user = userEvent.setup();

    renderAt('/staff', '/staff', <StaffPage />);

    expect(await screen.findByText('Yusuf Kaya')).toBeInTheDocument();
    expect(screen.getByText('Hoca · 0532 111 22 33')).toBeInTheDocument();
    await user.click(screen.getByText('Onayla'));
    expect(approveJoinRequest).toHaveBeenCalledWith('r1');
    expect(await screen.findByText('Yusuf Kaya personel olarak eklendi.')).toBeInTheDocument();
    expect(screen.queryByText('Katılım istekleri')).not.toBeInTheDocument();

    // Müdür iki rol için de davet oluşturabilir
    expect(screen.getByText('Yardımcı hoca davet bağlantısı oluştur')).toBeInTheDocument();
    await user.click(screen.getByText('Hoca davet bağlantısı oluştur'));
    expect(createInvitation).toHaveBeenCalledWith('head_teacher');

    expect(await screen.findByTestId('invite-link-head_teacher')).toHaveTextContent('/katil/AB7K2M9X');
    expect(screen.getByAltText('Hoca davet karekodu')).toBeInTheDocument();
    expect(screen.getByText('WhatsApp ile gönder').getAttribute('href')).toContain(encodeURIComponent(`${window.location.origin}/katil/AB7K2M9X`));
  });

  it('başöğretmen yalnızca yardımcı hoca daveti görür', async () => {
    writeSession({
      kind: 'tenant', token: 't', expiration: new Date(Date.now() + 3_600_000).toISOString(),
      tenantId: 'k1', tenantStatus: 'active', role: 'head_teacher', firstName: 'Ahmet', lastName: 'Hoca',
    });
    vi.mocked(getJoinRequests).mockResolvedValue([]);
    vi.mocked(getInvitations).mockResolvedValue([]);

    renderAt('/staff', '/staff', <StaffPage />);

    expect(await screen.findByText('Yardımcı hoca davet bağlantısı oluştur')).toBeInTheDocument();
    expect(screen.queryByText('Hoca davet bağlantısı oluştur')).not.toBeInTheDocument();
  });

  it('WhatsApp mesajı rolü ve bağlantıyı içerir', () => {
    const url = whatsAppShareUrl('assistant_teacher', 'https://site/katil/AB7K2M9X');
    const text = decodeURIComponent(url.replace('https://wa.me/?text=', ''));
    expect(text).toContain('yardımcı hoca olarak');
    expect(text).toContain('https://site/katil/AB7K2M9X');
  });
});
