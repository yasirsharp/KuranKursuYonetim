import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:shared_preferences/shared_preferences.dart';

/// İlk açılış tanıtımı görüldü mü? (null: henüz okunmadı)
final onboardingProvider = NotifierProvider<OnboardingNotifier, bool?>(OnboardingNotifier.new);

class OnboardingNotifier extends Notifier<bool?> {
  /// İçerik değişirse sürüm artırılarak tanıtım yeniden gösterilebilir.
  static const storageKey = 'hafiz_takip_onboarding_seen_v1';

  @override
  bool? build() {
    Future.microtask(_load);
    return null;
  }

  Future<void> _load() async {
    try {
      final prefs = await SharedPreferences.getInstance();
      // Okuma sürerken tanıtım tamamlandıysa (veya yeniden başlatıldıysa) o sonuç korunur
      state ??= prefs.getBool(storageKey) ?? false;
    } catch (_) {
      state ??= true; // depolama okunamıyorsa kullanıcıyı bekletmeyelim
    }
  }

  Future<void> complete() async {
    state = true;
    try {
      final prefs = await SharedPreferences.getInstance();
      await prefs.setBool(storageKey, true);
    } catch (_) {}
  }

  /// Profil ekranındaki "Tanıtımı tekrar izle".
  void replay() => state = false;
}
