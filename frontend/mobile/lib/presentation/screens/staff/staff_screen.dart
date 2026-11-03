import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';
import 'package:qr_flutter/qr_flutter.dart';
import 'package:share_plus/share_plus.dart';

import '../../../core/constants/app_colors.dart';
import '../../../core/constants/app_spacing.dart';
import '../../../core/network/api_exception.dart';
import '../../../core/utils/validators.dart';
import '../../../data/models/invitation_model.dart';
import '../../../data/repositories/invitation_repository.dart';
import '../../providers/auth_provider.dart';
import '../../providers/data_providers.dart';
import '../../widgets/message_view.dart';
import '../join/join_screen.dart' show invitationRoleLabels;

/// Paylaşılan davet mesajı (WhatsApp vb.).
String invitationShareText(String role, String link) =>
    'Hafız Takip: kursumuza ${(invitationRoleLabels[role] ?? role).toLowerCase()} olarak katılmak için '
    'bu bağlantıya dokunun ve bilgilerinizi yazın:\n$link';

/// Personel ve davetler (kurs müdürü, başhoca): katılım isteklerini onaylama ve rol başına davet bağlantısı/karekod.
class StaffScreen extends ConsumerStatefulWidget {
  const StaffScreen({super.key});

  @override
  ConsumerState<StaffScreen> createState() => _StaffScreenState();
}

class _StaffScreenState extends ConsumerState<StaffScreen> {
  bool _busy = false;

  List<String> get _invitableRoles =>
      ref.read(authProvider).session?.role == 'tenant_admin' ? const ['head_teacher', 'assistant_teacher'] : const ['assistant_teacher'];

  void _refresh() {
    ref.invalidate(staffInvitationsProvider);
    ref.invalidate(pendingJoinRequestsProvider);
  }

  Future<void> _run(Future<void> Function() action, String success) async {
    setState(() => _busy = true);
    final messenger = ScaffoldMessenger.of(context);
    try {
      await action();
      messenger.showSnackBar(SnackBar(content: Text(success)));
      _refresh();
    } on ApiException catch (e) {
      messenger.showSnackBar(SnackBar(content: Text(e.message), backgroundColor: AppColors.red));
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  Future<bool> _confirm(String title, String text, String action) async =>
      await showDialog<bool>(
        context: context,
        builder: (c) => AlertDialog(
          title: Text(title),
          content: Text(text),
          actions: [
            TextButton(onPressed: () => Navigator.pop(c, false), child: const Text('Vazgeç')),
            TextButton(onPressed: () => Navigator.pop(c, true), child: Text(action)),
          ],
        ),
      ) ??
      false;

  Future<void> _reject(JoinRequestModel request) async {
    final reason = TextEditingController();
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (c) => AlertDialog(
        title: Text('${request.fullName} reddedilsin mi?'),
        content: TextField(
          controller: reason,
          maxLength: 500,
          decoration: const InputDecoration(labelText: 'Gerekçe (isteğe bağlı)'),
        ),
        actions: [
          TextButton(onPressed: () => Navigator.pop(c, false), child: const Text('Vazgeç')),
          TextButton(onPressed: () => Navigator.pop(c, true), child: const Text('Reddet')),
        ],
      ),
    );
    final text = reason.text.trim();
    reason.dispose();
    if (confirmed != true) return;
    await _run(() => ref.read(invitationRepositoryProvider).reject(request.id, reason: text.isEmpty ? null : text),
        'Katılım isteği reddedildi.');
  }

  @override
  Widget build(BuildContext context) {
    final invitations = ref.watch(staffInvitationsProvider);
    final requests = ref.watch(pendingJoinRequestsProvider);

    return Scaffold(
      appBar: AppBar(title: const Text('Personel ve davetler')),
      body: RefreshIndicator(
        onRefresh: () async {
          _refresh();
          await ref.read(pendingJoinRequestsProvider.future);
        },
        child: ListView(
          padding: const EdgeInsets.all(AppSpacing.lg),
          children: [
            ...requests.when(
              data: (list) => list.isEmpty
                  ? const <Widget>[]
                  : [
                      _SectionTitle('Katılım istekleri (${list.length})'),
                      const Text('Onaylayınca telefon numarasıyla giriş yapabilirler.',
                          style: TextStyle(color: AppColors.muted)),
                      const SizedBox(height: AppSpacing.sm),
                      for (final r in list)
                        _RequestCard(
                          request: r,
                          busy: _busy,
                          onApprove: () => _run(() => ref.read(invitationRepositoryProvider).approve(r.id),
                              '${r.fullName} personel olarak eklendi.'),
                          onReject: () => _reject(r),
                        ),
                      const SizedBox(height: AppSpacing.lg),
                    ],
              loading: () => const [LinearProgressIndicator()],
              error: (e, _) => [MessageView(message: e is ApiException ? e.message : 'İstekler alınamadı.', onRetry: _refresh)],
            ),
            const _SectionTitle('Davet et'),
            const Text(
              'Bağlantıyı WhatsApp ile gönderin ya da karekodu gösterin. Gelen kişi bilgilerini yazar, '
              'telefonunu SMS koduyla doğrular; siz onaylayınca kursa eklenir.',
              style: TextStyle(color: AppColors.muted),
            ),
            const SizedBox(height: AppSpacing.md),
            ...invitations.when(
              data: (list) => [
                for (final role in _invitableRoles)
                  _InvitationCard(
                    role: role,
                    invitation: list.where((i) => i.role == role).firstOrNull,
                    busy: _busy,
                    onCreate: (renew) async {
                      if (renew &&
                          !await _confirm('Yeni bağlantı oluşturulsun mu?',
                              'Eski bağlantı ve karekod artık çalışmaz.', 'Oluştur')) {
                        return;
                      }
                      await _run(() => ref.read(invitationRepositoryProvider).create(role), 'Davet bağlantısı hazır.');
                    },
                    onRevoke: (invitation) async {
                      if (!await _confirm('Davet iptal edilsin mi?', 'Bağlantıyı alanlar artık katılamaz.', 'İptal et')) {
                        return;
                      }
                      await _run(() => ref.read(invitationRepositoryProvider).revoke(invitation.id), 'Davet iptal edildi.');
                    },
                  ),
              ],
              loading: () => const [Center(child: Padding(padding: EdgeInsets.all(24), child: CircularProgressIndicator()))],
              error: (e, _) => [MessageView(message: e is ApiException ? e.message : 'Davetler alınamadı.', onRetry: _refresh)],
            ),
          ],
        ),
      ),
    );
  }
}

class _SectionTitle extends StatelessWidget {
  const _SectionTitle(this.text);

