import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/network/api_client.dart';
import '../../core/storage/session_storage.dart';
import '../../data/models/session_model.dart';
import '../../data/repositories/auth_repository.dart';

enum AuthStatus { restoring, signedOut, signedIn }

class AuthState {
  const AuthState(this.status, [this.session]);

  final AuthStatus status;
  final SessionModel? session;
}

final sessionStorageProvider = Provider<SessionStorage>((ref) => SessionStorage());

final apiClientProvider = Provider<ApiClient>((ref) => ApiClient(sessionHolder: ref.read(authProvider.notifier)));

final authRepositoryProvider = Provider<AuthRepository>((ref) => AuthRepository(ref.read(apiClientProvider)));

final authProvider = NotifierProvider<AuthNotifier, AuthState>(AuthNotifier.new);

/// Oturumun tek sahibi: açılışta kayıtlı oturumu geri yükler, girişte kaydeder, çıkışta siler.
class AuthNotifier extends Notifier<AuthState> implements SessionHolder {
  @override
  AuthState build() {
    Future.microtask(_restore);
    return const AuthState(AuthStatus.restoring);
  }

  @override
  SessionModel? get currentSession => state.session;

  Future<void> _restore() async {
    final session = await ref.read(sessionStorageProvider).read();
    state = session == null ? const AuthState(AuthStatus.signedOut) : AuthState(AuthStatus.signedIn, session);
  }

  Future<void> signIn(SessionModel session) async {
    await ref.read(sessionStorageProvider).write(session);
    state = AuthState(AuthStatus.signedIn, session);
  }

  Future<void> signOut() async {
    final session = state.session;
    state = const AuthState(AuthStatus.signedOut);
    await ref.read(sessionStorageProvider).clear();
    if (session != null) await ref.read(authRepositoryProvider).logout(session.refreshToken);
  }

  @override
  Future<void> onSessionRefreshed(SessionModel session) async {
    await ref.read(sessionStorageProvider).write(session);
    state = AuthState(AuthStatus.signedIn, session);
  }

  @override
  Future<void> onSessionExpired() async {
    await ref.read(sessionStorageProvider).clear();
    state = const AuthState(AuthStatus.signedOut);
  }
}
