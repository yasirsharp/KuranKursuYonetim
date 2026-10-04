export interface IDashboardSummary {
  totalStudents: number;
  totalTeachers: number;
  totalGroups: number;
  activeStudents: number;
  hafizCount: number;
  averageCompletedPages: number;
  todayAttendanceRate: number;
  todayLessonCount: number;
}

export interface IStudentProgress {
  studentName: string;
  currentPage: number;
  completedPages: number;
  totalPages: number;
  progressPercentage: number;
  isHafiz: boolean;
  attendanceRate: number;
}

export interface IGroupOverview {
  groupName: string;
  teacherName: string;
  studentCount: number;
  averagePage: number;
  todayPresent: number;
  todayAbsent: number;
}
