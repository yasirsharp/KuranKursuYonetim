import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { getStudents } from '../../api/students';
import { createLesson } from '../../api/lessons';
import { Logger } from '../../services/logger';
import { Icon } from '../../components/ui/Icon';
import type { IStudent } from '../../types/student';
import { useAuth } from '../../context/AuthContext';

const log = Logger.create('LessonFormPage');

type LessonType = 'sabak' | 'sabki' | 'manzil';

export default function LessonFormPage() {
  const navigate = useNavigate();
  const { user } = useAuth();
  
  const [students, setStudents] = useState<IStudent[]>([]);
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');

  // Form State
  const [studentId, setStudentId] = useState('');
  const [lessonType, setLessonType] = useState<LessonType>('sabak');
  
  const [cuzNo, setCuzNo] = useState('');
  const [pageNo, setPageNo] = useState('');
  
  const [performance, setPerformance] = useState('iyi'); // pekiyi, iyi, hasne, zayif
  const [homeworkStatus, setHomeworkStatus] = useState('yapildi'); // yapildi, yapilmadi
  const [teacherNotes, setTeacherNotes] = useState('');

  useEffect(() => {
    async function loadData() {
      try {
        const data = await getStudents();
        setStudents(data);
        if (data.length > 0 && data[0]) setStudentId(data[0].id);
      } catch (err) {
        log.error('loadData', 'Öğrenciler yüklenemedi', err);
        setError('Öğrenciler yüklenemedi.');
      } finally {
        setLoading(false);
      }
    }
    loadData();
  }, []);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!studentId || !cuzNo || !pageNo) {
      setError('Lütfen zorunlu alanları (Öğrenci, Cüz No, Sayfa No) doldurun.');
      return;
    }
    
    setError('');
    setSubmitting(true);
    
    try {
      const isNewPage = lessonType === 'sabak';
      
      await createLesson({
        studentId,
        teacherId: user?.tenantId || '', // Normalde auth user id gelir
        lessonDate: new Date().toISOString(),
        cuzNo: parseInt(cuzNo, 10),
        pageNo: parseInt(pageNo, 10),
        isNewPage,
        performance,
        homeworkStatus,
        teacherNotes: teacherNotes.trim() || undefined,
      });
      
      log.info('handleSubmit', `${lessonType} dersi kaydedildi`);
      setSuccess('Ders başarıyla kaydedildi!');
      
      setTimeout(() => {
        navigate('/students/' + studentId);
      }, 1200);
      
    } catch (err) {
      log.error('handleSubmit', 'Ders kaydedilemedi', err);
      setError('Ders kaydedilirken bir hata oluştu.');
      setSubmitting(false);
    }
  };

  if (loading) {
    return (
      <div style={{ padding: '14px 0' }}>
        <div className="skeleton" style={{ width: '100%', height: 400, borderRadius: 16 }} />
      </div>
    );
  }

  const tabs: { key: LessonType; label: string; desc: string }[] = [
    { key: 'sabak', label: 'Sabak', desc: 'Yeni Ezber' },
    { key: 'sabki', label: 'Sabki', desc: 'Dünkü Ders' },
    { key: 'manzil', label: 'Manzil', desc: 'Eski Tekrar' },
  ];

  return (
    <div className="fade-in" style={{ paddingBottom: 100 }}>
      {/* Başlık */}
      <div style={{ display: 'flex', alignItems: 'center', gap: 12, marginBottom: 20 }}>
        <button 
          onClick={() => navigate(-1)}
          style={{ background: 'var(--color-card)', border: '1px solid var(--color-border)', borderRadius: '50%', width: 40, height: 40, display: 'flex', alignItems: 'center', justifyContent: 'center', cursor: 'pointer' }}
        >
          <Icon name="back" size={20} />
        </button>
        <h2 style={{ fontSize: 18, margin: 0, fontWeight: 700 }}>Ders Kaydı (Dinleme)</h2>
      </div>

      <form onSubmit={handleSubmit} style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
        {error && <div className="auth-error">{error}</div>}
        
        {success && (
          <div style={{ background: '#D1FAE5', color: '#065F46', padding: '12px 16px', borderRadius: 'var(--radius-sm)', fontSize: 13, fontWeight: 600, display: 'flex', alignItems: 'center', gap: 8 }}>
            <Icon name="check" size={16} />
            {success}
          </div>
        )}

        {/* Öğrenci Seçimi */}
        <div className="glass-card" style={{ padding: 16 }}>
          <label style={{ display: 'block', fontSize: 13, fontWeight: 700, marginBottom: 6, color: 'var(--color-muted)' }}>Öğrenci</label>
          <select 
            value={studentId} 
            onChange={e => setStudentId(e.target.value)}
            className="auth-input"
            style={{ textAlign: 'left', fontWeight: 600, letterSpacing: 'normal' }}
          >
            <option value="" disabled>Öğrenci Seçin</option>
            {students.map(s => (
              <option key={s.id} value={s.id}>{s.firstName} {s.lastName} (Sayfa {s.currentPage})</option>
            ))}
          </select>
        </div>

        {/* Ders Tipi (Sekmeler) */}
        <div style={{ display: 'flex', background: 'var(--color-card)', borderRadius: 12, padding: 4, border: '1px solid var(--color-border)' }}>
          {tabs.map(tab => (
            <button
              key={tab.key}
              type="button"
              onClick={() => setLessonType(tab.key)}
              style={{
                flex: 1, padding: '10px 0', borderRadius: 10, border: 'none',
                background: lessonType === tab.key ? 'var(--color-green)' : 'transparent',
                color: lessonType === tab.key ? '#fff' : 'var(--color-muted)',
                fontWeight: 700, cursor: 'pointer',
                transition: 'all 0.2s', display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 2,
              }}
            >
              <span style={{ fontSize: 14 }}>{tab.label}</span>
              <span style={{ fontSize: 10, opacity: lessonType === tab.key ? 0.9 : 0.6 }}>{tab.desc}</span>
            </button>
          ))}
        </div>

        <div className="glass-card fade-in" style={{ padding: 16, display: 'flex', flexDirection: 'column', gap: 16 }}>
          {/* Cüz ve Sayfa No */}
          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 12 }}>
            <div>
              <label style={{ display: 'block', fontSize: 13, fontWeight: 700, marginBottom: 6, color: 'var(--color-muted)' }}>Cüz No</label>
              <input 
                type="number" 
                min="1" max="30"
                value={cuzNo} 
                onChange={e => setCuzNo(e.target.value)}
                className="auth-input"
                style={{ textAlign: 'center', letterSpacing: 'normal' }}
                placeholder="1-30"
              />
            </div>
            <div>
              <label style={{ display: 'block', fontSize: 13, fontWeight: 700, marginBottom: 6, color: 'var(--color-muted)' }}>
                {lessonType === 'manzil' ? 'Sayfa Aralığı (örn: 1-10)' : 'Sayfa No'}
              </label>
              <input 
                type={lessonType === 'manzil' ? 'text' : 'number'} 
                min="1" max={lessonType === 'manzil' ? undefined : "20"}
                value={pageNo} 
                onChange={e => setPageNo(e.target.value)}
                className="auth-input"
                style={{ textAlign: 'center', letterSpacing: 'normal' }}
                placeholder={lessonType === 'manzil' ? "örn: 5" : "1-20"}
              />
              {lessonType === 'manzil' && (
                <div style={{ fontSize: 10, color: 'var(--color-muted)', marginTop: 4, textAlign: 'center' }}>
                  * Sadece başlangıç sayfasını girebilirsiniz.
                </div>
              )}
            </div>
          </div>

          {/* Performans */}
          <div>
            <label style={{ display: 'block', fontSize: 13, fontWeight: 700, marginBottom: 6, color: 'var(--color-muted)' }}>Performans</label>
            <div style={{ display: 'flex', gap: 8 }}>
              {[
                { val: 'pekiyi', label: 'Pekiyi', color: 'var(--color-green)' },
                { val: 'iyi', label: 'İyi', color: 'var(--color-blue)' },
                { val: 'hasne', label: 'Hasne', color: 'var(--color-gold)' },
                { val: 'zayif', label: 'Zayıf', color: 'var(--color-red)' }
              ].map(opt => (
                <button
                  key={opt.val}
                  type="button"
                  onClick={() => setPerformance(opt.val)}
                  style={{ 
                    flex: 1, 
                    padding: '8px 0', 
                    borderRadius: 8,
                    border: `2px solid ${performance === opt.val ? opt.color : 'var(--color-border)'}`,
                    background: performance === opt.val ? `${opt.color}15` : 'transparent',
                    color: performance === opt.val ? opt.color : 'var(--color-muted)',
                    fontWeight: 700,
                    fontSize: 12,
                    cursor: 'pointer'
                  }}
                >
                  {opt.label}
                </button>
              ))}
            </div>
          </div>
          
          {/* Ödev Durumu */}
          <div>
            <label style={{ display: 'block', fontSize: 13, fontWeight: 700, marginBottom: 6, color: 'var(--color-muted)' }}>Ödev / Hazırlık</label>
            <div style={{ display: 'flex', gap: 8 }}>
              {[
                { val: 'yapildi', label: 'Yapıldı', color: 'var(--color-green)' },
                { val: 'yapilmadi', label: 'Yapılmadı', color: 'var(--color-red)' },
              ].map(opt => (
                <button
                  key={opt.val}
                  type="button"
                  onClick={() => setHomeworkStatus(opt.val)}
                  style={{ 
                    flex: 1, 
                    padding: '8px 0', 
                    borderRadius: 8,
                    border: `2px solid ${homeworkStatus === opt.val ? opt.color : 'var(--color-border)'}`,
                    background: homeworkStatus === opt.val ? `${opt.color}15` : 'transparent',
                    color: homeworkStatus === opt.val ? opt.color : 'var(--color-muted)',
                    fontWeight: 700,
                    fontSize: 12,
                    cursor: 'pointer'
                  }}
                >
                  {opt.label}
                </button>
              ))}
            </div>
          </div>

          {/* Notlar */}
          <div>
            <label style={{ display: 'block', fontSize: 13, fontWeight: 700, marginBottom: 6, color: 'var(--color-muted)' }}>Öğretmen Notu (Opsiyonel)</label>
            <textarea 
              value={teacherNotes} 
              onChange={e => setTeacherNotes(e.target.value)}
              className="auth-input"
              style={{ textAlign: 'left', minHeight: 70, letterSpacing: 'normal', resize: 'vertical', fontSize: 14 }}
              placeholder="Eksikler veya öneriler..."
            />
          </div>
        </div>

        <button 
          type="submit" 
          disabled={submitting}
          className="auth-button auth-button-primary"
          style={{ marginTop: 8 }}
        >
          {submitting ? 'Kaydediliyor...' : 'Dersi Kaydet'}
        </button>
      </form>
    </div>
  );
}
