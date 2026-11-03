import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:hafiz_takip/core/network/api_exception.dart';
import 'package:hafiz_takip/core/theme/app_theme.dart';
import 'package:hafiz_takip/data/models/course_application_model.dart';
import 'package:hafiz_takip/data/models/session_model.dart';
import 'package:hafiz_takip/data/repositories/course_application_repository.dart';
import 'package:hafiz_takip/presentation/providers/data_providers.dart';
import 'package:hafiz_takip/presentation/screens/application/application_status_screen.dart';
import 'package:hafiz_takip/presentation/screens/application/apply_screen.dart';
import 'package:intl/date_symbol_data_local.dart';

class FakeCourseApplicationRepository implements CourseApplicationRepository {
  final calls = <String>[];
  CourseApplicationStatus status = const CourseApplicationStatus(courseName: 'Yeşilvadi Kursu', status: 'pending');

  @override
  Future<void> sendCode(String phone) async => calls.add('sendCode $phone');

  @override
  Future<SessionModel> submit(
      {required String phone, required String code, required CourseInfo info, required String firstName, required String lastName}) async {
    calls.add('submit $phone $code ${info.courseName}/${info.city}/${info.district}/${info.contactEmail} $firstName $lastName');
    // Oturum kaydı (güvenli depolama) testte çalışmaz; sunucu hatası yolu denenir
    throw const ApiException('Bu telefon numarasıyla onay bekleyen bir kurs başvurunuz zaten var.');
  }

  @override
  Future<CourseApplicationStatus> getMine() async => status;

  @override
  Future<void> resubmit(CourseInfo info) async {
    calls.add('resubmit ${info.courseName}');
    status = CourseApplicationStatus(courseName: info.courseName, status: 'pending', city: info.city, district: info.district);
  }
}

void usePhoneScreen(WidgetTester tester) {
  tester.view.physicalSize = const Size(1170, 2532);
  tester.view.devicePixelRatio = 3;
  addTearDown(tester.view.reset);
}

Widget wrap(Widget child, FakeCourseApplicationRepository repo) => ProviderScope(
      overrides: [courseApplicationRepositoryProvider.overrideWithValue(repo)],
      child: MaterialApp(theme: AppTheme.light, home: child),
    );

Future<void> tapText(WidgetTester tester, String text) async {
  await tester.ensureVisible(find.text(text));
  await tester.tap(find.text(text));
  await tester.pumpAndSettle();
}

void main() {
  setUpAll(() => initializeDateFormatting('tr_TR'));

  testWidgets('kurs başvurusu: telefon doğrulanır, eksik bilgide uyarır, tamamsa gönderir', (tester) async {
    usePhoneScreen(tester);
    final repo = FakeCourseApplicationRepository();
    await tester.pumpWidget(wrap(const ApplyScreen(), repo));

    await tester.enterText(find.byType(TextField), '0532 111 22 33');
    await tapText(tester, 'Doğrulama kodu gönder');
    expect(repo.calls, ['sendCode 5321112233']);

    await tester.enterText(find.widgetWithText(TextField, 'Doğrulama kodu'), '123456');
    await tapText(tester, 'Başvuruyu gönder');
    expect(find.text('Adınızı ve soyadınızı yazın.'), findsOneWidget);

    await tester.enterText(find.widgetWithText(TextField, 'Ad'), 'Kemal');
    await tester.enterText(find.widgetWithText(TextField, 'Soyad'), 'Arslan');
    await tapText(tester, 'Başvuruyu gönder');
    expect(find.text('Kurs adını yazın.'), findsOneWidget);

    await tester.enterText(find.widgetWithText(TextField, 'Kurs adı'), "Yeşilvadi Kur'an Kursu");
    await tester.enterText(find.widgetWithText(TextField, 'İl'), 'Konya');
    await tester.enterText(find.widgetWithText(TextField, 'İlçe'), 'Selçuklu');
    await tester.enterText(find.widgetWithText(TextField, 'E-posta (isteğe bağlı)'), 'mudur@yesilvadi.com');
    await tester.ensureVisible(find.text('Başvuruyu gönder'));
    await tester.tap(find.text('Başvuruyu gönder'));
    await tester.pump();

    await tester.pumpAndSettle();
    expect(find.text('Bu telefon numarasıyla onay bekleyen bir kurs başvurunuz zaten var.'), findsOneWidget);
    expect(repo.calls.last, "submit 5321112233 123456 Yeşilvadi Kur'an Kursu/Konya/Selçuklu/mudur@yesilvadi.com Kemal Arslan");
  });

  testWidgets('başvuru durumu: bekleyen başvuruda bilgi, reddedilende gerekçe ve yeniden gönderme', (tester) async {
    usePhoneScreen(tester);
    final repo = FakeCourseApplicationRepository();
    await tester.pumpWidget(wrap(const ApplicationStatusScreen(), repo));
    await tester.pumpAndSettle();
    expect(find.text('Kursunuz onay bekliyor'), findsOneWidget);
    expect(find.text('Durumu yenile'), findsOneWidget);

    repo.status = const CourseApplicationStatus(
      courseName: 'Yeşilvadi Kursu', status: 'rejected', city: 'Konya', district: 'Selçuklu', rejectionReason: 'Adres eksik',
    );
    await tapText(tester, 'Durumu yenile');
    expect(find.text('Başvurunuz onaylanmadı'), findsOneWidget);
    expect(find.text('Gerekçe: Adres eksik'), findsOneWidget);

    await tapText(tester, 'Bilgileri düzenle ve yeniden gönder');
    expect(find.widgetWithText(TextField, 'Kurs adı'), findsOneWidget);
    await tester.enterText(find.widgetWithText(TextField, 'Adres (isteğe bağlı)'), 'Merkez Mah. 1. Sok. No:3');
    await tapText(tester, 'Yeniden gönder');

    expect(repo.calls, ['resubmit Yeşilvadi Kursu']);
    expect(find.text('Kursunuz onay bekliyor'), findsOneWidget);
  });
}
