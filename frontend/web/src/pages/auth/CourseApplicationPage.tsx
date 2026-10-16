import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import AuthLayout from '../../layouts/AuthLayout';
import { sendApplicationCode, submitApplication } from '../../api/courses';
import { getErrorMessage, getFieldErrors } from '../../api/client';
import { toAuthUser } from '../../api/auth';
import { useAuth } from '../../context/AuthContext';
import { Logger } from '../../services/logger';
import { PATHS } from '../../utils/navigation';
import type { ICourseApplication } from '../../types/course';

const log = Logger.create('CourseApplicationPage');

type FormState = Omit<ICourseApplication, 'phoneNumber'>;

const EMPTY_FORM: FormState = {
  code: '',
  courseName: '',
  city: '',
  district: '',
  address: '',
  contactEmail: '',
  adminFirstName: '',
  adminLastName: '',
};

/**
 * Herkese açık "Kurs oluştur" akışı:
 * 1) telefon numarasına doğrulama kodu gönderilir
 * 2) kod + kurs bilgileri + yönetici adı gönderilir → kurs "onay bekliyor" olarak açılır, oturum başlar
 */
export default function CourseApplicationPage() {
  const navigate = useNavigate();
  const { login } = useAuth();
  const [step, setStep] = useState<'phone' | 'form'>('phone');
  const [phone, setPhone] = useState('');
  const [form, setForm] = useState<FormState>(EMPTY_FORM);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({});

  const cleanedPhone = phone.replace(/\s/g, '');

  const set = (key: keyof FormState) => (e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) =>
    setForm((f) => ({ ...f, [key]: e.target.value }));

  const handleSendCode = async (e?: React.FormEvent) => {
    e?.preventDefault();
    setError('');
    setLoading(true);
    try {
      await sendApplicationCode(cleanedPhone);
      setStep('form');
    } catch (err) {
      log.error('handleSendCode', 'Kod gönderilemedi', err);
      setError(getErrorMessage(err, 'Doğrulama kodu gönderilemedi.'));
    } finally {
      setLoading(false);
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');
    setFieldErrors({});
    setLoading(true);
    try {
      const result = await submitApplication({
        ...form,
        phoneNumber: cleanedPhone,
        address: form.address?.trim() || undefined,
        contactEmail: form.contactEmail?.trim() || undefined,
      });
      log.info('handleSubmit', 'Başvuru alındı', { tenantId: result.tenantId });
      login(toAuthUser(result));
      navigate(PATHS.applicationStatus, { replace: true, state: { justSubmitted: true } });
    } catch (err) {
      log.error('handleSubmit', 'Başvuru gönderilemedi', err);
      setFieldErrors(getFieldErrors(err));
      setError(getErrorMessage(err, 'Başvuru gönderilemedi.'));
    } finally {
      setLoading(false);
    }
  };

  const field = (
    key: keyof FormState,
    label: string,
    options: { optional?: boolean; type?: string; placeholder?: string; autoComplete?: string } = {}
  ) => (
    <div className="form-field">
      <label className="form-label" htmlFor={`app-${key}`}>
        {label} {options.optional && <span className="form-label-optional">(isteğe bağlı)</span>}
      </label>
      <input
        id={`app-${key}`}
        className="form-input"
        type={options.type ?? 'text'}
        value={form[key] ?? ''}
        onChange={set(key)}
        placeholder={options.placeholder}
        autoComplete={options.autoComplete}
        aria-invalid={!!fieldErrors[key]}
        required={!options.optional}
      />
      {fieldErrors[key] && <span className="form-field-error">{fieldErrors[key]}</span>}
    </div>
  );

  if (step === 'phone') {
    return (
      <AuthLayout>
        <form className="auth-form" onSubmit={handleSendCode}>
          <h2 className="auth-form-title">Kurs Oluştur</h2>
          <p className="auth-form-hint">
            Kursunuzu kaydedin; platform yöneticisi onayladıktan sonra hocalarınızı ve öğrencilerinizi eklemeye başlayabilirsiniz.
            Önce telefon numaranızı doğrulayalım.
          </p>

          {error && <div className="auth-error">{error}</div>}

          <input
            className="auth-input"
            type="tel"
            placeholder="05XX XXX XX XX"
            value={phone}
            onChange={(e) => setPhone(e.target.value)}
            maxLength={15}
            autoFocus
          />

          <button type="submit" className="auth-button auth-button-primary" disabled={loading || cleanedPhone.length < 10}>
            {loading ? 'Gönderiliyor...' : 'Doğrulama Kodu Gönder'}
          </button>

          <Link to={PATHS.login} className="auth-back-link">← Girişe dön</Link>
        </form>
      </AuthLayout>
    );
  }

  return (
    <AuthLayout>
      <form className="auth-form form-stack" onSubmit={handleSubmit} style={{ textAlign: 'left' }}>
        <h2 className="auth-form-title">Kurs Bilgileri</h2>
        <p className="auth-form-hint">
          <strong>{phone}</strong> numarasına gönderilen kodu ve kurs bilgilerinizi girin.
        </p>

        {error && <div className="auth-error">{error}</div>}

        <div className="form-field">
          <label className="form-label" htmlFor="app-code">Doğrulama kodu</label>
          <input
            id="app-code"
            className="form-input"
            inputMode="numeric"
            maxLength={6}
            value={form.code}
            onChange={(e) => setForm((f) => ({ ...f, code: e.target.value.replace(/\D/g, '') }))}
            aria-invalid={!!fieldErrors.code}
            autoFocus
            required
          />
          <button type="button" className="link-button" style={{ alignSelf: 'flex-start', fontSize: 12 }} onClick={() => handleSendCode()} disabled={loading}>
            Kodu tekrar gönder
          </button>
        </div>

        {field('courseName', 'Kurs adı', { placeholder: "Ör. Merkez Kur'an Kursu" })}
        <div className="form-row">
          {field('city', 'İl')}
          {field('district', 'İlçe')}
        </div>
        {field('address', 'Adres', { optional: true })}
        {field('contactEmail', 'E-posta', { optional: true, type: 'email', autoComplete: 'email' })}

        <p className="form-label" style={{ marginTop: 4 }}>Kurs müdürü (siz)</p>
        <div className="form-row">
          {field('adminFirstName', 'Ad', { autoComplete: 'given-name' })}
          {field('adminLastName', 'Soyad', { autoComplete: 'family-name' })}
        </div>

        <button type="submit" className="auth-button auth-button-primary" disabled={loading || form.code.length < 4}>
          {loading ? 'Gönderiliyor...' : 'Başvuruyu Gönder'}
        </button>

        <span className="auth-back-link" onClick={() => setStep('phone')}>← Numarayı değiştir</span>
      </form>
    </AuthLayout>
  );
}
