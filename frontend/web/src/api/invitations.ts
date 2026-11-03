import apiClient from './client';
import type { IApiResponse, IPaginatedResponse } from '../types/common';
import type {
  IInvitation,
  IInvitationPublic,
  IJoinRequest,
  IJoinWithInvitation,
  IStaffUser,
  InvitationRole,
  JoinRequestStatus,
} from '../types/invitation';

// ── Yönetici: davetler ve katılım istekleri ─────────────────────────

export async function getInvitations(): Promise<IInvitation[]> {
  const response = await apiClient.get<IApiResponse<IInvitation[]>>('/api/invitations');
  return response.data.data;
}

/** Yeni davet; aynı roldeki önceki bağlantı iptal edilir. */
export async function createInvitation(role: InvitationRole, validDays?: number): Promise<IInvitation> {
  const response = await apiClient.post<IApiResponse<IInvitation>>('/api/invitations', { role, validDays });
  return response.data.data;
}

export async function revokeInvitation(id: string): Promise<void> {
  await apiClient.delete(`/api/invitations/${id}`);
}

export async function getJoinRequests(status: JoinRequestStatus = 'pending'): Promise<IJoinRequest[]> {
  const response = await apiClient.get<IApiResponse<IJoinRequest[]>>('/api/invitations/join-requests', {
    params: { status },
  });
  return response.data.data;
}

export async function approveJoinRequest(id: string): Promise<string> {
  const response = await apiClient.post<IApiResponse<null>>(`/api/invitations/join-requests/${id}/approve`);
  return response.data.message;
}

export async function rejectJoinRequest(id: string, reason?: string): Promise<string> {
  const response = await apiClient.post<IApiResponse<null>>(`/api/invitations/join-requests/${id}/reject`, { reason });
  return response.data.message;
}

const STAFF_ROLES = ['tenant_admin', 'head_teacher', 'assistant_teacher'];

/** Kursun personeli (müdür, hocalar); veliler hariç */
export async function getStaff(): Promise<IStaffUser[]> {
  const response = await apiClient.get<IApiResponse<IPaginatedResponse<IStaffUser>>>('/api/users', {
    params: { pageSize: 100 },
  });
  const payload = response.data.data;
  return (payload?.data ?? payload?.items ?? []).filter((u) => STAFF_ROLES.includes(u.role));
}

// ── Herkese açık: davetle katılım ───────────────────────────────────

export async function getPublicInvitation(code: string): Promise<IInvitationPublic> {
  const response = await apiClient.get<IApiResponse<IInvitationPublic>>(`/api/join/${encodeURIComponent(code)}`);
  return response.data.data;
}

export async function sendJoinCode(code: string, phoneNumber: string): Promise<void> {
  await apiClient.post(`/api/join/${encodeURIComponent(code)}/send-code`, { phoneNumber });
}

export async function joinWithInvitation(code: string, data: IJoinWithInvitation): Promise<string> {
  const response = await apiClient.post<IApiResponse<null>>(`/api/join/${encodeURIComponent(code)}`, data);
  return response.data.message;
}

/** Davet bağlantısı: {site}/katil/{kod} */
export function invitationLink(code: string): string {
  return `${window.location.origin}/katil/${code}`;
}
