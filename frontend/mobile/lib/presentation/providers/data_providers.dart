import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/utils/date_utils.dart';
import '../../data/models/lesson_model.dart';
import '../../data/models/student_model.dart';
import '../../data/repositories/student_repository.dart';
import 'auth_provider.dart';

final studentRepositoryProvider = Provider((ref) => StudentRepository(ref.read(apiClientProvider)));
final lessonRepositoryProvider = Provider((ref) => LessonRepository(ref.read(apiClientProvider)));
final attendanceRepositoryProvider = Provider((ref) => AttendanceRepository(ref.read(apiClientProvider)));

/// Cihazın yerel günü ("yyyy-MM-dd"); test için değiştirilebilir.
final todayProvider = Provider<String>((ref) => apiDate(DateTime.now()));

final studentsProvider = FutureProvider.autoDispose<List<StudentModel>>(
  (ref) => ref.read(studentRepositoryProvider).getActiveStudents(),
);

/// Bugün ders kaydı girilen öğrenciler (öğrenci → o günün son dersi).
final todayLessonsProvider = FutureProvider.autoDispose<Map<String, LessonModel>>((ref) async {
  final lessons = await ref.read(lessonRepositoryProvider).getForDay(ref.watch(todayProvider));
  return {for (final lesson in lessons) lesson.studentId: lesson};
});

final studentDetailProvider = FutureProvider.autoDispose.family<StudentModel, String>(
  (ref, id) => ref.read(studentRepositoryProvider).getById(id),
);

final studentLessonsProvider = FutureProvider.autoDispose.family<List<LessonModel>, String>(
  (ref, id) => ref.read(studentRepositoryProvider).getRecentLessons(id),
);

final studentAttendanceProvider = FutureProvider.autoDispose.family<List<AttendanceModel>, String>(
  (ref, id) => ref.read(studentRepositoryProvider).getRecentAttendance(id),
);
