import '../../core/constants/api_constants.dart';
import '../../core/network/api_client.dart';
import '../models/invitation_model.dart';

/// Davetle personel katılımı (backend: /api/invitations yönetici, /api/join/{kod} herkese açık).
class InvitationRepository {
  InvitationRepository(this._api);

  final ApiClient _api;

  static String linkFor(String code) => '${ApiConstants.webBaseUrl}/katil/$code';

  // ── Herkese açık ──

  Future<InvitationPublicModel> getPublic(String code) => _api.get(
        '/api/join/${Uri.encodeComponent(code)}',
        parse: (data) => InvitationPublicModel.fromJson(data as Map<String, dynamic>),
      );

  Future<void> sendJoinCode(String code, String phone) =>
      _api.postVoid('/api/join/${Uri.encodeComponent(code)}/send-code', body: {'phoneNumber': phone});

  Future<void> join(
    String code, {
    required String firstName,
    required String lastName,
    required String phone,
    String? email,
    required String otp,
  }) =>
      _api.postVoid('/api/join/${Uri.encodeComponent(code)}', body: {
        'firstName': firstName,
        'lastName': lastName,
        'phoneNumber': phone,
        'email': ?email,
        'code': otp,
      });

  // ── Yönetici ──

  Future<List<InvitationModel>> getActive() => _api.get(
        '/api/invitations',
        parse: (data) => (data as List).map((e) => InvitationModel.fromJson(e as Map<String, dynamic>)).toList(),
      );

  Future<InvitationModel> create(String role) => _api.post(
        '/api/invitations',
        body: {'role': role},
        parse: (data) => InvitationModel.fromJson(data as Map<String, dynamic>),
      );

  Future<void> revoke(String id) => _api.delete('/api/invitations/$id');

  Future<List<JoinRequestModel>> getPendingRequests() => _api.get(
        '/api/invitations/join-requests',
        parse: (data) => (data as List).map((e) => JoinRequestModel.fromJson(e as Map<String, dynamic>)).toList(),
      );

  Future<void> approve(String id) => _api.postVoid('/api/invitations/join-requests/$id/approve');

  Future<void> reject(String id, {String? reason}) =>
      _api.postVoid('/api/invitations/join-requests/$id/reject', body: {'reason': ?reason});
}
