import { useEffect, useState } from 'react';
import { getDashboardSummary } from '../../api/dashboard';
import { getStudents } from '../../api/students';
import { Logger } from '../../services/logger';
import { Icon, IconName } from '../../components/ui/Icon';
import type { IDashboardSummary } from '../../types/dashboard';
import type { IStudent } from '../../types/student';
import { useNavigate } from 'react-router-dom';

const log = Logger.create('DashboardPage');

export default function DashboardPage() {
  const navigate = useNavigate();
  const [summary, setSummary] = useState<IDashboardSummary | null>(null);
  const [students, setStudents] = useState<IStudent[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  useEffect(() => {
    async function fetchData() {
      try {
        log.debug('fetchData', 'Dashboard verileri çekiliyor...');
        const [summaryData, studentsData] = await Promise.all([
          getDashboardSummary(),
          getStudents(),
        ]);
        setSummary(summaryData);
        setStudents(Array.isArray(studentsData) ? studentsData : []);
        log.info('fetchData', 'Dashboard verileri başarıyla yüklendi');
      } catch (err) {
        log.error('fetchData', 'Veriler çekilirken hata oluştu', err);
        setError('Veriler yüklenemedi. Lütfen tekrar deneyin.');
      } finally {
        setLoading(false);
      }
    }
    fetchData();
  }, []);

  const todayStr = new Date().toLocaleDateString('tr-TR', {
    weekday: 'long',
    day: 'numeric',
    month: 'long',
    year: 'numeric',
  });

  if (loading) {
    return (
      <div style={{ padding: '14px 0' }}>
        <div className="skeleton" style={{ width: 120, height: 16, marginBottom: 8 }} />
        <div className="skeleton" style={{ width: 200, height: 24, marginBottom: 24 }} />
        <div className="stat-grid">
          <div className="skeleton" style={{ height: 70 }} />
          <div className="skeleton" style={{ height: 70 }} />
          <div className="skeleton" style={{ height: 70 }} />
          <div className="skeleton" style={{ height: 70 }} />
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div style={{ padding: 20, textAlign: 'center', color: 'var(--color-red)' }}>
        <Icon name="warn" size={40} className="margin-auto mb-10" />
        <p>{error}</p>
      </div>
    );
  }

  const statCards: { label: string; value: number | string; icon: IconName; color: string }[] = [
    { label: 'Toplam Öğrenci', value: summary?.totalStudents || 0, icon: 'users', color: 'var(--color-green)' },
    { label: 'Bugün İşlenen', value: summary?.todayLessonCount || 0, icon: 'book', color: 'var(--color-blue)' },
    {
      label: 'Devamsızlık Oranı',
      value: summary?.todayAttendanceRate != null ? `%${Math.round(100 - summary.todayAttendanceRate)}` : '%0',
      icon: 'warn',
      color: 'var(--color-red)',
    },
    {
      label: 'Ort. Sayfa',
      value: summary?.averageCompletedPages || 0,
      icon: 'repeat',
      color: 'var(--color-purple)',
    },
  ];

  return (
    <div style={{ paddingBottom: 100 }}>
      {/* Karşılama */}
      <div style={{ marginBottom: 16 }}>
        <p style={{ color: 'var(--color-muted)', fontSize: 12, margin: 0 }}>{todayStr}</p>
        <h2 style={{ color: 'var(--color-green)', margin: '2px 0 0', fontSize: 21, fontFamily: 'var(--font-display)' }}>
          Hafız Takip Paneli
        </h2>
      </div>

      {/* İstatistik Kartları */}
      <div className="stat-grid">
        {statCards.map((s, i) => (
          <div key={i} className="stat-card">
            <div className="stat-card-icon" style={{ background: `${s.color}18` }}>
              <Icon name={s.icon} size={18} color={s.color} />
            </div>
            <div>
              <div className="stat-card-value" style={{ color: s.color }}>{s.value}</div>
              <div className="stat-card-label">{s.label}</div>
            </div>
          </div>
        ))}
      </div>

      {/* Hızlı Erişim Butonları */}
      <div style={{ display: 'flex', gap: 8, marginBottom: 16 }}>
        <button
          onClick={() => navigate('/attendance')}
          style={{
            flex: 1, padding: '12px 0', borderRadius: 10, border: 'none',
            background: 'var(--color-green)', color: '#fff', fontWeight: 700, fontSize: 13,
            cursor: 'pointer', display: 'flex', alignItems: 'center', justifyContent: 'center', gap: 6,
          }}
        >
          <Icon name="check" size={16} /> Yoklama Al
        </button>
        <button
          onClick={() => navigate('/lessons/new')}
          style={{
            flex: 1, padding: '12px 0', borderRadius: 10, border: '2px solid var(--color-green)',
            background: 'transparent', color: 'var(--color-green)', fontWeight: 700, fontSize: 13,
            cursor: 'pointer', display: 'flex', alignItems: 'center', justifyContent: 'center', gap: 6,
          }}
        >
          <Icon name="plus" size={16} /> Ders Kaydet
        </button>
      </div>

      {/* Öğrenci Listesi Önizleme */}
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 8 }}>
        <h3 className="section-title" style={{ margin: 0 }}>Öğrenciler</h3>
        <button
          onClick={() => navigate('/students')}
          style={{ background: 'none', border: 'none', color: 'var(--color-green)', fontSize: 12, fontWeight: 700, cursor: 'pointer' }}
        >
          Tümünü Gör
        </button>
      </div>
      
      <div className="student-list">
        {(Array.isArray(students) ? students : []).slice(0, 5).map(student => {
          const pct = Math.round(((student.currentPage || 0) / 604) * 100);
          return (
            <div key={student.id} className="student-card" onClick={() => navigate(`/students/${student.id}`)}>
              <div className="student-avatar" style={{ width: 44, height: 44, background: 'var(--color-green)', fontSize: 14 }}>
                {student.firstName?.[0] || ''}{student.lastName?.[0] || ''}
              </div>
              <div className="student-info">
                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 2 }}>
                  <span className="student-name">{student.firstName} {student.lastName}</span>
                </div>
                <div className="student-meta">
                  Sayfa {student.currentPage} · Durum: {student.status}
                </div>
                <div className="progress-bar-track">
                  <div className="progress-bar-fill" style={{ width: `${pct}%` }} />
                </div>
                <div style={{ fontSize: 10, color: 'var(--color-muted)', marginTop: 3 }}>
                  % {pct} tamamlandı
                </div>
              </div>
            </div>
          );
        })}
        {students.length === 0 && (
          <div style={{ textAlign: 'center', padding: '20px 0', color: 'var(--color-muted)', fontSize: 14 }}>
            Henüz öğrenci kaydı bulunmuyor.
          </div>
        )}
      </div>
    </div>
  );
}
