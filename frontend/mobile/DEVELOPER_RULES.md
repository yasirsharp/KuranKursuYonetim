# 📱 Frontend Mobile Developer Rules

> **Flutter · Dart · Riverpod · Dio**
> Bu dosya mobil uygulama geliştirme kurallarını tanımlar. Tüm ekip üyeleri bu kuralları bilmelidir.

> **Ekim 2026 notu (ilk sürüm):** Kod Riverpod 3 ile yazıldı: `StateNotifier` yerine `Notifier`/`FutureProvider` kullanılır.
> Kod üretimi (build_runner, json_serializable, freezed) şimdilik kullanılmıyor; modeller elle `fromJson` yazılır.
> Giriş e-posta + kod iledir (`/api/auth/send-email-code`, `/api/auth/verify-email-code`). Offline kuyruk (Bölüm 7) sonraki sürümde.

---

## 1. Proje Yapısı

```
lib/
├── main.dart
├── app.dart                         # MaterialApp + GoRouter
│
├── core/                            # Çapraz-kesit katman
│   ├── constants/
│   │   ├── app_colors.dart          # Renk paleti
│   │   ├── app_strings.dart         # Sabit metinler
│   │   ├── app_spacing.dart         # Boşluk değerleri
│   │   └── api_constants.dart       # Base URL, timeout
│   ├── theme/
│   │   └── app_theme.dart           # ThemeData (Playfair + Nunito)
│   ├── network/
│   │   ├── api_client.dart          # Dio + interceptors
│   │   ├── api_exceptions.dart      # Hata tipleri
│   │   └── connectivity_service.dart # İnternet durumu
│   ├── storage/
│   │   ├── secure_storage.dart      # Token saklama
│   │   └── local_database.dart      # Hive/Isar (offline)
│   └── utils/
│       ├── surah_list.dart          # 114 sure
│       ├── date_utils.dart          # Tarih yardımcıları
│       └── validators.dart          # Form doğrulama
│
├── data/                            # Veri katmanı
│   ├── models/                      # JSON ↔ Dart modelleri
│   │   ├── student_model.dart
│   │   ├── lesson_model.dart
│   │   └── ...
│   ├── repositories/                # Repository pattern
│   │   ├── auth_repository.dart
│   │   ├── student_repository.dart
│   │   └── ...
│   └── datasources/
│       ├── remote/                  # API çağrıları
│       │   ├── auth_remote_ds.dart
│       │   └── student_remote_ds.dart
│       └── local/                   # Offline cache
│           └── student_local_ds.dart
│
├── presentation/                    # UI katmanı
│   ├── widgets/                     # Paylaşılan widget'lar
│   │   ├── app_button.dart
│   │   ├── app_card.dart
│   │   ├── app_input.dart
│   │   ├── student_avatar.dart
│   │   ├── performance_badge.dart
│   │   ├── progress_bar.dart
│   │   ├── bottom_nav.dart
│   │   ├── loading_skeleton.dart
│   │   └── empty_state.dart
│   ├── screens/                     # Ekranlar (feature-based)
│   │   ├── auth/
│   │   │   ├── login_screen.dart
│   │   │   └── otp_screen.dart
│   │   ├── dashboard/
│   │   │   ├── dashboard_screen.dart
│   │   │   └── widgets/            # Ekrana özel widget'lar
│   │   │       ├── stat_card.dart
│   │   │       └── student_tile.dart
│   │   ├── students/
│   │   ├── lessons/
│   │   ├── attendance/
│   │   ├── etut/
│   │   ├── parents/
│   │   └── settings/
│   └── providers/                   # Riverpod providers
│       ├── auth_provider.dart
│       ├── student_provider.dart
│       └── ...
│
└── router/
    └── app_router.dart              # GoRouter tanımları
```

---

## 2. Naming Conventions

