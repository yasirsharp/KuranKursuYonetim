import '../../core/network/api_client.dart';
import '../models/session_model.dart';

/// Tek kullanımlık kodla giriş: telefon (SMS; /api/auth/send-otp, verify-otp)
/// veya e-posta (/api/auth/send-email-code, verify-email-code).
class AuthRepository {
  AuthRepository(this._api);

  final ApiClient _api;

  Future<void> sendCode(LoginTarget target) => target.isPhone
      ? _api.postVoid('/api/auth/send-otp', body: {'phoneNumber': target.value})
      : sendEmailCode(target.value);

  Future<LoginResult> verifyCode(LoginTarget target, {required String code, String? tenantId}) => target.isPhone
      ? _verify('/api/auth/verify-otp', {'phoneNumber': target.value, 'code': code, 'tenantId': ?tenantId})
      : verifyEmailCode(email: target.value, code: code, tenantId: tenantId);

  Future<void> sendEmailCode(String email) =>
      _api.postVoid('/api/auth/send-email-code', body: {'email': email.trim()});

  Future<LoginResult> verifyEmailCode({required String email, required String code, String? tenantId}) =>
      _verify('/api/auth/verify-email-code', {'email': email.trim(), 'code': code, 'tenantId': ?tenantId});

  Future<LoginResult> _verify(String path, Map<String, Object?> body) {
    return _api.post(
      path,
      body: body,
      parse: (data) {
        final json = data as Map<String, dynamic>;
        if (json['requiresTenantSelection'] == true) {
          final options = (json['tenants'] as List).map((t) => TenantOption.fromJson(t as Map<String, dynamic>)).toList();
          return LoginResult.selectTenant(options);
        }
        return LoginResult.session(SessionModel.fromJson(json));
      },
    );
  }

  /// Refresh token sunucuda iptal edilir; hata olsa da yerel oturum kapatılır.
  Future<void> logout(String refreshToken) async {
    try {
      await _api.postVoid('/api/auth/logout', body: {'refreshToken': refreshToken});
    } catch (_) {}
  }
}
