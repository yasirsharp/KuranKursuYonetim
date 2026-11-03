import { useCallback, useEffect, useState } from 'react';
import QRCode from 'qrcode';
import { useAuth } from '../../context/AuthContext';
import {
  approveJoinRequest,
  createInvitation,
  getInvitations,
  getJoinRequests,
  getStaff,
  invitationLink,
  rejectJoinRequest,
  revokeInvitation,
} from '../../api/invitations';
import { getErrorMessage } from '../../api/client';
import { Logger } from '../../services/logger';
import { formatDateTime } from '../../utils/navigation';
import { formatPhone, whatsAppShareUrl } from '../../utils/invitation';
import {
  INVITATION_ROLE_LABELS,
  type IInvitation,
  type IJoinRequest,
  type IStaffUser,
  type InvitationRole,
} from '../../types/invitation';

const log = Logger.create('StaffPage');

const STAFF_ROLE_LABELS: Record<string, string> = {
  tenant_admin: 'Kurs müdürü',
  head_teacher: 'Hoca',
  assistant_teacher: 'Yardımcı hoca',
};

/**
 * Personel: davet bağlantıları (QR + WhatsApp), katılım istekleri (onay/red) ve personel listesi.
 * Kurs müdürü hoca ve yardımcı hoca davet eder; başöğretmen yalnızca yardımcı hoca.
 */
