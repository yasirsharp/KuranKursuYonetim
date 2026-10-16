import { useCallback, useEffect, useState } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import AuthLayout from '../../layouts/AuthLayout';
import { getMyApplication, resubmitApplication } from '../../api/courses';
import { getErrorMessage, getFieldErrors } from '../../api/client';
import { useAuth } from '../../context/AuthContext';
import { Logger } from '../../services/logger';
import { formatDateTime, PATHS } from '../../utils/navigation';
import type { ICourseApplicationInfo, ICourseApplicationStatus } from '../../types/course';

const log = Logger.create('ApplicationStatusPage');

/** Onay beklerken durum bu aralıkla kendiliğinden yenilenir */
const POLL_MS = 30_000;

export default function ApplicationStatusPage() {
  const navigate = useNavigate();
  const location = useLocation();
  const { user, updateUser, logout } = useAuth();
  const justSubmitted = (location.state as { justSubmitted?: boolean } | null)?.justSubmitted;

  const [application, setApplication] = useState<ICourseApplicationStatus | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [editing, setEditing] = useState(false);
  const [form, setForm] = useState<ICourseApplicationInfo>({ courseName: '', city: '', district: '' });
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({});
  const [saving, setSaving] = useState(false);
  const [notice, setNotice] = useState(justSubmitted ? 'Başvurunuz alındı.' : '');

  const load = useCallback(async () => {
    try {
      const data = await getMyApplication();
      setApplication(data);
      setError('');
      if (user && user.tenantStatus !== data.status) updateUser({ tenantStatus: data.status });
    } catch (err) {
      log.error('load', 'Başvuru durumu alınamadı', err);
      setError(getErrorMessage(err, 'Başvuru durumu alınamadı.'));
    } finally {
      setLoading(false);
    }
  }, [user, updateUser]);

  useEffect(() => {
    load();
    // yalnızca ilk yüklemede; sonrası aşağıdaki zamanlayıcı ve "Durumu yenile" ile
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  useEffect(() => {
    if (application?.status !== 'pending') return;
    const timer = setInterval(load, POLL_MS);
    return () => clearInterval(timer);
  }, [application?.status, load]);

  const startEditing = () => {
    if (!application) return;
    setForm({
      courseName: application.courseName,
      city: application.city ?? '',
      district: application.district ?? '',
      address: application.address ?? '',
      contactEmail: application.contactEmail ?? '',
    });
    setFieldErrors({});
    setEditing(true);
  };

  const handleResubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setSaving(true);
    setFieldErrors({});
    setError('');
    try {
      await resubmitApplication({
        ...form,
        address: form.address?.trim() || undefined,
        contactEmail: form.contactEmail?.trim() || undefined,
      });
      setEditing(false);
      setNotice('Başvurunuz yeniden onaya gönderildi.');
      await load();
    } catch (err) {
      setFieldErrors(getFieldErrors(err));
      setError(getErrorMessage(err, 'Başvuru gönderilemedi.'));
    } finally {
      setSaving(false);
    }
  };

  const handleLogout = () => {
    logout();
    navigate(PATHS.login, { replace: true });
  };

  const goToCourse = () => {
    updateUser({ tenantStatus: application?.status });
    navigate('/', { replace: true });
  };

  const input = (key: keyof ICourseApplicationInfo, label: string, optional = false) => (
    <div className="form-field">
      <label className="form-label" htmlFor={`re-${key}`}>
        {label} {optional && <span className="form-label-optional">(isteğe bağlı)</span>}
      </label>
      <input
        id={`re-${key}`}
        className="form-input"
        value={form[key] ?? ''}
        onChange={(e) => setForm((f) => ({ ...f, [key]: e.target.value }))}
        aria-invalid={!!fieldErrors[key]}
        required={!optional}
      />
      {fieldErrors[key] && <span className="form-field-error">{fieldErrors[key]}</span>}
    </div>
  );

  if (loading) {
    return (
      <AuthLayout>
        <div className="auth-form"><div className="skeleton" style={{ height: 160 }} /></div>
      </AuthLayout>
    );
  }

  const status = application?.status;
  const approved = status === 'active' || status === 'trial';

  return (
    <AuthLayout>
      <div className="auth-form form-stack" style={{ textAlign: 'left' }}>
        {notice && <div className="notice notice-success">{notice}</div>}
        {error && <div className="auth-error">{error}</div>}

        {application && (
          <div className="status-hero">
            {status === 'pending' && (
              <>
                <div className="status-hero-icon" style={{ background: 'var(--color-gold-pale)' }}>⏳</div>
                <h2 className="auth-form-title">Kursunuz onay bekliyor</h2>
                <p className="auth-form-hint">
                  Başvurunuz platform yöneticisine iletildi. Onaylandığında SMS ile bilgilendirileceksiniz;
                  bu sayfa da kendiliğinden güncellenir.
                </p>
              </>
            )}
            {status === 'rejected' && (
              <>
                <div className="status-hero-icon" style={{ background: 'var(--color-red-pale)' }}>✕</div>
                <h2 className="auth-form-title">Başvurunuz onaylanmadı</h2>
                <p className="auth-form-hint">Bilgilerinizi düzenleyip başvurunuzu yeniden gönderebilirsiniz.</p>
              </>
            )}
            {approved && (
              <>
                <div className="status-hero-icon" style={{ background: 'var(--color-green-pale)' }}>✓</div>
                <h2 className="auth-form-title">Kursunuz onaylandı</h2>
                <p className="auth-form-hint">Artık kursunuzu kurmaya başlayabilirsiniz.</p>
              </>
            )}
          </div>
        )}

        {status === 'rejected' && application?.rejectionReason && (
          <div className="notice notice-error">
            <strong>Gerekçe:</strong> {application.rejectionReason}
          </div>
        )}

        {application && !editing && (
          <div className="info-card">
            <span className="info-card-title">{application.courseName}</span>
            <dl className="info-list">
              <dt>İl / İlçe</dt>
              <dd>{[application.city, application.district].filter(Boolean).join(' / ') || '—'}</dd>
              {application.address && (<><dt>Adres</dt><dd>{application.address}</dd></>)}
              {application.contactEmail && (<><dt>E-posta</dt><dd>{application.contactEmail}</dd></>)}
              <dt>Başvuru</dt>
              <dd>{formatDateTime(application.submittedAt)}</dd>
              {application.reviewedAt && (<><dt>İnceleme</dt><dd>{formatDateTime(application.reviewedAt)}</dd></>)}
            </dl>
          </div>
        )}

        {editing && (
          <form className="form-stack" onSubmit={handleResubmit}>
            {input('courseName', 'Kurs adı')}
            <div className="form-row">
              {input('city', 'İl')}
              {input('district', 'İlçe')}
            </div>
            {input('address', 'Adres', true)}
            {input('contactEmail', 'E-posta', true)}
            <div className="button-row">
              <button type="button" className="auth-button auth-button-secondary" onClick={() => setEditing(false)} disabled={saving}>Vazgeç</button>
              <button type="submit" className="auth-button auth-button-primary" disabled={saving}>
                {saving ? 'Gönderiliyor...' : 'Yeniden Gönder'}
              </button>
            </div>
          </form>
        )}

        {!editing && (
          <div className="form-stack">
            {approved && (
              <button className="auth-button auth-button-primary" onClick={goToCourse}>Kursuma Git</button>
            )}
            {status === 'rejected' && (
              <button className="auth-button auth-button-primary" onClick={startEditing}>Bilgileri Düzenle ve Yeniden Gönder</button>
            )}
            {status === 'pending' && (
              <button className="auth-button auth-button-secondary" onClick={() => { setNotice(''); setLoading(true); load(); }}>Durumu Yenile</button>
            )}
            <span className="auth-back-link" onClick={handleLogout}>Çıkış yap</span>
          </div>
        )}
      </div>
    </AuthLayout>
  );
}
