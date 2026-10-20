import { useNavigate } from 'react-router-dom';
import { useAuth } from '../../context/AuthContext';
import { Icon } from '../../components/ui/Icon';
import { PATHS } from '../../utils/navigation';

const ROLE_LABELS: Record<string, string> = {
  tenant_admin: 'Kurs müdürü',
  head_teacher: 'Başhoca',
  assistant_teacher: 'Yardımcı hoca',
  parent: 'Veli',
};

export default function MorePage() {
  const navigate = useNavigate();
  const { user, logout } = useAuth();
  const isStaff = user?.role === 'tenant_admin' || user?.role === 'head_teacher' || user?.role === 'assistant_teacher';

  const handleLogout = () => {
    logout();
    navigate(PATHS.login, { replace: true });
  };

  return (
    <div className="form-stack fade-in" style={{ paddingBottom: 90 }}>
      <div className="info-card" style={{ flexDirection: 'row', alignItems: 'center', gap: 12 }}>
        <div className="student-avatar">{user?.firstName?.charAt(0)}</div>
        <div>
          <div className="info-card-title">{user?.firstName} {user?.lastName}</div>
          <div style={{ fontSize: 13, color: 'var(--color-muted)' }}>{ROLE_LABELS[user?.role ?? ''] ?? user?.role}</div>
        </div>
      </div>

      <nav className="menu-list">
        {isStaff && (
          <button className="menu-item" onClick={() => navigate(PATHS.settings)}>
            <Icon name="cal" size={20} color="var(--color-green)" />
            Kurs Ayarları
            <span className="menu-item-hint">{user?.role === 'tenant_admin' ? 'Düzenle' : 'Görüntüle'}</span>
          </button>
        )}
        {isStaff && (
          <button className="menu-item" onClick={() => navigate(PATHS.calendar)}>
            <Icon name="cal" size={20} color="var(--color-green)" />
            Dönem ve Takvim
            <span className="menu-item-hint">Tatiller, ders günleri</span>
          </button>
        )}
        <button className="menu-item menu-item-danger" onClick={handleLogout}>
          <Icon name="back" size={20} color="var(--color-red)" />
          Çıkış Yap
        </button>
      </nav>
    </div>
  );
}
