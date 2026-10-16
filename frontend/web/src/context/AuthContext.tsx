import { createContext, useContext, useState, useEffect, useCallback, type ReactNode } from 'react';
import { Logger } from '../services/logger';
import { clearSession, isSessionUsable, readSession, SESSION_EVENT, writeSession } from '../services/session';
import { logoutSession } from '../api/auth';
import type { IAuthUser, UserRole } from '../types/auth';

const log = Logger.create('AuthContext');

interface AuthContextValue {
  user: IAuthUser | null;
  isAuthenticated: boolean;
  login: (user: IAuthUser) => void;
  /** Oturum kaydının bir kısmını günceller (ör. kurs onaylanınca tenantStatus) */
  updateUser: (changes: Partial<IAuthUser>) => void;
  logout: () => void;
}

const AuthContext = createContext<AuthContextValue | null>(null);

function loadUserFromStorage(): IAuthUser | null {
  const user = readSession();
  if (!user) return null;

  // Access token dolmuş olsa da refresh token geçerliyse oturum sürer (ilk istekte yenilenir)
  if (!isSessionUsable(user)) {
    log.warn('loadUserFromStorage', 'Oturum süresi dolmuş — oturum siliniyor');
    clearSession();
    return null;
  }

  log.info('loadUserFromStorage', 'Oturum geri yüklendi', {
    name: `${user.firstName} ${user.lastName}`,
    role: user.role,
  });
  return user;
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<IAuthUser | null>(() => loadUserFromStorage());

  // API istemcisi oturumu değiştirdiğinde (token yenileme, kurs durumu, oturum düşmesi) state'i eşitle
  useEffect(() => {
    const sync = () => setUser(readSession());
    window.addEventListener(SESSION_EVENT, sync);
    return () => window.removeEventListener(SESSION_EVENT, sync);
  }, []);

  const login = useCallback((authUser: IAuthUser) => {
    log.info('login', 'Oturum açıldı', {
      name: `${authUser.firstName} ${authUser.lastName}`,
      role: authUser.role,
    });
    writeSession(authUser);
    setUser(authUser);
  }, []);

  const updateUser = useCallback((changes: Partial<IAuthUser>) => {
    const current = readSession();
    if (!current) return;
    const next = { ...current, ...changes };
    writeSession(next);
    setUser(next);
  }, []);

  const logout = useCallback(() => {
    log.info('logout', 'Oturum kapatıldı');
    const refreshToken = readSession()?.refreshToken;
    clearSession();
    setUser(null);
    // Sunucudaki refresh token da iptal edilsin (başarısız olsa da yerel oturum kapanmıştır)
    if (refreshToken) logoutSession(refreshToken).catch(() => undefined);
  }, []);

  // Süresi dolan oturumu periyodik kontrol et
  useEffect(() => {
    const interval = setInterval(() => {
      if (user && !isSessionUsable(user)) {
        log.warn('tokenCheck', 'Oturum süresi doldu — otomatik çıkış');
        logout();
      }
    }, 60_000); // Her dakika kontrol

    return () => clearInterval(interval);
  }, [user, logout]);

  const value: AuthContextValue = {
    user,
    isAuthenticated: user !== null,
    login,
    updateUser,
    logout,
  };

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
}

/**
 * Kullanıcının belirli bir role sahip olup olmadığını kontrol eder.
 * Rol hiyerarşisi: super_admin > tenant_admin > head_teacher > assistant_teacher > parent
 */
export function hasMinimumRole(userRole: UserRole, requiredRole: UserRole): boolean {
  const hierarchy: UserRole[] = [
    'super_admin',
    'tenant_admin',
    'head_teacher',
    'assistant_teacher',
    'parent',
  ];
  const userIndex = hierarchy.indexOf(userRole);
  const requiredIndex = hierarchy.indexOf(requiredRole);
  return userIndex >= 0 && userIndex <= requiredIndex;
}
