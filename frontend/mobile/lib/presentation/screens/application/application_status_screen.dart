import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';

import '../../../core/constants/app_colors.dart';
import '../../../core/constants/app_spacing.dart';
import '../../../core/network/api_exception.dart';
import '../../../data/models/course_application_model.dart';
import '../../providers/auth_provider.dart';
import '../../providers/data_providers.dart';
import '../../widgets/message_view.dart';
import 'course_info_fields.dart';

/// Onay bekleyen veya reddedilen kursun müdürü: başvuru durumu, red gerekçesi, düzeltip yeniden gönderme.
/// Onaylanmışsa oturumun kurs durumu güncellenir ve yönlendirici uygulamayı açar.
class ApplicationStatusScreen extends ConsumerStatefulWidget {
  const ApplicationStatusScreen({super.key});

  @override
  ConsumerState<ApplicationStatusScreen> createState() => _ApplicationStatusScreenState();
}

class _ApplicationStatusScreenState extends ConsumerState<ApplicationStatusScreen> {
  CourseApplicationStatus? _status;
  CourseInfoControllers? _editing;
  bool _loading = true;
  bool _busy = false;
  String? _error;

  @override
  void initState() {
    super.initState();
    _load();
  }

  @override
  void dispose() {
    _editing?.dispose();
    super.dispose();
  }

  Future<void> _load() async {
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final status = await ref.read(courseApplicationRepositoryProvider).getMine();
      if (!mounted) return;
      final session = ref.read(authProvider).session;
      if (session != null && session.tenantStatus != status.status) {
        // Onaylandıysa yönlendirici öğrenci listesine geçer
        await ref.read(authProvider.notifier).signIn(session.withTenantStatus(status.status));
      }
      if (mounted) setState(() => _status = status);
    } on ApiException catch (e) {
      if (mounted) setState(() => _error = e.message);
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  Future<void> _resubmit() async {
    final editing = _editing!;
    final problem = editing.validate();
    if (problem != null) {
      setState(() => _error = problem);
      return;
    }
    setState(() {
      _busy = true;
      _error = null;
    });
    try {
      await ref.read(courseApplicationRepositoryProvider).resubmit(editing.toInfo());
      editing.dispose();
      _editing = null;
      await _load();
    } on ApiException catch (e) {
      setState(() => _error = e.message);
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  Future<void> _signOut() => ref.read(authProvider.notifier).signOut();

  @override
  Widget build(BuildContext context) {
    final status = _status;
    return Scaffold(
      appBar: AppBar(
        title: const Text('Başvuru durumu'),
        actions: [IconButton(onPressed: _signOut, tooltip: 'Çıkış yap', icon: const Icon(Icons.logout))],
      ),
      body: SafeArea(
        child: _loading && status == null
            ? const Center(child: CircularProgressIndicator())
            : status == null
                ? MessageView(message: _error ?? 'Başvuru bilgisi alınamadı.', onRetry: _load)
                : RefreshIndicator(
                    onRefresh: _load,
                    child: ListView(
                      padding: const EdgeInsets.all(AppSpacing.xl),
                      children: _editing != null ? _editView(_editing!) : _statusView(status),
                    ),
                  ),
      ),
    );
  }

  List<Widget> _statusView(CourseApplicationStatus s) {
    final place = [s.district, s.city].where((p) => p != null && p.isNotEmpty).join(' / ');
    return [
      Icon(s.isRejected ? Icons.cancel_outlined : Icons.hourglass_top,
          size: 56, color: s.isRejected ? AppColors.red : AppColors.gold),
      const SizedBox(height: AppSpacing.md),
      Text(s.isRejected ? 'Başvurunuz onaylanmadı' : 'Kursunuz onay bekliyor',
          textAlign: TextAlign.center, style: const TextStyle(fontSize: 22, fontWeight: FontWeight.w800)),
      const SizedBox(height: AppSpacing.sm),
      Text(
        s.isRejected
            ? 'Bilgilerinizi düzeltip yeniden gönderebilirsiniz.'
            : 'Platform yöneticisi başvurunuzu inceliyor. Onaylanınca uygulama kendiliğinden açılır'
                '${s.contactEmail != null ? ' ve ${s.contactEmail} adresine e-posta gelir' : ''}.',
        textAlign: TextAlign.center,
        style: const TextStyle(color: AppColors.muted, fontSize: 15),
      ),
      if (s.isRejected && s.rejectionReason != null) ...[
        const SizedBox(height: AppSpacing.lg),
        Container(
          padding: const EdgeInsets.all(AppSpacing.md),
          decoration: BoxDecoration(color: const Color(0xFFFDECEC), borderRadius: BorderRadius.circular(12)),
          child: Text('Gerekçe: ${s.rejectionReason}', style: const TextStyle(color: AppColors.red, fontSize: 15)),
        ),
      ],
      const SizedBox(height: AppSpacing.xl),
      Card(
        child: Padding(
          padding: const EdgeInsets.all(AppSpacing.md),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(s.courseName, style: const TextStyle(fontSize: 17, fontWeight: FontWeight.w800)),
              if (place.isNotEmpty) Text(place),
              if (s.address != null) Text(s.address!, style: const TextStyle(color: AppColors.muted)),
              if (s.submittedAt != null)
                Text('Gönderildi: ${DateFormat('d MMMM y HH:mm', 'tr_TR').format(s.submittedAt!)}',
                    style: const TextStyle(color: AppColors.muted, fontSize: 12)),
            ],
          ),
        ),
      ),
      if (_error != null) Padding(padding: const EdgeInsets.only(top: AppSpacing.md), child: Text(_error!, style: const TextStyle(color: AppColors.red))),
      const SizedBox(height: AppSpacing.lg),
      if (s.isRejected)
        FilledButton(
          onPressed: () => setState(() => _editing = CourseInfoControllers(initial: s)),
          child: const Text('Bilgileri düzenle ve yeniden gönder'),
        )
      else
        OutlinedButton.icon(
          onPressed: _loading ? null : _load,
          icon: const Icon(Icons.refresh),
          label: Text(_loading ? 'Bakılıyor...' : 'Durumu yenile'),
        ),
    ];
  }

  List<Widget> _editView(CourseInfoControllers editing) => [
        const Text('Kurs bilgilerini düzeltin', style: TextStyle(fontSize: 20, fontWeight: FontWeight.w800)),
        const SizedBox(height: AppSpacing.lg),
        CourseInfoFields(controllers: editing),
        if (_error != null) Padding(padding: const EdgeInsets.only(top: AppSpacing.md), child: Text(_error!, style: const TextStyle(color: AppColors.red))),
        const SizedBox(height: AppSpacing.lg),
        FilledButton(onPressed: _busy ? null : _resubmit, child: Text(_busy ? 'Gönderiliyor...' : 'Yeniden gönder')),
        TextButton(
          onPressed: _busy ? null : () => setState(() {
            editing.dispose();
            _editing = null;
            _error = null;
          }),
          child: const Text('Vazgeç'),
        ),
      ];
}
