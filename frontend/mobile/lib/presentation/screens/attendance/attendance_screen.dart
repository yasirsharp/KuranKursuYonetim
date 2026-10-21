import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/constants/app_colors.dart';
import '../../../core/constants/app_spacing.dart';
import '../../../core/constants/labels.dart';
import '../../../core/network/api_exception.dart';
import '../../../core/utils/date_utils.dart';
import '../../../data/models/student_model.dart';
import '../../providers/data_providers.dart';
import '../../widgets/message_view.dart';

/// Bugünkü yoklama ve öğrenci listesi birlikte yüklenir; daha önce kaydedilen durumlar korunur.
final todayAttendanceProvider = FutureProvider.autoDispose<(List<StudentModel>, Map<String, String>)>((ref) async {
  final students = await ref.watch(studentsProvider.future);
  final records = await ref.read(attendanceRepositoryProvider).getForDay(ref.watch(todayProvider));
  return (students, {for (final r in records) r.studentId: r.status});
});

/// Yoklama: herkes "Geldi" başlar, dokundukça Gelmedi → Geç → İzinli; aynı gün tekrar açılıp düzeltilebilir.
class AttendanceScreen extends ConsumerStatefulWidget {
  const AttendanceScreen({super.key});

  @override
  ConsumerState<AttendanceScreen> createState() => _AttendanceScreenState();
}

class _AttendanceScreenState extends ConsumerState<AttendanceScreen> {
  /// Kullanıcının bu ekranda yaptığı değişiklikler (öğrenci → durum kodu).
  final Map<String, String> _changes = {};
  bool _saving = false;

  String _statusOf(String studentId, Map<String, String> saved) =>
      _changes[studentId] ?? saved[studentId] ?? AttendanceStatus.present.code;

  Future<void> _save(List<StudentModel> students, Map<String, String> saved) async {
    setState(() => _saving = true);
    final messenger = ScaffoldMessenger.of(context);
    try {
      await ref.read(attendanceRepositoryProvider).saveBulk(
            date: ref.read(todayProvider),
            statusByStudent: {for (final s in students) s.id: _statusOf(s.id, saved)},
          );
      _changes.clear();
      ref.invalidate(todayAttendanceProvider);
      messenger.showSnackBar(const SnackBar(content: Text('Yoklama kaydedildi.')));
    } on ApiException catch (e) {
      messenger.showSnackBar(SnackBar(content: Text(e.message), backgroundColor: AppColors.red));
    } finally {
      if (mounted) setState(() => _saving = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final dataAsync = ref.watch(todayAttendanceProvider);

    return Scaffold(
      appBar: AppBar(title: const Text('Yoklama')),
      body: dataAsync.when(
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (error, _) => MessageView(
          message: error.toString(),
          icon: Icons.cloud_off,
          onRetry: () => ref.invalidate(todayAttendanceProvider),
        ),
        data: (data) {
          final (students, saved) = data;
          if (students.isEmpty) return const MessageView(message: 'Size bağlı öğrenci yok.', icon: Icons.people_outline);

          final counts = <String, int>{};
          for (final s in students) {
            final code = _statusOf(s.id, saved);
            counts[code] = (counts[code] ?? 0) + 1;
          }
          final alreadySaved = saved.isNotEmpty;

          return Column(
            children: [
              Padding(
                padding: const EdgeInsets.fromLTRB(AppSpacing.lg, AppSpacing.lg, AppSpacing.lg, AppSpacing.sm),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    Text(displayDate(DateTime.now()), style: const TextStyle(color: AppColors.muted)),
                    const SizedBox(height: AppSpacing.xs),
                    Text(
                      alreadySaved ? 'Bugünkü yoklama kayıtlı; değiştirip yeniden kaydedebilirsiniz.' : 'Durumu değiştirmek için öğrenciye dokunun.',
                      style: const TextStyle(fontSize: 13),
                    ),
                    const SizedBox(height: AppSpacing.sm),
                    Wrap(
                      spacing: AppSpacing.sm,
                      children: [
                        for (final status in AttendanceStatus.cycle)
                          Chip(
                            label: Text('${status.label} ${counts[status.code] ?? 0}'),
                            backgroundColor: status.pale,
                            side: BorderSide.none,
                            labelStyle: TextStyle(color: status.color, fontWeight: FontWeight.w700),
                          ),
                      ],
                    ),
                  ],
                ),
              ),
              Expanded(
                child: ListView.separated(
                  padding: const EdgeInsets.symmetric(horizontal: AppSpacing.lg),
                  itemCount: students.length,
                  separatorBuilder: (_, _) => const SizedBox(height: AppSpacing.sm),
                  itemBuilder: (_, i) {
                    final student = students[i];
                    final status = AttendanceStatus.of(_statusOf(student.id, saved));
                    return Card(
                      child: ListTile(
                        minVerticalPadding: AppSpacing.md,
                        title: Text(student.fullName, style: const TextStyle(fontWeight: FontWeight.w700)),
                        subtitle: student.groupName.isEmpty ? null : Text(student.groupName),
                        trailing: Container(
                          width: 92,
                          padding: const EdgeInsets.symmetric(vertical: AppSpacing.sm),
                          alignment: Alignment.center,
                          decoration: BoxDecoration(color: status.pale, borderRadius: BorderRadius.circular(AppSpacing.radiusSm)),
                          child: Text(status.label, style: TextStyle(color: status.color, fontWeight: FontWeight.w800)),
                        ),
                        onTap: () => setState(() => _changes[student.id] = status.next.code),
                      ),
                    );
                  },
                ),
              ),
              SafeArea(
                top: false,
                child: Padding(
                  padding: const EdgeInsets.all(AppSpacing.lg),
                  child: FilledButton(
                    onPressed: _saving ? null : () => _save(students, saved),
                    child: Text(_saving ? 'Kaydediliyor...' : 'Yoklamayı kaydet'),
                  ),
                ),
              ),
            ],
          );
        },
      ),
    );
  }
}
