import { useEffect, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import AuthLayout from '../../layouts/AuthLayout';
import { getPublicInvitation, joinWithInvitation, sendJoinCode } from '../../api/invitations';
import { getErrorMessage, getFieldErrors } from '../../api/client';
import { Logger } from '../../services/logger';
import { PATHS } from '../../utils/navigation';
import { INVITATION_ROLE_LABELS, type IInvitationPublic } from '../../types/invitation';

const log = Logger.create('JoinPage');

type Step = 'loading' | 'invalid' | 'form' | 'code' | 'done';

/**
 * Davet bağlantısı (…/katil/KOD): kurs yöneticisinin gönderdiği bağlantıyla hoca/yardımcı hoca olarak katılma.
 * 1) davet bilgisi  2) ad, soyad, telefon → SMS kodu  3) kod → istek kurs yöneticisinin onayına düşer.
 * Telefon uygulaması kuruluysa Android bu bağlantıyı doğrudan uygulamada açar.
 */
export default function JoinPage() {
  const { code = '' } = useParams();
  const [step, setStep] = useState<Step>('loading');
  const [invitation, setInvitation] = useState<IInvitationPublic | null>(null);
  const [form, setForm] = useState({ firstName: '', lastName: '', phone: '', email: '' });
  const [otp, setOtp] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({});
  const [doneMessage, setDoneMessage] = useState('');

  useEffect(() => {
    let cancelled = false;
    getPublicInvitation(code)
      .then((data) => {
        if (cancelled) return;
        setInvitation(data);
        setStep('form');
      })
      .catch((err) => {
        if (cancelled) return;
        log.warn('load', 'Davet geçersiz', err);
        setError(getErrorMessage(err, 'Davet bağlantısı geçersiz veya süresi dolmuş.'));
        setStep('invalid');
      });
    return () => {
      cancelled = true;
    };
  }, [code]);

  const cleanedPhone = form.phone.replace(/\s/g, '');
  const set = (key: keyof typeof form) => (e: React.ChangeEvent<HTMLInputElement>) =>
    setForm((f) => ({ ...f, [key]: e.target.value }));

  const handleSendCode = async (e?: React.FormEvent) => {
    e?.preventDefault();
    setError('');
    setFieldErrors({});
    setLoading(true);
    try {
      await sendJoinCode(code, cleanedPhone);
      setOtp('');
      setStep('code');
    } catch (err) {
      log.error('handleSendCode', 'Kod gönderilemedi', err);
      setFieldErrors(getFieldErrors(err));
      setError(getErrorMessage(err, 'Doğrulama kodu gönderilemedi.'));
    } finally {
      setLoading(false);
    }
  };

  const submit = async (value: string) => {
    setError('');
    setLoading(true);
    try {
      const message = await joinWithInvitation(code, {
        firstName: form.firstName.trim(),
        lastName: form.lastName.trim(),
        phoneNumber: cleanedPhone,
        email: form.email.trim() || undefined,
        code: value,
      });
      setDoneMessage(message);
      setStep('done');
    } catch (err) {
      log.error('submit', 'Katılım isteği gönderilemedi', err);
      setError(getErrorMessage(err, 'Katılım isteği gönderilemedi.'));
    } finally {
      setLoading(false);
    }
  };

  const onOtpChange = (value: string) => {
    const digits = value.replace(/\D/g, '').slice(0, 6);
    setOtp(digits);
    if (digits.length === 6 && !loading) void submit(digits);
  };

  const roleLabel = invitation ? INVITATION_ROLE_LABELS[invitation.role] ?? invitation.role : '';
  const place = [invitation?.district, invitation?.city].filter(Boolean).join(' / ');

  if (step === 'loading') {
    return (
      <AuthLayout>
        <div className="auth-form">
          <p className="auth-form-hint">Davet açılıyor...</p>
        </div>
      </AuthLayout>
    );
  }

  if (step === 'invalid') {
    return (
      <AuthLayout>
        <div className="auth-form">
          <h2 className="auth-form-title">Davet açılamadı</h2>
          <div className="auth-error">{error}</div>
          <p className="auth-form-hint">Kurs yöneticinizden yeni bir davet bağlantısı isteyin.</p>
          <Link to={PATHS.login} className="auth-back-link">Kursa zaten kayıtlıysanız giriş yapın →</Link>
        </div>
      </AuthLayout>
    );
  }

  if (step === 'done') {
    return (
      <AuthLayout>
        <div className="auth-form join-done">
          <div className="join-done-icon" aria-hidden="true">✅</div>
          <h2 className="auth-form-title">İsteğiniz gönderildi</h2>
          <p className="auth-form-hint">{doneMessage}</p>
          <ol className="join-next-steps">
            <li>Kurs yöneticiniz isteğinizi onaylayacak.</li>
            <li>
              Telefonunuza <strong>Hafız Takip</strong> uygulamasını kurun.
            </li>
            <li>
              Uygulamada <strong>{cleanedPhone}</strong> numaranızla giriş yapın.
            </li>
          </ol>
          <Link to={PATHS.mobileApp} className="auth-button auth-button-primary">📱 Uygulamayı indir</Link>
          <Link to={PATHS.login} className="auth-back-link">Bilgisayardan giriş yap →</Link>
        </div>
      </AuthLayout>
    );
  }

  return (
    <AuthLayout>
      <div className="join-invite-card">
        <span className="join-invite-label">Davetlisiniz</span>
        <strong className="join-invite-course">{invitation?.courseName}</strong>
        {place && <span className="join-invite-place">{place}</span>}
        <span className="join-invite-role">{roleLabel} olarak katılın</span>
      </div>

      {step === 'form' ? (
        <form className="auth-form form-stack" onSubmit={handleSendCode} style={{ textAlign: 'left' }}>
          <p className="auth-form-hint">Bilgilerinizi yazın; telefonunuza bir doğrulama kodu göndereceğiz.</p>
          {error && <div className="auth-error">{error}</div>}

          <div className="form-row">
            <div className="form-field">
              <label className="form-label" htmlFor="join-first">Ad</label>
              <input id="join-first" className="form-input" value={form.firstName} onChange={set('firstName')}
                autoComplete="given-name" aria-invalid={!!fieldErrors.firstName} required />
              {fieldErrors.firstName && <span className="form-field-error">{fieldErrors.firstName}</span>}
            </div>
            <div className="form-field">
              <label className="form-label" htmlFor="join-last">Soyad</label>
              <input id="join-last" className="form-input" value={form.lastName} onChange={set('lastName')}
                autoComplete="family-name" aria-invalid={!!fieldErrors.lastName} required />
              {fieldErrors.lastName && <span className="form-field-error">{fieldErrors.lastName}</span>}
            </div>
          </div>
          <div className="form-field">
            <label className="form-label" htmlFor="join-phone">Cep telefonu</label>
            <input id="join-phone" className="form-input" type="tel" inputMode="tel" placeholder="05XX XXX XX XX"
              value={form.phone} onChange={set('phone')} maxLength={17} autoComplete="tel"
              aria-invalid={!!fieldErrors.phoneNumber} required />
            {fieldErrors.phoneNumber && <span className="form-field-error">{fieldErrors.phoneNumber}</span>}
            <span className="form-section-hint">Uygulamaya bu numarayla giriş yapacaksınız.</span>
          </div>
          <div className="form-field">
            <label className="form-label" htmlFor="join-email">
              E-posta <span className="form-label-optional">(isteğe bağlı)</span>
            </label>
            <input id="join-email" className="form-input" type="email" value={form.email} onChange={set('email')}
              autoComplete="email" />
          </div>

          <button type="submit" className="auth-button auth-button-primary"
            disabled={loading || !form.firstName.trim() || !form.lastName.trim() || cleanedPhone.length < 10}>
            {loading ? 'Gönderiliyor...' : 'Doğrulama Kodu Gönder'}
          </button>
        </form>
      ) : (
        <form className="auth-form" onSubmit={(e) => { e.preventDefault(); void submit(otp); }}>
          <p className="auth-form-hint">
            <strong>{form.phone}</strong> numarasına SMS ile gelen 6 haneli kodu girin.
          </p>
          {error && <div className="auth-error">{error}</div>}
          <input
            className="auth-input otp-single-input"
            inputMode="numeric"
            autoComplete="one-time-code"
            aria-label="Doğrulama kodu"
            placeholder="••••••"
            value={otp}
            onChange={(e) => onOtpChange(e.target.value)}
            autoFocus
          />
          <button type="submit" className="auth-button auth-button-primary" disabled={loading || otp.length !== 6}>
            {loading ? 'Gönderiliyor...' : 'Katılım İsteği Gönder'}
          </button>
          <button type="button" className="link-button" onClick={() => handleSendCode()} disabled={loading}>
            Kodu tekrar gönder
          </button>
          <span className="auth-back-link" onClick={() => setStep('form')}>← Bilgileri değiştir</span>
        </form>
      )}
    </AuthLayout>
  );
}
