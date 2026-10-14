import { useEffect, useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { getStudentById } from '../../api/students';
import { getStudentLessons } from '../../api/lessons';
import { getStudentAttendances } from '../../api/attendance';
import { Logger } from '../../services/logger';
import { Icon } from '../../components/ui/Icon';
import type { IStudent } from '../../types/student';
import type { ILesson } from '../../types/lesson';
import type { IAttendance } from '../../types/attendance';

const log = Logger.create('StudentDetailPage');

type TabKey = 'profile' | 'lessons' | 'attendance';

export default function StudentDetailPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [student, setStudent] = useState<IStudent | null>(null);
  const [lessons, setLessons] = useState<ILesson[]>([]);
  const [attendances, setAttendances] = useState<IAttendance[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [activeTab, setActiveTab] = useState<TabKey>('profile');
  const [lessonsLoading, setLessonsLoading] = useState(false);
  const [attendancesLoading, setAttendancesLoading] = useState(false);

  useEffect(() => {
    async function loadStudent() {
      if (!id) return;
      try {
        log.debug('loadStudent', `Öğrenci detayları yükleniyor: ${id}`);
        const data = await getStudentById(id);
        setStudent(data);
        log.info('loadStudent', `Öğrenci yüklendi: ${data.firstName} ${data.lastName}`);
      } catch (err) {
        log.error('loadStudent', 'Öğrenci yüklenemedi', err);
        setError('Öğrenci bilgileri alınamadı.');
      } finally {
        setLoading(false);
      }
    }
    loadStudent();
  }, [id]);

  // Ders geçmişini tab açılınca lazy-load et
  useEffect(() => {
    if (activeTab === 'lessons' && id && lessons.length === 0 && !lessonsLoading) {
      loadLessons(id);
    }
    if (activeTab === 'attendance' && id && attendances.length === 0 && !attendancesLoading) {
      loadAttendances(id);
    }
  }, [activeTab, id]);

  const loadLessons = async (studentId: string) => {
    try {
      setLessonsLoading(true);
      const data = await getStudentLessons(studentId);
      setLessons(data);
      log.info('loadLessons', `${data.length} ders yüklendi`);
    } catch (err) {
      log.error('loadLessons', 'Ders geçmişi yüklenemedi', err);
    } finally {
      setLessonsLoading(false);
    }
  };

  const loadAttendances = async (studentId: string) => {
    try {
      setAttendancesLoading(true);
      const result = await getStudentAttendances(studentId, 1, 30);
      const list = result.data || result.items || [];
      setAttendances(list);
      log.info('loadAttendances', `${list.length} devamsızlık kaydı yüklendi`);
    } catch (err) {
      log.error('loadAttendances', 'Devamsızlık kayıtları yüklenemedi', err);
    } finally {
      setAttendancesLoading(false);
    }
  };

  if (loading) {
    return (
      <div style={{ padding: '14px 0' }}>
        <div className="skeleton" style={{ width: '100%', height: 200, borderRadius: 16 }} />
      </div>
    );
  }

  if (error || !student) {
    return (
      <div style={{ padding: 20, textAlign: 'center', color: 'var(--color-red)' }}>
        <Icon name="warn" size={40} className="margin-auto mb-10" />
        <p>{error || 'Öğrenci bulunamadı'}</p>
        <button onClick={() => navigate(-1)} style={{ marginTop: 10, background: 'var(--color-border)', border: 'none', padding: '8px 16px', borderRadius: 8, cursor: 'pointer' }}>Geri Dön</button>
      </div>
    );
  }

  const pct = Math.round((student.currentPage / 604) * 100);

  const tabs: { key: TabKey; label: string; icon: string }[] = [
    { key: 'profile', label: 'Profil', icon: '👤' },
    { key: 'lessons', label: 'Dersler', icon: '📖' },
    { key: 'attendance', label: 'Devamsızlık', icon: '📅' },
  ];

  return (
    <div style={{ paddingBottom: 100 }}>
      {/* Üst Kısım */}
      <div style={{ display: 'flex', alignItems: 'center', gap: 12, marginBottom: 20 }}>
        <button
          onClick={() => navigate(-1)}
          style={{ background: 'var(--color-card)', border: '1px solid var(--color-border)', borderRadius: '50%', width: 40, height: 40, display: 'flex', alignItems: 'center', justifyContent: 'center', cursor: 'pointer' }}
        >
          <Icon name="back" size={20} color="var(--color-text)" />
        </button>
        <h2 style={{ fontSize: 18, margin: 0, fontWeight: 700 }}>Öğrenci Detayı</h2>
      </div>

      {/* Avatar ve Bilgiler */}
      <div className="glass-card" style={{ padding: 20, textAlign: 'center', marginBottom: 16 }}>
        <div style={{ width: 80, height: 80, borderRadius: '50%', background: 'var(--color-green)', color: '#fff', fontSize: 32, fontWeight: 700, display: 'flex', alignItems: 'center', justifyContent: 'center', margin: '0 auto 12px' }}>
          {student.firstName[0]}{student.lastName[0]}
        </div>
        <h3 style={{ margin: '0 0 4px', fontSize: 22, fontFamily: 'var(--font-display)', color: 'var(--color-green)' }}>
          {student.firstName} {student.lastName}
        </h3>
        <p style={{ margin: 0, color: 'var(--color-muted)', fontSize: 14 }}>TC: {student.identificationNumber}</p>

        <div style={{ display: 'flex', gap: 10, justifyContent: 'center', marginTop: 16 }}>
          <div style={{ background: 'var(--color-bg-outer)', padding: '8px 16px', borderRadius: 12 }}>
            <div style={{ fontSize: 11, color: 'var(--color-muted)' }}>Sayfa</div>
            <div style={{ fontWeight: 800, color: 'var(--color-text)' }}>{student.currentPage}</div>
          </div>
          <div style={{ background: 'var(--color-bg-outer)', padding: '8px 16px', borderRadius: 12 }}>
            <div style={{ fontSize: 11, color: 'var(--color-muted)' }}>Durum</div>
            <div style={{ fontWeight: 800, color: 'var(--color-green)' }}>{student.status}</div>
          </div>
        </div>

        <div style={{ marginTop: 20 }}>
          <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: 12, marginBottom: 6 }}>
            <span style={{ fontWeight: 600 }}>Hafızlık İlerlemesi</span>
            <span style={{ color: 'var(--color-green)', fontWeight: 700 }}>% {pct}</span>
          </div>
          <div className="progress-bar-track">
            <div className="progress-bar-fill" style={{ width: `${pct}%` }} />
          </div>
        </div>

        {/* Aksiyon Butonları */}
        <div style={{ display: 'flex', gap: 10, marginTop: 16 }}>
          <button
            onClick={() => navigate(`/students/${student.id}/edit`)}
            style={{
              flex: 1, padding: '10px 0', borderRadius: 10, border: '2px solid var(--color-green)',
              background: 'transparent', color: 'var(--color-green)', fontWeight: 700, fontSize: 13,
              cursor: 'pointer', display: 'flex', alignItems: 'center', justifyContent: 'center', gap: 6,
              transition: 'all 0.2s',
            }}
          >
            <Icon name="more" size={14} /> Düzenle
          </button>
          <button
            onClick={() => navigate('/lessons/new')}
            style={{
              flex: 1, padding: '10px 0', borderRadius: 10, border: 'none', background: 'var(--color-green)',
              color: '#fff', fontWeight: 700, fontSize: 13, cursor: 'pointer', display: 'flex',
              alignItems: 'center', justifyContent: 'center', gap: 6, transition: 'all 0.2s',
            }}
          >
            <Icon name="plus" size={14} /> Ders Ekle
          </button>
        </div>
      </div>

      {/* Sekmeler */}
      <div style={{ display: 'flex', background: 'var(--color-card)', borderRadius: 12, padding: 4, marginBottom: 16, border: '1px solid var(--color-border)' }}>
        {tabs.map(tab => (
          <button
            key={tab.key}
            onClick={() => setActiveTab(tab.key)}
            style={{
              flex: 1, padding: '10px 0', borderRadius: 10, border: 'none',
              background: activeTab === tab.key ? 'var(--color-green)' : 'transparent',
              color: activeTab === tab.key ? '#fff' : 'var(--color-muted)',
              fontWeight: 700, fontSize: 12, cursor: 'pointer',
              transition: 'all 0.2s', display: 'flex', alignItems: 'center', justifyContent: 'center', gap: 4,
            }}
          >
            <span>{tab.icon}</span> {tab.label}
          </button>
        ))}
      </div>

      {/* Sekme İçerikleri */}
      {activeTab === 'profile' && (
        <div className="fade-in">
          <div className="glass-card" style={{ padding: 16 }}>
            <h4 style={{ fontSize: 14, fontWeight: 700, color: 'var(--color-green)', margin: '0 0 12px' }}>Bilgiler</h4>
            <div style={{ display: 'flex', flexDirection: 'column', gap: 10 }}>
              {[
                { label: 'Ad Soyad', value: `${student.firstName} ${student.lastName}` },
                { label: 'TC Kimlik No', value: student.identificationNumber || '—' },
                { label: 'Mevcut Sayfa', value: `${student.currentPage} / 604` },
                { label: 'Tamamlanan Sayfa', value: String(student.completedPages || 0) },
                { label: 'Durum', value: student.status },
              ].map((item, i) => (
                <div key={i} style={{ display: 'flex', justifyContent: 'space-between', padding: '6px 0', borderBottom: '1px solid var(--color-border)' }}>
                  <span style={{ fontSize: 13, color: 'var(--color-muted)' }}>{item.label}</span>
                  <span style={{ fontSize: 13, fontWeight: 700 }}>{item.value}</span>
                </div>
              ))}
            </div>
          </div>
        </div>
      )}

      {activeTab === 'lessons' && (
        <div className="fade-in">
          {lessonsLoading ? (
            <div style={{ display: 'flex', flexDirection: 'column', gap: 8 }}>
              {[1, 2, 3].map(i => (
                <div key={i} className="skeleton" style={{ height: 72, borderRadius: 12 }} />
              ))}
            </div>
          ) : lessons.length === 0 ? (
            <div className="glass-card" style={{ padding: 20, textAlign: 'center', color: 'var(--color-muted)' }}>
              <Icon name="book" size={32} style={{ margin: '0 auto 10px', opacity: 0.5 }} />
              <p style={{ margin: 0, fontSize: 14 }}>Henüz ders kaydı bulunmuyor.</p>
            </div>
          ) : (
            <div style={{ display: 'flex', flexDirection: 'column', gap: 8 }}>
              {lessons.map(lesson => {
                const date = new Date(lesson.lessonDate).toLocaleDateString('tr-TR', { day: 'numeric', month: 'short' });
                // Backend PerformanceLevel kodları
                const performanceStyles: Record<string, { label: string; color: string }> = {
                  mukemmel: { label: 'Mükemmel', color: 'var(--color-green)' },
                  cok_iyi: { label: 'Çok İyi', color: 'var(--color-blue)' },
                  iyi: { label: 'İyi', color: 'var(--color-purple)' },
                  orta: { label: 'Orta', color: 'var(--color-gold)' },
                  zayif: { label: 'Zayıf', color: 'var(--color-red)' },
                };
                const performance = performanceStyles[lesson.performance];
                return (
                  <div key={lesson.id} className="glass-card" style={{ padding: 14, display: 'flex', alignItems: 'center', gap: 12 }}>
                    <div style={{
                      width: 44, height: 44, borderRadius: 10, background: 'var(--color-green-pale)',
                      display: 'flex', alignItems: 'center', justifyContent: 'center', flexShrink: 0,
                    }}>
                      <Icon name="book" size={20} color="var(--color-green)" />
                    </div>
                    <div style={{ flex: 1 }}>
                      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                        <span style={{ fontWeight: 700, fontSize: 14 }}>Cüz {lesson.cuzNo} · Sayfa {lesson.pageNo}</span>
                        <span style={{ fontSize: 11, color: 'var(--color-muted)' }}>{date}</span>
                      </div>
                      <div style={{ display: 'flex', gap: 8, marginTop: 4 }}>
                        {lesson.isNewPage && (
                          <span style={{ fontSize: 10, background: 'var(--color-green-pale)', color: 'var(--color-green)', padding: '2px 8px', borderRadius: 6, fontWeight: 700 }}>
                            Yeni Sayfa
                          </span>
                        )}
                        <span style={{
                          fontSize: 10, padding: '2px 8px', borderRadius: 6, fontWeight: 700,
                          color: performance?.color || 'var(--color-muted)',
                          border: `1px solid ${performance?.color || 'var(--color-border)'}`,
                        }}>
                          {performance?.label || lesson.performance}
                        </span>
                      </div>
                    </div>
                  </div>
                );
              })}
            </div>
          )}
        </div>
      )}

      {activeTab === 'attendance' && (
        <div className="fade-in">
          {attendancesLoading ? (
            <div style={{ display: 'flex', flexDirection: 'column', gap: 8 }}>
              {[1, 2, 3].map(i => (
                <div key={i} className="skeleton" style={{ height: 50, borderRadius: 12 }} />
              ))}
            </div>
          ) : attendances.length === 0 ? (
            <div className="glass-card" style={{ padding: 20, textAlign: 'center', color: 'var(--color-muted)' }}>
              <Icon name="check" size={32} style={{ margin: '0 auto 10px', opacity: 0.5 }} />
              <p style={{ margin: 0, fontSize: 14 }}>Henüz devamsızlık kaydı bulunmuyor.</p>
            </div>
          ) : (
            <div style={{ display: 'flex', flexDirection: 'column', gap: 6 }}>
              {attendances.map(att => {
                const date = new Date(att.date).toLocaleDateString('tr-TR', { day: 'numeric', month: 'short', weekday: 'short' });
                const statusMap: Record<string, { label: string; color: string; bg: string }> = {
                  present: { label: 'Geldi', color: '#10B981', bg: '#D1FAE5' },
                  absent: { label: 'Yok', color: '#EF4444', bg: '#FEE2E2' },
                  excused: { label: 'İzinli', color: '#F59E0B', bg: '#FFF8E0' },
                };
                const s = statusMap[att.status] || statusMap['present']!;
                return (
                  <div key={att.id} className="glass-card" style={{ padding: '10px 14px', display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                    <span style={{ fontSize: 13, fontWeight: 600 }}>{date}</span>
                    <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
                      {att.notes && (
                        <span style={{ fontSize: 11, color: 'var(--color-muted)', maxWidth: 120, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
                          {att.notes}
                        </span>
                      )}
                      <span style={{
                        fontSize: 11, fontWeight: 700, padding: '3px 10px', borderRadius: 8,
                        color: s.color, background: s.bg,
                      }}>
                        {s.label}
                      </span>
                    </div>
                  </div>
                );
              })}
            </div>
          )}
        </div>
      )}
    </div>
  );
}
