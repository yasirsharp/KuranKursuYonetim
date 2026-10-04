export interface ISendOtpRequest {
  phoneNumber: string;
}

export interface IVerifyOtpRequest {
  phoneNumber: string;
  code: string;
}

export interface ITokenResponse {
  token: string;
  expiration: string;
  tenantId: string;
  role: string;
  firstName: string;
  lastName: string;
}

export type UserRole =
  | 'super_admin'
  | 'tenant_admin'
  | 'head_teacher'
  | 'assistant_teacher'
  | 'parent';

export interface IAuthUser {
  token: string;
  tenantId: string;
  role: UserRole;
  firstName: string;
  lastName: string;
  expiration: string;
}
