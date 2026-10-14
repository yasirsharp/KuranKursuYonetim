import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { getStudents } from '../../api/students';
import { Logger } from '../../services/logger';
import { Icon } from '../../components/ui/Icon';
import type { IStudent } from '../../types/student';

const log = Logger.create('StudentListPage');

export default function StudentListPage() {
  const navigate = useNavigate();
  const [students, setStudents] = useState<IStudent[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [search, setSearch] = useState('');

  useEffect(() => {
    async function loadStudents() {
      try {
        log.debug('loadStudents', 'Öğrenciler yükleniyor...');
        const data = await getStudents();
        setStudents(data);
        log.info('loadStudents', `${data.length} öğrenci yüklendi`);
      } catch (err) {
        log.error('loadStudents', 'Öğrenciler yüklenemedi', err);
        setError('Öğrenciler yüklenirken bir hata oluştu.');
      } finally {
        setLoading(false);
      }
    }
    loadStudents();
  }, []);

  const filtered = students.filter(s => 
    `${s.firstName} ${s.lastName}`.toLowerCase().includes(search.toLowerCase())
  );

  return (
    <div style={{ paddingBottom: 100 }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 16 }}>
        <h2 className="section-title" style={{ fontSize: 20, margin: 0 }}>Öğrenciler</h2>
        <button 
          onClick={() => navigate('/students/new')}
          style={{ background: 'var(--color-green)', color: '#fff', border: 'none', borderRadius: 8, padding: '8px 12px', display: 'flex', alignItems: 'center', gap: 6, cursor: 'pointer', fontWeight: 600 }}
        >
          <Icon name="plus" size={16} /> Yeni Kayıt
        </button>
      </div>

      <div style={{ marginBottom: 16 }}>
        <input 
          type="text" 
          placeholder="Öğrenci ara..." 
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          style={{ width: '100%', padding: '12px 16px', borderRadius: 12, border: '1px solid var(--color-border)', outline: 'none' }}
        />
      </div>

      {loading ? (
        <div className="student-list">
          {[1, 2, 3, 4].map(i => (
            <div key={i} className="student-card skeleton" style={{ height: 72 }} />
          ))}
        </div>
      ) : error ? (
        <div style={{ padding: 20, textAlign: 'center', color: 'var(--color-red)' }}>
          {error}
        </div>
      ) : (
        <div className="student-list">
          {filtered.map(student => {
            const pct = Math.round((student.currentPage / 604) * 100);
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
                </div>
              </div>
            );
          })}
          {filtered.length === 0 && (
            <div style={{ textAlign: 'center', padding: '20px 0', color: 'var(--color-muted)', fontSize: 14 }}>
              Öğrenci bulunamadı.
            </div>
          )}
        </div>
      )}
    </div>
  );
}