  final String text;

  @override
  Widget build(BuildContext context) => Padding(
        padding: const EdgeInsets.only(bottom: AppSpacing.xs),
        child: Text(text, style: const TextStyle(fontSize: 18, fontWeight: FontWeight.w800, color: AppColors.green)),
      );
}

class _RequestCard extends StatelessWidget {
  const _RequestCard({required this.request, required this.busy, required this.onApprove, required this.onReject});

  final JoinRequestModel request;
  final bool busy;
  final VoidCallback onApprove;
  final VoidCallback onReject;

  @override
  Widget build(BuildContext context) {
    return Card(
      color: AppColors.goldPale,
      child: Padding(
        padding: const EdgeInsets.all(AppSpacing.md),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Text(request.fullName, style: const TextStyle(fontSize: 17, fontWeight: FontWeight.w800)),
            const SizedBox(height: 2),
            Text('${invitationRoleLabels[request.role] ?? request.role} · ${formatPhone(request.phoneNumber)}'),
            if (request.email != null) Text(request.email!, style: const TextStyle(color: AppColors.muted)),
            Text(DateFormat('d MMMM y HH:mm', 'tr_TR').format(request.createdAt),
                style: const TextStyle(color: AppColors.muted, fontSize: 12)),
            const SizedBox(height: AppSpacing.md),
            Row(
              children: [
                Expanded(child: FilledButton(onPressed: busy ? null : onApprove, child: const Text('Onayla'))),
                const SizedBox(width: AppSpacing.sm),
                Expanded(child: OutlinedButton(onPressed: busy ? null : onReject, child: const Text('Reddet'))),
              ],
            ),
          ],
        ),
      ),
    );
  }
}

class _InvitationCard extends StatelessWidget {
  const _InvitationCard({
    required this.role,
    required this.invitation,
    required this.busy,
    required this.onCreate,
    required this.onRevoke,
  });

  final String role;
  final InvitationModel? invitation;
  final bool busy;
  final ValueChanged<bool> onCreate;
  final ValueChanged<InvitationModel> onRevoke;

  @override
  Widget build(BuildContext context) {
    final label = invitationRoleLabels[role] ?? role;
    final current = invitation;
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(AppSpacing.md),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Text('$label daveti', style: const TextStyle(fontSize: 16, fontWeight: FontWeight.w800)),
            if (current == null) ...[
              const SizedBox(height: AppSpacing.md),
              FilledButton.icon(
                onPressed: busy ? null : () => onCreate(false),
                icon: const Icon(Icons.qr_code_2),
                label: Text('$label davet bağlantısı oluştur'),
              ),
            ] else
              ..._active(context, current, label),
          ],
        ),
      ),
    );
  }

  List<Widget> _active(BuildContext context, InvitationModel current, String label) {
    final link = InvitationRepository.linkFor(current.code);
    return [
      Text('${DateFormat('d MMMM y', 'tr_TR').format(current.expiresAt)} tarihine kadar geçerli',
          style: const TextStyle(color: AppColors.muted, fontSize: 12)),
      const SizedBox(height: AppSpacing.md),
      Center(
        child: Container(
          padding: const EdgeInsets.all(10),
          decoration: BoxDecoration(color: Colors.white, borderRadius: BorderRadius.circular(12)),
          child: QrImageView(
            data: link,
            size: 200,
            semanticsLabel: '$label davet karekodu',
            eyeStyle: const QrEyeStyle(eyeShape: QrEyeShape.square, color: AppColors.green),
            dataModuleStyle: const QrDataModuleStyle(dataModuleShape: QrDataModuleShape.square, color: AppColors.green),
          ),
        ),
      ),
      const SizedBox(height: AppSpacing.sm),
      SelectableText(link, textAlign: TextAlign.center, style: const TextStyle(color: AppColors.green, fontSize: 13)),
      const SizedBox(height: AppSpacing.md),
      FilledButton.icon(
        onPressed: () => SharePlus.instance.share(ShareParams(text: invitationShareText(role, link), subject: '$label daveti')),
        icon: const Icon(Icons.share),
        label: const Text('WhatsApp ile gönder / paylaş'),
      ),
      const SizedBox(height: AppSpacing.sm),
      OutlinedButton.icon(
        onPressed: () async {
          await Clipboard.setData(ClipboardData(text: link));
          if (context.mounted) {
            ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Bağlantı kopyalandı.')));
          }
        },
        icon: const Icon(Icons.copy),
        label: const Text('Bağlantıyı kopyala'),
      ),
      Row(
        children: [
          Expanded(child: TextButton(onPressed: busy ? null : () => onCreate(true), child: const Text('Yeni bağlantı'))),
          Expanded(
            child: TextButton(
              onPressed: busy ? null : () => onRevoke(current),
              child: const Text('İptal et', style: TextStyle(color: AppColors.red)),
            ),
          ),
        ],
      ),
    ];
  }
}
