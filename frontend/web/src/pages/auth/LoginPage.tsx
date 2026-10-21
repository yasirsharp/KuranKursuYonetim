import { useState } from 'react';
import { Link, useNavigate, useSearchParams } from 'react-router-dom';
import { sendEmailCode, sendOtp } from '../../api/auth';
import { getErrorMessage } from '../../api/client';
import { Logger } from '../../services/logger';
import AuthLayout from '../../layouts/AuthLayout';
import { PATHS } from '../../utils/navigation';

const log = Logger.create('LoginPage');

const REASON_MESSAGES: Record<string, string> = {
  'kurs-pasif': 'Kurumunuzun hesabı askıya alınmış veya kapatılmış. Lütfen platform yöneticisiyle iletişime geçin.',
};

export default function LoginPage() {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const [method, setMethod] = useState<'phone' | 'email'>('phone');
  const [phone, setPhone] = useState('');
  const [email, setEmail] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const reason = REASON_MESSAGES[searchParams.get('neden') ?? ''];

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');

    if (method === 'email') {
      const address = email.trim();
      if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(address)) {
        setError('Geçerli bir e-posta adresi girin.');
        return;
      }
      setLoading(true);
      try {
        await sendEmailCode(address);
        navigate(PATHS.verifyOtp, { state: { email: address } });
      } catch (err) {
        log.error('handleSubmit', 'E-posta kodu gönderilemedi', err);
        setError(getErrorMessage(err, 'Kod gönderilemedi. Lütfen tekrar deneyin.'));
      } finally {
        setLoading(false);
      }
      return;
    }

    const cleaned = phone.replace(/\s/g, '');
    if (cleaned.length < 10) {
      setError('Geçerli bir telefon numarası girin.');
      return;
    }

    setLoading(true);
    log.info('handleSubmit', 'OTP gönderiliyor', { phone: cleaned });

    try {
      await sendOtp({ phoneNumber: cleaned });
      log.info('handleSubmit', 'OTP gönderildi, doğrulama sayfasına yönlendiriliyor');
      navigate(PATHS.verifyOtp, { state: { phoneNumber: cleaned } });
    } catch (err) {
      log.error('handleSubmit', 'OTP gönderilemedi', err);
      setError(getErrorMessage(err, 'Kod gönderilemedi. Lütfen tekrar deneyin.'));
    } finally {
      setLoading(false);
    }
  };

  return (
    <AuthLayout>
      <form className="auth-form" onSubmit={handleSubmit}>
        <h2 className="auth-form-title">Giriş Yap</h2>
        <p className="auth-form-hint">
          {method === 'phone' ? 'Telefon numaranızı girerek devam edin' : 'Kayıtlı e-posta adresinizi girerek devam edin'}
        </p>

        <div className="tab-bar" role="tablist">
          <button type="button" role="tab" aria-selected={method === 'phone'}
            className={`tab-button ${method === 'phone' ? 'active' : ''}`} onClick={() => { setMethod('phone'); setError(''); }}>
            Telefon
          </button>
          <button type="button" role="tab" aria-selected={method === 'email'}
            className={`tab-button ${method === 'email' ? 'active' : ''}`} onClick={() => { setMethod('email'); setError(''); }}>
            E-posta
          </button>
        </div>

        {reason && <div className="auth-error">{reason}</div>}
        {error && <div className="auth-error">{error}</div>}

        {method === 'phone' ? (
          <input
            className="auth-input"
            type="tel"
            placeholder="05XX XXX XX XX"
            value={phone}
            onChange={(e) => setPhone(e.target.value)}
            maxLength={15}
            autoFocus
          />
        ) : (
          <input
            className="auth-input"
            type="email"
            placeholder="ornek@kurs.com"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            maxLength={255}
            autoComplete="email"
            style={{ letterSpacing: 'normal' }}
            autoFocus
          />
        )}

        <button
          type="submit"
          className="auth-button auth-button-primary"
          disabled={loading || (method === 'phone' ? phone.replace(/\s/g, '').length < 10 : !email.includes('@'))}
        >
          {loading ? 'Gönderiliyor...' : 'Doğrulama Kodu Gönder'}
        </button>

        {import.meta.env.VITE_SHOW_DEV_HINTS === 'true' && (
          <p style={{ fontSize: 11, color: 'var(--color-muted)', marginTop: 4 }}>
            Test ortamı: SMS/e-posta gönderilmez, doğrulama kodu <strong>123456</strong>
          </p>
        )}
      </form>

      <div className="auth-links">
        <span>
          Kursunuz henüz kayıtlı değil mi?{' '}
          <Link to={PATHS.courseApplication} style={{ color: 'var(--color-green)', fontWeight: 800 }}>
            Kurs oluştur
          </Link>
        </span>
        <Link to={PATHS.platformLogin} style={{ color: 'var(--color-muted)', fontSize: 12 }}>
          Platform yöneticisi girişi
        </Link>
      </div>
    </AuthLayout>
  );
}
