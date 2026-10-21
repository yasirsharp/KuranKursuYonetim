import '../../core/network/api_client.dart';
import '../models/lesson_model.dart';
import '../models/student_model.dart';

class StudentRepository {
  StudentRepository(this._api);

  final ApiClient _api;

  /// Hocanın erişebildiği aktif öğrenciler (backend erişim kapsamını uygular). Sayfa başına en fazla 100.
  Future<List<StudentModel>> getActiveStudents() async {
    final students = await _getAllPages(_api, '/api/students', {'status': 'active'}, StudentModel.fromJson);
    students.sort((a, b) => a.fullName.compareTo(b.fullName));
    return students;
  }

  Future<StudentModel> getById(String id) =>
      _api.get('/api/students/$id', parse: (data) => StudentModel.fromJson(data as Map<String, dynamic>));

  Future<List<LessonModel>> getRecentLessons(String studentId) => _api.get(
        '/api/lessons/student/$studentId',
        query: {'page': 1, 'pageSize': 30},
        parse: (data) => _Page.fromJson(data as Map<String, dynamic>, LessonModel.fromJson).items,
      );

  Future<List<AttendanceModel>> getRecentAttendance(String studentId) => _api.get(
        '/api/attendance/student/$studentId',
        query: {'page': 1, 'pageSize': 30},
        parse: (data) => _Page.fromJson(data as Map<String, dynamic>, AttendanceModel.fromJson).items,
      );
}

class LessonRepository {
  LessonRepository(this._api);

  final ApiClient _api;

  Future<void> create(NewLesson lesson) => _api.postVoid('/api/lessons', body: lesson.toJson());

  /// Bir günün ders kayıtları (ör. bugün kimin dinlettiğini göstermek için).
  Future<List<LessonModel>> getForDay(String date) => _getAllPages(
        _api,
        '/api/lessons',
        {'from': date, 'to': date},
        LessonModel.fromJson,
      );
}

class AttendanceRepository {
  AttendanceRepository(this._api);

  final ApiClient _api;

  /// Bir günün yoklaması (ekran daha önce kaydedilen durumlarla açılır).
  Future<List<AttendanceModel>> getForDay(String date) => _getAllPages(
        _api,
        '/api/attendance',
        {'from': date, 'to': date},
        AttendanceModel.fromJson,
      );

  /// Aynı gün için daha önce alınmış yoklama varsa backend günceller.
  Future<void> saveBulk({required String date, required Map<String, String> statusByStudent}) => _api.postVoid(
        '/api/attendance/bulk',
        body: {
          'date': date,
          'attendances': [
            for (final entry in statusByStudent.entries) {'studentId': entry.key, 'status': entry.value, 'notes': ''},
          ],
        },
      );
}

/// Sayfalı bir listeyi sonuna kadar okur (sayfa başına en fazla 100, güvenlik için en çok 10 sayfa).
Future<List<T>> _getAllPages<T>(
  ApiClient api,
  String path,
  Map<String, dynamic> query,
  T Function(Map<String, dynamic>) parse,
) async {
  final items = <T>[];
  for (var page = 1; page <= 10; page++) {
    final result = await api.get(
      path,
      query: {...query, 'page': page, 'pageSize': 100},
      parse: (data) => _Page.fromJson(data as Map<String, dynamic>, parse),
    );
    items.addAll(result.items);
    if (items.length >= result.totalCount || result.items.isEmpty) break;
  }
  return items;
}

/// Backend: PagedResult (data, totalCount, page, pageSize).
class _Page<T> {
  const _Page(this.items, this.totalCount);

  final List<T> items;
  final int totalCount;

  factory _Page.fromJson(Map<String, dynamic> json, T Function(Map<String, dynamic>) parse) {
    final list = (json['data'] as List? ?? const []).map((e) => parse(e as Map<String, dynamic>)).toList();
    return _Page(list, (json['totalCount'] as num?)?.toInt() ?? list.length);
  }
}
