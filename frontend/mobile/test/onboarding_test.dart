import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:hafiz_takip/core/theme/app_theme.dart';
import 'package:hafiz_takip/presentation/providers/onboarding_provider.dart';
import 'package:hafiz_takip/presentation/screens/onboarding/onboarding_screen.dart';
import 'package:shared_preferences/shared_preferences.dart';

void main() {
  setUp(() => SharedPreferences.setMockInitialValues({}));

  Future<ProviderContainer> pump(WidgetTester tester) async {
    final container = ProviderContainer();
    addTearDown(container.dispose);
    await tester.pumpWidget(UncontrolledProviderScope(
      container: container,
      child: MaterialApp(theme: AppTheme.light, home: const OnboardingScreen()),
    ));
    await tester.pump(const Duration(milliseconds: 100));
    return container;
  }

  testWidgets('dört adımda ilerler, sonunda tanıtımı tamamlar ve kaydeder', (tester) async {
    final container = await pump(tester);

    expect(find.text("Hafız Takip'e hoş geldiniz"), findsOneWidget);
    for (final title in ['Dersi üç dokunuşta kaydedin', 'Yoklama tek dokunuş', 'Hemen başlayalım']) {
      await tester.tap(find.text('Devam'));
      for (var i = 0; i < 8; i++) {
        await tester.pump(const Duration(milliseconds: 100));
      }
      expect(find.text(title), findsOneWidget);
    }

    expect(find.text('Giriş yap'), findsOneWidget);
    await tester.tap(find.text('Giriş yap'));
    await tester.pump(const Duration(milliseconds: 100));

    expect(container.read(onboardingProvider), isTrue);
    final prefs = await SharedPreferences.getInstance();
    expect(prefs.getBool(OnboardingNotifier.storageKey), isTrue);
  });

  testWidgets('"Geç" tanıtımı hemen tamamlar; kaydırarak da ilerlenir', (tester) async {
    final container = await pump(tester);

    await tester.fling(find.text("Hafız Takip'e hoş geldiniz"), const Offset(-400, 0), 1000);
    await tester.pump(const Duration(milliseconds: 600));
    expect(find.text('Dersi üç dokunuşta kaydedin'), findsOneWidget);

    await tester.tap(find.text('Geç'));
    await tester.pump(const Duration(milliseconds: 100));
    expect(container.read(onboardingProvider), isTrue);
  });
}
