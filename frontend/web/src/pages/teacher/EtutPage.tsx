import { useEffect, useState } from 'react';
import { getEtutAssignments, completeEtutAssignment } from '../../api/etut';
import { Logger } from '../../services/logger';
import { AppCard } from '../../components/ui/AppCard';
import { Icon } from '../../components/ui/Icon';
import type { IDailyAssignment } from '../../types/etut';

const log = Logger.create('EtutPage');

export default function EtutPage() {
  const [assignments, setAssignments] = useState<IDailyAssignment[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  
  // Seçili (tamamlanan) görevlerin tutulduğu state
  // key: assignmentId, value: completedTaskIds[]
  const [completedTasks, setCompletedTasks] = useState<Record<string, string[]>>({});
  
  // Etüt notlarının tutulduğu state
  // key: assignmentId, value: etutTeacherNote
  const [etutNotes, setEtutNotes] = useState<Record<string, string>>({});
  
  // Kaydedilen ödevlerin statüsünü tutmak için
  const [savingState, setSavingState] = useState<Record<string, boolean>>({});
  const [savedState, setSavedState] = useState<Record<string, boolean>>({});

  useEffect(() => {
    loadAssignments();
  }, []);

  const loadAssignments = async () => {
    try {
      setLoading(true);
      setError('');
      
      const todayDate = new Date().toISOString().split('T')[0]!;
      const data = await getEtutAssignments(todayDate);
      
      // Tamamlanmamış veya etüt sırasında ilgilenilenleri listele
      const list = data.data || data.items || [];
      setAssignments(list);
      
      // Başlangıç statelerini doldur
      const initialCompleted: Record<string, string[]> = {};
      const initialNotes: Record<string, string> = {};
      
      list.forEach(a => {
        initialCompleted[a.id] = [...(a.completedInEtut || [])];
        initialNotes[a.id] = a.etutTeacherNote || '';
      });
      
      setCompletedTasks(initialCompleted);
      setEtutNotes(initialNotes);
      
      log.info('loadAssignments', `${list.length} etüt görevi yüklendi`);
    } catch (err) {
      log.error('loadAssignments', 'Etüt görevleri yüklenemedi', err);
      setError('Etüt görevleri alınırken bir hata oluştu.');
    } finally {
      setLoading(false);
    }
  };

  const handleToggleTask = (assignmentId: string, taskId: string) => {
    setCompletedTasks(prev => {
      const current = prev[assignmentId] || [];
      if (current.includes(taskId)) {
        return { ...prev, [assignmentId]: current.filter(id => id !== taskId) };
      } else {
        return { ...prev, [assignmentId]: [...current, taskId] };
      }
    });
  };

  const handleNoteChange = (assignmentId: string, note: string) => {
    setEtutNotes(prev => ({
      ...prev,
      [assignmentId]: note
    }));
  };

  const handleSave = async (assignment: IDailyAssignment) => {
    try {
      setSavingState(prev => ({ ...prev, [assignment.id]: true }));
      
      await completeEtutAssignment({
        id: assignment.id,
        completedTaskIds: completedTasks[assignment.id] || [],
        etutTeacherNote: etutNotes[assignment.id] || undefined,
        finishEtut: true // Etüt hocası işlemi bitirdi, kalanlar veliye düşer
      });
      
      setSavedState(prev => ({ ...prev, [assignment.id]: true }));
      log.info('handleSave', `${assignment.studentName} için etüt kaydedildi.`);
      
      // 2 saniye sonra başarılı statüsünü temizle
      setTimeout(() => {
        setSavedState(prev => ({ ...prev, [assignment.id]: false }));
      }, 2000);
      
    } catch (err) {
      log.error('handleSave', 'Etüt kaydedilemedi', err);
      alert('Etüt kaydedilirken bir hata oluştu.');
    } finally {
      setSavingState(prev => ({ ...prev, [assignment.id]: false }));
    }
  };

  if (loading) {
    return (
      <div style={{ padding: '14px 0' }}>
        <div className="skeleton" style={{ width: '60%', height: 24, marginBottom: 8 }} />
        <div className="skeleton" style={{ width: '40%', height: 16, marginBottom: 24 }} />
        {[1, 2, 3].map(i => (
          <div key={i} className="skeleton" style={{ height: 120, marginBottom: 12, borderRadius: 12 }} />
        ))}
      </div>
    );
  }

  const todayStr = new Date().toLocaleDateString('tr-TR', { weekday: 'long', day: 'numeric', month: 'long', year: 'numeric' });

  return (
    <div className="fade-in" style={{ padding: 20, paddingBottom: 100 }}>
      <h1 style={{ fontSize: 24, fontWeight: 700, marginBottom: 5 }}>Akşam Etüdü</h1>
      <p style={{ color: 'var(--color-muted)', marginBottom: 20, fontSize: 13 }}>
        {todayStr}
      </p>

      {error && (
        <div style={{ background: '#FEE2E2', color: '#B91C1C', padding: 15, borderRadius: 12, marginBottom: 20 }}>
          {error}
        </div>
      )}

      {assignments.length === 0 && !error ? (
        <div className="glass-card" style={{ padding: 30, textAlign: 'center', color: 'var(--color-muted)' }}>
          <Icon name="check" size={40} style={{ margin: '0 auto 10px', opacity: 0.5 }} />
          <h3 style={{ fontSize: 16, fontWeight: 700, marginBottom: 6, color: 'var(--color-text)' }}>Bugün İçin Etüt Yok</h3>
          <p style={{ fontSize: 13, margin: 0 }}>Şu ana kadar atanmış bir etüt görevi bulunmuyor.</p>
        </div>
      ) : (
        <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
          {assignments.map(assignment => {
            const isSaving = savingState[assignment.id];
            const isSaved = savedState[assignment.id];
            
            return (
              <AppCard key={assignment.id} style={{ padding: 0, overflow: 'hidden' }}>
                {/* Header */}
                <div style={{ background: 'var(--color-bg-inner)', padding: '12px 16px', display: 'flex', justifyContent: 'space-between', alignItems: 'center', borderBottom: '1px solid var(--color-border)' }}>
                  <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
                    <div style={{ width: 32, height: 32, borderRadius: '50%', background: 'var(--color-green)', color: '#fff', display: 'flex', alignItems: 'center', justifyContent: 'center', fontSize: 12, fontWeight: 700 }}>
                      {assignment.studentName.substring(0, 2).toUpperCase()}
                    </div>
                    <div>
                      <h3 style={{ fontSize: 15, fontWeight: 700, margin: 0, color: 'var(--color-text)' }}>{assignment.studentName}</h3>
                      {assignment.isBoarderToday && (
                        <span style={{ fontSize: 10, color: 'var(--color-gold)', fontWeight: 700 }}>Yatılı</span>
                      )}
                    </div>
                  </div>
                </div>

                {/* Tasks */}
                <div style={{ padding: '16px' }}>
                  <h4 style={{ fontSize: 12, fontWeight: 700, color: 'var(--color-muted)', textTransform: 'uppercase', marginBottom: 12, letterSpacing: 0.5 }}>Atanan Görevler</h4>
                  
                  {assignment.assignedTasks.length === 0 ? (
                    <div style={{ fontSize: 13, color: 'var(--color-muted)', fontStyle: 'italic' }}>Görev atanmamış.</div>
                  ) : (
                    <div style={{ display: 'flex', flexDirection: 'column', gap: 8 }}>
                      {assignment.assignedTasks.map(task => {
                        const isChecked = (completedTasks[assignment.id] || []).includes(task.id);
                        
                        let badgeColor = 'var(--color-muted)';
                        let badgeBg = 'var(--color-border)';
                        if (task.type === 'ezber') { badgeColor = 'var(--color-green)'; badgeBg = 'var(--color-green-pale)'; }
                        else if (task.type === 'has') { badgeColor = 'var(--color-blue)'; badgeBg = 'var(--color-blue-pale)'; }
                        else if (task.type === 'ders') { badgeColor = 'var(--color-purple)'; badgeBg = 'var(--color-purple-pale)'; }

                        return (
                          <div key={task.id} 
                            onClick={() => handleToggleTask(assignment.id, task.id)}
                            style={{ 
                              display: 'flex', alignItems: 'center', gap: 12, padding: '10px 12px', 
                              borderRadius: 10, border: `1px solid ${isChecked ? 'var(--color-green)' : 'var(--color-border)'}`,
                              background: isChecked ? 'var(--color-green-pale)' : 'transparent',
                              cursor: 'pointer', transition: 'all 0.2s'
                            }}
                          >
                            {/* Checkbox */}
                            <div style={{ 
                              width: 22, height: 22, borderRadius: 6, 
                              border: `2px solid ${isChecked ? 'var(--color-green)' : 'var(--color-muted)'}`,
                              background: isChecked ? 'var(--color-green)' : 'transparent',
                              display: 'flex', alignItems: 'center', justifyContent: 'center'
                            }}>
                              {isChecked && <Icon name="check" size={14} color="#fff" />}
                            </div>
                            
                            {/* Görev Bilgisi */}
                            <div style={{ flex: 1, display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
                              <span style={{ fontSize: 14, fontWeight: 600, color: 'var(--color-text)', textDecoration: isChecked ? 'line-through' : 'none', opacity: isChecked ? 0.6 : 1 }}>
                                Cüz {task.cuz}, Sayfa {task.page}
                              </span>
                              <span style={{ fontSize: 10, fontWeight: 700, padding: '2px 8px', borderRadius: 6, color: badgeColor, background: badgeBg, textTransform: 'capitalize' }}>
                                {task.type}
                              </span>
                            </div>
                          </div>
                        );
                      })}
                    </div>
                  )}

                  {/* Not Alanı */}
                  <div style={{ marginTop: 16 }}>
                    <label style={{ display: 'block', fontSize: 12, fontWeight: 700, marginBottom: 6, color: 'var(--color-muted)' }}>Etüt Notu (Opsiyonel)</label>
                    <textarea 
                      value={etutNotes[assignment.id] || ''}
                      onChange={e => handleNoteChange(assignment.id, e.target.value)}
                      placeholder="Veliye veya hocalara iletilecek notlar..."
                      style={{ 
                        width: '100%', padding: '10px 12px', borderRadius: 8, 
                        border: '1px solid var(--color-border)', fontSize: 13, 
                        resize: 'vertical', minHeight: 60, fontFamily: 'var(--font-body)',
                        outline: 'none', background: 'var(--color-bg)' 
                      }}
                    />
                  </div>

                  {/* Kaydet Butonu */}
                  <div style={{ marginTop: 12, display: 'flex', justifyContent: 'flex-end' }}>
                    <button
                      onClick={() => handleSave(assignment)}
                      disabled={isSaving}
                      style={{
                        padding: '10px 20px', borderRadius: 10, border: 'none',
                        background: isSaved ? '#10B981' : 'var(--color-green)', 
                        color: '#fff', fontWeight: 700, fontSize: 13, cursor: 'pointer',
                        display: 'flex', alignItems: 'center', gap: 6, transition: 'all 0.2s'
                      }}
                    >
                      {isSaving ? 'Kaydediliyor...' : isSaved ? <><Icon name="check" size={14} /> Kaydedildi</> : 'Etüdü Tamamla'}
                    </button>
                  </div>
                </div>
              </AppCard>
            );
          })}
        </div>
      )}
    </div>
  );
}
