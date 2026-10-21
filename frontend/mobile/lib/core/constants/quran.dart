/// Türkiye ayetberkenar mushafı (backend: QuranConstants).
class Quran {
  Quran._();

  static const totalPages = 604;
  static const totalJuz = 30;

  /// Ayetberkenar mushafta 1. cüz 1–21. sayfalar, diğerleri 20'şer sayfadır (ör. 2. cüz 22–41).
  /// Backend'deki QuranConstants.JuzOfPage ile aynı hesap.
  static int juzOfPage(int page) => page <= 21 ? 1 : ((page - 2) ~/ 20 + 1).clamp(1, totalJuz);

  static int clampPage(int page) => page.clamp(1, totalPages);
}
