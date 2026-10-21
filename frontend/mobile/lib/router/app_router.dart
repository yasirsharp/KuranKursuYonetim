import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../core/constants/labels.dart';
import '../presentation/providers/auth_provider.dart';
import '../presentation/screens/attendance/attendance_screen.dart';
import '../presentation/screens/auth/code_screen.dart';
import '../presentation/screens/auth/login_screen.dart';
import '../presentation/screens/profile/profile_screen.dart';
import '../presentation/screens/shell/app_shell.dart';
import '../presentation/screens/students/student_detail_screen.dart';
import '../presentation/screens/students/students_screen.dart';

/// Kullanıcı bu oturumla uygulamayı kullanabilir mi? Kullanamıyorsa nedeni.
String? blockedReason(AuthState auth) {
  final session = auth.session;
  if (session == null) return null;
  if (!RoleLabels.staffRoles.contains(session.role)) {
    return 'Mobil uygulama şimdilik hocalar ve kurs müdürleri içindir.';
  }
  if (!session.isTenantActive) {
    return session.tenantStatus == 'pending'
        ? 'Kursunuzun başvurusu onay bekliyor. Onaylanınca uygulamayı kullanabilirsiniz.'
        : 'Kursunuzun hesabı şu anda kullanıma kapalı. Platform yöneticisiyle iletişime geçin.';
  }
  return null;
}

/// Oturum durumu değişince yönlendirme yeniden değerlendirilir.
String? authRedirect(AuthState auth, String location) {
  final inLogin = location.startsWith('/login');
  switch (auth.status) {
    case AuthStatus.restoring:
      return location == '/splash' ? null : '/splash';
    case AuthStatus.signedOut:
      return inLogin ? null : '/login';
    case AuthStatus.signedIn:
      if (blockedReason(auth) != null) return location == '/blocked' ? null : '/blocked';
      if (inLogin || location == '/splash' || location == '/blocked') return '/students';
      return null;
  }
}

final routerProvider = Provider<GoRouter>((ref) {
  final authListenable = ValueNotifier<AuthState>(ref.read(authProvider));
  ref.listen(authProvider, (_, next) => authListenable.value = next);
  ref.onDispose(authListenable.dispose);

  return GoRouter(
    initialLocation: '/splash',
    refreshListenable: authListenable,
    redirect: (context, state) => authRedirect(authListenable.value, state.matchedLocation),
    routes: [
      GoRoute(path: '/splash', builder: (_, _) => const Scaffold(body: Center(child: CircularProgressIndicator()))),
      GoRoute(
        path: '/login',
        builder: (_, _) => const LoginScreen(),
        routes: [
          GoRoute(path: 'code', builder: (_, state) => CodeScreen(email: state.extra as String? ?? '')),
        ],
      ),
      GoRoute(
        path: '/blocked',
        builder: (_, _) => BlockedScreen(
          message: blockedReason(authListenable.value) ?? '',
          onSignOut: () => ref.read(authProvider.notifier).signOut(),
        ),
      ),
      StatefulShellRoute.indexedStack(
        builder: (_, _, shell) => AppShell(navigationShell: shell),
        branches: [
          StatefulShellBranch(routes: [
            GoRoute(
              path: '/students',
              builder: (_, _) => const StudentsScreen(),
              routes: [
                GoRoute(path: ':id', builder: (_, state) => StudentDetailScreen(studentId: state.pathParameters['id']!)),
              ],
            ),
          ]),
          StatefulShellBranch(routes: [
            GoRoute(path: '/attendance', builder: (_, _) => const AttendanceScreen()),
          ]),
          StatefulShellBranch(routes: [
            GoRoute(path: '/profile', builder: (_, _) => const ProfileScreen()),
          ]),
        ],
      ),
    ],
  );
});