### 2.1 Dosya İsimleri
| Tür | Format | Örnek |
|-----|--------|-------|
| Tüm dosyalar | snake_case.dart | `student_model.dart` |
| Widget | snake_case.dart | `student_avatar.dart` |
| Screen | _screen.dart suffix | `dashboard_screen.dart` |
| Model | _model.dart suffix | `student_model.dart` |
| Provider | _provider.dart suffix | `auth_provider.dart` |
| Repository | _repository.dart suffix | `student_repository.dart` |
| Service | _service.dart suffix | `connectivity_service.dart` |

### 2.2 Sınıf İsimleri
```dart
// PascalCase
class StudentModel { ... }
class DashboardScreen extends ConsumerWidget { ... }
class StudentRepository { ... }
class AppColors { ... }  // Constants sınıfı

// Provider isimleri — camelCase
final studentListProvider = FutureProvider<List<StudentModel>>(...);
final authStateProvider = StateNotifierProvider<AuthNotifier, AuthState>(...);
```

### 2.3 Değişken İsimleri
```dart
// camelCase
final studentName = 'Yusuf';
final isLoading = true;

// Private: _prefix
final _apiClient = Dio();

// Constant: lowerCamelCase (Dart convention)
const maxPageCount = 604;
const defaultPageSize = 20;
```

---

## 3. Widget Kuralları

### 3.1 Widget Yapısı
```dart
class StudentCard extends StatelessWidget {
  // 1. Constructor (const tercih et)
  const StudentCard({
    super.key,
    required this.student,
    required this.onTap,
    this.isCompact = false,
  });

  // 2. Properties
  final StudentModel student;
  final VoidCallback onTap;
  final bool isCompact;

  // 3. Build
  @override
  Widget build(BuildContext context) {
    return GestureDetector(
      onTap: onTap,
      child: Container(
        // ...
      ),
    );
  }
}
```

### 3.2 Widget Ayırma Kuralı
```dart
// ✅ build() 80 satırı geçiyorsa, alt widget'lara ayır
@override
Widget build(BuildContext context) {
  return Column(
    children: [
      _buildHeader(),     // Private method
      _buildProgressBar(),
      _buildLessonList(),
    ],
  );
}

Widget _buildHeader() {
  return Row(...);
}

// ✅ Tekrar kullanılacaksa ayrı widget sınıfı yap
// ❌ Tekrar kullanılmayacaksa gereksiz sınıf oluşturma
```

### 3.3 Const Kullanımı
```dart
// ✅ Mümkün olan her yerde const kullan
const SizedBox(height: 8),
const EdgeInsets.all(14),
const Text('Hafız Takip'),

// ✅ const constructor tanımla
const StudentCard({super.key, required this.student});
```

### 3.4 Dokunma Hedefleri
```dart
// Minimum 44x44 dokunma alanı (erişilebilirlik)
SizedBox(
  height: 44,
  width: 44,
  child: IconButton(
    onPressed: onPressed,
    icon: Icon(Icons.arrow_back),
  ),
),
```

---

## 4. State Management (Riverpod)

### 4.1 Provider Tipleri
```dart
// Basit değer
final appThemeProvider = Provider<ThemeData>((ref) => AppTheme.light);

// Async veri (API'den)
final studentListProvider = FutureProvider.autoDispose<List<StudentModel>>((ref) async {
  final repo = ref.read(studentRepositoryProvider);
  return repo.getAll();
});

// Parametreli
final studentDetailProvider = FutureProvider.autoDispose.family<StudentModel, String>(
  (ref, studentId) async {
    final repo = ref.read(studentRepositoryProvider);
    return repo.getById(studentId);
  },
);

// Mutable state
final authProvider = StateNotifierProvider<AuthNotifier, AuthState>((ref) {
  return AuthNotifier(ref.read(authRepositoryProvider));
});
```

### 4.2 Provider Kullanım Kuralları
```dart
// ✅ ConsumerWidget veya ConsumerStatefulWidget kullan
class DashboardScreen extends ConsumerWidget {
  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final studentsAsync = ref.watch(studentListProvider);

    return studentsAsync.when(
      loading: () => const LoadingSkeleton(),
      error: (err, stack) => ErrorWidget(message: err.toString()),
      data: (students) => StudentList(students: students),
    );
  }
}

// ✅ ref.watch → build içinde (reaktif)
// ✅ ref.read → callback/handler içinde (tek seferlik)
// ❌ ref.watch → callback içinde KULLANILMAZ
```

