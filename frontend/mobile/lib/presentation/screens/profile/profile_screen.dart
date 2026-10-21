import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/constants/api_constants.dart';
import '../../../core/constants/app_colors.dart';
import '../../../core/constants/app_spacing.dart';
import '../../../core/constants/labels.dart';
import '../../providers/auth_provider.dart';

class ProfileScreen extends ConsumerWidget {
  const ProfileScreen({super.key});

  Future<void> _confirmSignOut(BuildContext context, WidgetRef ref) async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (dialogContext) => AlertDialog(
        title: const Text('Çıkış yapılsın mı?'),
        content: const Text('Tekrar girmek için e-postanıza yeni kod gönderilir.'),
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
          const SizedBox(height: AppSpacing.lg),
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
