import { Outlet, useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { PATHS } from '../utils/navigation';

/** Platform yöneticisi paneli: masaüstünde geniş, mobilde tek sütun */
export default function PlatformLayout() {
  const { user, logout } = useAuth();
  const navigate = useNavigate();

  const handleLogout = () => {
    logout();
    navigate(PATHS.platformLogin, { replace: true });
  };

  return (
    <div className="platform-layout">
      <header className="platform-header">
        <div className="platform-header-title">
          <span>☪️</span>
          <span>Hafız Takip · Platform</span>
        </div>
        <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
          <span style={{ fontSize: 13, opacity: 0.85 }}>{user?.email}</span>
          <button type="button" onClick={handleLogout}>Çıkış</button>
        </div>
      </header>
      <main className="platform-content">
        <Outlet />
      </main>
    </div>
  );
}
