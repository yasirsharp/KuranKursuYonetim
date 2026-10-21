import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/constants/app_colors.dart';
import '../../../core/constants/app_spacing.dart';
import '../../../core/constants/labels.dart';
import '../../../core/constants/quran.dart';
import '../../../core/network/api_exception.dart';
import '../../../data/models/lesson_model.dart';
import '../../../data/models/student_model.dart';
import '../../providers/data_providers.dart';

/// Önerilen sayfa: yeni ezberde son sayfanın bir sonrakisi, tekrarda son sayfa.
int suggestedPage(StudentModel student, {required bool isNewPage}) {
  if (student.currentPage <= 0) return 1;
  return Quran.clampPage(isNewPage ? student.currentPage + 1 : student.currentPage);
}

/// Hızlı ders kaydı: sayfa önerili gelir, performans seçilip kaydedilir.
/// Kaydedilirse true döner (liste sıradaki öğrenciye geçer).
Future<bool> showQuickLessonSheet(BuildContext context, StudentModel student) async {
  final saved = await showModalBottomSheet<bool>(
    context: context,
    isScrollControlled: true,
    useSafeArea: true,
    backgroundColor: AppColors.card,
    shape: const RoundedRectangleBorder(borderRadius: BorderRadius.vertical(top: Radius.circular(20))),
    builder: (_) => QuickLessonSheet(student: student),
  );
  return saved ?? false;
}

class QuickLessonSheet extends ConsumerStatefulWidget {
  const QuickLessonSheet({super.key, required this.student});

  final StudentModel student;

  @override
  ConsumerState<QuickLessonSheet> createState() => _QuickLessonSheetState();
}

class _QuickLessonSheetState extends ConsumerState<QuickLessonSheet> {
  bool _isNewPage = true;
  late int _page = suggestedPage(widget.student, isNewPage: true);
  String? _performance;
  String _homework = 'yapildi';
  final _notesController = TextEditingController();
  bool _showNotes = false;
  bool _saving = false;
  String? _error;

  @override
  void dispose() {
    _notesController.dispose();
    super.dispose();
  }

  void _setType(bool isNewPage) => setState(() {
        _isNewPage = isNewPage;
        _page = suggestedPage(widget.student, isNewPage: isNewPage);
      });

  void _changePage(int delta) => setState(() => _page = Quran.clampPage(_page + delta));

