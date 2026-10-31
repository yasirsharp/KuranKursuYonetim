/** API Base URL — development'ta Vite proxy, production'da tam URL */
export const API_BASE_URL = import.meta.env.VITE_API_URL || '';

/**
 * Android uygulamasının (APK) indirme adresi: GitHub Actions her derlemede bu sürüme yükler
 * (.github/workflows/mobile-apk.yml). Derlemede VITE_ANDROID_APK_URL ile değiştirilebilir.
 */
export const ANDROID_APK_URL =
  import.meta.env.VITE_ANDROID_APK_URL ||
  'https://github.com/yasirsharp/KuranKursuYonetim/releases/download/mobil-test/hafiz-takip.apk';

/** localStorage key'leri */
export const STORAGE_KEYS = {
  TOKEN: 'hafiz_takip_token',
  USER: 'hafiz_takip_user',
} as const;

/** Kullanıcı rolleri */
export const ROLES = {
  SUPER_ADMIN: 'super_admin',
  TENANT_ADMIN: 'tenant_admin',
  HEAD_TEACHER: 'head_teacher',
  ASSISTANT_TEACHER: 'assistant_teacher',
  PARENT: 'parent',
} as const;

/** Kur'an sabitleri */
export const QURAN = {
  TOTAL_PAGES: 604,
  TOTAL_JUZ: 30,
  TOTAL_SURAS: 114,
} as const;