### 4.3 Ne Nerede Saklanır?
| Veri | Yöntem | Neden |
|------|--------|-------|
| Auth state (token, user) | `StateNotifierProvider` + SecureStorage | Kalıcı, güvenli |
| Öğrenci listesi | `FutureProvider.autoDispose` | Sunucu verisi, cache'lenir |
| Form state | `StatefulWidget` setState | Geçici, lokal |
| Ayarlar | `Provider` + SharedPreferences | Kalıcı, basit |
| Offline kuyruk | `StateNotifierProvider` + Hive | Senkronizasyon |

---

## 5. API Entegrasyonu

### 5.1 Dio Client
```dart
// core/network/api_client.dart
class ApiClient {
  late final Dio _dio;

  ApiClient() {
    _dio = Dio(BaseOptions(
      baseUrl: ApiConstants.baseUrl,
      connectTimeout: const Duration(seconds: 10),
      receiveTimeout: const Duration(seconds: 10),
      headers: {'Content-Type': 'application/json'},
    ));

    _dio.interceptors.addAll([
      _AuthInterceptor(),      // JWT token ekleme
      _TenantInterceptor(),    // tenant bilgisi
      _LogInterceptor(),       // Debug logları
      _ErrorInterceptor(),     // Hata yönetimi
    ]);
  }
}
```

### 5.2 Model (JSON Serialization)
```dart
// json_serializable veya freezed kullan
@JsonSerializable()
class StudentModel {
  final String id;
  final String tenantId;
  final String name;
  final int currentPage;
  final int currentJuz;
  final String currentSurah;
  final String performance;
  final DateTime createdAt;

  const StudentModel({
    required this.id,
    required this.tenantId,
    required this.name,
    required this.currentPage,
    required this.currentJuz,
    required this.currentSurah,
    required this.performance,
    required this.createdAt,
  });

  factory StudentModel.fromJson(Map<String, dynamic> json) =>
      _$StudentModelFromJson(json);

  Map<String, dynamic> toJson() => _$StudentModelToJson(this);

  // İlerleme yüzdesi
  double get progressPercent => (currentPage / 604) * 100;
}
```

### 5.3 Repository Pattern
```dart
class StudentRepository {
  final StudentRemoteDataSource _remote;
  final StudentLocalDataSource _local;
  final ConnectivityService _connectivity;

  // Offline-first: önce local, sonra remote
  Future<List<StudentModel>> getAll() async {
    if (await _connectivity.isConnected) {
      try {
        final students = await _remote.getAll();
        await _local.cacheStudents(students);  // Cache güncelle
        return students;
      } catch (e) {
        return _local.getCachedStudents();  // API hata → cache
      }
    } else {
      return _local.getCachedStudents();  // Offline → cache
    }
  }
}
```

---

## 6. Tasarım Sistemi

### 6.1 Renkler
```dart
// core/constants/app_colors.dart
class AppColors {
  AppColors._();

  // Temel
  static const bg = Color(0xFFF4EEE0);
  static const card = Color(0xFFFFFDF7);
  static const text = Color(0xFF1C1A14);
  static const muted = Color(0xFF7A7060);
  static const border = Color(0xFFE0D8C0);

  // Yeşil (Dominant)
  static const green = Color(0xFF1B5C3A);
  static const greenLight = Color(0xFF2E7D52);
  static const greenPale = Color(0xFFE8F5EE);

  // Altın (Vurgu)
  static const gold = Color(0xFFB8881A);
  static const goldLight = Color(0xFFD4A827);
  static const goldPale = Color(0xFFFFF8E0);

  // Durum
  static const red = Color(0xFFB03020);
  static const redPale = Color(0xFFFFECEC);
  static const blue = Color(0xFF1A4A8C);
  static const bluePale = Color(0xFFEAF0FF);
  static const purple = Color(0xFF5E2D8C);
  static const purplePale = Color(0xFFF2EAFF);
}
```