  Future<void> _save() async {
    if (_performance == null) {
      setState(() => _error = 'Performansı seçin.');
      return;
    }
    setState(() {
      _saving = true;
      _error = null;
    });
    try {
      await ref.read(lessonRepositoryProvider).create(NewLesson(
            studentId: widget.student.id,
            lessonDate: ref.read(todayProvider),
            pageNo: _page,
            isNewPage: _isNewPage,
            performance: _performance!,
            homeworkStatus: _homework,
            teacherNotes: _notesController.text,
          ));
      if (mounted) Navigator.of(context).pop(true);
    } on ApiException catch (e) {
      setState(() {
        _saving = false;
        _error = e.message;
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    final student = widget.student;
    return Padding(
      padding: EdgeInsets.only(bottom: MediaQuery.of(context).viewInsets.bottom),
      child: SingleChildScrollView(
        padding: const EdgeInsets.fromLTRB(AppSpacing.xl, AppSpacing.md, AppSpacing.xl, AppSpacing.xl),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Center(
              child: Container(
                width: 40,
                height: 4,
                decoration: BoxDecoration(color: AppColors.border, borderRadius: BorderRadius.circular(2)),
              ),
            ),
            const SizedBox(height: AppSpacing.lg),
            Text(student.fullName, style: const TextStyle(fontSize: 20, fontWeight: FontWeight.w800)),
            Text(
              student.currentPage > 0 ? 'Son sayfa ${student.currentPage}' : 'Henüz ders kaydı yok',
              style: const TextStyle(color: AppColors.muted),
            ),
            const SizedBox(height: AppSpacing.lg),
            SegmentedButton<bool>(
              segments: const [
                ButtonSegment(value: true, label: Text('Yeni ezber')),
                ButtonSegment(value: false, label: Text('Tekrar')),
              ],
              selected: {_isNewPage},
              onSelectionChanged: (s) => _setType(s.first),
              showSelectedIcon: false,
            ),
            const SizedBox(height: AppSpacing.lg),
            _buildPageStepper(),
            const SizedBox(height: AppSpacing.lg),
            const Text('Performans', style: TextStyle(fontWeight: FontWeight.w700)),
            const SizedBox(height: AppSpacing.sm),
            _buildPerformanceButtons(),
            const SizedBox(height: AppSpacing.lg),
            const Text('Ödev / hazırlık', style: TextStyle(fontWeight: FontWeight.w700)),
            const SizedBox(height: AppSpacing.sm),
            Wrap(
              spacing: AppSpacing.sm,
              children: [
                for (final entry in HomeworkStatus.labels.entries)
                  ChoiceChip(
                    label: Text(entry.value),
                    selected: _homework == entry.key,
                    onSelected: (_) => setState(() => _homework = entry.key),
                  ),
              ],
            ),
            if (_showNotes) ...[
              const SizedBox(height: AppSpacing.md),
              TextField(
                controller: _notesController,
                maxLines: 2,
                maxLength: 2000,
                decoration: const InputDecoration(labelText: 'Hoca notu'),
              ),
            ] else
              Align(
                alignment: Alignment.centerLeft,
                child: TextButton.icon(
                  onPressed: () => setState(() => _showNotes = true),
                  icon: const Icon(Icons.edit_note),
                  label: const Text('Not ekle'),
                ),
              ),
            if (_error != null)
              Padding(
                padding: const EdgeInsets.only(top: AppSpacing.sm),
                child: Text(_error!, style: const TextStyle(color: AppColors.red)),
              ),
            const SizedBox(height: AppSpacing.md),
            FilledButton(
              onPressed: _saving ? null : _save,
              child: Text(_saving ? 'Kaydediliyor...' : 'Kaydet ve sıradakine geç'),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildPageStepper() {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: AppSpacing.sm, vertical: AppSpacing.xs),
      decoration: BoxDecoration(
        border: Border.all(color: AppColors.border),
        borderRadius: BorderRadius.circular(AppSpacing.radiusMd),
      ),
      child: Row(
        children: [
          IconButton.filledTonal(
            onPressed: _page > 1 ? () => _changePage(-1) : null,
            icon: const Icon(Icons.remove),
            tooltip: 'Önceki sayfa',
          ),
          Expanded(
            child: Column(
              children: [
                Text('Sayfa $_page', style: const TextStyle(fontSize: 22, fontWeight: FontWeight.w800)),
                Text('${Quran.juzOfPage(_page)}. cüz', style: const TextStyle(color: AppColors.muted)),
              ],
            ),
          ),
          IconButton.filledTonal(
            onPressed: _page < Quran.totalPages ? () => _changePage(1) : null,
            icon: const Icon(Icons.add),
            tooltip: 'Sonraki sayfa',
          ),
        ],
      ),
    );
  }

  Widget _buildPerformanceButtons() {
    return Row(
      children: [
        for (final level in PerformanceLevel.all)
          Expanded(
            child: Padding(
              padding: const EdgeInsets.symmetric(horizontal: 3),
              child: _PerformanceButton(
                level: level,
                selected: _performance == level.code,
                onTap: () => setState(() {
                  _performance = level.code;
                  _error = null;
                }),
              ),
            ),
          ),
      ],
    );
  }
}

class _PerformanceButton extends StatelessWidget {
  const _PerformanceButton({required this.level, required this.selected, required this.onTap});

  final PerformanceLevel level;
  final bool selected;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return Semantics(
      button: true,
      selected: selected,
      label: level.label,
      child: InkWell(
        onTap: onTap,
        borderRadius: BorderRadius.circular(AppSpacing.radiusSm),
        child: Container(
          height: 56,
          alignment: Alignment.center,
          decoration: BoxDecoration(
            color: selected ? level.color.withValues(alpha: 0.12) : null,
            border: Border.all(color: selected ? level.color : AppColors.border, width: 2),
            borderRadius: BorderRadius.circular(AppSpacing.radiusSm),
          ),
          child: Text(
            level.label,
            textAlign: TextAlign.center,
            style: TextStyle(fontSize: 12, fontWeight: FontWeight.w700, color: selected ? level.color : AppColors.muted),
          ),
        ),
      ),
    );
  }
}