export default function StaffPage() {
  const { user } = useAuth();
  const invitableRoles: InvitationRole[] =
    user?.role === 'tenant_admin' ? ['head_teacher', 'assistant_teacher'] : ['assistant_teacher'];

  const [invitations, setInvitations] = useState<IInvitation[]>([]);
  const [requests, setRequests] = useState<IJoinRequest[]>([]);
  const [staff, setStaff] = useState<IStaffUser[]>([]);
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');
  const [notice, setNotice] = useState('');
  const [rejecting, setRejecting] = useState<IJoinRequest | null>(null);
  const [rejectReason, setRejectReason] = useState('');

  const load = useCallback(async () => {
    try {
      const [inv, req, people] = await Promise.all([getInvitations(), getJoinRequests(), getStaff()]);
      setInvitations(inv);
      setRequests(req);
      setStaff(people);
    } catch (err) {
      log.error('load', 'Personel bilgileri alınamadı', err);
      setError(getErrorMessage(err, 'Personel bilgileri alınamadı.'));
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void load();
  }, [load]);

  const run = async (action: () => Promise<string | void>, success?: string) => {
    setError('');
    setNotice('');
    setBusy(true);
    try {
      const message = await action();
      setNotice(success ?? message ?? '');
      await load();
    } catch (err) {
      log.error('run', 'İşlem başarısız', err);
      setError(getErrorMessage(err));
    } finally {
      setBusy(false);
    }
  };

  const handleCreate = (role: InvitationRole, renew: boolean) => {
    if (renew && !window.confirm('Yeni bağlantı oluşturulunca eski bağlantı ve karekod çalışmaz. Devam edilsin mi?')) return;
    void run(async () => {
      await createInvitation(role);
    }, 'Davet bağlantısı hazır. WhatsApp ile gönderebilir veya karekodu gösterebilirsiniz.');
  };

  const handleRevoke = (invitation: IInvitation) => {
    if (!window.confirm('Bu davet bağlantısı iptal edilsin mi? Bağlantıyı alanlar artık katılamaz.')) return;
    void run(async () => {
      await revokeInvitation(invitation.id);
    }, 'Davet bağlantısı iptal edildi.');
  };

  const handleReject = () => {
    if (!rejecting) return;
    const target = rejecting;
    setRejecting(null);
    void run(() => rejectJoinRequest(target.id, rejectReason.trim() || undefined));
    setRejectReason('');
  };

  if (loading) {
    return (
      <div className="form-stack">
        <div className="skeleton" style={{ height: 160 }} />
        <div className="skeleton" style={{ height: 120 }} />
      </div>
    );
  }

  return (
    <div className="form-stack fade-in" style={{ paddingBottom: 90 }}>
      <h1 className="page-title">Personel</h1>

      {error && <div className="notice notice-error">{error}</div>}
      {notice && <div className="notice notice-success">{notice}</div>}

      {requests.length > 0 && (
        <section className="form-section" aria-labelledby="requests-title">
          <h2 className="form-section-title" id="requests-title">
            Katılım istekleri <span className="count-badge">{requests.length}</span>
          </h2>
          <p className="form-section-hint">Davet bağlantısıyla gelen kişiler. Onaylayınca telefon numarasıyla giriş yapabilirler.</p>
          <ul className="join-request-list">
            {requests.map((r) => (
              <li key={r.id} className="join-request">
                <div className="join-request-info">
                  <strong>{r.firstName} {r.lastName}</strong>
                  <span>{INVITATION_ROLE_LABELS[r.role]} · {formatPhone(r.phoneNumber)}</span>
                  {r.email && <span>{r.email}</span>}
                  <span className="join-request-date">{formatDateTime(r.createdAt)}</span>
                </div>
                <div className="button-row">
                  <button className="auth-button auth-button-primary" disabled={busy}
                    onClick={() => void run(() => approveJoinRequest(r.id))}>
                    Onayla
                  </button>
                  <button className="auth-button auth-button-secondary" disabled={busy}
                    onClick={() => { setRejecting(r); setRejectReason(''); }}>
                    Reddet
                  </button>
                </div>
                {rejecting?.id === r.id && (
                  <div className="form-stack join-reject-form">
                    <label className="form-label" htmlFor="reject-reason">
                      Gerekçe <span className="form-label-optional">(isteğe bağlı)</span>
                    </label>
                    <input id="reject-reason" className="form-input" maxLength={500} value={rejectReason}
                      onChange={(e) => setRejectReason(e.target.value)} autoFocus />
                    <div className="button-row">
                      <button className="auth-button auth-button-primary" disabled={busy} onClick={handleReject}>
                        Reddet
                      </button>
                      <button className="auth-button auth-button-secondary" onClick={() => setRejecting(null)}>
                        Vazgeç
                      </button>
                    </div>
                  </div>
                )}
              </li>
            ))}
          </ul>
        </section>
      )}

      <section className="form-section" aria-labelledby="invites-title">
        <h2 className="form-section-title" id="invites-title">Davet et</h2>
        <p className="form-section-hint">
          Bağlantıyı WhatsApp ile gönderin ya da karekodu gösterin. Gelen kişi bilgilerini yazar, telefonunu SMS koduyla
          doğrular; siz onaylayınca kursa eklenir.
        </p>
        {invitableRoles.map((role) => (
          <InvitationCard
            key={role}
            role={role}
            invitation={invitations.find((i) => i.role === role)}
            busy={busy}
            onCreate={(renew) => handleCreate(role, renew)}
            onRevoke={handleRevoke}
          />
        ))}
      </section>

      <section className="form-section" aria-labelledby="staff-title">
        <h2 className="form-section-title" id="staff-title">Kurs personeli ({staff.length})</h2>
        <ul className="staff-list">
          {staff.map((s) => (
            <li key={s.id} className="staff-item">
              <div className="student-avatar">{s.firstName.charAt(0)}</div>
              <div className="staff-item-info">
                <strong>{s.firstName} {s.lastName}</strong>
                <span>{STAFF_ROLE_LABELS[s.role] ?? s.role} · {formatPhone(s.phoneNumber)}</span>
              </div>
            </li>
          ))}
        </ul>
      </section>
    </div>
  );
}

function InvitationCard({
  role,
  invitation,
  busy,
  onCreate,
  onRevoke,
}: {
  role: InvitationRole;
  invitation?: IInvitation;
  busy: boolean;
  onCreate: (renew: boolean) => void;
  onRevoke: (invitation: IInvitation) => void;
}) {
  const [qr, setQr] = useState('');
  const [copied, setCopied] = useState(false);
  const link = invitation ? invitationLink(invitation.code) : '';

  useEffect(() => {
    if (!link) {
      setQr('');
      return;
    }
    let cancelled = false;
    QRCode.toDataURL(link, { width: 220, margin: 1, color: { dark: '#1B5C3A', light: '#FFFDF7' } })
      .then((url: string) => !cancelled && setQr(url))
      .catch(() => !cancelled && setQr(''));
    return () => {
      cancelled = true;
    };
  }, [link]);

  const copy = async () => {
    try {
      await navigator.clipboard.writeText(link);
      setCopied(true);
      setTimeout(() => setCopied(false), 2000);
    } catch {
      window.prompt('Bağlantıyı kopyalayın:', link);
    }
  };

  return (
    <div className="invite-card">
      <div className="invite-card-head">
        <strong>{INVITATION_ROLE_LABELS[role]} daveti</strong>
        {invitation && <span className="invite-card-expiry">{formatDateTime(invitation.expiresAt)} tarihine kadar geçerli</span>}
      </div>
      {invitation ? (
        <>
          <div className="invite-card-body">
            {qr && <img className="invite-qr" src={qr} alt={`${INVITATION_ROLE_LABELS[role]} davet karekodu`} width={180} height={180} />}
            <div className="invite-card-actions">
              <code className="invite-link" data-testid={`invite-link-${role}`}>{link}</code>
              <a className="auth-button auth-button-primary invite-whatsapp" href={whatsAppShareUrl(role, link)}
                target="_blank" rel="noopener noreferrer">
                WhatsApp ile gönder
              </a>
              <button className="auth-button auth-button-secondary" onClick={() => void copy()}>
                {copied ? 'Kopyalandı ✓' : 'Bağlantıyı kopyala'}
              </button>
            </div>
          </div>
          <div className="button-row">
            <button className="link-button" disabled={busy} onClick={() => onCreate(true)}>Yeni bağlantı oluştur</button>
            <button className="link-button link-button-danger" disabled={busy} onClick={() => onRevoke(invitation)}>İptal et</button>
          </div>
        </>
      ) : (
        <button className="auth-button auth-button-primary" disabled={busy} onClick={() => onCreate(false)}>
          {INVITATION_ROLE_LABELS[role]} davet bağlantısı oluştur
        </button>
      )}
    </div>
  );
}
