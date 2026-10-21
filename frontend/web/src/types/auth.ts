export interface ISendOtpRequest {
  phoneNumber: string;
}

export interface IVerifyEmailCodeRequest {
  email: string;
  code: string;
  /** Adres birden fazla kursta kayıtlıysa seçilen kurs */
  tenantId?: string;
}

export interface IVerifyOtpRequest {
  phoneNumber: string;
  code: string;
  /** Numara birden fazla kursta kayıtlıysa seçilen kurs */
  tenantId?: string;
}

/** Kurs durumu (backend: Core.Enums.TenantStatus) */
export type TenantStatus = 'pending' | 'trial' | 'active' | 'suspended' | 'cancelled' | 'rejected';

export interface ITenantOption {
  tenantId: string;
  tenantName: string;
  tenantStatus: TenantStatus;
  role: string;
}

export interface ITokenResponse {
  token: string;
  expiration: string;
  refreshToken: string;
  refreshTokenExpiration: string;
  userId: string;
  tenantId: string;
  tenantStatus: TenantStatus;
  role: string;
  firstName: string;
  lastName: string;
  /** true ise token yoktur; kullanıcı tenants içinden kurs seçip aynı kodla tekrar doğrular */
  requiresTenantSelection: boolean;
  tenants: ITenantOption[];
}

/** Platform yöneticisi girişi (e-posta + şifre) yanıtı */
export interface IPlatformTokenResponse {
  token: string;
  expiration: string;
  userId: string;
  email: string;
  role: string;
}

export type UserRole =
  | 'super_admin'
  | 'tenant_admin'
  | 'head_teacher'
  | 'assistant_teacher'
  | 'parent';

/** Oturum türü: kurs kullanıcısı (telefon + OTP) veya platform yöneticisi (e-posta + şifre) */
export type AuthKind = 'tenant' | 'platform';

export interface IAuthUser {
  kind?: AuthKind;
  token: string;
  expiration: string;
  userId?: string;
  tenantId: string;
  /** Onay bekleyen/reddedilen kursun kullanıcısı yalnızca başvuru durumu ekranını görür */
  tenantStatus?: TenantStatus;
  role: UserRole;
  firstName: string;
  lastName: string;
  email?: string;
  /** Yalnızca kurs kullanıcılarında; access token süresi dolunca sessizce yenilenir */
  refreshToken?: string;
  refreshTokenExpiration?: string;
}
