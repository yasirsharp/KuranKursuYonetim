import { Outlet, useNavigate, useLocation } from 'react-router-dom';
import { Icon, IconName } from '../components/ui/Icon';

export default function TeacherLayout() {
  const navigate = useNavigate();
  const location = useLocation();

  const today = new Date().toLocaleDateString('tr-TR', { day: 'numeric', month: 'short' });

  const tabs: { path: string; label: string; icon: IconName }[] = [
    { path: '/', label: 'Ana Sayfa', icon: 'home' },
    { path: '/students', label: 'Öğrenciler', icon: 'book' },
    { path: '/lessons/new', label: 'Ders Kayıt', icon: 'plus' },
    { path: '/attendance', label: 'Yoklama', icon: 'check' },
    { path: '/etut', label: 'Etüt', icon: 'repeat' },
  ];

  return (
    <div className="teacher-layout">
      {/* BAŞLIK ÇUBUĞU */}
      <div className="teacher-header">
        <div className="teacher-header-title">
          <span>☪️</span>
          <span>Hafız Takip</span>
        </div>
        <div className="teacher-header-date">
          <span>{today}</span>
        </div>
      </div>

      {/* İÇERİK */}
      <div className="teacher-content">
        <Outlet />
      </div>

      {/* ALT NAVİGASYON */}
      <div className="bottom-nav">
        {tabs.map((tab) => {
          const isActive = location.pathname === tab.path || (tab.path !== '/' && location.pathname.startsWith(tab.path));
          return (
            <button
              key={tab.path}
              className={`bottom-nav-item ${isActive ? 'active' : ''}`}
              onClick={() => navigate(tab.path)}
            >
              <Icon name={tab.icon} size={22} color={isActive ? 'currentColor' : 'var(--color-muted)'} />
              <span className="bottom-nav-item-label">{tab.label}</span>
            </button>
          );
        })}
      </div>
    </div>
  );
}
