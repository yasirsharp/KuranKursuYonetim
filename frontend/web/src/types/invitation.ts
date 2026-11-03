/** Davetle katılabilecek roller */
export type InvitationRole = 'head_teacher' | 'assistant_teacher';

export type JoinRequestStatus = 'pending' | 'approved' | 'rejected';

export interface IInvitation {
  id: string;
  code: string;
  role: InvitationRole;
  expiresAt: string;
  createdAt: string;
}

/** Herkese açık davet sayfası */
export interface IInvitationPublic {
  code: string;
  courseName: string;
  city?: string | null;
  district?: string | null;
  role: InvitationRole;
  expiresAt: string;
}

export interface IJoinWithInvitation {
  firstName: string;
  lastName: string;
  phoneNumber: string;
  email?: string;
  code: string;
}

export interface IJoinRequest {
  id: string;
  firstName: string;
  lastName: string;
  phoneNumber: string;
  email?: string | null;
  role: InvitationRole;
  status: JoinRequestStatus;
  createdAt: string;
  reviewedAt?: string | null;
  rejectionReason?: string | null;
}

export interface IStaffUser {
  id: string;
  firstName: string;
  lastName: string;
  phoneNumber: string;
  email?: string | null;
  role: string;
}

export const INVITATION_ROLE_LABELS: Record<InvitationRole, string> = {
  head_teacher: 'Hoca',
  assistant_teacher: 'Yardımcı hoca',
};
