import apiClient from './client';
import type { IApiResponse } from '../types/common';
import type {
  IAcademicTerm,
  ICalendarEvent,
  ICalendarEventInput,
  IHolidaySuggestionList,
  ISchoolDays,
  ITermInput,
  ITermPlanning,
} from '../types/calendar';

// ── Dönemler ────────────────────────────────────────────────────────

export async function getTerms(): Promise<IAcademicTerm[]> {
  const response = await apiClient.get<IApiResponse<IAcademicTerm[]>>('/api/calendar/terms');
  return response.data.data;
}

export async function getTermPlanning(): Promise<ITermPlanning> {
  const response = await apiClient.get<IApiResponse<ITermPlanning>>('/api/calendar/terms/planning');
  return response.data.data;
}

export async function createTerm(term: ITermInput): Promise<IAcademicTerm> {
  const response = await apiClient.post<IApiResponse<IAcademicTerm>>('/api/calendar/terms', term);
  return response.data.data;
}

export async function updateTerm(id: string, term: ITermInput): Promise<void> {
  await apiClient.put('/api/calendar/terms', { id, ...term });
}

export async function deleteTerm(id: string): Promise<void> {
  await apiClient.delete(`/api/calendar/terms/${id}`);
}

// ── Takvim kayıtları ────────────────────────────────────────────────

export async function getEvents(from: string, to: string): Promise<ICalendarEvent[]> {
  const response = await apiClient.get<IApiResponse<ICalendarEvent[]>>('/api/calendar/events', { params: { from, to } });
  return response.data.data;
}

export async function createEvent(event: ICalendarEventInput): Promise<ICalendarEvent> {
  const response = await apiClient.post<IApiResponse<ICalendarEvent>>('/api/calendar/events', event);
  return response.data.data;
}

export async function deleteEvent(id: string): Promise<void> {
  await apiClient.delete(`/api/calendar/events/${id}`);
}

// ── Tatiller ve ders günleri ────────────────────────────────────────

export async function getHolidaySuggestions(from: string, to: string): Promise<IHolidaySuggestionList> {
  const response = await apiClient.get<IApiResponse<IHolidaySuggestionList>>('/api/calendar/holidays', { params: { from, to } });
  return response.data.data;
}

/** Seçilen tatilleri takvime ekler; eklenen sayısını döner */
export async function importHolidays(from: string, to: string, keys: string[]): Promise<number> {
  const response = await apiClient.post<IApiResponse<{ added: number; skipped: number }>>('/api/calendar/holidays/import', { from, to, keys });
  return response.data.data.added;
}

export async function getSchoolDays(from: string, to: string): Promise<ISchoolDays> {
  const response = await apiClient.get<IApiResponse<ISchoolDays>>('/api/calendar/school-days', { params: { from, to } });
  return response.data.data;
}
