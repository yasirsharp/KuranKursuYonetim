/// Backend: LessonDto.
class LessonModel {
  const LessonModel({
    required this.id,
    required this.studentId,
    required this.teacherName,
    required this.lessonDate,
    required this.isNewPage,
    required this.performance,
    required this.homeworkStatus,
    this.cuzNo,
    this.pageNo,
    this.surahName,
    this.teacherNotes,
  });

  final String id;
  final String studentId;
  final String teacherName;
  final DateTime lessonDate;
  final int? cuzNo;
  final int? pageNo;
  final String? surahName;
  final bool isNewPage;
  final String performance;
  final String homeworkStatus;
  final String? teacherNotes;

  factory LessonModel.fromJson(Map<String, dynamic> json) => LessonModel(
        id: json['id'] as String,
        studentId: json['studentId'] as String,
        teacherName: json['teacherName'] as String? ?? '',
        lessonDate: DateTime.parse(json['lessonDate'] as String),
        cuzNo: (json['cuzNo'] as num?)?.toInt(),
        pageNo: (json['pageNo'] as num?)?.toInt(),
        surahName: (json['surahName'] as String?)?.trim().isEmpty ?? true ? null : json['surahName'] as String,
        isNewPage: json['isNewPage'] as bool? ?? false,
        performance: json['performance'] as String? ?? '',
        homeworkStatus: json['homeworkStatus'] as String? ?? '',
        teacherNotes: json['teacherNotes'] as String?,
      );
}

/// Yeni ders kaydı (backend: CreateLessonDto). Cüz ve sure backend'de sayfadan hesaplanır.
class NewLesson {
  const NewLesson({
    required this.studentId,
    required this.lessonDate,
    required this.pageNo,
    required this.isNewPage,
    required this.performance,
    required this.homeworkStatus,
    this.teacherNotes,
  });

  final String studentId;
  final String lessonDate;
  final int pageNo;
  final bool isNewPage;
  final String performance;
  final String homeworkStatus;
  final String? teacherNotes;

  Map<String, dynamic> toJson() => {
        'studentId': studentId,
        'lessonDate': lessonDate,
        'pageNo': pageNo,
        'isNewPage': isNewPage,
        'performance': performance,
        'homeworkStatus': homeworkStatus,
        if (teacherNotes != null && teacherNotes!.trim().isNotEmpty) 'teacherNotes': teacherNotes!.trim(),
      };
}

/// Backend: AttendanceDto.
class AttendanceModel {
  const AttendanceModel({required this.id, required this.studentId, required this.date, required this.status, this.notes});

  final String id;
  final String studentId;
  final DateTime date;
  final String status;
  final String? notes;

  factory AttendanceModel.fromJson(Map<String, dynamic> json) => AttendanceModel(
        id: json['id'] as String,
        studentId: json['studentId'] as String,
        date: DateTime.parse(json['date'] as String),
        status: json['status'] as String? ?? '',
        notes: json['notes'] as String?,
      );
}
