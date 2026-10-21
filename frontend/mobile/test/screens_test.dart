import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_riverpod/misc.dart' show Override;
import 'package:flutter_test/flutter_test.dart';
import 'package:hafiz_takip/core/theme/app_theme.dart';
import 'package:hafiz_takip/data/models/lesson_model.dart';
import 'package:hafiz_takip/data/models/student_model.dart';
import 'package:hafiz_takip/presentation/providers/data_providers.dart';
import 'package:hafiz_takip/presentation/screens/attendance/attendance_screen.dart';
import 'package:hafiz_takip/presentation/screens/auth/login_screen.dart';
import 'package:hafiz_takip/presentation/screens/students/students_screen.dart';
import 'package:intl/date_symbol_data_local.dart';

const students = [
  StudentModel(id: 's1', firstName: 'Ali', lastName: 'Demir', groupName: 'A Sınıfı', currentPage: 40, completedPages: 40, isHafiz: false, status: 'active'),
  StudentModel(id: 's2', firstName: 'Yusuf', lastName: 'Kaya', groupName: 'A Sınıfı', currentPage: 0, completedPages: 0, isHafiz: false, status: 'active'),
];

Widget wrap(Widget child, {List<Override> overrides = const []}) => ProviderScope(
      overrides: overrides,
      child: MaterialApp(theme: AppTheme.light, home: child),
    );

void main() {
  setUpAll(() => initializeDateFormatting('tr_TR'));

  testWidgets('giriş: geçersiz e-postada uyarı verir', (tester) async {
    await tester.pumpWidget(wrap(const LoginScreen()));

    await tester.enterText(find.byType(TextField), 'hoca@kurs');
    await tester.tap(find.text('Giriş kodu gönder'));
    await tester.pump();

    expect(find.text('Geçerli bir e-posta adresi girin.'), findsOneWidget);
  });

  testWidgets('öğrenci listesi bugün dinletenleri sayar ve işaretler', (tester) async {
    final lesson = LessonModel(
      id: 'l1', studentId: 's1', teacherName: 'Ahmet', lessonDate: DateTime(2026, 10, 21),
      pageNo: 41, isNewPage: true, performance: 'cok_iyi', homeworkStatus: 'yapildi',
    );
    await tester.pumpWidget(wrap(const StudentsScreen(), overrides: [
      studentsProvider.overrideWith((ref) => Future.value(students)),
      todayLessonsProvider.overrideWith((ref) => Future.value({'s1': lesson})),
    ]));
    await tester.pumpAndSettle();

    expect(find.text('Bugün 1 / 2 öğrenci dinletti'), findsOneWidget);
    expect(find.text('Çok iyi'), findsOneWidget);
    expect(find.text('Dinle'), findsOneWidget);
    expect(find.text('Başlamadı · A Sınıfı'), findsOneWidget);
  });

  testWidgets('hızlı ders kaydı: sayfa önerili gelir, performans seçilmeden kaydetmez', (tester) async {
    await tester.pumpWidget(wrap(const StudentsScreen(), overrides: [
      studentsProvider.overrideWith((ref) => Future.value(students)),
      todayLessonsProvider.overrideWith((ref) => Future.value(<String, LessonModel>{})),
    ]));
    await tester.pumpAndSettle();

    await tester.tap(find.text('Ali Demir'));
    await tester.pumpAndSettle();

    expect(find.text('Sayfa 41'), findsOneWidget);
    expect(find.text('2. cüz'), findsOneWidget);

    await tester.tap(find.text('Tekrar'));
    await tester.pump();
    expect(find.text('Sayfa 40'), findsOneWidget);

    await tester.ensureVisible(find.text('Kaydet ve sıradakine geç'));
    await tester.tap(find.text('Kaydet ve sıradakine geç'));
    await tester.pump();
    expect(find.text('Performansı seçin.'), findsOneWidget);
  });

  testWidgets('yoklama: dokununca durum değişir, sayaçlar güncellenir', (tester) async {
    await tester.pumpWidget(wrap(const AttendanceScreen(), overrides: [
      todayAttendanceProvider.overrideWith((ref) => Future.value((students, <String, String>{'s2': 'excused'}))),
    ]));
    await tester.pumpAndSettle();

    expect(find.text('Geldi 1'), findsOneWidget);
    expect(find.text('İzinli 1'), findsOneWidget);

    await tester.tap(find.text('Ali Demir'));
    await tester.pump();

    expect(find.text('Geldi 0'), findsOneWidget);
    expect(find.text('Gelmedi 1'), findsOneWidget);
  });
}
