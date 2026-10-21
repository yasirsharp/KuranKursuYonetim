import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/constants/app_colors.dart';
import '../../../core/constants/app_spacing.dart';
import '../../../core/constants/labels.dart';
import '../../../core/constants/quran.dart';
import '../../../core/utils/date_utils.dart';
import '../../../data/models/lesson_model.dart';
import '../../../data/models/student_model.dart';
import '../../providers/data_providers.dart';
import '../../widgets/message_view.dart';

/// Öğrencinin ilerlemesi, son dersleri ve son yoklamaları.
class StudentDetailScreen extends ConsumerWidget {
  const StudentDetailScreen({super.key, required this.studentId});

  final String studentId;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final studentAsync = ref.watch(studentDetailProvider(studentId));

    return DefaultTabController(
      length: 2,
      child: Scaffold(
        appBar: AppBar(
          title: Text(studentAsync.value?.fullName ?? 'Öğrenci'),
          bottom: const TabBar(
            labelColor: Colors.white,
            unselectedLabelColor: Colors.white70,
            indicatorColor: Colors.white,
            tabs: [Tab(text: 'Dersler'), Tab(text: 'Yoklama')],
          ),
        ),
        body: studentAsync.when(
          loading: () => const Center(child: CircularProgressIndicator()),
          error: (error, _) => MessageView(
            message: error.toString(),
            icon: Icons.cloud_off,
            onRetry: () => ref.invalidate(studentDetailProvider(studentId)),
          ),
          data: (student) => Column(
            children: [
              _ProgressHeader(student: student),
              Expanded(
                child: TabBarView(
                  children: [
                    _LessonList(studentId: studentId),
                    _AttendanceList(studentId: studentId),
                  ],
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

class _ProgressHeader extends StatelessWidget {
  const _ProgressHeader({required this.student});

  final StudentModel student;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.all(AppSpacing.lg),
      child: Card(
        child: Padding(
          padding: const EdgeInsets.all(AppSpacing.lg),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Text(
                student.isHafiz
                    ? 'Hafızlığını tamamladı'
                    : '${student.completedPages} / ${Quran.totalPages} sayfa ezberlendi',
                style: const TextStyle(fontSize: 16, fontWeight: FontWeight.w800),
              ),
              const SizedBox(height: AppSpacing.sm),
              ClipRRect(
                borderRadius: BorderRadius.circular(4),
                child: LinearProgressIndicator(
                  value: student.progress,
                  minHeight: 8,
                  backgroundColor: AppColors.border,
                  color: AppColors.green,
                ),
              ),
              const SizedBox(height: AppSpacing.sm),
              Text(
                [
                  if (student.currentPage > 0) 'Son sayfa ${student.currentPage} (${Quran.juzOfPage(student.currentPage)}. cüz)',
                  if (student.groupName.isNotEmpty) student.groupName,
                ].join(' · '),
                style: const TextStyle(color: AppColors.muted),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

class _LessonList extends ConsumerWidget {
  const _LessonList({required this.studentId});

  final String studentId;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    return ref.watch(studentLessonsProvider(studentId)).when(
          loading: () => const Center(child: CircularProgressIndicator()),
          error: (error, _) => MessageView(message: error.toString(), icon: Icons.cloud_off),
          data: (lessons) => lessons.isEmpty
              ? const MessageView(message: 'Henüz ders kaydı yok.', icon: Icons.menu_book_outlined)
              : ListView.separated(
                  padding: const EdgeInsets.fromLTRB(AppSpacing.lg, 0, AppSpacing.lg, AppSpacing.xxl),
                  itemCount: lessons.length,
                  separatorBuilder: (_, _) => const Divider(height: 1),
                  itemBuilder: (_, i) => _LessonTile(lesson: lessons[i]),
                ),
        );
  }
}

class _LessonTile extends StatelessWidget {
  const _LessonTile({required this.lesson});

  final LessonModel lesson;

  @override
  Widget build(BuildContext context) {
    final level = PerformanceLevel.of(lesson.performance);
    final where = [
      if (lesson.pageNo != null) 'Sayfa ${lesson.pageNo}',
      if (lesson.surahName != null) lesson.surahName!,
    ].join(' · ');

    return ListTile(
      contentPadding: EdgeInsets.zero,
      title: Text(
        '${lesson.isNewPage ? 'Yeni ezber' : 'Tekrar'}${where.isEmpty ? '' : ' · $where'}',
        style: const TextStyle(fontWeight: FontWeight.w700),
      ),
      subtitle: Text([
        shortDate(lesson.lessonDate),
        if (lesson.teacherName.isNotEmpty) lesson.teacherName,
        if (lesson.teacherNotes != null && lesson.teacherNotes!.isNotEmpty) lesson.teacherNotes!,
      ].join(' · ')),
      trailing: Text(
        level?.label ?? lesson.performance,
        style: TextStyle(color: level?.color ?? AppColors.muted, fontWeight: FontWeight.w700),
      ),
    );
  }
}

class _AttendanceList extends ConsumerWidget {
  const _AttendanceList({required this.studentId});

  final String studentId;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    return ref.watch(studentAttendanceProvider(studentId)).when(
          loading: () => const Center(child: CircularProgressIndicator()),
          error: (error, _) => MessageView(message: error.toString(), icon: Icons.cloud_off),
          data: (records) => records.isEmpty
              ? const MessageView(message: 'Henüz yoklama kaydı yok.', icon: Icons.event_available_outlined)
              : ListView.separated(
                  padding: const EdgeInsets.fromLTRB(AppSpacing.lg, 0, AppSpacing.lg, AppSpacing.xxl),
                  itemCount: records.length,
                  separatorBuilder: (_, _) => const Divider(height: 1),
                  itemBuilder: (_, i) {
                    final status = AttendanceStatus.of(records[i].status);
                    return ListTile(
                      contentPadding: EdgeInsets.zero,
                      title: Text(displayDate(records[i].date)),
                      subtitle: (records[i].notes ?? '').isEmpty ? null : Text(records[i].notes!),
                      trailing: Text(status.label, style: TextStyle(color: status.color, fontWeight: FontWeight.w700)),
                    );
                  },
                ),
        );
  }
}
