import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../core/constants/app_colors.dart';
import '../../../core/constants/app_spacing.dart';
import '../../../core/constants/labels.dart';
import '../../../core/utils/date_utils.dart';
import '../../../data/models/lesson_model.dart';
import '../../../data/models/student_model.dart';
import '../../providers/data_providers.dart';
import '../../widgets/message_view.dart';
import 'quick_lesson_sheet.dart';

/// Ana ekran: hocanın öğrencileri, bugün kimin dinlettiği ve hızlı ders kaydı.
class StudentsScreen extends ConsumerStatefulWidget {
  const StudentsScreen({super.key});

  @override
  ConsumerState<StudentsScreen> createState() => _StudentsScreenState();
}

class _StudentsScreenState extends ConsumerState<StudentsScreen> {
  String _search = '';

  Future<void> _refresh() async {
    ref.invalidate(studentsProvider);
    ref.invalidate(todayLessonsProvider);
    await ref.read(studentsProvider.future);
  }

  /// Ders kaydedilince listeyi yeniler ve bugün henüz dinletmemiş sıradaki öğrenciyi açar.
  Future<void> _record(StudentModel student, List<StudentModel> visible, Map<String, LessonModel> today) async {
    final saved = await showQuickLessonSheet(context, student);
    if (!saved || !mounted) return;

    ref.invalidate(studentsProvider);
    ref.invalidate(todayLessonsProvider);
    ScaffoldMessenger.of(context)
      ..hideCurrentSnackBar()
      ..showSnackBar(SnackBar(content: Text('${student.fullName} için ders kaydedildi.')));

    final index = visible.indexWhere((s) => s.id == student.id);
    final next = visible.skip(index + 1).where((s) => !today.containsKey(s.id)).firstOrNull;
    if (next != null) await _record(next, visible, {...today, student.id: _placeholderLesson(student.id)});
  }

  static LessonModel _placeholderLesson(String studentId) => LessonModel(
        id: '',
        studentId: studentId,
        teacherName: '',
        lessonDate: DateTime.now(),
        isNewPage: true,
        performance: '',
        homeworkStatus: '',
      );

  @override
  Widget build(BuildContext context) {
    final studentsAsync = ref.watch(studentsProvider);
    final today = ref.watch(todayLessonsProvider).value ?? const <String, LessonModel>{};

    return Scaffold(
      appBar: AppBar(title: const Text('Öğrencilerim')),
      body: studentsAsync.when(
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (error, _) => MessageView(message: error.toString(), icon: Icons.cloud_off, onRetry: _refresh),
        data: (students) {
          final query = _search.trim().toLowerCase();
          final visible = query.isEmpty
              ? students
              : students.where((s) => s.fullName.toLowerCase().contains(query)).toList();
          final doneCount = students.where((s) => today.containsKey(s.id)).length;

          return RefreshIndicator(
            onRefresh: _refresh,
            child: CustomScrollView(
              slivers: [
                SliverToBoxAdapter(child: _buildHeader(doneCount, students.length)),
                if (visible.isEmpty)
                  SliverFillRemaining(
                    hasScrollBody: false,
                    child: MessageView(
                      message: students.isEmpty ? 'Size bağlı öğrenci yok.' : 'Aramaya uyan öğrenci yok.',
                      icon: Icons.people_outline,
                    ),
                  )
                else
                  SliverPadding(
                    padding: const EdgeInsets.fromLTRB(AppSpacing.lg, 0, AppSpacing.lg, AppSpacing.xxl),
                    sliver: SliverList.separated(
                      itemCount: visible.length,
                      separatorBuilder: (_, _) => const SizedBox(height: AppSpacing.sm),
                      itemBuilder: (_, i) {
                        final student = visible[i];
                        return StudentCard(
                          student: student,
                          todayLesson: today[student.id],
                          onTap: () => _record(student, visible, today),
                          onOpenDetail: () => context.push('/students/${student.id}'),
                        );
                      },
                    ),
                  ),
              ],
            ),
          );
        },
      ),
    );
  }

  Widget _buildHeader(int done, int total) {
    return Padding(
      padding: const EdgeInsets.all(AppSpacing.lg),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Text(displayDate(DateTime.now()), style: const TextStyle(color: AppColors.muted)),
          const SizedBox(height: AppSpacing.xs),
          Text('Bugün $done / $total öğrenci dinletti', style: const TextStyle(fontSize: 18, fontWeight: FontWeight.w800)),
          const SizedBox(height: AppSpacing.sm),
          ClipRRect(
            borderRadius: BorderRadius.circular(4),
            child: LinearProgressIndicator(
              value: total == 0 ? 0 : done / total,
              minHeight: 8,
              backgroundColor: AppColors.border,
              color: AppColors.green,
            ),
          ),
          const SizedBox(height: AppSpacing.md),
          TextField(
            onChanged: (value) => setState(() => _search = value),
            decoration: const InputDecoration(prefixIcon: Icon(Icons.search), hintText: 'Öğrenci ara'),
          ),
        ],
      ),
    );
  }
}

class StudentCard extends StatelessWidget {
  const StudentCard({
    super.key,
    required this.student,
    required this.todayLesson,
    required this.onTap,
    required this.onOpenDetail,
  });

  final StudentModel student;
  final LessonModel? todayLesson;
  final VoidCallback onTap;
  final VoidCallback onOpenDetail;

  @override
  Widget build(BuildContext context) {
    final done = todayLesson != null;
    final level = done ? PerformanceLevel.of(todayLesson!.performance) : null;

    return Card(
      child: InkWell(
        onTap: onTap,
        borderRadius: BorderRadius.circular(AppSpacing.radiusLg),
        child: Padding(
          padding: const EdgeInsets.fromLTRB(AppSpacing.md, AppSpacing.md, AppSpacing.xs, AppSpacing.md),
          child: Row(
            children: [
              CircleAvatar(
                radius: 22,
                backgroundColor: done ? AppColors.greenPale : AppColors.goldPale,
                foregroundColor: done ? AppColors.green : AppColors.gold,
                child: done ? const Icon(Icons.check) : Text(student.initials, style: const TextStyle(fontWeight: FontWeight.w800)),
              ),
              const SizedBox(width: AppSpacing.md),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(student.fullName, style: const TextStyle(fontSize: 16, fontWeight: FontWeight.w700)),
                    const SizedBox(height: 2),
                    Text(
                      [
                        if (student.currentPage > 0) 'Sayfa ${student.currentPage}' else 'Başlamadı',
                        if (student.groupName.isNotEmpty) student.groupName,
                      ].join(' · '),
                      style: const TextStyle(color: AppColors.muted, fontSize: 13),
                    ),
                  ],
                ),
              ),
              if (done)
                Text(
                  level?.label ?? 'Dinletti',
                  style: TextStyle(color: level?.color ?? AppColors.green, fontWeight: FontWeight.w700),
                )
              else
                const Text('Dinle', style: TextStyle(color: AppColors.green, fontWeight: FontWeight.w700)),
              IconButton(
                onPressed: onOpenDetail,
                icon: const Icon(Icons.chevron_right),
                tooltip: '${student.fullName} ayrıntıları',
              ),
            ],
          ),
        ),
      ),
    );
  }
}
