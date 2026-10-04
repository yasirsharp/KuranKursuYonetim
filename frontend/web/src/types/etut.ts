export interface IAssignmentTaskItem {
  id: string;
  type: string; // "ezber" | "has" | "ders"
  cuz: number;
  page: number;
}

export interface IDailyAssignment {
  id: string;
  tenantId: string;
  studentId: string;
  studentName: string;
  teacherId: string;
  teacherName: string;
  assignmentDate: string;
  isBoarderToday: boolean;
  assignedTasks: IAssignmentTaskItem[];
  completedInEtut: string[];
  completedAtHome: string[];
  status: string;
  etutTeacherNote: string | null;
  parentNote: string | null;
}

export interface ICompleteEtutAssignment {
  id: string;
  completedTaskIds: string[];
  etutTeacherNote?: string;
  finishEtut?: boolean;
}

export interface ICreateDailyAssignment {
  studentId: string;
  assignmentDate: string;
  overrideIsBoarder?: boolean;
  assignedTasks: { type: string; cuz: number; page: number }[];
}
