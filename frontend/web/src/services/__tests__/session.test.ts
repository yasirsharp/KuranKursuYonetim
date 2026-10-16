import { describe, it, expect } from 'vitest';
import { isApplicationOnly, isPlatformUser, isSessionUsable } from '../session';
import { homePathFor } from '../../utils/navigation';
import type { IAuthUser } from '../../types/auth';

const hour = 60 * 60 * 1000;
const base: IAuthUser = {
  kind: 'tenant',
  token: 't',
  expiration: new Date(Date.now() + hour).toISOString(),
  tenantId: 'k1',
  tenantStatus: 'active',
  role: 'head_teacher',
  firstName: 'Ahmet',
  lastName: 'Yılmaz',
};

describe('session', () => {
  it('access token geçerliyse oturum kullanılabilir', () => {
    expect(isSessionUsable(base)).toBe(true);
  });

  it('access token dolmuş ama refresh token geçerliyse oturum sürer', () => {
    const user = {
      ...base,
      expiration: new Date(Date.now() - hour).toISOString(),
      refreshToken: 'r',
      refreshTokenExpiration: new Date(Date.now() + 24 * hour).toISOString(),
    };
    expect(isSessionUsable(user)).toBe(true);
  });

  it('ikisi de dolmuşsa oturum biter', () => {
    const user = {
      ...base,
      expiration: new Date(Date.now() - hour).toISOString(),
      refreshToken: 'r',
      refreshTokenExpiration: new Date(Date.now() - hour).toISOString(),
    };
    expect(isSessionUsable(user)).toBe(false);
  });

  it('onay bekleyen ve reddedilen kurs yalnızca başvuru ekranını kullanır', () => {
    expect(isApplicationOnly({ ...base, tenantStatus: 'pending' })).toBe(true);
    expect(isApplicationOnly({ ...base, tenantStatus: 'rejected' })).toBe(true);
    expect(isApplicationOnly(base)).toBe(false);
  });

  it('açılış sayfası kullanıcı türüne göre seçilir', () => {
    expect(homePathFor(null)).toBe('/login');
    expect(homePathFor(base)).toBe('/');
    expect(homePathFor({ ...base, tenantStatus: 'pending' })).toBe('/basvuru-durumu');
    const platform: IAuthUser = { ...base, kind: 'platform', role: 'super_admin', tenantId: '' };
    expect(isPlatformUser(platform)).toBe(true);
    expect(homePathFor(platform)).toBe('/platform');
  });
});
