import { describe, it, expect, beforeEach } from 'vitest';
import { renderHook, act } from '@testing-library/react';
import React from 'react';
import { AuthProvider, useAuth, hasMinimumRole } from '../AuthContext';
import { STORAGE_KEYS } from '../../utils/constants';
import type { IAuthUser } from '../../types/auth';

const wrapper = ({ children }: { children: React.ReactNode }) => (
  <AuthProvider>{children}</AuthProvider>
);

describe('AuthContext', () => {
  const futureDate = new Date(Date.now() + 1000 * 60 * 60).toISOString();
  const mockUser: IAuthUser = {
    userId: 'u1',
    tenantId: 't1',
    firstName: 'Ahmet',
    lastName: 'Hoca',
    role: 'head_teacher',
    token: 'jwt-token-123',
    expiration: futureDate,
  };

  beforeEach(() => {
    localStorage.clear();
  });

  it('başlangıçta localStorage boşsa isAuthenticated false ve user null olmalı', () => {
    const { result } = renderHook(() => useAuth(), { wrapper });

    expect(result.current.isAuthenticated).toBe(false);
    expect(result.current.user).toBeNull();
  });

  it('localStorage içinde geçerli kullanıcı varsa oturumu geri yüklemeli', () => {
    localStorage.setItem(STORAGE_KEYS.USER, JSON.stringify(mockUser));
    localStorage.setItem(STORAGE_KEYS.TOKEN, mockUser.token);

    const { result } = renderHook(() => useAuth(), { wrapper });

    expect(result.current.isAuthenticated).toBe(true);
    expect(result.current.user?.firstName).toBe('Ahmet');
    expect(result.current.user?.token).toBe('jwt-token-123');
  });

  it('login çağrıldığında token ve kullanıcıyı state ile localStorage içine kaydetmeli', () => {
    const { result } = renderHook(() => useAuth(), { wrapper });

    act(() => {
      result.current.login(mockUser);
    });

    expect(result.current.isAuthenticated).toBe(true);
    expect(result.current.user?.userId).toBe('u1');
    expect(localStorage.getItem(STORAGE_KEYS.TOKEN)).toBe('jwt-token-123');
    expect(localStorage.getItem(STORAGE_KEYS.USER)).toContain('head_teacher');
  });

  it('logout çağrıldığında localStorage temizlenmeli ve user null olmalı', () => {
    localStorage.setItem(STORAGE_KEYS.USER, JSON.stringify(mockUser));
    localStorage.setItem(STORAGE_KEYS.TOKEN, mockUser.token);

    const { result } = renderHook(() => useAuth(), { wrapper });

    act(() => {
      result.current.logout();
    });

    expect(result.current.isAuthenticated).toBe(false);
    expect(result.current.user).toBeNull();
    expect(localStorage.getItem(STORAGE_KEYS.TOKEN)).toBeNull();
    expect(localStorage.getItem(STORAGE_KEYS.USER)).toBeNull();
  });

  describe('hasMinimumRole', () => {
    it('super_admin tüm alt rolleri karşılamalı', () => {
      expect(hasMinimumRole('super_admin', 'super_admin')).toBe(true);
      expect(hasMinimumRole('super_admin', 'tenant_admin')).toBe(true);
      expect(hasMinimumRole('super_admin', 'head_teacher')).toBe(true);
      expect(hasMinimumRole('super_admin', 'parent')).toBe(true);
    });

    it('head_teacher assistant_teacher ve parent gereksinimlerini karşılamalı ama tenant_admin karşılamamalı', () => {
      expect(hasMinimumRole('head_teacher', 'assistant_teacher')).toBe(true);
      expect(hasMinimumRole('head_teacher', 'parent')).toBe(true);
      expect(hasMinimumRole('head_teacher', 'tenant_admin')).toBe(false);
      expect(hasMinimumRole('head_teacher', 'super_admin')).toBe(false);
    });

    it('parent sadece parent rolünü karşılamalı', () => {
      expect(hasMinimumRole('parent', 'parent')).toBe(true);
      expect(hasMinimumRole('parent', 'head_teacher')).toBe(false);
    });
  });
});
