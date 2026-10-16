import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { getCourseInfo, getSettings, updateCourseInfo, updateSettings } from '../../api/courses';
import { getErrorMessage, getFieldErrors } from '../../api/client';
import { useAuth } from '../../context/AuthContext';
import { Icon } from '../../components/ui/Icon';
import { Logger } from '../../services/logger';
import type { ICourseInfo, ITenantSettings } from '../../types/course';

const log = Logger.create('SettingsPage');

const DAYS = [
  { value: 1, label: 'Pzt' },
  { value: 2, label: 'Sal' },
  { value: 3, label: 'Çar' },
  { value: 4, label: 'Per' },
  { value: 5, label: 'Cum' },
  { value: 6, label: 'Cmt' },
  { value: 7, label: 'Paz' },
];

const TIME_ZONES = [
  { value: 'Europe/Istanbul', label: 'Türkiye (İstanbul)' },
  { value: 'Europe/Berlin', label: 'Almanya (Berlin)' },
  { value: 'Europe/Amsterdam', label: 'Hollanda (Amsterdam)' },
  { value: 'Europe/Brussels', label: 'Belçika (Brüksel)' },
  { value: 'Europe/Vienna', label: 'Avusturya (Viyana)' },
  { value: 'Europe/Paris', label: 'Fransa (Paris)' },
  { value: 'Europe/London', label: 'İngiltere (Londra)' },
];

