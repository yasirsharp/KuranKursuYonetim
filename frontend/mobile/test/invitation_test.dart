import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:hafiz_takip/core/network/api_exception.dart';
import 'package:hafiz_takip/core/theme/app_theme.dart';
import 'package:hafiz_takip/data/models/invitation_model.dart';
import 'package:hafiz_takip/data/repositories/invitation_repository.dart';
import 'package:hafiz_takip/presentation/providers/data_providers.dart';
import 'package:hafiz_takip/presentation/screens/join/join_screen.dart';
import 'package:hafiz_takip/presentation/screens/staff/staff_screen.dart';
import 'package:intl/date_symbol_data_local.dart';
import 'package:qr_flutter/qr_flutter.dart';

/// Sunucu yerine: çağrıları kaydeder.
class FakeInvitationRepository implements InvitationRepository {
  final calls = <String>[];
  bool invalid = false;
  final invitations = <InvitationModel>[];
  final requests = <JoinRequestModel>[];

  @override
  Future<InvitationPublicModel> getPublic(String code) async {
    calls.add('getPublic $code');
    if (invalid) throw const ApiException('Davet bağlantısı geçersiz veya süresi dolmuş.');
    return InvitationPublicModel(code: code, courseName: "Merkez Kur'an Kursu", role: 'head_teacher', city: 'Konya', district: 'Meram');
  }

  @override
  Future<void> sendJoinCode(String code, String phone) async => calls.add('sendJoinCode $code $phone');

  @override
  Future<void> join(String code,
          {required String firstName, required String lastName, required String phone, String? email, required String otp}) async =>
      calls.add('join $code $firstName $lastName $phone $email $otp');

  @override
  Future<List<InvitationModel>> getActive() async => List.of(invitations);

  @override
  Future<InvitationModel> create(String role) async {
    calls.add('create $role');
    final created = InvitationModel(id: 'i-$role', code: 'AB7K2M9X', role: role, expiresAt: DateTime(2026, 11, 10));
    invitations.add(created);
    return created;
  }

  @override
  Future<void> revoke(String id) async => calls.add('revoke $id');

  @override
  Future<List<JoinRequestModel>> getPendingRequests() async => List.of(requests);

  @override
  Future<void> approve(String id) async {
    calls.add('approve $id');
    requests.removeWhere((r) => r.id == id);
  }

  @override
  Future<void> reject(String id, {String? reason}) async => calls.add('reject $id $reason');
}

/// Telefon ekranı boyutu (390×844).
void usePhoneScreen(WidgetTester tester) {
  tester.view.physicalSize = const Size(1170, 2532);
  tester.view.devicePixelRatio = 3;
  addTearDown(tester.view.reset);
}

Widget wrap(Widget child, FakeInvitationRepository repo) => ProviderScope(
      overrides: [invitationRepositoryProvider.overrideWithValue(repo)],
      child: MaterialApp(theme: AppTheme.light, home: child),
    );

void main() {
  setUpAll(() => initializeDateFormatting('tr_TR'));

  testWidgets('katılım: kurs ve rolü gösterir, bilgiler + SMS koduyla istek gönderir', (tester) async {
    usePhoneScreen(tester);
    final repo = FakeInvitationRepository();
    await tester.pumpWidget(wrap(const JoinScreen(code: 'ab7k-2m9x'), repo));
    await tester.pumpAndSettle();

    expect(repo.calls.first, 'getPublic AB7K2M9X');
    expect(find.text("Merkez Kur'an Kursu"), findsOneWidget);
    expect(find.text('Hoca olarak katılın'), findsOneWidget);

    await tester.enterText(find.widgetWithText(TextField, 'Ad'), 'Yusuf');
    await tester.enterText(find.widgetWithText(TextField, 'Soyad'), 'Kaya');
    await tester.enterText(find.widgetWithText(TextField, 'Cep telefonu'), '0532 111 22 33');
    await tester.ensureVisible(find.text('Doğrulama kodu gönder'));
    await tester.tap(find.text('Doğrulama kodu gönder'));
    await tester.pumpAndSettle();

    expect(repo.calls, contains('sendJoinCode AB7K2M9X 5321112233'));
    expect(find.textContaining('0532 111 22 33 numarasına SMS'), findsOneWidget);

    await tester.enterText(find.widgetWithText(TextField, 'Doğrulama kodu'), '123456');
    await tester.pumpAndSettle();

    expect(repo.calls, contains('join AB7K2M9X Yusuf Kaya 5321112233 null 123456'));
    expect(find.text('İsteğiniz gönderildi'), findsOneWidget);
  });

  testWidgets('katılım: eksik numarada uyarır, sunucuya gitmez', (tester) async {
    usePhoneScreen(tester);
    final repo = FakeInvitationRepository();
    await tester.pumpWidget(wrap(const JoinScreen(code: 'AB7K2M9X'), repo));
    await tester.pumpAndSettle();

    await tester.enterText(find.widgetWithText(TextField, 'Ad'), 'Yusuf');
    await tester.enterText(find.widgetWithText(TextField, 'Soyad'), 'Kaya');
    await tester.enterText(find.widgetWithText(TextField, 'Cep telefonu'), '0532 111');
    await tester.ensureVisible(find.text('Doğrulama kodu gönder'));
    await tester.tap(find.text('Doğrulama kodu gönder'));
    await tester.pump();

    expect(find.text('Cep telefonu numaranızı 05XX XXX XX XX şeklinde girin.'), findsOneWidget);
    expect(repo.calls.where((c) => c.startsWith('sendJoinCode')), isEmpty);
  });

  testWidgets('katılım: geçersiz bağlantıda sunucunun mesajı', (tester) async {
    usePhoneScreen(tester);
    final repo = FakeInvitationRepository()..invalid = true;
    await tester.pumpWidget(wrap(const JoinScreen(code: 'ESKIKOD1'), repo));
    await tester.pumpAndSettle();

    expect(find.text('Davet açılamadı'), findsOneWidget);
    expect(find.text('Davet bağlantısı geçersiz veya süresi dolmuş.'), findsOneWidget);
  });

  testWidgets('yönetici: isteği onaylar, davet oluşturunca karekod ve paylaş düğmesi çıkar', (tester) async {
    usePhoneScreen(tester);
    final repo = FakeInvitationRepository()
      ..requests.add(JoinRequestModel(
        id: 'r1', firstName: 'Yusuf', lastName: 'Kaya', phoneNumber: '5321112233',
        role: 'assistant_teacher', createdAt: DateTime(2026, 11, 3, 9, 30),
      ));
    await tester.pumpWidget(wrap(const StaffScreen(), repo));
    await tester.pumpAndSettle();

    expect(find.text('Katılım istekleri (1)'), findsOneWidget);
    expect(find.text('Yardımcı hoca · 0532 111 22 33'), findsOneWidget);
    await tester.tap(find.text('Onayla'));
    await tester.pumpAndSettle();
    expect(repo.calls, contains('approve r1'));
    expect(find.text('Katılım istekleri (1)'), findsNothing);

    // Oturum yok (test): başhoca gibi yalnızca yardımcı hoca daveti görünür
    final create = find.text('Yardımcı hoca davet bağlantısı oluştur');
    await tester.ensureVisible(create);
    await tester.tap(create);
    await tester.pumpAndSettle();

    expect(repo.calls, contains('create assistant_teacher'));
    expect(find.byType(QrImageView), findsOneWidget);
    expect(find.text('https://hafiztakip.yasirsharp.com.tr/katil/AB7K2M9X'), findsOneWidget);
    expect(find.text('WhatsApp ile gönder / paylaş'), findsOneWidget);
  });
}
