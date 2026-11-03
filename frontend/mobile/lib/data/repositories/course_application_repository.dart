import '../../core/network/api_client.dart';
import '../models/course_application_model.dart';
import '../models/session_model.dart';

/// Kurs başvurusu (backend: /api/course-applications). Başvuru kursu "onay bekliyor" açar ve oturum döner.
class CourseApplicationRepository {
  CourseApplicationRepository(this._api);

  final ApiClient _api;

  Future<void> sendCode(String phone) => _api.postVoid('/api/course-applications/send-code', body: {'phoneNumber': phone});

  Future<SessionModel> submit({
    required String phone,
    required String code,
    required CourseInfo info,
    required String firstName,
    required String lastName,
  }) =>
      _api.post(
        '/api/course-applications',
        body: {
          ...info.toJson(),
          'phoneNumber': phone,
          'code': code,
          'adminFirstName': firstName,
          'adminLastName': lastName,
        },
        parse: (data) => SessionModel.fromJson(data as Map<String, dynamic>),
      );

  Future<CourseApplicationStatus> getMine() => _api.get(
        '/api/course-applications/me',
        parse: (data) => CourseApplicationStatus.fromJson(data as Map<String, dynamic>),
      );

  Future<void> resubmit(CourseInfo info) => _api.put('/api/course-applications/me', body: info.toJson());
}
