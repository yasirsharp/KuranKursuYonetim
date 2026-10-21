import 'dart:convert';

import 'package:flutter_secure_storage/flutter_secure_storage.dart';

import '../../data/models/session_model.dart';

/// Oturum (access + refresh token) cihazın güvenli deposunda saklanır.
class SessionStorage {
  SessionStorage([FlutterSecureStorage? storage]) : _storage = storage ?? const FlutterSecureStorage();

  static const _key = 'hafiz_takip_session';

  final FlutterSecureStorage _storage;

  Future<SessionModel?> read() async {
    try {
      final raw = await _storage.read(key: _key);
      if (raw == null) return null;
      return SessionModel.fromJson(jsonDecode(raw) as Map<String, dynamic>);
    } catch (_) {
      // Bozuk kayıt: oturum yok sayılır, kullanıcı yeniden giriş yapar
      await clear();
      return null;
    }
  }

  Future<void> write(SessionModel session) => _storage.write(key: _key, value: jsonEncode(session.toJson()));

  Future<void> clear() => _storage.delete(key: _key);
}
