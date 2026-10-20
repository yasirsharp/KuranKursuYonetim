import { useCallback, useEffect, useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  createEvent,
  createTerm,
  deleteEvent,
  deleteTerm,
  getEvents,
  getHolidaySuggestions,
  getSchoolDays,
  getTermPlanning,
  getTerms,
  importHolidays,
  updateTerm,
} from '../../api/calendar';
import { getErrorMessage, getFieldErrors } from '../../api/client';
import { useAuth } from '../../context/AuthContext';
import { Icon } from '../../components/ui/Icon';
import { Logger } from '../../services/logger';
import type {
  CalendarEventType,
  IAcademicTerm,
  ICalendarEvent,
  ICalendarEventInput,
  IHolidaySuggestionList,
  ITermInput,
  ITermPlanning,
} from '../../types/calendar';

const log = Logger.create('CalendarPage');

const EVENT_TYPES: { value: CalendarEventType; label: string }[] = [
  { value: 'official_holiday', label: 'Resmi tatil' },
  { value: 'religious_holiday', label: 'Dini bayram' },
  { value: 'break', label: 'Ara tatil' },
  { value: 'closure', label: 'Kurs kapalı' },
  { value: 'event', label: 'Etkinlik' },
];

const TERM_STATE_LABELS = { current: 'Devam ediyor', upcoming: 'Başlamadı', past: 'Bitti' } as const;
const TERM_STATE_CLASS = { current: 'status-active', upcoming: 'status-pending', past: 'status-suspended' } as const;

/** "2026-09-14" → "14.09.2026" (saat dilimi kaymasın diye metin olarak çevrilir) */
export function formatDay(value: string): string {
  const [y, m, d] = value.split('-');
  return `${d}.${m}.${y}`;
}

function formatRange(start: string, end: string): string {
  return start === end ? formatDay(start) : `${formatDay(start)} – ${formatDay(end)}`;
}

const eventTypeLabel = (type: string) => EVENT_TYPES.find((t) => t.value === type)?.label ?? type;

const emptyTerm: ITermInput = { name: '', startDate: '', endDate: '', notes: '' };
const emptyEvent: ICalendarEventInput = { title: '', eventType: 'closure', startDate: '', endDate: '', isDayOff: true, description: '' };

