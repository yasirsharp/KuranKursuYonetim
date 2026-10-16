import { useCallback, useEffect, useState } from 'react';
import { approveTenant, getTenants, rejectTenant } from '../../api/courses';
import { getErrorMessage } from '../../api/client';
import { Logger } from '../../services/logger';
import { formatDateTime } from '../../utils/navigation';
import { TENANT_STATUS_LABELS, type ITenant } from '../../types/course';
import type { TenantStatus } from '../../types/auth';

const log = Logger.create('PlatformTenantsPage');

const TABS: { status?: TenantStatus; label: string }[] = [
  { status: 'pending', label: 'Onay bekleyenler' },
  { status: 'active', label: 'Aktif' },
  { status: 'rejected', label: 'Reddedilenler' },
  { status: undefined, label: 'Tümü' },
];

/** Kurs başvurularının onay kuyruğu ve kurs listesi */
export default function PlatformTenantsPage() {
  const [tab, setTab] = useState(0);
  const [search, setSearch] = useState('');
  const [tenants, setTenants] = useState<ITenant[]>([]);
  const [total, setTotal] = useState(0);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [notice, setNotice] = useState('');
  const [busyId, setBusyId] = useState<string | null>(null);
  const [rejecting, setRejecting] = useState<ITenant | null>(null);
  const [reason, setReason] = useState('');

  const status = TABS[tab]?.status;

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const result = await getTenants({ status, search: search.trim() || undefined });
      setTenants(result.data ?? result.items ?? []);
      setTotal(result.totalCount);
      setError('');
    } catch (err) {
      log.error('load', 'Kurslar alınamadı', err);
      setError(getErrorMessage(err, 'Kurslar alınamadı.'));
    } finally {
      setLoading(false);
    }
  }, [status, search]);

  useEffect(() => {
    const timer = setTimeout(load, 250); // aramada her tuşta istek atılmasın
    return () => clearTimeout(timer);
  }, [load]);

  const handleApprove = async (tenant: ITenant) => {
    if (!window.confirm(`"${tenant.name}" başvurusu onaylansın mı? Kurs müdürüne SMS gönderilecek.`)) return;
    setBusyId(tenant.id);
    setNotice('');
    try {
      await approveTenant(tenant.id);
      setNotice(`"${tenant.name}" onaylandı.`);
      await load();
    } catch (err) {
      setError(getErrorMessage(err, 'Onaylanamadı.'));
    } finally {
      setBusyId(null);
    }
  };

  const handleReject = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!rejecting) return;
    setBusyId(rejecting.id);
    try {
      await rejectTenant(rejecting.id, reason.trim());
      setNotice(`"${rejecting.name}" reddedildi; gerekçe kurs müdürüne SMS ile iletildi.`);
      setRejecting(null);
      setReason('');
      await load();
    } catch (err) {
      setError(getErrorMessage(err, 'Reddedilemedi.'));
    } finally {
      setBusyId(null);
    }
  };

  return (
    <>
      <div>
        <h1 className="page-title">Kurslar</h1>
        <p className="page-subtitle">Yeni kurs başvurularını inceleyin, onaylayın veya gerekçesiyle reddedin.</p>
      </div>

      <div className="tab-bar" role="tablist">
        {TABS.map((t, i) => (
          <button key={t.label} role="tab" aria-selected={i === tab}
            className={`tab-button ${i === tab ? 'active' : ''}`} onClick={() => setTab(i)}>
            {t.label}
          </button>
        ))}
      </div>

      <input className="form-input" placeholder="Kurs adı veya il ara..." value={search}
        onChange={(e) => setSearch(e.target.value)} aria-label="Kurs ara" />

      {notice && <div className="notice notice-success">{notice}</div>}
      {error && <div className="notice notice-error">{error}</div>}

      {loading ? (
        <div className="card-grid">
          <div className="skeleton" style={{ height: 170 }} />
          <div className="skeleton" style={{ height: 170 }} />
        </div>
      ) : tenants.length === 0 ? (
        <div className="notice notice-info">
          {status === 'pending' ? 'Onay bekleyen başvuru yok.' : 'Kayıt bulunamadı.'}
        </div>
      ) : (
        <>
          <p className="page-subtitle">{total} kurs</p>
          <div className="card-grid">
            {tenants.map((t) => (
              <article key={t.id} className="info-card">
                <div className="info-card-header">
                  <span className="info-card-title">{t.name}</span>
                  <span className={`status-badge status-${t.status}`}>{TENANT_STATUS_LABELS[t.status] ?? t.status}</span>
                </div>
                <dl className="info-list">
                  <dt>İl / İlçe</dt>
                  <dd>{[t.city, t.district].filter(Boolean).join(' / ') || '—'}</dd>
                  {t.address && (<><dt>Adres</dt><dd>{t.address}</dd></>)}
                  <dt>Telefon</dt>
                  <dd>{t.contactPhone || '—'}</dd>
                  {t.contactEmail && (<><dt>E-posta</dt><dd>{t.contactEmail}</dd></>)}
                  <dt>Başvuru</dt>
                  <dd>{formatDateTime(t.submittedAt ?? t.createdAt)}</dd>
                  {t.reviewedAt && (<><dt>İnceleme</dt><dd>{formatDateTime(t.reviewedAt)}</dd></>)}
                  {t.rejectionReason && (<><dt>Ret gerekçesi</dt><dd>{t.rejectionReason}</dd></>)}
                </dl>
                {t.status === 'pending' && (
                  <div className="button-row">
                    <button className="auth-button auth-button-secondary auth-button-small"
                      disabled={busyId === t.id} onClick={() => { setRejecting(t); setReason(''); }}>
                      Reddet
                    </button>
                    <button className="auth-button auth-button-primary auth-button-small"
                      disabled={busyId === t.id} onClick={() => handleApprove(t)}>
                      {busyId === t.id ? 'İşleniyor...' : 'Onayla'}
                    </button>
                  </div>
                )}
              </article>
            ))}
          </div>
        </>
      )}

      {rejecting && (
        <div className="modal-backdrop" onClick={() => setRejecting(null)}>
          <form className="modal-sheet" onClick={(e) => e.stopPropagation()} onSubmit={handleReject}>
            <h2 className="form-section-title">Başvuruyu reddet</h2>
            <p className="page-subtitle">
              <strong>{rejecting.name}</strong> — gerekçe kurs müdürüne SMS ile gönderilir ve başvuru ekranında görünür.
            </p>
            <textarea className="form-textarea" placeholder="Ör. Adres bilgisi eksik, lütfen tam adresi ekleyin."
              value={reason} onChange={(e) => setReason(e.target.value)} maxLength={500} autoFocus aria-label="Ret gerekçesi" />
            <div className="button-row">
              <button type="button" className="auth-button auth-button-secondary auth-button-small" onClick={() => setRejecting(null)}>
                Vazgeç
              </button>
              <button type="submit" className="auth-button auth-button-danger auth-button-small"
                disabled={reason.trim().length < 5 || busyId === rejecting.id}>
                Reddet
              </button>
            </div>
          </form>
        </div>
      )}
    </>
  );
}
