import { useState, useRef, useEffect } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import { verifyOtp } from '../../api/auth';
import { useAuth } from '../../context/AuthContext';
import { Logger } from '../../services/logger';
import AuthLayout from '../../layouts/AuthLayout';
import type { UserRole } from '../../types/auth';

const log = Logger.create('OtpVerifyPage');
const OTP_LENGTH = 6;

export default function OtpVerifyPage() {
  const location = useLocation();
  const navigate = useNavigate();
  const { login } = useAuth();
  const phoneNumber = (location.state as { phoneNumber?: string })?.phoneNumber;

  const [otp, setOtp] = useState<string[]>(Array(OTP_LENGTH).fill(''));
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const inputRefs = useRef<(HTMLInputElement | null)[]>([]);

  // Telefon numarası yoksa login'e yönlendir
  useEffect(() => {
    if (!phoneNumber) {
      log.warn('mount', 'Telefon numarası yok — login sayfasına yönlendiriliyor');
      navigate('/login', { replace: true });
    }
  }, [phoneNumber, navigate]);

  const handleChange = (index: number, value: string) => {
    if (!/^\d*$/.test(value)) return;

    const newOtp = [...otp];
    newOtp[index] = value.slice(-1);
    setOtp(newOtp);

    // Sonraki input'a odaklan
    if (value && index < OTP_LENGTH - 1) {
      inputRefs.current[index + 1]?.focus();
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

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');

    const code = otp.join('');
    if (code.length !== OTP_LENGTH) {
      setError('Lütfen 6 haneli kodu eksiksiz girin.');
      return;
    }

    if (!phoneNumber) return;

    setLoading(true);
    log.info('handleSubmit', 'OTP doğrulanıyor', { phone: phoneNumber });

    try {
      const result = await verifyOtp({ phoneNumber, code });
      log.info('handleSubmit', 'Giriş başarılı', {
        name: `${result.firstName} ${result.lastName}`,
        role: result.role,
      });

      login({
        token: result.token,
        tenantId: result.tenantId,
        role: result.role as UserRole,
        firstName: result.firstName,
        lastName: result.lastName,
        expiration: result.expiration,
      });

      navigate('/', { replace: true });
    } catch (err) {
      log.error('handleSubmit', 'OTP doğrulama başarısız', err);
      setError('Geçersiz veya süresi dolmuş kod.');
      setOtp(Array(OTP_LENGTH).fill(''));
      inputRefs.current[0]?.focus();
    } finally {
      setLoading(false);
    }
  };

  if (!phoneNumber) return null;

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
              maxLength={1}
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

        <span className="auth-back-link" onClick={() => navigate('/login')}>
          ← Farklı numara ile giriş yap
        </span>
      </form>
    </AuthLayout>
  );
}
