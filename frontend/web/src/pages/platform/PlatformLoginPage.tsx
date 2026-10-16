import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import AuthLayout from '../../layouts/AuthLayout';
import { platformLogin, platformToAuthUser } from '../../api/auth';
import { getErrorMessage } from '../../api/client';
import { useAuth } from '../../context/AuthContext';
import { Logger } from '../../services/logger';
import { PATHS } from '../../utils/navigation';

const log = Logger.create('PlatformLoginPage');

export default function PlatformLoginPage() {
  const navigate = useNavigate();
  const { login } = useAuth();
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');
    setLoading(true);
    try {
      const result = await platformLogin(email.trim(), password);
      login(platformToAuthUser(result));
      navigate(PATHS.platform, { replace: true });
    } catch (err) {
      log.warn('handleSubmit', 'Platform girişi başarısız', err);
      setError(getErrorMessage(err, 'Giriş yapılamadı.'));
    } finally {
      setLoading(false);
    }
  };

  return (
    <AuthLayout>
      <form className="auth-form" onSubmit={handleSubmit} style={{ textAlign: 'left' }}>
        <h2 className="auth-form-title" style={{ textAlign: 'center' }}>Platform Yöneticisi</h2>
        <p className="auth-form-hint" style={{ textAlign: 'center' }}>Kurs başvurularını onaylamak için giriş yapın</p>

        {error && <div className="auth-error">{error}</div>}

        <div className="form-field">
          <label className="form-label" htmlFor="pl-email">E-posta</label>
          <input id="pl-email" className="form-input" type="email" autoComplete="username"
            value={email} onChange={(e) => setEmail(e.target.value)} required autoFocus />
        </div>
        <div className="form-field">
          <label className="form-label" htmlFor="pl-password">Şifre</label>
          <input id="pl-password" className="form-input" type="password" autoComplete="current-password"
            value={password} onChange={(e) => setPassword(e.target.value)} required />
        </div>

        <button type="submit" className="auth-button auth-button-primary" disabled={loading || !email || !password}>
          {loading ? 'Giriş yapılıyor...' : 'Giriş Yap'}
        </button>

        <Link to={PATHS.login} className="auth-back-link" style={{ textAlign: 'center' }}>← Kurs girişine dön</Link>
      </form>
    </AuthLayout>
  );
}
