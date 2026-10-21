import '../../core/network/api_client.dart';
import '../models/session_model.dart';

/// E-posta + tek kullanımlık kod ile giriş (backend: /api/auth/send-email-code, verify-email-code).
class AuthRepository {
  AuthRepository(this._api);

  final ApiClient _api;

  Future<void> sendEmailCode(String email) =>
      _api.postVoid('/api/auth/send-email-code', body: {'email': email.trim()});

  Future<LoginResult> verifyEmailCode({required String email, required String code, String? tenantId}) {
    return _api.post(
      '/api/auth/verify-email-code',
      body: {'email': email.trim(), 'code': code, 'tenantId': ?tenantId},
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
