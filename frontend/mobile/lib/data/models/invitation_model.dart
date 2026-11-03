/// Davetle katılabilecek roller: head_teacher (hoca) | assistant_teacher (yardımcı hoca).
class InvitationModel {
  const InvitationModel({required this.id, required this.code, required this.role, required this.expiresAt});

  final String id;
  final String code;
  final String role;
  final DateTime expiresAt;

  factory InvitationModel.fromJson(Map<String, dynamic> json) => InvitationModel(
        id: json['id'] as String,
        code: json['code'] as String,
        role: json['role'] as String,
        expiresAt: DateTime.parse(json['expiresAt'] as String).toLocal(),
      );
}

/// Herkese açık davet bilgisi: hangi kursa, hangi rolle.
class InvitationPublicModel {
  const InvitationPublicModel({
    required this.code,
    required this.courseName,
    required this.role,
    this.city,
    this.district,
  });

  final String code;
  final String courseName;
  final String role;
  final String? city;
  final String? district;

  String get place => [district, city].where((p) => p != null && p.isNotEmpty).join(' / ');

  factory InvitationPublicModel.fromJson(Map<String, dynamic> json) => InvitationPublicModel(
        code: json['code'] as String,
        courseName: json['courseName'] as String,
        role: json['role'] as String,
        city: json['city'] as String?,
        district: json['district'] as String?,
      );
}

class JoinRequestModel {
  const JoinRequestModel({
    required this.id,
    required this.firstName,
    required this.lastName,
    required this.phoneNumber,
    required this.role,
    required this.createdAt,
    this.email,
  });

  final String id;
  final String firstName;
  final String lastName;
  final String phoneNumber;
  final String? email;
  final String role;
  final DateTime createdAt;

  String get fullName => '$firstName $lastName';

  factory JoinRequestModel.fromJson(Map<String, dynamic> json) => JoinRequestModel(
        id: json['id'] as String,
        firstName: json['firstName'] as String,
        lastName: json['lastName'] as String,
        phoneNumber: json['phoneNumber'] as String,
        email: json['email'] as String?,
        role: json['role'] as String,
        createdAt: DateTime.parse(json['createdAt'] as String).toLocal(),
      );
}

/// Bağlantıdaki veya elle yazılan kod: harf/rakam dışı atılır, büyük harfe çevrilir.
String normalizeInvitationCode(String value) => value.replaceAll(RegExp(r'[^A-Za-z0-9]'), '').toUpperCase();
