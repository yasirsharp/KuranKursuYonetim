import type { TenantStatus } from './auth';

/** Kurs bilgileri — başvuru formu ve reddedilen başvurunun yeniden gönderilmesi */
export interface ICourseApplicationInfo {
  courseName: string;
  city: string;
  district: string;
  address?: string;
  contactEmail?: string;
}

/** Herkese açık "Kurs oluştur" formu */
export interface ICourseApplication extends ICourseApplicationInfo {
  phoneNumber: string;
  code: string;
  adminFirstName: string;
  adminLastName: string;
}

export interface ICourseApplicationStatus {
  tenantId: string;
  courseName: string;
  status: TenantStatus;
  city?: string;
  district?: string;
  address?: string;
  contactEmail?: string;
  submittedAt?: string;
  reviewedAt?: string;
  rejectionReason?: string;
}

/** Platform yöneticisinin gördüğü kurs kaydı */
export interface ITenant {
  id: string;
  name: string;
  slug: string;
  status: TenantStatus;
  contactEmail?: string;
  contactPhone?: string;
  isActive: boolean;
  city?: string;
  district?: string;
  address?: string;
  submittedAt?: string;
  reviewedAt?: string;
  rejectionReason?: string;
  createdAt: string;
}

/** Kurs ayarları (backend: TenantSettingsDto). Saatler "HH:mm". */
export interface ITenantSettings {
  /** 1 = Pazartesi … 7 = Pazar */
  workingDays: number[];
  dayStartTime: string;
  dayEndTime: string;
  defaultSlotMinutes: number;
  timeZone: string;
  absenceThreshold: number;
  absencePeriodDays: number;
  etutStartTime: string | null;
  reviewMode: 'spaced' | 'cyclic';
  dailyReviewTargetPages: number;
  notifyParentOnAbsence: boolean;
  notifyParentOnMilestone: boolean;
}

export interface ICourseInfo {
  id: string;
  name: string;
  slug: string;
  status: TenantStatus;
  contactEmail?: string;
  contactPhone?: string;
}

export const TENANT_STATUS_LABELS: Record<TenantStatus, string> = {
  pending: 'Onay bekliyor',
  trial: 'Deneme',
  active: 'Aktif',
  suspended: 'Askıda',
  cancelled: 'İptal',
  rejected: 'Reddedildi',
};
