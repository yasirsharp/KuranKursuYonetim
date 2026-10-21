import '../../core/constants/quran.dart';

/// Backend: StudentDto.
class StudentModel {
  const StudentModel({
    required this.id,
    required this.firstName,
    required this.lastName,
    required this.groupName,
    required this.currentPage,
    required this.completedPages,
    required this.isHafiz,
    required this.status,
    this.groupId,
  });

  final String id;
  final String? groupId;
  final String groupName;
  final String firstName;
  final String lastName;

  /// Son ezberlenen mushaf sayfası (0: henüz başlamadı).
  final int currentPage;
  final int completedPages;
  final bool isHafiz;
  final String status;

  String get fullName => '$firstName $lastName'.trim();

  String get initials {
    final first = firstName.isNotEmpty ? firstName[0] : '';
    final last = lastName.isNotEmpty ? lastName[0] : '';
    return (first + last).toUpperCase();
  }

  double get progress => (completedPages / Quran.totalPages).clamp(0, 1).toDouble();

  factory StudentModel.fromJson(Map<String, dynamic> json) => StudentModel(
        id: json['id'] as String,
        groupId: json['groupId'] as String?,
        groupName: json['groupName'] as String? ?? '',
        firstName: json['firstName'] as String? ?? '',
        lastName: json['lastName'] as String? ?? '',
        currentPage: (json['currentPage'] as num?)?.toInt() ?? 0,
        completedPages: (json['completedPages'] as num?)?.toInt() ?? 0,
        isHafiz: json['isHafiz'] as bool? ?? false,
        status: json['status'] as String? ?? '',
      );
}
