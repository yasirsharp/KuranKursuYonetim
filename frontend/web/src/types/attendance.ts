export interface IAttendance {
  id: string;
  tenantId: string;
  studentId: string;
  date: string;
  status: string;
  notes: string | null;
  createdAt: string;
}

export interface ICreateAttendance {
  studentId: string;
  date: string;
  status: string;
  notes?: string;
}

export interface IBulkAttendance {
  date: string;
  groupId: string;
  attendances: ICreateAttendanceItem[];
}

export interface ICreateAttendanceItem {
  studentId: string;
  status: string;
  notes?: string;
}
