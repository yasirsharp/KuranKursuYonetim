import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import MobileAppPage from '../auth/MobileAppPage';
import { ANDROID_APK_URL } from '../../utils/constants';

vi.mock('qrcode', () => ({ default: { toDataURL: vi.fn().mockResolvedValue('data:image/png;base64,AA==') } }));

describe('MobileAppPage', () => {
  it('APK indirme düğmesini ve 5 kurulum adımını gösterir', async () => {
    render(<MemoryRouter><MobileAppPage /></MemoryRouter>);

    const link = screen.getByRole('link', { name: /Uygulamayı indir/ });
    expect(link).toHaveAttribute('href', ANDROID_APK_URL);
    expect(ANDROID_APK_URL).toMatch(/hafiz-takip\.apk$/);
    for (const step of ['İndirin', 'Dosyayı açın', 'İzin verin', 'Yükleyin', 'Giriş yapın']) {
      expect(screen.getByRole('heading', { name: step })).toBeInTheDocument();
    }
    expect(await screen.findByAltText(/karekod/)).toBeInTheDocument();
  });
});