### 6.2 Tema
```dart
// core/theme/app_theme.dart
class AppTheme {
  static ThemeData get light => ThemeData(
    scaffoldBackgroundColor: AppColors.bg,
    fontFamily: 'Nunito',
    colorScheme: ColorScheme.fromSeed(
      seedColor: AppColors.green,
      background: AppColors.bg,
      surface: AppColors.card,
    ),
    textTheme: const TextTheme(
      headlineLarge: TextStyle(
        fontFamily: 'Playfair Display',
        fontWeight: FontWeight.w700,
        color: AppColors.green,
      ),
      // ...
    ),
    cardTheme: CardTheme(
      color: AppColors.card,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(14),
        side: BorderSide(color: AppColors.border),
      ),
      elevation: 2,
    ),
  );
}
```

### 6.3 Spacing Sabitleri
```dart
class AppSpacing {
  AppSpacing._();

  static const double xs = 4;
  static const double sm = 8;
  static const double md = 12;
  static const double lg = 16;
  static const double xl = 20;
  static const double xxl = 24;

  static const double radiusSm = 8;
  static const double radiusMd = 12;
  static const double radiusLg = 14;
}
```

---

## 7. Offline Stratejisi

### 7.1 Offline-First Yaklaşımı
```
Online:   API → Ekran + Cache güncelle
Offline:  Cache → Ekran
Yazma:    İşlemi kuyrukla → Online olunca gönder
```

### 7.2 Offline Kuyruk
```dart
// Offline yapılan yazma işlemleri kuyruklanır
class OfflineQueue {
  // Hive'da saklanan kuyruk
  Future<void> enqueue(PendingOperation operation) async { ... }

  // İnternet gelince otomatik işle
  Future<void> processQueue() async {
    final operations = await getPendingOperations();
    for (final op in operations) {
      try {
        await _sendToApi(op);
        await _markAsCompleted(op.id);
      } catch (e) {
        // Başarısız → kuyruğa geri koy, retry
      }
    }
  }
}
```

### 7.3 Çakışma Yönetimi
```
Strateji: Last-Write-Wins (updated_at karşılaştırması)

1. Offline'da kayıt güncellenir → local updated_at = now()
2. Online olunca API'ye gönderilir
3. API, server'daki updated_at ile karşılaştırır
4. Client daha yeni → güncelle
5. Server daha yeni → çakışma bildirimi → kullanıcı seçer
```

---

## 8. Navigasyon (GoRouter)

```dart
final appRouter = GoRouter(
  initialLocation: '/login',
  redirect: (context, state) {
    final isLoggedIn = /* auth check */;
    if (!isLoggedIn && !state.matchedLocation.startsWith('/login')) {
      return '/login';
    }
    return null;
  },
  routes: [
    GoRoute(path: '/login', builder: (_, __) => const LoginScreen()),
    ShellRoute(
      builder: (_, __, child) => AppShell(child: child),  // BottomNav
      routes: [
        GoRoute(path: '/dashboard', builder: (_, __) => const DashboardScreen()),
        GoRoute(path: '/students', builder: (_, __) => const StudentListScreen()),
        GoRoute(path: '/students/:id', builder: (_, state) =>
          StudentDetailScreen(id: state.pathParameters['id']!)),
        GoRoute(path: '/lessons', builder: (_, __) => const LessonListScreen()),
        GoRoute(path: '/attendance', builder: (_, __) => const AttendanceScreen()),
        GoRoute(path: '/etut', builder: (_, __) => const EtutScreen()),
        GoRoute(path: '/parents', builder: (_, __) => const ParentScreen()),
        GoRoute(path: '/settings', builder: (_, __) => const SettingsScreen()),
      ],
    ),
  ],
);
```

---

## 9. Test Kuralları

