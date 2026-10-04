export interface ILesson {
  id: string;
  tenantId: string;
  studentId: string;
  teacherId: string;
  studentName: string;
  teacherName: string;
  lessonDate: string;
  cuzNo: number | null;
  pageNo: number | null;
  surahName: string | null;
  isNewPage: boolean;
  performance: string;
  homeworkStatus: string;
  teacherNotes: string | null;
}

export interface ICreateLesson {
  studentId: string;
  teacherId: string;
  lessonDate: string;
  cuzNo?: number;
  pageNo?: number;
  surahName?: string;
  isNewPage: boolean;
  performance: string;
  homeworkStatus: string;
  teacherNotes?: string;
}

export interface IUpdateLesson {
  id: string;
  cuzNo?: number;
  pageNo?: number;
  surahName?: string;
  performance: string;
  homeworkStatus: string;
  teacherNotes?: string;
}
