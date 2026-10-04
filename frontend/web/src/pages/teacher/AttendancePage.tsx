import { useEffect, useState } from 'react';
import { getStudents } from '../../api/students';
import { createBulkAttendance } from '../../api/attendance';
import { Logger } from '../../services/logger';
import { AppCard } from '../../components/ui/AppCard';
import { AppButton } from '../../components/ui/AppButton';
import { Icon } from '../../components/ui/Icon';
import type { IStudent } from '../../types/student';
import type { ICreateAttendanceItem } from '../../types/attendance';

const log = Logger.create('AttendancePage');

export default function AttendancePage() {
  const [students, setStudents] = useState<IStudent[]>([]);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');

  // Yoklama durumu: studentId -> status
  const [attendanceState, setAttendanceState] = useState<Record<string, string>>({});
  // Mazeret notları: studentId -> note
  const [notesState, setNotesState] = useState<Record<string, string>>({});
  // Açık not alanları: studentId -> boolean
  const [openNotes, setOpenNotes] = useState<Record<string, boolean>>({});

  useEffect(() => {
    loadStudents();
  }, []);

  const loadStudents = async () => {
    try {
      setLoading(true);
      setError('');
      const data = await getStudents();
      setStudents(data);

      // Default: herkes 'present'
      const initialState: Record<string, string> = {};
      data.forEach(s => {
        initialState[s.id] = 'present';
      });
      setAttendanceState(initialState);
    } catch (err) {
      log.error('loadStudents', 'Öğrenciler yüklenemedi', err);
      setError('Öğrenci listesi alınırken bir hata oluştu.');
    } finally {
      setLoading(false);
    }
  };

  const handleStatusChange = (studentId: string, status: string) => {
    setAttendanceState(prev => ({
      ...prev,
      [studentId]: status
    }));

    // Eğer excused (izinli) seçildiyse not alanını otomatik aç
    if (status === 'excused') {
      setOpenNotes(prev => ({ ...prev, [studentId]: true }));
    }
  };

  const handleNoteChange = (studentId: string, note: string) => {
    setNotesState(prev => ({
      ...prev,
      [studentId]: note
    }));
  };

  const toggleNote = (studentId: string) => {
    setOpenNotes(prev => ({
      ...prev,
      [studentId]: !prev[studentId]
    }));
  };

  const handleSave = async () => {
    try {
      setSaving(true);
      setError('');
      setSuccess('');

      if (students.length === 0) return;

      const groupId = students[0]?.groupId || '';

      const attendances: ICreateAttendanceItem[] = students.map(s => ({
        studentId: s.id,
        status: attendanceState[s.id] || 'present',
        notes: notesState[s.id] || ''
      }));

      const todayDate = new Date().toISOString().split('T')[0];

      await createBulkAttendance({
        date: todayDate as string,
        groupId: groupId,
        attendances: attendances
      });

      log.info('handleSave', 'Toplu yoklama başarıyla kaydedildi.');
      setSuccess('Yoklama başarıyla kaydedildi!');
      setTimeout(() => setSuccess(''), 3000);
    } catch (err) {
      log.error('handleSave', 'Yoklama kaydedilemedi', err);
      setError('Yoklama kaydedilirken bir hata oluştu. Daha önce bugün için yoklama girilmiş olabilir.');
    } finally {
      setSaving(false);
    }
  };

  // Toplu işlem butonları
  const handleMarkAll = (status: string) => {
    const newState: Record<string, string> = {};
    students.forEach(s => {
      newState[s.id] = status;
    });
    setAttendanceState(newState);
  };

  // İstatistikler
  const presentCount = Object.values(attendanceState).filter(s => s === 'present').length;
  const absentCount = Object.values(attendanceState).filter(s => s === 'absent').length;
  const excusedCount = Object.values(attendanceState).filter(s => s === 'excused').length;

  if (loading) {
    return (
      <div style={{ padding: '14px 0' }}>
        <div className="skeleton" style={{ width: '60%', height: 24, marginBottom: 8 }} />
        <div className="skeleton" style={{ width: '40%', height: 16, marginBottom: 24 }} />
        {[1, 2, 3, 4].map(i => (
          <div key={i} className="skeleton" style={{ height: 72, marginBottom: 10, borderRadius: 12 }} />
        ))}
      </div>
    );
  }

  return (
    <div className="fade-in" style={{ padding: 20, paddingBottom: 100 }}>
      <h1 style={{ fontSize: 24, fontWeight: 700, marginBottom: 5 }}>Toplu Yoklama</h1>
      <p style={{ color: 'var(--color-muted)', marginBottom: 12, fontSize: 13 }}>
        {new Date().toLocaleDateString('tr-TR', { weekday: 'long', day: 'numeric', month: 'long', year: 'numeric' })}
      </p>

      {/* Özet İstatistik */}
      <div style={{ display: 'flex', gap: 8, marginBottom: 16 }}>
        <div style={{ flex: 1, background: 'var(--color-green-pale)', borderRadius: 10, padding: '8px 12px', textAlign: 'center' }}>
          <div style={{ fontSize: 18, fontWeight: 800, color: 'var(--color-green)' }}>{presentCount}</div>
          <div style={{ fontSize: 10, color: 'var(--color-green)', fontWeight: 600 }}>Geldi</div>
        </div>
        <div style={{ flex: 1, background: 'var(--color-red-pale)', borderRadius: 10, padding: '8px 12px', textAlign: 'center' }}>
          <div style={{ fontSize: 18, fontWeight: 800, color: 'var(--color-red)' }}>{absentCount}</div>
          <div style={{ fontSize: 10, color: 'var(--color-red)', fontWeight: 600 }}>Yok</div>
        </div>
        <div style={{ flex: 1, background: 'var(--color-gold-pale)', borderRadius: 10, padding: '8px 12px', textAlign: 'center' }}>
          <div style={{ fontSize: 18, fontWeight: 800, color: 'var(--color-gold)' }}>{excusedCount}</div>
          <div style={{ fontSize: 10, color: 'var(--color-gold)', fontWeight: 600 }}>İzinli</div>
        </div>
      </div>

      {/* Toplu İşlem Butonları */}
      <div style={{ display: 'flex', gap: 8, marginBottom: 20 }}>
        <button
          onClick={() => handleMarkAll('present')}
          style={{
            flex: 1, padding: '8px 0', borderRadius: 8, border: '1px solid var(--color-border)',
            background: 'var(--color-card)', color: 'var(--color-green)', fontWeight: 700, fontSize: 12,
            cursor: 'pointer', transition: 'all 0.2s',
          }}
        >
          Hepsini Geldi
        </button>
        <button
          onClick={() => handleMarkAll('absent')}
          style={{
            flex: 1, padding: '8px 0', borderRadius: 8, border: '1px solid var(--color-border)',
            background: 'var(--color-card)', color: 'var(--color-red)', fontWeight: 700, fontSize: 12,
            cursor: 'pointer', transition: 'all 0.2s',
          }}
        >
          Hepsini Yok
        </button>
      </div>

      {error && (
        <div style={{ background: '#FEE2E2', color: '#B91C1C', padding: 15, borderRadius: 12, marginBottom: 20 }}>
          {error}
        </div>
      )}

      {success && (
        <div style={{ background: '#D1FAE5', color: '#065F46', padding: 15, borderRadius: 12, marginBottom: 20, display: 'flex', alignItems: 'center', gap: 10 }}>
          <Icon name="check" size={20} />
          {success}
        </div>
      )}

      <div style={{ display: 'flex', flexDirection: 'column', gap: 10, marginBottom: 30 }}>
        {students.map((student) => (
          <AppCard key={student.id} style={{ padding: 14 }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
              <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
                <div
                  style={{
                    width: 36, height: 36, borderRadius: '50%', background: 'var(--color-green)',
                    color: '#fff', display: 'flex', alignItems: 'center', justifyContent: 'center',
                    fontSize: 13, fontWeight: 700, flexShrink: 0,
                  }}
                >
                  {student.firstName[0]}{student.lastName[0]}
                </div>
                <h3 style={{ fontWeight: 600, fontSize: 14, margin: 0 }}>{student.firstName} {student.lastName}</h3>
              </div>

              <div style={{ display: 'flex', gap: 6 }}>
                {[
                  { val: 'present', label: 'Geldi', activeColor: '#10B981' },
                  { val: 'absent', label: 'Yok', activeColor: '#EF4444' },
                  { val: 'excused', label: 'İzinli', activeColor: '#F59E0B' },
                ].map(opt => (
                  <button
                    key={opt.val}
                    onClick={() => handleStatusChange(student.id, opt.val)}
                    style={{
                      background: attendanceState[student.id] === opt.val ? opt.activeColor : 'transparent',
                      color: attendanceState[student.id] === opt.val ? 'white' : opt.activeColor,
                      border: `1px solid ${opt.activeColor}`,
                      padding: '6px 10px',
                      borderRadius: 8,
                      fontWeight: 600,
                      fontSize: 12,
                      cursor: 'pointer',
                      transition: 'all 0.2s',
                    }}
                  >
                    {opt.label}
                  </button>
                ))}
              </div>
            </div>

            {/* Mazeret Notu Alanı */}
            {(attendanceState[student.id] === 'excused' || attendanceState[student.id] === 'absent') && (
              <div style={{ marginTop: 10 }}>
                {!openNotes[student.id] ? (
                  <button
                    onClick={() => toggleNote(student.id)}
                    style={{
                      background: 'none', border: 'none', color: 'var(--color-muted)',
                      fontSize: 12, cursor: 'pointer', fontWeight: 600, padding: 0,
                      textDecoration: 'underline',
                    }}
                  >
                    + Not Ekle
                  </button>
                ) : (
                  <textarea
                    value={notesState[student.id] || ''}
                    onChange={(e) => handleNoteChange(student.id, e.target.value)}
                    placeholder={attendanceState[student.id] === 'excused' ? 'Mazeret sebebini yazın...' : 'Not ekle (opsiyonel)...'}
                    style={{
                      width: '100%', padding: '8px 12px', borderRadius: 8,
                      border: '1px solid var(--color-border)', fontSize: 13,
                      resize: 'vertical', minHeight: 50, fontFamily: 'var(--font-body)',
                      outline: 'none', background: 'var(--color-bg)',
                    }}
                  />
                )}
              </div>
            )}
          </AppCard>
        ))}

        {students.length === 0 && (
          <div style={{ textAlign: 'center', padding: 30, color: 'var(--color-muted)' }}>
            Öğrenci bulunamadı.
          </div>
        )}
      </div>

      <div style={{ position: 'fixed', bottom: 80, left: 20, right: 20, zIndex: 10 }}>
        <AppButton
          variant="primary"
          fullWidth
          onClick={handleSave}
          disabled={saving || students.length === 0}
        >
          {saving ? 'Kaydediliyor...' : `Yoklamayı Kaydet (${students.length} Öğrenci)`}
        </AppButton>
      </div>
    </div>
  );
}
