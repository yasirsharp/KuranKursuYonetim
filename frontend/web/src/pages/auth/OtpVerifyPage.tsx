import { useState, useRef, useEffect } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import { toAuthUser, verifyOtp } from '../../api/auth';
import { getErrorMessage } from '../../api/client';
import { useAuth } from '../../context/AuthContext';
import { Logger } from '../../services/logger';
import AuthLayout from '../../layouts/AuthLayout';
import { homePathFor, PATHS } from '../../utils/navigation';
import { TENANT_STATUS_LABELS } from '../../types/course';
import type { ITenantOption } from '../../types/auth';

const log = Logger.create('OtpVerifyPage');
const OTP_LENGTH = 6;

const ROLE_LABELS: Record<string, string> = {
  tenant_admin: 'Kurs müdürü',
  head_teacher: 'Başhoca',
  assistant_teacher: 'Yardımcı hoca',
  parent: 'Veli',
  student: 'Öğrenci',
};

export default function OtpVerifyPage() {
  const location = useLocation();
  const navigate = useNavigate();
  const { login } = useAuth();
  const phoneNumber = (location.state as { phoneNumber?: string })?.phoneNumber;

  const [otp, setOtp] = useState<string[]>(Array(OTP_LENGTH).fill(''));
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  // Numara birden fazla kursta kayıtlıysa kurs seçimi
  const [tenantOptions, setTenantOptions] = useState<ITenantOption[] | null>(null);
  const inputRefs = useRef<(HTMLInputElement | null)[]>([]);

  // Telefon numarası yoksa login'e yönlendir
  useEffect(() => {
    if (!phoneNumber) {
      log.warn('mount', 'Telefon numarası yok — login sayfasına yönlendiriliyor');
      navigate(PATHS.login, { replace: true });
    }
  }, [phoneNumber, navigate]);

  const handleChange = (index: number, value: string) => {
    const digits = value.replace(/\D/g, '');
    if (value && !digits) return;

    // Hızlı yazım veya telefonun SMS'ten otomatik doldurması birden fazla rakamı tek kutuya getirebilir:
    // rakamlar bu kutudan itibaren dağıtılır
    const newOtp = [...otp];
    // Dolu kutuya tek rakam yazılırsa ("3" → "37") yalnızca yeni rakam alınır
    const typedOverExisting = digits.length === 2 && !!otp[index] && digits.startsWith(otp[index]!);
    const chars = digits.length > 1 && !typedOverExisting
      ? digits.slice(0, OTP_LENGTH - index).split('')
      : [digits.slice(-1)];
    chars.forEach((c, i) => { newOtp[index + i] = c; });
    setOtp(newOtp);

    // Sonraki boş kutuya odaklan
    if (digits && index < OTP_LENGTH - 1) {
      inputRefs.current[Math.min(index + chars.length, OTP_LENGTH - 1)]?.focus();
    }
  };

  const handleKeyDown = (index: number, e: React.KeyboardEvent) => {
    if (e.key === 'Backspace' && !otp[index] && index > 0) {
      inputRefs.current[index - 1]?.focus();
    }
  };

  const handlePaste = (e: React.ClipboardEvent) => {
    e.preventDefault();
    const pasted = e.clipboardData.getData('text').replace(/\D/g, '').slice(0, OTP_LENGTH);
    const newOtp = [...otp];
    for (let i = 0; i < pasted.length; i++) {
      newOtp[i] = pasted[i] ?? '';
    }
    setOtp(newOtp);
    inputRefs.current[Math.min(pasted.length, OTP_LENGTH - 1)]?.focus();
  };

  const verify = async (tenantId?: string) => {
    if (!phoneNumber) return;
    const code = otp.join('');

    setLoading(true);
    setError('');
    log.info('verify', 'OTP doğrulanıyor', { phone: phoneNumber, tenantId });

    try {
      const result = await verifyOtp({ phoneNumber, code, tenantId });

      if (result.requiresTenantSelection) {
        // Kod doğru ama numara birden fazla kursta kayıtlı: kod tüketilmedi, kurs seçilince aynı kodla tekrar doğrulanır
        setTenantOptions(result.tenants);
        return;
      }

      log.info('verify', 'Giriş başarılı', { name: `${result.firstName} ${result.lastName}`, role: result.role });
      const user = toAuthUser(result);
      login(user);
      navigate(homePathFor(user), { replace: true });
    } catch (err) {
      log.error('verify', 'OTP doğrulama başarısız', err);
      setError(getErrorMessage(err, 'Geçersiz veya süresi dolmuş kod.'));
      if (!tenantId) {
        setOtp(Array(OTP_LENGTH).fill(''));
        inputRefs.current[0]?.focus();
      }
    } finally {
      setLoading(false);
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (otp.join('').length !== OTP_LENGTH) {
      setError('Lütfen 6 haneli kodu eksiksiz girin.');
      return;
    }
    await verify();
  };

  if (!phoneNumber) return null;

  if (tenantOptions) {
    return (
      <AuthLayout>
        <div className="auth-form">
          <h2 className="auth-form-title">Kurs Seçin</h2>
          <p className="auth-form-hint">Numaranız birden fazla kursta kayıtlı. Hangisine giriş yapmak istiyorsunuz?</p>

          {error && <div className="auth-error">{error}</div>}

          <div className="form-stack">
            {tenantOptions.map((t) => (
              <button
                key={t.tenantId}
                type="button"
                className="info-card"
                style={{ cursor: 'pointer', textAlign: 'left' }}
                disabled={loading}
                onClick={() => verify(t.tenantId)}
              >
                <div className="info-card-header">
                  <span className="info-card-title">{t.tenantName}</span>
                  {t.tenantStatus !== 'active' && (
                    <span className={`status-badge status-${t.tenantStatus}`}>{TENANT_STATUS_LABELS[t.tenantStatus]}</span>
                  )}
                </div>
                <span style={{ fontSize: 13, color: 'var(--color-muted)' }}>{ROLE_LABELS[t.role] ?? t.role}</span>
              </button>
            ))}
          </div>

          <span className="auth-back-link" onClick={() => navigate(PATHS.login)}>
            ← Farklı numara ile giriş yap
          </span>
        </div>
      </AuthLayout>
    );
  }

  return (
    <AuthLayout>
      <form className="auth-form" onSubmit={handleSubmit}>
        <h2 className="auth-form-title">Doğrulama Kodu</h2>
        <p className="auth-form-hint">
          <strong>{phoneNumber}</strong> numarasına gönderilen 6 haneli kodu girin
        </p>

        {error && <div className="auth-error">{error}</div>}

        <div className="otp-input-group" onPaste={handlePaste}>
          {otp.map((digit, index) => (
            <input
              key={index}
              ref={(el) => { inputRefs.current[index] = el; }}
              className="otp-input"
              type="text"
              inputMode="numeric"
              autoComplete={index === 0 ? 'one-time-code' : 'off'}
              aria-label={`Kodun ${index + 1}. hanesi`}
              value={digit}
              onChange={(e) => handleChange(index, e.target.value)}
              onKeyDown={(e) => handleKeyDown(index, e)}
              autoFocus={index === 0}
            />
          ))}
        </div>

        <button
          type="submit"
          className="auth-button auth-button-primary"
          disabled={loading || otp.join('').length !== OTP_LENGTH}
        >
          {loading ? 'Doğrulanıyor...' : 'Giriş Yap'}
        </button>

        <span className="auth-back-link" onClick={() => navigate(PATHS.login)}>
          ← Farklı numara ile giriş yap
        </span>
      </form>
    </AuthLayout>
  );
}
