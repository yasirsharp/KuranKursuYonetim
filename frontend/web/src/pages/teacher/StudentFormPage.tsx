import { useState, useEffect } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { createStudent, updateStudent, getStudentById } from '../../api/students';
import { Logger } from '../../services/logger';
import { Icon } from '../../components/ui/Icon';

const log = Logger.create('StudentFormPage');

export default function StudentFormPage() {
  const navigate = useNavigate();
  const { id } = useParams<{ id: string }>();
  const isEdit = Boolean(id);

  const [loading, setLoading] = useState(isEdit);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');

  // Form state
  const [firstName, setFirstName] = useState('');
  const [lastName, setLastName] = useState('');
  const [identificationNumber, setIdentificationNumber] = useState('');
  const [groupId, setGroupId] = useState('');
  const [isBoarder, setIsBoarder] = useState(false);
  const [status, setStatus] = useState('active');

  useEffect(() => {
    if (isEdit && id) {
      loadStudent(id);
    }
  }, [isEdit, id]);

  const loadStudent = async (studentId: string) => {
    try {
      setLoading(true);
      const student = await getStudentById(studentId);
      setFirstName(student.firstName);
      setLastName(student.lastName);
      setIdentificationNumber(student.identificationNumber);
      setGroupId(student.groupId || '');
      setStatus(student.status);
      log.info('loadStudent', `Öğrenci yüklendi: ${student.firstName} ${student.lastName}`);
    } catch (err) {
      log.error('loadStudent', 'Öğrenci yüklenemedi', err);
      setError('Öğrenci bilgileri yüklenemedi.');
    } finally {
      setLoading(false);
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!firstName.trim() || !lastName.trim()) {
      setError('Ad ve soyad alanları zorunludur.');
      return;
    }

    setError('');
    setSuccess('');
    setSubmitting(true);

    try {
      if (isEdit && id) {
        await updateStudent({
          id,
          firstName: firstName.trim(),
          lastName: lastName.trim(),
          identificationNumber: identificationNumber.trim(),
          groupId: groupId || undefined,
          status,
        });
        log.info('handleSubmit', 'Öğrenci güncellendi');
        setSuccess('Öğrenci başarıyla güncellendi!');
        setTimeout(() => navigate(`/students/${id}`), 1200);
      } else {
        await createStudent({
          firstName: firstName.trim(),
          lastName: lastName.trim(),
          identificationNumber: identificationNumber.trim(),
          groupId: groupId || undefined,
        });
        log.info('handleSubmit', 'Yeni öğrenci oluşturuldu');
        setSuccess('Öğrenci başarıyla kaydedildi!');
        setTimeout(() => navigate('/students'), 1200);
      }
    } catch (err) {
      log.error('handleSubmit', 'Öğrenci kaydedilemedi', err);
      setError('Kayıt sırasında bir hata oluştu. Lütfen tekrar deneyin.');
    } finally {
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

  return (
    <div className="fade-in" style={{ paddingBottom: 100 }}>
      {/* Üst Bar */}
      <div style={{ display: 'flex', alignItems: 'center', gap: 12, marginBottom: 20 }}>
        <button
          onClick={() => navigate(-1)}
          style={{
            background: 'var(--color-card)',
            border: '1px solid var(--color-border)',
            borderRadius: '50%',
            width: 40,
            height: 40,
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            cursor: 'pointer',
          }}
        >
          <Icon name="back" size={20} />
        </button>
        <h2 style={{ fontSize: 18, margin: 0, fontWeight: 700 }}>
          {isEdit ? 'Öğrenci Düzenle' : 'Yeni Öğrenci Kaydı'}
        </h2>
      </div>

      <form onSubmit={handleSubmit} style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
        {/* Hata Mesajı */}
        {error && <div className="auth-error">{error}</div>}

        {/* Başarı Mesajı */}
        {success && (
          <div
            style={{
              background: '#D1FAE5',
              color: '#065F46',
              padding: '12px 16px',
              borderRadius: 'var(--radius-sm)',
              fontSize: 13,
              fontWeight: 600,
              display: 'flex',
              alignItems: 'center',
              gap: 8,
            }}
          >
            <Icon name="check" size={16} />
            {success}
          </div>
        )}

        {/* Kişisel Bilgiler */}
        <div className="glass-card" style={{ padding: 16, display: 'flex', flexDirection: 'column', gap: 16 }}>
          <h3 style={{ fontSize: 14, fontWeight: 700, color: 'var(--color-green)', margin: 0 }}>
            Kişisel Bilgiler
          </h3>

          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 12 }}>
            <div>
              <label style={labelStyle}>Ad *</label>
              <input
                type="text"
                value={firstName}
                onChange={(e) => setFirstName(e.target.value)}
                className="auth-input"
                style={inputStyle}
                placeholder="Öğrenci adı"
                required
              />
            </div>
            <div>
              <label style={labelStyle}>Soyad *</label>
              <input
                type="text"
                value={lastName}
                onChange={(e) => setLastName(e.target.value)}
                className="auth-input"
                style={inputStyle}
                placeholder="Öğrenci soyadı"
                required
              />
            </div>
          </div>

          <div>
            <label style={labelStyle}>TC Kimlik No</label>
            <input
              type="text"
              value={identificationNumber}
              onChange={(e) => {
                const val = e.target.value.replace(/\D/g, '').slice(0, 11);
                setIdentificationNumber(val);
              }}
              className="auth-input"
              style={inputStyle}
              placeholder="11 haneli TC kimlik numarası"
              maxLength={11}
              inputMode="numeric"
            />
          </div>

          <div>
            <label style={{ display: 'flex', alignItems: 'center', gap: 10, cursor: 'pointer' }}>
              <input
                type="checkbox"
                checked={isBoarder}
                onChange={(e) => setIsBoarder(e.target.checked)}
                style={{ width: 20, height: 20, accentColor: 'var(--color-green)' }}
              />
              <span style={{ fontWeight: 600 }}>Yatılı Öğrenci</span>
            </label>
          </div>
        </div>

        {/* Durum (sadece düzenlemede) */}
        {isEdit && (
          <div className="glass-card" style={{ padding: 16, display: 'flex', flexDirection: 'column', gap: 12 }}>
            <h3 style={{ fontSize: 14, fontWeight: 700, color: 'var(--color-green)', margin: 0 }}>
              Kayıt Durumu
            </h3>
            <div style={{ display: 'flex', gap: 8 }}>
              {[
                { val: 'active', label: 'Aktif', color: 'var(--color-green)' },
                { val: 'passive', label: 'Pasif', color: 'var(--color-gold)' },
                { val: 'graduated', label: 'Mezun', color: 'var(--color-blue)' },
                { val: 'archived', label: 'Arşiv', color: 'var(--color-muted)' },
              ].map((opt) => (
                <button
                  key={opt.val}
                  type="button"
                  onClick={() => setStatus(opt.val)}
                  style={{
                    flex: 1,
                    padding: '10px 0',
                    borderRadius: 8,
                    border: `2px solid ${status === opt.val ? opt.color : 'var(--color-border)'}`,
                    background: status === opt.val ? `${opt.color}15` : 'transparent',
                    color: status === opt.val ? opt.color : 'var(--color-muted)',
                    fontWeight: 700,
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
        )}

        {/* Kaydet Butonu */}
        <button
          type="submit"
          disabled={submitting}
          className="auth-button auth-button-primary"
        >
          {submitting
            ? 'Kaydediliyor...'
            : isEdit
              ? 'Değişiklikleri Kaydet'
              : 'Öğrenciyi Kaydet'}
        </button>
      </form>
    </div>
  );
}

// Ortak stiller
const labelStyle: React.CSSProperties = {
  display: 'block',
  fontSize: 13,
  fontWeight: 700,
  marginBottom: 6,
  color: 'var(--color-muted)',
};

const inputStyle: React.CSSProperties = {
  textAlign: 'left',
  letterSpacing: 'normal',
  fontWeight: 600,
};
