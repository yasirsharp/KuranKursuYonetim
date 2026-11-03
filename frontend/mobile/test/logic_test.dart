import 'dart:io';

import 'package:dio/dio.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:hafiz_takip/core/constants/labels.dart';
import 'package:hafiz_takip/core/constants/quran.dart';
import 'package:hafiz_takip/core/network/api_exception.dart';
import 'package:hafiz_takip/core/utils/validators.dart';
import 'package:hafiz_takip/data/models/invitation_model.dart';
import 'package:hafiz_takip/data/models/session_model.dart';
import 'package:hafiz_takip/data/models/student_model.dart';
import 'package:hafiz_takip/presentation/providers/auth_provider.dart';
import 'package:hafiz_takip/presentation/screens/staff/staff_screen.dart';
import 'package:hafiz_takip/presentation/screens/students/quick_lesson_sheet.dart';
import 'package:hafiz_takip/router/app_router.dart';

StudentModel student({int currentPage = 0}) => StudentModel(
      id: 's1',
      firstName: 'Yusuf',
      lastName: 'Kaya',
      groupName: 'A Sınıfı',
      currentPage: currentPage,
      completedPages: currentPage,
      isHafiz: false,
      status: 'active',
    );

SessionModel session({String role = 'head_teacher', String tenantStatus = 'active'}) => SessionModel(
      token: 't',
      refreshToken: 'r',
      userId: 'u',
      tenantId: 'k',
      tenantStatus: tenantStatus,
      role: role,
      firstName: 'Ahmet',
      lastName: 'Yılmaz',
    );

void main() {
  test('cüz hesabı backend ile aynı (ayetberkenar mushaf)', () {
    expect({1: 1, 21: 1, 22: 2, 41: 2, 42: 3, 582: 30, 581: 29, 604: 30}.map((page, _) => MapEntry(page, Quran.juzOfPage(page))),
        {1: 1, 21: 1, 22: 2, 41: 2, 42: 3, 582: 30, 581: 29, 604: 30});
  });

  test('önerilen sayfa: yeni ezberde bir sonraki, tekrarda son sayfa, sınırlar içinde', () {
    expect(suggestedPage(student(currentPage: 0), isNewPage: true), 1);
    expect(suggestedPage(student(currentPage: 120), isNewPage: true), 121);
    expect(suggestedPage(student(currentPage: 120), isNewPage: false), 120);
    expect(suggestedPage(student(currentPage: 604), isNewPage: true), 604);
  });

  test('yoklama durumu dokundukça sırayla değişir ve başa döner', () {
    expect(AttendanceStatus.present.next, AttendanceStatus.absent);
    expect(AttendanceStatus.excused.next, AttendanceStatus.present);
    expect(AttendanceStatus.of('bilinmeyen'), AttendanceStatus.present);
  });

  test('e-posta ve kod doğrulama', () {
    expect(isValidEmail(' hoca@kurs.com '), isTrue);
    expect(isValidEmail('hoca@kurs'), isFalse);
    expect(isValidCode('123456'), isTrue);
    expect(isValidCode('12345a'), isFalse);
  });

  test('davet kodu: tire/boşluk atılır, büyük harfe çevrilir; paylaşım mesajı bağlantıyı içerir', () {
    expect(normalizeInvitationCode(' ab7k-2m9x '), 'AB7K2M9X');
    final text = invitationShareText('assistant_teacher', 'https://site/katil/AB7K2M9X');
    expect(text, contains('yardımcı hoca olarak'));
    expect(text, endsWith('https://site/katil/AB7K2M9X'));
  });

  test('cep numarası sunucunun beklediği 10 haneye indirilir', () {
    expect(normalizePhone('0532 111 22 33'), '5321112233');
    expect(normalizePhone('+90 532 111 22 33'), '5321112233');
    expect(normalizePhone('5321112233'), '5321112233');
    expect(normalizePhone('0212 111 22 33'), isNull, reason: 'sabit hat');
    expect(normalizePhone('0532 111 22'), isNull);
    expect(formatPhone('5321112233'), '0532 111 22 33');
  });

  group('yönlendirme', () {
    test('davet bağlantısı oturum, açılış ve tanıtımdan bağımsız açılır', () {
      for (final auth in [const AuthState(AuthStatus.restoring), const AuthState(AuthStatus.signedOut), AuthState(AuthStatus.signedIn, session())]) {
        expect(authRedirect(auth, '/katil/AB7K2M9X', onboardingSeen: false), isNull);
      }
    });

    test('oturum yoksa giriş ekranı, giriş ekranındayken yönlendirme yok', () {
      const auth = AuthState(AuthStatus.signedOut);
      expect(authRedirect(auth, '/students'), '/login');
      expect(authRedirect(auth, '/login/code'), isNull);
    });

    test('giriş yapan hoca öğrenci listesine gider', () {
      final auth = AuthState(AuthStatus.signedIn, session());
      expect(authRedirect(auth, '/login/code'), '/students');
      expect(authRedirect(auth, '/attendance'), isNull);
    });

    test('ilk açılışta tanıtım gösterilir; okunurken açılış ekranında beklenir', () {
      const auth = AuthState(AuthStatus.signedOut);
      expect(authRedirect(auth, '/login', onboardingSeen: null), '/splash');
      expect(authRedirect(auth, '/login', onboardingSeen: false), '/welcome');
      expect(authRedirect(auth, '/welcome', onboardingSeen: false), isNull);
      expect(authRedirect(auth, '/welcome', onboardingSeen: true), '/login');
    });

        test('onay bekleyen kurs ve veli rolü engellenir', () {
      expect(authRedirect(AuthState(AuthStatus.signedIn, session(tenantStatus: 'pending')), '/students'), '/blocked');
      expect(authRedirect(AuthState(AuthStatus.signedIn, session(role: 'parent')), '/students'), '/blocked');
      expect(blockedReason(AuthState(AuthStatus.signedIn, session(tenantStatus: 'pending'))), contains('onay bekliyor'));
    });
  });

  group('API hata mesajı', () {
    final request = RequestOptions(path: '/api/lessons');

    test('backend mesajı ve alan hataları okunur', () {
      final error = DioException(
        requestOptions: request,
        response: Response(requestOptions: request, statusCode: 400, data: {
          'success': false,
          'message': 'Doğrulama hatası',
          'errors': {'PageNo': ['Sayfa numarası 1-604 arasında olmalıdır.']},
        }),
      );
      final e = ApiException.fromDio(error);
      expect(e.message, 'Doğrulama hatası');
      expect(e.fieldErrors['pageNo'], 'Sayfa numarası 1-604 arasında olmalıdır.');
    });

    test('geçersiz sertifikada bağlantı mesajı yerine sertifika mesajı', () {
      final e = ApiException.fromDio(DioException(
        requestOptions: request,
        type: DioExceptionType.connectionError,
        error: const HandshakeException('CERTIFICATE_VERIFY_FAILED'),
      ));
      expect(e.message, contains('sertifikası geçersiz'));
    });

        test('yanıt yoksa bağlantı mesajı', () {
      final e = ApiException.fromDio(DioException(requestOptions: request, type: DioExceptionType.connectionError));
      expect(e.message, contains('İnternet bağlantınızı'));
    });
  });
}