/** Eğitim dönemleri ve kurs takvimi: kurs müdürü düzenler, hocalar görüntüler */
export default function CalendarPage() {
  const navigate = useNavigate();
  const { user } = useAuth();
  const canEdit = user?.role === 'tenant_admin';

  const [planning, setPlanning] = useState<ITermPlanning | null>(null);
  const [terms, setTerms] = useState<IAcademicTerm[]>([]);
  const [selectedTermId, setSelectedTermId] = useState<string | null>(null);
  const [events, setEvents] = useState<ICalendarEvent[]>([]);
  const [schoolDayCount, setSchoolDayCount] = useState<number | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [message, setMessage] = useState<{ ok: boolean; text: string } | null>(null);

  // Dönem formu (yeni veya düzenleme)
  const [termForm, setTermForm] = useState<ITermInput | null>(null);
  const [editingTermId, setEditingTermId] = useState<string | null>(null);
  const [termErrors, setTermErrors] = useState<Record<string, string>>({});
  const [holidays, setHolidays] = useState<IHolidaySuggestionList | null>(null);
  const [holidaysLoading, setHolidaysLoading] = useState(false);
  const [selectedHolidayKeys, setSelectedHolidayKeys] = useState<string[]>([]);
  const [savingTerm, setSavingTerm] = useState(false);

  // Takvim kaydı formu
  const [eventForm, setEventForm] = useState<ICalendarEventInput | null>(null);
  const [eventErrors, setEventErrors] = useState<Record<string, string>>({});
  const [savingEvent, setSavingEvent] = useState(false);

  const selectedTerm = useMemo(() => terms.find((t) => t.id === selectedTermId) ?? null, [terms, selectedTermId]);

  const loadTerms = useCallback(async () => {
    const [p, list] = await Promise.all([getTermPlanning(), getTerms()]);
    setPlanning(p);
    setTerms(list);
    setSelectedTermId((current) =>
      current && list.some((t) => t.id === current)
        ? current
        : (p.currentTerm ?? p.nextTerm ?? list[0] ?? null)?.id ?? null
    );
  }, []);

  useEffect(() => {
    (async () => {
      try {
        await loadTerms();
      } catch (err) {
        log.error('load', 'Dönemler alınamadı', err);
        setError(getErrorMessage(err, 'Dönemler alınamadı.'));
      } finally {
        setLoading(false);
      }
    })();
  }, [loadTerms]);

  const loadTermCalendar = useCallback(async (term: IAcademicTerm) => {
    try {
      const [list, days] = await Promise.all([getEvents(term.startDate, term.endDate), getSchoolDays(term.startDate, term.endDate)]);
      setEvents(list);
      setSchoolDayCount(days.schoolDayCount);
    } catch (err) {
      log.error('events', 'Takvim alınamadı', err);
      setError(getErrorMessage(err, 'Takvim alınamadı.'));
    }
  }, []);

  useEffect(() => {
    if (selectedTerm) loadTermCalendar(selectedTerm);
    else {
      setEvents([]);
      setSchoolDayCount(null);
    }
  }, [selectedTerm, loadTermCalendar]);

  // Dönem tarihleri girilince resmi ve dini tatiller getirilir
  const termFrom = termForm?.startDate ?? '';
  const termTo = termForm?.endDate ?? '';
  useEffect(() => {
    if (!termFrom || !termTo || termFrom >= termTo) {
      setHolidays(null);
      return;
    }
    let cancelled = false;
    setHolidaysLoading(true);
    const timer = setTimeout(async () => {
      try {
        const list = await getHolidaySuggestions(termFrom, termTo);
        if (cancelled) return;
        setHolidays(list);
        setSelectedHolidayKeys(list.holidays.filter((h) => !h.alreadyAdded).map((h) => h.key));
      } catch (err) {
        if (!cancelled) {
          log.error('holidays', 'Tatiller alınamadı', err);
          setHolidays(null);
        }
      } finally {
        if (!cancelled) setHolidaysLoading(false);
      }
    }, 400);
    return () => {
      cancelled = true;
      clearTimeout(timer);
    };
  }, [termFrom, termTo]);

  const openTermForm = (term?: IAcademicTerm | ITermInput | null) => {
    setMessage(null);
    setTermErrors({});
    setEditingTermId(term && 'id' in term ? term.id : null);
    setTermForm(term ? { name: term.name, startDate: term.startDate, endDate: term.endDate, notes: term.notes ?? '' } : { ...emptyTerm });
  };

  const closeTermForm = () => {
    setTermForm(null);
    setEditingTermId(null);
    setHolidays(null);
  };

  const toggleHoliday = (key: string) =>
    setSelectedHolidayKeys((keys) => (keys.includes(key) ? keys.filter((k) => k !== key) : [...keys, key]));

  const saveTerm = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!termForm) return;
    setSavingTerm(true);
    setTermErrors({});
    setMessage(null);
    try {
      const input = { ...termForm, name: termForm.name.trim(), notes: termForm.notes?.trim() || null };
      let termId = editingTermId;
      if (termId) await updateTerm(termId, input);
      else termId = (await createTerm(input)).id;

      let added = 0;
      if (selectedHolidayKeys.length > 0) added = await importHolidays(input.startDate, input.endDate, selectedHolidayKeys);

      closeTermForm();
      setSelectedTermId(termId);
      await loadTerms();
      setMessage({ ok: true, text: `Dönem kaydedildi${added > 0 ? `; ${added} tatil takvime eklendi` : ''}.` });
    } catch (err) {
      setTermErrors(getFieldErrors(err));
      setMessage({ ok: false, text: getErrorMessage(err, 'Dönem kaydedilemedi.') });
    } finally {
      setSavingTerm(false);
    }
  };

  const removeTerm = async (term: IAcademicTerm) => {
    if (!window.confirm(`"${term.name}" dönemi silinsin mi? Takvimdeki tatiller silinmez.`)) return;
    try {
      await deleteTerm(term.id);
      await loadTerms();
      setMessage({ ok: true, text: 'Dönem silindi.' });
    } catch (err) {
      setMessage({ ok: false, text: getErrorMessage(err, 'Dönem silinemedi.') });
    }
  };

  const saveEvent = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!eventForm) return;
    setSavingEvent(true);
    setEventErrors({});
    try {
      await createEvent({
        ...eventForm,
        title: eventForm.title.trim(),
        endDate: eventForm.endDate || eventForm.startDate,
        description: eventForm.description?.trim() || null,
      });
      setEventForm(null);
      if (selectedTerm) await loadTermCalendar(selectedTerm);
      setMessage({ ok: true, text: 'Takvim kaydı eklendi.' });
    } catch (err) {
      setEventErrors(getFieldErrors(err));
      setMessage({ ok: false, text: getErrorMessage(err, 'Takvim kaydı eklenemedi.') });
    } finally {
      setSavingEvent(false);
    }
  };

  const removeEvent = async (event: ICalendarEvent) => {
    if (!window.confirm(`"${event.title}" takvimden kaldırılsın mı?`)) return;
    try {
      await deleteEvent(event.id);
      if (selectedTerm) await loadTermCalendar(selectedTerm);
    } catch (err) {
      setMessage({ ok: false, text: getErrorMessage(err, 'Takvim kaydı silinemedi.') });
    }
  };

  const fieldError = (errors: Record<string, string>, key: string) =>
    errors[key] && <span className="form-field-error">{errors[key]}</span>;

  if (loading) {
    return (
      <div style={{ padding: '14px 0' }} className="form-stack">
        <div className="skeleton" style={{ height: 120 }} />
        <div className="skeleton" style={{ height: 240 }} />
      </div>
    );
  }

  return (
    <div className="form-stack fade-in" style={{ paddingBottom: 90 }}>
      <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
        <button className="link-button" style={{ textDecoration: 'none' }} onClick={() => navigate(-1)} aria-label="Geri">
          <Icon name="back" size={20} />
        </button>
        <h1 className="page-title" style={{ fontSize: 20 }}>Dönem ve Takvim</h1>
      </div>

      {!canEdit && <div className="notice notice-info">Dönem ve takvimi yalnızca kurs müdürü değiştirebilir; siz görüntülüyorsunuz.</div>}
      {error && <div className="notice notice-error">{error}</div>}
      {message && <div className={`notice ${message.ok ? 'notice-success' : 'notice-error'}`}>{message.text}</div>}

      {/* Dönem özeti ve yeni dönem hatırlatması */}
      {planning?.currentTerm && (
        <div className="info-card">
          <div className="info-card-title">{planning.currentTerm.name}</div>
          <div style={{ fontSize: 13, color: 'var(--color-muted)' }}>
            {formatRange(planning.currentTerm.startDate, planning.currentTerm.endDate)}
            {planning.daysUntilCurrentTermEnds !== null && ` · bitişe ${planning.daysUntilCurrentTermEnds} gün`}
          </div>
        </div>
      )}
      {planning?.shouldPlanNextTerm && canEdit && !termForm && (
        <div className="notice notice-warning">
          {planning.currentTerm ? 'Dönem sonu yaklaşıyor; yeni dönemi planlayın.' : 'Henüz devam eden bir dönem yok.'}
          {planning.suggestedNextTerm && (
            <> Öneri: <strong>{planning.suggestedNextTerm.name}</strong> ({formatRange(planning.suggestedNextTerm.startDate, planning.suggestedNextTerm.endDate)})</>
          )}
          <div style={{ marginTop: 8 }}>
            <button type="button" className="auth-button auth-button-primary auth-button-small"
              onClick={() => openTermForm(planning.suggestedNextTerm)}>
              Yeni Dönemi Planla
            </button>
          </div>
        </div>
      )}

      {/* Dönem formu */}
      {termForm && (
        <form className="form-section" onSubmit={saveTerm}>
          <h2 className="form-section-title">{editingTermId ? 'Dönemi Düzenle' : 'Yeni Dönem'}</h2>
          <div className="form-field">
            <label className="form-label" htmlFor="t-name">Dönem adı</label>
            <input id="t-name" className="form-input" value={termForm.name} placeholder="2026-2027 Eğitim Dönemi"
              onChange={(e) => setTermForm({ ...termForm, name: e.target.value })} required />
            {fieldError(termErrors, 'name')}
          </div>
          <div className="form-row">
            <div className="form-field">
              <label className="form-label" htmlFor="t-start">Başlangıç</label>
              <input id="t-start" className="form-input" type="date" value={termForm.startDate}
                onChange={(e) => setTermForm({ ...termForm, startDate: e.target.value })} required />
              {fieldError(termErrors, 'startDate')}
            </div>
            <div className="form-field">
              <label className="form-label" htmlFor="t-end">Bitiş</label>
              <input id="t-end" className="form-input" type="date" value={termForm.endDate}
                onChange={(e) => setTermForm({ ...termForm, endDate: e.target.value })} required />
              {fieldError(termErrors, 'endDate')}
            </div>
          </div>

          {holidaysLoading && <div className="notice notice-info">Resmi ve dini tatiller yükleniyor…</div>}
          {!holidaysLoading && holidays && (
            <div className="form-field">
              <span className="form-label">Takvime eklenecek tatiller</span>
              {holidays.missingReligiousHolidayYears.length > 0 && (
                <div className="notice notice-warning">
                  {holidays.missingReligiousHolidayYears.join(', ')} yılı dini bayram tarihleri henüz listede yok; bu bayramları takvime elle ekleyin.
                </div>
              )}
              <div className="holiday-list">
                {holidays.holidays.map((h) => (
                  <label key={h.key} className="form-check">
                    <input type="checkbox" disabled={h.alreadyAdded}
                      checked={h.alreadyAdded || selectedHolidayKeys.includes(h.key)}
                      onChange={() => toggleHoliday(h.key)} />
                    <span>
                      {h.title} <span className="holiday-date">{formatRange(h.startDate, h.endDate)}</span>
                      {!h.isDayOff && <span className="holiday-date"> · yarım gün</span>}
                      {h.alreadyAdded && <span className="holiday-date"> · takvimde</span>}
                    </span>
                  </label>
                ))}
              </div>
            </div>
          )}

          <div className="button-row">
            <button type="button" className="auth-button auth-button-secondary" onClick={closeTermForm}>Vazgeç</button>
            <button type="submit" className="auth-button auth-button-primary" disabled={savingTerm}>
              {savingTerm ? 'Kaydediliyor...' : 'Dönemi Kaydet'}
            </button>
          </div>
        </form>
      )}

      {/* Dönemler */}
      <section className="form-section">
        <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
          <h2 className="form-section-title">Dönemler</h2>
          {canEdit && !termForm && (
            <button type="button" className="link-button" onClick={() => openTermForm()}>+ Yeni dönem</button>
          )}
        </div>
        {terms.length === 0 && <p className="form-section-hint">Henüz dönem açılmadı.</p>}
        <nav className="menu-list">
          {terms.map((t) => (
            <div key={t.id} className={`menu-item term-item ${t.id === selectedTermId ? 'selected' : ''}`}
              role="button" tabIndex={0} onClick={() => setSelectedTermId(t.id)}
              onKeyDown={(e) => e.key === 'Enter' && setSelectedTermId(t.id)}>
              <div style={{ flex: 1 }}>
                <div>{t.name}</div>
                <div className="holiday-date">{formatRange(t.startDate, t.endDate)}</div>
              </div>
              <span className={`status-badge ${TERM_STATE_CLASS[t.state]}`}>{TERM_STATE_LABELS[t.state]}</span>
              {canEdit && (
                <>
                  <button type="button" className="link-button" onClick={(e) => { e.stopPropagation(); openTermForm(t); }}>Düzenle</button>
                  <button type="button" className="link-button" onClick={(e) => { e.stopPropagation(); removeTerm(t); }}>Sil</button>
                </>
              )}
            </div>
          ))}
        </nav>
      </section>

      {/* Seçili dönemin takvimi */}
      {selectedTerm && (
        <section className="form-section">
          <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
            <h2 className="form-section-title">{selectedTerm.name} Takvimi</h2>
            {canEdit && !eventForm && (
              <button type="button" className="link-button"
                onClick={() => { setEventErrors({}); setEventForm({ ...emptyEvent }); }}>+ Kayıt ekle</button>
            )}
          </div>
          {schoolDayCount !== null && (
            <p className="form-section-hint">Bu dönemde <strong>{schoolDayCount}</strong> ders günü var (çalışma günleri ve tatiller düşülerek).</p>
          )}

          {eventForm && (
            <form className="form-stack calendar-event-form" onSubmit={saveEvent}>
              <div className="form-field">
                <label className="form-label" htmlFor="e-title">Başlık</label>
                <input id="e-title" className="form-input" value={eventForm.title} placeholder="Yarıyıl tatili"
                  onChange={(e) => setEventForm({ ...eventForm, title: e.target.value })} required />
                {fieldError(eventErrors, 'title')}
              </div>
              <div className="form-field">
                <label className="form-label" htmlFor="e-type">Tür</label>
                <select id="e-type" className="form-select" value={eventForm.eventType}
                  onChange={(e) => {
                    const eventType = e.target.value as CalendarEventType;
                    setEventForm({ ...eventForm, eventType, isDayOff: eventType !== 'event' });
                  }}>
                  {EVENT_TYPES.map((t) => <option key={t.value} value={t.value}>{t.label}</option>)}
                </select>
              </div>
              <div className="form-row">
                <div className="form-field">
                  <label className="form-label" htmlFor="e-start">Başlangıç</label>
                  <input id="e-start" className="form-input" type="date" value={eventForm.startDate}
                    onChange={(e) => setEventForm({ ...eventForm, startDate: e.target.value })} required />
                </div>
                <div className="form-field">
                  <label className="form-label" htmlFor="e-end">Bitiş <span className="form-label-optional">(tek gün ise boş)</span></label>
                  <input id="e-end" className="form-input" type="date" value={eventForm.endDate} min={eventForm.startDate}
                    onChange={(e) => setEventForm({ ...eventForm, endDate: e.target.value })} />
                  {fieldError(eventErrors, 'endDate')}
                </div>
              </div>
              <label className="form-check">
                <input type="checkbox" checked={eventForm.isDayOff}
                  onChange={(e) => setEventForm({ ...eventForm, isDayOff: e.target.checked })} />
                Bu günlerde ders yapılmaz
              </label>
              <div className="button-row">
                <button type="button" className="auth-button auth-button-secondary" onClick={() => setEventForm(null)}>Vazgeç</button>
                <button type="submit" className="auth-button auth-button-primary" disabled={savingEvent}>
                  {savingEvent ? 'Kaydediliyor...' : 'Ekle'}
                </button>
              </div>
            </form>
          )}

          {events.length === 0 && <p className="form-section-hint">Bu dönemde takvim kaydı yok.</p>}
          <ul className="calendar-event-list">
            {events.map((ev) => (
              <li key={ev.id} className={`calendar-event ${ev.isDayOff ? 'day-off' : ''}`}>
                <div style={{ flex: 1 }}>
                  <div className="calendar-event-title">{ev.title}</div>
                  <div className="holiday-date">
                    {formatRange(ev.startDate, ev.endDate)} · {eventTypeLabel(ev.eventType)}
                    {!ev.isDayOff && ' · ders var'}
                  </div>
                  {ev.description && <div className="holiday-date">{ev.description}</div>}
                </div>
                {canEdit && (
                  <button type="button" className="link-button" aria-label={`${ev.title} sil`} onClick={() => removeEvent(ev)}>Sil</button>
                )}
              </li>
            ))}
          </ul>
        </section>
      )}
    </div>
  );
}
