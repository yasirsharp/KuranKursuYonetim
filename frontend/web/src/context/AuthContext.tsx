import { createContext, useContext, useState, useEffect, useCallback, type ReactNode } from 'react';
import { Logger } from '../services/logger';
import { STORAGE_KEYS } from '../utils/constants';
import type { IAuthUser, UserRole } from '../types/auth';

const log = Logger.create('AuthContext');

interface AuthContextValue {
  user: IAuthUser | null;
  isAuthenticated: boolean;
  login: (user: IAuthUser) => void;
  logout: () => void;
}

const AuthContext = createContext<AuthContextValue | null>(null);

function loadUserFromStorage(): IAuthUser | null {
  try {
    const raw = localStorage.getItem(STORAGE_KEYS.USER);
    if (!raw) return null;

    const user = JSON.parse(raw) as IAuthUser;

    // Token süresi dolmuş mu kontrol et
    if (new Date(user.expiration) < new Date()) {
      log.warn('loadUserFromStorage', 'Token süresi dolmuş — oturum siliniyor');
      localStorage.removeItem(STORAGE_KEYS.TOKEN);
      localStorage.removeItem(STORAGE_KEYS.USER);
      return null;
    }

    log.info('loadUserFromStorage', 'Oturum geri yüklendi', {
      name: `${user.firstName} ${user.lastName}`,
      role: user.role,
    });

    return user;
  } catch {
    log.error('loadUserFromStorage', 'localStorage okunamadı');
    return null;
  }
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<IAuthUser | null>(() => loadUserFromStorage());

  const login = useCallback((authUser: IAuthUser) => {
    log.info('login', 'Oturum açıldı', {
      name: `${authUser.firstName} ${authUser.lastName}`,
      role: authUser.role,
    });

    localStorage.setItem(STORAGE_KEYS.TOKEN, authUser.token);
    localStorage.setItem(STORAGE_KEYS.USER, JSON.stringify(authUser));
    setUser(authUser);
  }, []);

  const logout = useCallback(() => {
    log.info('logout', 'Oturum kapatıldı');
    localStorage.removeItem(STORAGE_KEYS.TOKEN);
    localStorage.removeItem(STORAGE_KEYS.USER);
    setUser(null);
  }, []);

  // Token süresi dolan oturumu periyodik kontrol et
  useEffect(() => {
    const interval = setInterval(() => {
      if (user && new Date(user.expiration) < new Date()) {
        log.warn('tokenCheck', 'Token süresi doldu — otomatik çıkış');
        logout();
      }
    }, 60_000); // Her dakika kontrol

    return () => clearInterval(interval);
  }, [user, logout]);

  const value: AuthContextValue = {
    user,
    isAuthenticated: user !== null,
    login,
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
