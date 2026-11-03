import { INVITATION_ROLE_LABELS, type InvitationRole } from '../types/invitation';

/** 5321112233 → 0532 111 22 33 */
export function formatPhone(phone: string): string {
  const d = phone.replace(/\D/g, '');
  if (d.length !== 10) return phone;
  return `0${d.slice(0, 3)} ${d.slice(3, 6)} ${d.slice(6, 8)} ${d.slice(8)}`;
}

export function whatsAppShareUrl(role: InvitationRole, link: string): string {
  const text =
    `Hafız Takip: kursumuza ${INVITATION_ROLE_LABELS[role].toLocaleLowerCase('tr-TR')} olarak katılmak için ` +
    `bu bağlantıya dokunun ve bilgilerinizi yazın:\n${link}`;
  return `https://wa.me/?text=${encodeURIComponent(text)}`;
}

