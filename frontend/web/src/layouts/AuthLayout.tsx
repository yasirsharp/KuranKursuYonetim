import '../styles/globals.css';
import '../styles/layouts.css';

export default function AuthLayout({ children }: { children: React.ReactNode }) {
  return (
    <div className="auth-layout">
      <div className="auth-container">
        <div className="auth-logo">
          <span className="auth-logo-icon">☪️</span>
          <h1 className="auth-logo-title">Hafız Takip</h1>
          <p className="auth-logo-subtitle">Kur'an-ı Kerim Ezberleme Takip Sistemi</p>
        </div>
        <div className="auth-content fade-in">
          {children}
        </div>
      </div>
    </div>
  );
}
