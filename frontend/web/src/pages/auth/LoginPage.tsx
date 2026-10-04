import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { sendOtp } from '../../api/auth';
import { Logger } from '../../services/logger';
import AuthLayout from '../../layouts/AuthLayout';

const log = Logger.create('LoginPage');

export default function LoginPage() {
  const navigate = useNavigate();
  const [phone, setPhone] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');

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
      navigate('/verify-otp', { state: { phoneNumber: cleaned } });
    } catch (err) {
      log.error('handleSubmit', 'OTP gönderilemedi', err);
      setError('Kullanıcı bulunamadı veya bir hata oluştu.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <AuthLayout>
      <form className="auth-form" onSubmit={handleSubmit}>
        <h2 className="auth-form-title">Giriş Yap</h2>
        <p className="auth-form-hint">Telefon numaranızı girerek devam edin</p>

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

        <button
          type="submit"
          className="auth-button auth-button-primary"
          disabled={loading || phone.replace(/\s/g, '').length < 10}
        >
          {loading ? 'Gönderiliyor...' : 'Doğrulama Kodu Gönder'}
        </button>

        <p style={{ fontSize: 11, color: 'var(--color-muted)', marginTop: 4 }}>
          Geliştirme ortamında OTP kodu: <strong>123456</strong>
        </p>
      </form>
    </AuthLayout>
  );
}
