import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../core/constants/api_constants.dart';
import '../../../core/constants/app_colors.dart';
import '../../../core/constants/app_spacing.dart';
import '../../../core/constants/labels.dart';
import '../../providers/auth_provider.dart';
import '../../providers/onboarding_provider.dart';
import '../onboarding/onboarding_screen.dart';

class ProfileScreen extends ConsumerWidget {
  const ProfileScreen({super.key});

  Future<void> _confirmSignOut(BuildContext context, WidgetRef ref) async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (dialogContext) => AlertDialog(
        title: const Text('Çıkış yapılsın mı?'),
        content: const Text('Tekrar girmek için telefonunuza veya e-postanıza yeni kod gönderilir.'),
        actions: [
          TextButton(onPressed: () => Navigator.pop(dialogContext, false), child: const Text('Vazgeç')),
          TextButton(onPressed: () => Navigator.pop(dialogContext, true), child: const Text('Çıkış yap')),
        ],
      ),
    );
    if (confirmed == true) await ref.read(authProvider.notifier).signOut();
  }

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final session = ref.watch(authProvider).session;

    return Scaffold(
      appBar: AppBar(title: const Text('Profil')),
      body: ListView(
        padding: const EdgeInsets.all(AppSpacing.lg),
        children: [
          Card(
            child: ListTile(
              contentPadding: const EdgeInsets.all(AppSpacing.md),
              leading: CircleAvatar(
                radius: 24,
                backgroundColor: AppColors.greenPale,
                foregroundColor: AppColors.green,
                child: Text(
                  (session?.firstName.isNotEmpty ?? false) ? session!.firstName[0] : '?',
                  style: const TextStyle(fontWeight: FontWeight.w800, fontSize: 18),
                ),
              ),
              title: Text(session?.fullName ?? '', style: const TextStyle(fontWeight: FontWeight.w800)),
              subtitle: Text(RoleLabels.labels[session?.role] ?? session?.role ?? ''),
            ),
          ),
          if (session?.role == 'tenant_admin' || session?.role == 'head_teacher') ...[
            const SizedBox(height: AppSpacing.md),
            Card(
              child: ListTile(
                leading: const Icon(Icons.group_add, color: AppColors.green),
                title: const Text('Personel ve davetler', style: TextStyle(fontWeight: FontWeight.w700)),
                subtitle: const Text('Karekod veya WhatsApp ile hoca ekleyin'),
                trailing: const Icon(Icons.chevron_right),
                onTap: () => context.push('/profile/staff'),
              ),
            ),
          ],
          const SizedBox(height: AppSpacing.lg),
          OutlinedButton.icon(
            onPressed: () => Navigator.of(context).push(MaterialPageRoute<void>(
              fullscreenDialog: true,
              builder: (_) => const _ReplayOnboarding(),
            )),
            icon: const Icon(Icons.auto_awesome, color: AppColors.gold),
            label: const Text('Tanıtımı tekrar izle'),
          ),
          const SizedBox(height: AppSpacing.md),
          OutlinedButton.icon(
            onPressed: () => _confirmSignOut(context, ref),
            icon: const Icon(Icons.logout, color: AppColors.red),
            label: const Text('Çıkış yap', style: TextStyle(color: AppColors.red)),
          ),
          const SizedBox(height: AppSpacing.xxl),
          const Text(
            'Sunucu: ${ApiConstants.baseUrl}',
            textAlign: TextAlign.center,
            style: TextStyle(fontSize: 12, color: AppColors.muted),
          ),
        ],
      ),
    );
  }
}

/// Giriş yapmış kullanıcı için tanıtımı yeniden gösterir; bitince profil ekranına döner.
class _ReplayOnboarding extends ConsumerWidget {
  const _ReplayOnboarding();

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    ref.listen(onboardingProvider, (previous, next) {
      if (previous == false && next == true) Navigator.of(context).pop();
    });
    return const _ReplayHost();
  }
}

class _ReplayHost extends ConsumerStatefulWidget {
  const _ReplayHost();

  @override
  ConsumerState<_ReplayHost> createState() => _ReplayHostState();
}

class _ReplayHostState extends ConsumerState<_ReplayHost> {
  @override
  void initState() {
    super.initState();
    Future.microtask(() => ref.read(onboardingProvider.notifier).replay());
  }

  @override
  Widget build(BuildContext context) => const OnboardingScreen();
}