```dart
// Widget testi: her ekran için temel render testi
testWidgets('DashboardScreen renders stat cards', (tester) async {
  await tester.pumpWidget(
    ProviderScope(
      overrides: [
        studentListProvider.overrideWith((_) => [mockStudent]),
      ],
      child: MaterialApp(home: DashboardScreen()),
    ),
  );

  expect(find.text('Toplam Öğrenci'), findsOneWidget);
});

// Repository testi: mock data source ile
test('StudentRepository returns cached data when offline', () async {
  when(connectivity.isConnected).thenAnswer((_) async => false);
  when(localDs.getCachedStudents()).thenAnswer((_) async => [mockStudent]);

  final result = await repository.getAll();

  expect(result.length, 1);
  verify(localDs.getCachedStudents()).called(1);
  verifyNever(remoteDs.getAll());
});
```

---

## 10. Diğer Katmanları Tanıma

### 10.1 Backend Hakkında
- **.NET 8 N-Tier:** API yanıtları `{ success: bool, message: string, data: T }` formatında
- **Auth:** `POST /api/auth/send-otp` → `POST /api/auth/verify-otp` → JWT döner
- **JWT:** `Authorization: Bearer {token}` header'ı ile gönderilir
- **Multi-tenant:** Backend tenant_id'yi JWT'den çözer, mobil tenant_id göndermeye gerek yok
- **Pagination:** `?page=1&pageSize=20`, yanıtta `totalCount` döner

### 10.2 Veritabanı Hakkında
- Tarihler ISO 8601 formatında (`2026-05-11T00:00:00Z`)
- UUID string olarak kullanılır
- Enum'lar string: `'head_teacher'`, `'present'`, `'Mükemmel'`
- Soft delete: silinen kayıtlar API'den dönmez

### 10.3 Frontend (Web) Hakkında
- **Aynı API, aynı tasarım dili** — mobil ve web tutarlı olmalı
- Web: React + TypeScript, Mobil: Flutter + Dart
- Renk paleti, font ailesi, kart yapıları **birebir aynı**
- Özellik paritesi: web'de olan her özellik mobilde de olmalı
- Farklılıklar:
  - Web: PWA ile offline → Service Worker + IndexedDB
  - Mobil: Hive/Isar ile offline → daha güçlü offline deneyim
  - Mobil: push notification (FCM), web: browser notification
  - Mobil: WhatsApp deep-link (url_launcher), web: `wa.me` linki

---

## 11. Platform Spesifik Kurallar

### 11.1 Android
```yaml
# android/app/build.gradle
minSdkVersion: 21
targetSdkVersion: 34
```

### 11.2 iOS
```yaml
# ios/Runner/Info.plist
MinimumOSVersion: 13.0
```

### 11.3 İzinler
```
Android:
  - INTERNET
  - ACCESS_NETWORK_STATE
  - CAMERA (fotoğraf yükleme)
  - READ_EXTERNAL_STORAGE (dosya seçme)

iOS:
  - NSCameraUsageDescription
  - NSPhotoLibraryUsageDescription
```

---

## 12. Paketler (pubspec.yaml)

```yaml
dependencies:
  flutter:
    sdk: flutter

  # State Management
  flutter_riverpod: ^2.x
  riverpod_annotation: ^2.x

  # Networking
  dio: ^5.x
  connectivity_plus: ^6.x

  # Routing
  go_router: ^14.x

  # Storage
  hive_flutter: ^1.x        # Offline cache
  flutter_secure_storage: ^9.x  # Token saklama
  shared_preferences: ^2.x

  # UI
  google_fonts: ^6.x         # Playfair Display, Nunito
  flutter_svg: ^2.x
  shimmer: ^3.x              # Loading skeleton
  fl_chart: ^0.x             # Grafikler
  intl: ^0.x                 # Tarih formatı (Türkçe)

  # Utilities
  url_launcher: ^6.x         # WhatsApp deep-link
  json_annotation: ^4.x
  freezed_annotation: ^2.x
  image_picker: ^1.x         # Fotoğraf seçme

dev_dependencies:
  build_runner: ^2.x
  json_serializable: ^6.x
  freezed: ^2.x
  riverpod_generator: ^2.x
  flutter_lints: ^4.x
  mockito: ^5.x
```

---

*Son güncelleme: Temmuz 2026*
