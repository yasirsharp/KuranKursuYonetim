import type { IAuthUser } from '../types/auth';
import { isApplicationOnly, isPlatformUser } from '../services/session';

export const PATHS = {
  login: '/login',
  verifyOtp: '/verify-otp',
  courseApplication: '/kurs-olustur',
  applicationStatus: '/basvuru-durumu',
  platformLogin: '/platform/giris',
  platform: '/platform',
  settings: '/settings',
  more: '/more',
} as const;

/** Giriş sonrası kullanıcının açılış sayfası */
export function homePathFor(user: IAuthUser | null): string {
  if (!user) return PATHS.login;
  if (isPlatformUser(user)) return PATHS.platform;
  if (isApplicationOnly(user)) return PATHS.applicationStatus;
  return '/';
}

/** "16 Ekim 2026 14:30" biçimi */
export function formatDateTime(value?: string | null): string {
  if (!value) return '—';
  return new Date(value).toLocaleString('tr-TR', {
    day: 'numeric',
    month: 'long',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  });
}
