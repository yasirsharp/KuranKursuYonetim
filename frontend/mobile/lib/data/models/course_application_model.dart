/// Kurs bilgileri (başvuru ve yeniden gönderme).
class CourseInfo {
  const CourseInfo({required this.courseName, required this.city, required this.district, this.address, this.contactEmail});

  final String courseName;
  final String city;
  final String district;
  final String? address;
  final String? contactEmail;

  Map<String, dynamic> toJson() => {
        'courseName': courseName,
        'city': city,
        'district': district,
        'address': ?address,
        'contactEmail': ?contactEmail,
      };
}

/// Başvuru sahibinin gördüğü durum (backend: CourseApplicationStatusDto). status: pending | rejected | active | trial …
class CourseApplicationStatus {
  const CourseApplicationStatus({
    required this.courseName,
    required this.status,
    this.city,
    this.district,
    this.address,
    this.contactEmail,
    this.submittedAt,
    this.rejectionReason,
  });

  final String courseName;
  final String status;
  final String? city;
  final String? district;
  final String? address;
  final String? contactEmail;
  final DateTime? submittedAt;
  final String? rejectionReason;

  bool get isApproved => status == 'active' || status == 'trial';
  bool get isRejected => status == 'rejected';

  factory CourseApplicationStatus.fromJson(Map<String, dynamic> json) => CourseApplicationStatus(
        courseName: json['courseName'] as String? ?? '',
        status: json['status'] as String? ?? '',
        city: json['city'] as String?,
        district: json['district'] as String?,
        address: json['address'] as String?,
        contactEmail: json['contactEmail'] as String?,
        submittedAt: json['submittedAt'] == null ? null : DateTime.parse(json['submittedAt'] as String).toLocal(),
        rejectionReason: json['rejectionReason'] as String?,
      );
}