/** Kurs ayarları: kurs müdürü düzenler, hocalar görüntüler */
export default function SettingsPage() {
  const navigate = useNavigate();
  const { user } = useAuth();
  const canEdit = user?.role === 'tenant_admin';

  const [settings, setSettings] = useState<ITenantSettings | null>(null);
  const [course, setCourse] = useState<ICourseInfo | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [savingSettings, setSavingSettings] = useState(false);
  const [savingCourse, setSavingCourse] = useState(false);
  const [settingsMsg, setSettingsMsg] = useState<{ ok: boolean; text: string } | null>(null);
  const [courseMsg, setCourseMsg] = useState<{ ok: boolean; text: string } | null>(null);
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({});

  useEffect(() => {
    (async () => {
      try {
        const [s, c] = await Promise.all([getSettings(), getCourseInfo()]);
        setSettings(s);
        setCourse(c);
      } catch (err) {
        log.error('load', 'Ayarlar alınamadı', err);
        setError(getErrorMessage(err, 'Ayarlar alınamadı.'));
      } finally {
        setLoading(false);
      }
    })();
  }, []);

  const patch = (changes: Partial<ITenantSettings>) => setSettings((s) => (s ? { ...s, ...changes } : s));

  const toggleDay = (day: number) => {
    if (!settings || !canEdit) return;
    const days = settings.workingDays.includes(day)
      ? settings.workingDays.filter((d) => d !== day)
      : [...settings.workingDays, day].sort();
    patch({ workingDays: days });
  };

  const saveSettings = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!settings) return;
    setSavingSettings(true);
    setSettingsMsg(null);
    setFieldErrors({});
    try {
      await updateSettings(settings);
      setSettingsMsg({ ok: true, text: 'Ayarlar kaydedildi.' });
    } catch (err) {
      setFieldErrors(getFieldErrors(err));
      setSettingsMsg({ ok: false, text: getErrorMessage(err, 'Ayarlar kaydedilemedi.') });
    } finally {
      setSavingSettings(false);
    }
  };

  const saveCourse = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!course) return;
    setSavingCourse(true);
    setCourseMsg(null);
    try {
      await updateCourseInfo({
        name: course.name,
        contactEmail: course.contactEmail?.trim() || undefined,
        contactPhone: course.contactPhone?.trim() || undefined,
      });
      setCourseMsg({ ok: true, text: 'Kurs bilgileri kaydedildi.' });
    } catch (err) {
      setCourseMsg({ ok: false, text: getErrorMessage(err, 'Kurs bilgileri kaydedilemedi.') });
    } finally {
      setSavingCourse(false);
    }
  };

  const err = (key: string) => fieldErrors[key] && <span className="form-field-error">{fieldErrors[key]}</span>;

  if (loading) {
    return (
      <div style={{ padding: '14px 0' }} className="form-stack">
        <div className="skeleton" style={{ height: 140 }} />
        <div className="skeleton" style={{ height: 260 }} />
      </div>
    );
  }

  return (
    <div className="form-stack fade-in" style={{ paddingBottom: 90 }}>
      <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
        <button className="link-button" style={{ textDecoration: 'none' }} onClick={() => navigate(-1)} aria-label="Geri">
          <Icon name="back" size={20} />
        </button>
        <h1 className="page-title" style={{ fontSize: 20 }}>Kurs Ayarları</h1>
      </div>

      {!canEdit && (
        <div className="notice notice-info">Ayarları yalnızca kurs müdürü değiştirebilir; siz görüntülüyorsunuz.</div>
      )}
      {error && <div className="notice notice-error">{error}</div>}

      {course && (
        <form className="form-section" onSubmit={saveCourse}>
          <h2 className="form-section-title">Kurs Bilgileri</h2>
          <fieldset disabled={!canEdit} className="form-stack" style={{ border: 'none' }}>
            <div className="form-field">
              <label className="form-label" htmlFor="c-name">Kurs adı</label>
              <input id="c-name" className="form-input" value={course.name}
                onChange={(e) => setCourse({ ...course, name: e.target.value })} required />
            </div>
            <div className="form-field">
              <label className="form-label" htmlFor="c-email">E-posta</label>
              <input id="c-email" className="form-input" type="email" value={course.contactEmail ?? ''}
                onChange={(e) => setCourse({ ...course, contactEmail: e.target.value })} />
            </div>
            <div className="form-field">
              <label className="form-label" htmlFor="c-phone">Telefon</label>
              <input id="c-phone" className="form-input" type="tel" value={course.contactPhone ?? ''}
                onChange={(e) => setCourse({ ...course, contactPhone: e.target.value })} />
            </div>
          </fieldset>
          {courseMsg && <div className={`notice ${courseMsg.ok ? 'notice-success' : 'notice-error'}`}>{courseMsg.text}</div>}
          {canEdit && (
            <button type="submit" className="auth-button auth-button-primary" disabled={savingCourse}>
              {savingCourse ? 'Kaydediliyor...' : 'Kurs Bilgilerini Kaydet'}
            </button>
          )}
        </form>
      )}

      {settings && (
        <form className="form-stack" onSubmit={saveSettings}>
          <fieldset disabled={!canEdit} className="form-stack" style={{ border: 'none' }}>
            <section className="form-section">
              <h2 className="form-section-title">Ders Programı</h2>
              <p className="form-section-hint">Haftalık ders programı bu gün ve saatlere göre kurulur.</p>

              <div className="form-field">
                <span className="form-label">Çalışma günleri</span>
                <div className="day-picker">
                  {DAYS.map((d) => (
                    <button key={d.value} type="button" aria-pressed={settings.workingDays.includes(d.value)}
                      className={`day-chip ${settings.workingDays.includes(d.value) ? 'selected' : ''}`}
                      onClick={() => toggleDay(d.value)} disabled={!canEdit}>
                      {d.label}
                    </button>
                  ))}
                </div>
                {err('workingDays')}
              </div>

              <div className="form-row">
                <div className="form-field">
                  <label className="form-label" htmlFor="s-start">Gün başlangıcı</label>
                  <input id="s-start" className="form-input" type="time" value={settings.dayStartTime}
                    onChange={(e) => patch({ dayStartTime: e.target.value })} required />
                  {err('dayStartTime')}
                </div>
                <div className="form-field">
                  <label className="form-label" htmlFor="s-end">Gün bitişi</label>
                  <input id="s-end" className="form-input" type="time" value={settings.dayEndTime}
                    onChange={(e) => patch({ dayEndTime: e.target.value })} required />
                  {err('dayEndTime')}
                </div>
              </div>

              <div className="form-row">
                <div className="form-field">
                  <label className="form-label" htmlFor="s-slot">Ders süresi (dk)</label>
                  <input id="s-slot" className="form-input" type="number" min={5} max={240} value={settings.defaultSlotMinutes}
                    onChange={(e) => patch({ defaultSlotMinutes: Number(e.target.value) })} required />
                  {err('defaultSlotMinutes')}
                </div>
                <div className="form-field">
                  <label className="form-label" htmlFor="s-tz">Saat dilimi</label>
                  <select id="s-tz" className="form-select" value={settings.timeZone}
                    onChange={(e) => patch({ timeZone: e.target.value })}>
                    {!TIME_ZONES.some((t) => t.value === settings.timeZone) && (
                      <option value={settings.timeZone}>{settings.timeZone}</option>
                    )}
                    {TIME_ZONES.map((t) => <option key={t.value} value={t.value}>{t.label}</option>)}
                  </select>
                </div>
              </div>

              <label className="form-check">
                <input type="checkbox" checked={settings.etutStartTime !== null}
                  onChange={(e) => patch({ etutStartTime: e.target.checked ? '19:00' : null })} />
                Akşam etüdü var
              </label>
              {settings.etutStartTime !== null && (
                <div className="form-field">
                  <label className="form-label" htmlFor="s-etut">Etüt başlangıcı</label>
                  <input id="s-etut" className="form-input" type="time" value={settings.etutStartTime}
                    onChange={(e) => patch({ etutStartTime: e.target.value })} />
                </div>
              )}
            </section>

            <section className="form-section">
              <h2 className="form-section-title">Devamsızlık ve Tekrar</h2>
              <div className="form-row">
                <div className="form-field">
                  <label className="form-label" htmlFor="s-abs">Uyarı eşiği (gün)</label>
                  <input id="s-abs" className="form-input" type="number" min={1} max={365} value={settings.absenceThreshold}
                    onChange={(e) => patch({ absenceThreshold: Number(e.target.value) })} />
                  {err('absenceThreshold')}
                </div>
                <div className="form-field">
                  <label className="form-label" htmlFor="s-absp">Değerlendirme süresi (gün)</label>
                  <input id="s-absp" className="form-input" type="number" min={1} max={365} value={settings.absencePeriodDays}
                    onChange={(e) => patch({ absencePeriodDays: Number(e.target.value) })} />
                  {err('absencePeriodDays')}
                </div>
              </div>
              <p className="form-section-hint" style={{ marginTop: -4 }}>
                Son {settings.absencePeriodDays} günde {settings.absenceThreshold} veya daha fazla devamsızlık yapan öğrenci için uyarı verilir.
              </p>

              <div className="form-field">
                <label className="form-label" htmlFor="s-review">Tekrar yöntemi</label>
                <select id="s-review" className="form-select" value={settings.reviewMode}
                  onChange={(e) => patch({ reviewMode: e.target.value as ITenantSettings['reviewMode'] })}>
                  <option value="spaced">Akıllı tekrar (zayıf sayfalar daha sık)</option>
                  <option value="cyclic">Döngüsel tekrar (cüzler sırayla)</option>
                </select>
              </div>
              <div className="form-field">
                <label className="form-label" htmlFor="s-target">Günlük tekrar hedefi (sayfa)</label>
                <input id="s-target" className="form-input" type="number" min={1} max={604} value={settings.dailyReviewTargetPages}
                  onChange={(e) => patch({ dailyReviewTargetPages: Number(e.target.value) })} />
                {err('dailyReviewTargetPages')}
              </div>
            </section>

            <section className="form-section">
              <h2 className="form-section-title">Veli Bildirimleri</h2>
              <label className="form-check">
                <input type="checkbox" checked={settings.notifyParentOnAbsence}
                  onChange={(e) => patch({ notifyParentOnAbsence: e.target.checked })} />
                Devamsızlıkta veliye bildirim gönder
              </label>
              <label className="form-check">
                <input type="checkbox" checked={settings.notifyParentOnMilestone}
                  onChange={(e) => patch({ notifyParentOnMilestone: e.target.checked })} />
                Cüz bitirme gibi başarılarda veliye bildirim gönder
              </label>
            </section>
          </fieldset>

          {settingsMsg && <div className={`notice ${settingsMsg.ok ? 'notice-success' : 'notice-error'}`}>{settingsMsg.text}</div>}
          {canEdit && (
            <button type="submit" className="auth-button auth-button-primary" disabled={savingSettings}>
              {savingSettings ? 'Kaydediliyor...' : 'Ayarları Kaydet'}
            </button>
          )}
        </form>
      )}
    </div>
  );
}
