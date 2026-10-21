/// Giriş yapmış kullanıcının oturumu (backend: TokenResponseDto).
class SessionModel {
  const SessionModel({
    required this.token,
    required this.refreshToken,
    required this.userId,
    required this.tenantId,
    required this.tenantStatus,
    required this.role,
    required this.firstName,
    required this.lastName,
  });

  final String token;
  final String refreshToken;
  final String userId;
  final String tenantId;
  final String tenantStatus;
  final String role;
  final String firstName;
  final String lastName;

  String get fullName => '$firstName $lastName'.trim();

  /// Onay bekleyen veya reddedilen kursun yöneticisi uygulamayı kullanamaz.
  bool get isTenantActive => tenantStatus == 'active' || tenantStatus == 'trial';

  factory SessionModel.fromJson(Map<String, dynamic> json) => SessionModel(
        token: json['token'] as String,
        refreshToken: json['refreshToken'] as String,
        userId: json['userId'] as String,
        tenantId: json['tenantId'] as String,
        tenantStatus: json['tenantStatus'] as String? ?? '',
        role: json['role'] as String? ?? '',
        firstName: json['firstName'] as String? ?? '',
        lastName: json['lastName'] as String? ?? '',
      );

  Map<String, dynamic> toJson() => {
        'token': token,
        'refreshToken': refreshToken,
        'userId': userId,
        'tenantId': tenantId,
        'tenantStatus': tenantStatus,
        'role': role,
        'firstName': firstName,
        'lastName': lastName,
      };

  /// Yenilenen token'larla aynı oturum.
  SessionModel withTokens(String token, String refreshToken) => SessionModel(
        token: token,
        refreshToken: refreshToken,
        userId: userId,
        tenantId: tenantId,
        tenantStatus: tenantStatus,
        role: role,
        firstName: firstName,
        lastName: lastName,
      );
}

/// E-posta birden fazla kursta kayıtlıysa seçilecek kurs.
class TenantOption {
  const TenantOption({required this.tenantId, required this.tenantName, required this.tenantStatus, required this.role});

  final String tenantId;
  final String tenantName;
  final String tenantStatus;
  final String role;

  factory TenantOption.fromJson(Map<String, dynamic> json) => TenantOption(
        tenantId: json['tenantId'] as String,
        tenantName: json['tenantName'] as String? ?? '',
        tenantStatus: json['tenantStatus'] as String? ?? '',
        role: json['role'] as String? ?? '',
      );
}

/// Kod doğrulamanın sonucu: ya oturum açıldı ya da kurs seçilmesi gerekiyor.
class LoginResult {
  const LoginResult.session(SessionModel this.session) : tenantOptions = const [];
  const LoginResult.selectTenant(this.tenantOptions) : session = null;

  final SessionModel? session;
  final List<TenantOption> tenantOptions;

  bool get requiresTenantSelection => session == null;
}
