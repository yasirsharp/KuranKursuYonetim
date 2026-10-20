/** Tarihler "yyyy-MM-dd" biçimindedir (saat dilimi taşımaz). */

export type TermState = 'current' | 'upcoming' | 'past';

export interface IAcademicTerm {
  id: string;
  name: string;
  startDate: string;
  endDate: string;
  notes?: string | null;
  state: TermState;
}

export interface ITermInput {
  name: string;
  startDate: string;
  endDate: string;
  notes?: string | null;
}

export interface ITermPlanning {
  currentTerm: IAcademicTerm | null;
  nextTerm: IAcademicTerm | null;
  daysUntilCurrentTermEnds: number | null;
  shouldPlanNextTerm: boolean;
  suggestedNextTerm: ITermInput | null;
}

export type CalendarEventType = 'official_holiday' | 'religious_holiday' | 'break' | 'closure' | 'event';

export interface ICalendarEvent {
  id: string;
  termId?: string | null;
  title: string;
  eventType: CalendarEventType;
  startDate: string;
  endDate: string;
  isDayOff: boolean;
  description?: string | null;
  holidayKey?: string | null;
}

export interface ICalendarEventInput {
  title: string;
  eventType: CalendarEventType;
  startDate: string;
  endDate: string;
  isDayOff: boolean;
  description?: string | null;
}

export interface IHolidaySuggestion {
  key: string;
  title: string;
  eventType: CalendarEventType;
  startDate: string;
  endDate: string;
  isDayOff: boolean;
  description?: string | null;
  alreadyAdded: boolean;
}

export interface IHolidaySuggestionList {
  from: string;
  to: string;
  holidays: IHolidaySuggestion[];
  missingReligiousHolidayYears: number[];
}

export interface ISchoolDays {
  from: string;
  to: string;
  schoolDayCount: number;
  schoolDays: string[];
  offDays: { date: string; reason: string; title?: string | null }[];
}
