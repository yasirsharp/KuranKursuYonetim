import { STORAGE_KEYS } from '../utils/constants';
import type { IAuthUser } from '../types/auth';

/**
 * Oturum kaydı (localStorage) için tek giriş noktası.
 * API istemcisi (token yenileme, kurs durumu değişimi) ile AuthContext aynı kaydı kullanır;
 * değişiklikler SESSION_EVENT ile React tarafına bildirilir.
 */
export const SESSION_EVENT = 'hafiz-takip:session';

export function readSession(): IAuthUser | null {
  try {
    const raw = localStorage.getItem(STORAGE_KEYS.USER);
    return raw ? (JSON.parse(raw) as IAuthUser) : null;
  } catch {
    return null;
  }
}

export function writeSession(user: IAuthUser): void {
  localStorage.setItem(STORAGE_KEYS.TOKEN, user.token);
  localStorage.setItem(STORAGE_KEYS.USER, JSON.stringify(user));
  window.dispatchEvent(new Event(SESSION_EVENT));
}

export function clearSession(): void {
  localStorage.removeItem(STORAGE_KEYS.TOKEN);
  localStorage.removeItem(STORAGE_KEYS.USER);
  window.dispatchEvent(new Event(SESSION_EVENT));
}

/** Oturum hâlâ kullanılabilir mi? Kurs kullanıcısında access token dolmuş olsa da refresh token geçerliyse evet. */
export function isSessionUsable(user: IAuthUser, now = new Date()): boolean {
  if (new Date(user.expiration) > now) return true;
  return !!user.refreshToken && !!user.refreshTokenExpiration && new Date(user.refreshTokenExpiration) > now;
}

/** Onay bekleyen veya reddedilen kursun kullanıcısı yalnızca başvuru durumu ekranını kullanabilir */
export function isApplicationOnly(user: IAuthUser | null): boolean {
  return user?.kind !== 'platform' && (user?.tenantStatus === 'pending' || user?.tenantStatus === 'rejected');
}

export function isPlatformUser(user: IAuthUser | null): boolean {
  return user?.kind === 'platform' || user?.role === 'super_admin';
}
