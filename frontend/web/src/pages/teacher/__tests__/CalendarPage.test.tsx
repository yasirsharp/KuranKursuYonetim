import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import CalendarPage, { formatDay } from '../CalendarPage';
import {
  createTerm,
  getEvents,
  getHolidaySuggestions,
  getSchoolDays,
  getTermPlanning,
  getTerms,
  importHolidays,
} from '../../../api/calendar';
import type { IAcademicTerm } from '../../../types/calendar';

let role = 'tenant_admin';
vi.mock('../../../context/AuthContext', () => ({
  useAuth: () => ({ user: { role, firstName: 'Ali', lastName: 'Kaya' } }),
}));

vi.mock('../../../api/calendar', () => ({
  getTerms: vi.fn(),
  getTermPlanning: vi.fn(),
  createTerm: vi.fn(),
  updateTerm: vi.fn(),
  deleteTerm: vi.fn(),
  getEvents: vi.fn(),
  createEvent: vi.fn(),
  deleteEvent: vi.fn(),
  getHolidaySuggestions: vi.fn(),
  importHolidays: vi.fn(),
  getSchoolDays: vi.fn(),
}));

const currentTerm: IAcademicTerm = {
  id: 't1', name: '2026-2027 Eğitim Dönemi', startDate: '2026-09-14', endDate: '2027-08-28', state: 'current',
};

describe('CalendarPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    role = 'tenant_admin';
    vi.mocked(getTerms).mockResolvedValue([currentTerm]);
    vi.mocked(getEvents).mockResolvedValue([
      { id: 'e1', title: 'Cumhuriyet Bayramı', eventType: 'official_holiday', startDate: '2026-10-29', endDate: '2026-10-29', isDayOff: true },
    ]);
    vi.mocked(getSchoolDays).mockResolvedValue({ from: '2026-09-14', to: '2027-08-28', schoolDayCount: 268, schoolDays: [], offDays: [] });
  });

  it('tarihi saat dilimine kaydırmadan gün.ay.yıl biçiminde gösterir', () => {
    expect(formatDay('2026-09-14')).toBe('14.09.2026');
  });

  it('devam eden dönemi, takvim kayıtlarını ve ders günü sayısını gösterir', async () => {
    vi.mocked(getTermPlanning).mockResolvedValue({
      currentTerm, nextTerm: null, daysUntilCurrentTermEnds: 312, shouldPlanNextTerm: false, suggestedNextTerm: null,
    });

    render(<MemoryRouter><CalendarPage /></MemoryRouter>);

    expect(await screen.findByText('Cumhuriyet Bayramı')).toBeInTheDocument();
    expect(screen.getByText('268')).toBeInTheDocument();
    expect(screen.getByText(/bitişe 312 gün/)).toBeInTheDocument();
    expect(getEvents).toHaveBeenCalledWith('2026-09-14', '2027-08-28');
    expect(screen.queryByText('Yeni Dönemi Planla')).not.toBeInTheDocument();
  });

  it('dönem sonu yaklaşınca öneriyle yeni dönem açar ve seçilen tatilleri ekler', async () => {
    vi.mocked(getTermPlanning).mockResolvedValue({
      currentTerm, nextTerm: null, daysUntilCurrentTermEnds: 40, shouldPlanNextTerm: true,
      suggestedNextTerm: { name: '2027-2028 Eğitim Dönemi', startDate: '2027-09-13', endDate: '2028-08-26' },
    });
    let resolveHolidays: (v: Awaited<ReturnType<typeof getHolidaySuggestions>>) => void = () => {};
    vi.mocked(getHolidaySuggestions).mockReturnValue(new Promise((r) => { resolveHolidays = r; }));
    vi.mocked(createTerm).mockResolvedValue({ ...currentTerm, id: 't2', name: '2027-2028 Eğitim Dönemi', state: 'upcoming' });
    vi.mocked(importHolidays).mockResolvedValue(1);
    const user = userEvent.setup();

    render(<MemoryRouter><CalendarPage /></MemoryRouter>);
    await user.click(await screen.findByText('Yeni Dönemi Planla'));

    expect(screen.getByLabelText('Dönem adı')).toHaveValue('2027-2028 Eğitim Dönemi');
    expect(await screen.findByText('Resmi ve dini tatiller yükleniyor…')).toBeInTheDocument();

    resolveHolidays({
      from: '2027-09-13', to: '2028-08-26', missingReligiousHolidayYears: [2028],
      holidays: [
        { key: '2027-cumhuriyet', title: 'Cumhuriyet Bayramı', eventType: 'official_holiday', startDate: '2027-10-29', endDate: '2027-10-29', isDayOff: true, alreadyAdded: false },
        { key: '2028-yilbasi', title: 'Yılbaşı', eventType: 'official_holiday', startDate: '2028-01-01', endDate: '2028-01-01', isDayOff: true, alreadyAdded: false },
      ],
    });

    expect(await screen.findByText(/2028 yılı dini bayram tarihleri/)).toBeInTheDocument();
    await user.click(screen.getByText('Yılbaşı'));
    await user.click(screen.getByText('Dönemi Kaydet'));

    await waitFor(() => expect(importHolidays).toHaveBeenCalledWith('2027-09-13', '2028-08-26', ['2027-cumhuriyet']));
    expect(createTerm).toHaveBeenCalledWith(expect.objectContaining({ name: '2027-2028 Eğitim Dönemi', startDate: '2027-09-13' }));
    expect(await screen.findByText(/1 tatil takvime eklendi/)).toBeInTheDocument();
  });

  it('hoca takvimi görür ama düzenleyemez', async () => {
    role = 'head_teacher';
    vi.mocked(getTermPlanning).mockResolvedValue({
      currentTerm, nextTerm: null, daysUntilCurrentTermEnds: 40, shouldPlanNextTerm: true, suggestedNextTerm: null,
    });

    render(<MemoryRouter><CalendarPage /></MemoryRouter>);

    expect(await screen.findByText('Cumhuriyet Bayramı')).toBeInTheDocument();
    expect(screen.getByText(/yalnızca kurs müdürü değiştirebilir/)).toBeInTheDocument();
    expect(screen.queryByText('+ Yeni dönem')).not.toBeInTheDocument();
    expect(screen.queryByText('Yeni Dönemi Planla')).not.toBeInTheDocument();
  });
});
