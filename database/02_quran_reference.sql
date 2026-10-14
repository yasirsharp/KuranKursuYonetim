-- =============================================================
-- 02_quran_reference.sql — Kur'an referans verisi (platform seviyesi, tenant_id yok)
--
-- Mushaf düzeni: Türkiye ayetberkenar hafız mushafı (604 sayfa).
--   Cüz eşlemesi formüldür: cüz 1 = s.1-21, cüz n = s.(20n-18)…(20n+1), cüz 30 = s.582-604.
--   Sure başlangıç sayfaları aşağıdaki tablodadır.
--
-- ⚠ DOĞRULAMA GEREKİR: Sure başlangıç sayfaları ilk taslak olarak hazırlanmıştır.
--   Bir hocanın Türkiye ayetberkenar mushafından database/KURAN_REFERANS_KONTROL.md
--   listesiyle kontrol etmesi ve farkları bu dosyaya işlemesi gerekir.
--
-- Bu dosya tüm ortamlarda çalışır ve tekrar çalıştırılabilir (tabloları gerekirse oluşturur,
-- veriyi günceller). Migration'lardan ÖNCE çalıştırılır (bkz. database/db.ps1).
-- =============================================================

SET client_encoding = 'UTF8';

CREATE TABLE IF NOT EXISTS quran_surahs (
    id INTEGER PRIMARY KEY CHECK (id BETWEEN 1 AND 114),
    name_tr VARCHAR(50) NOT NULL,
    ayah_count INTEGER NOT NULL CHECK (ayah_count > 0),
    start_page INTEGER NOT NULL CHECK (start_page BETWEEN 1 AND 604)
);

CREATE TABLE IF NOT EXISTS quran_pages (
    page INTEGER PRIMARY KEY CHECK (page BETWEEN 1 AND 604),
    juz INTEGER NOT NULL CHECK (juz BETWEEN 1 AND 30),
    first_surah_id INTEGER NOT NULL REFERENCES quran_surahs(id),
    last_surah_id INTEGER NOT NULL REFERENCES quran_surahs(id)
);

-- ── 114 sure: (sıra, ad, ayet sayısı, başlangıç sayfası) ─────────
INSERT INTO quran_surahs (id, name_tr, ayah_count, start_page) VALUES
    (  1, 'Fatiha', 7, 1),
    (  2, 'Bakara', 286, 2),
    (  3, 'Al-i İmran', 200, 50),
    (  4, 'Nisa', 176, 77),
    (  5, 'Maide', 120, 106),
    (  6, 'Enam', 165, 128),
    (  7, 'Araf', 206, 151),
    (  8, 'Enfal', 75, 177),
    (  9, 'Tevbe', 129, 187),
    ( 10, 'Yunus', 109, 208),
    ( 11, 'Hud', 123, 221),
    ( 12, 'Yusuf', 111, 235),
    ( 13, 'Rad', 43, 249),
    ( 14, 'İbrahim', 52, 255),
    ( 15, 'Hicr', 99, 262),
    ( 16, 'Nahl', 128, 267),
    ( 17, 'İsra', 111, 282),
    ( 18, 'Kehf', 110, 293),
    ( 19, 'Meryem', 98, 305),
    ( 20, 'Taha', 135, 312),
    ( 21, 'Enbiya', 112, 322),
    ( 22, 'Hac', 78, 332),
    ( 23, 'Müminun', 118, 342),
    ( 24, 'Nur', 64, 350),
    ( 25, 'Furkan', 77, 359),
    ( 26, 'Şuara', 227, 367),
    ( 27, 'Neml', 93, 377),
    ( 28, 'Kasas', 88, 385),
    ( 29, 'Ankebut', 69, 396),
    ( 30, 'Rum', 60, 404),
    ( 31, 'Lokman', 34, 411),
    ( 32, 'Secde', 30, 415),
    ( 33, 'Ahzab', 73, 418),
    ( 34, 'Sebe', 54, 428),
    ( 35, 'Fatır', 45, 434),
    ( 36, 'Yasin', 83, 440),
    ( 37, 'Saffat', 182, 446),
    ( 38, 'Sad', 88, 453),
    ( 39, 'Zümer', 75, 458),
    ( 40, 'Mümin', 85, 467),
    ( 41, 'Fussilet', 54, 477),
    ( 42, 'Şura', 53, 483),
    ( 43, 'Zuhruf', 89, 489),
    ( 44, 'Duhan', 59, 496),
    ( 45, 'Casiye', 37, 499),
    ( 46, 'Ahkaf', 35, 502),
    ( 47, 'Muhammed', 38, 507),
    ( 48, 'Fetih', 29, 511),
    ( 49, 'Hucurat', 18, 515),
    ( 50, 'Kaf', 45, 518),
    ( 51, 'Zariyat', 60, 520),
    ( 52, 'Tur', 49, 523),
    ( 53, 'Necm', 62, 526),
    ( 54, 'Kamer', 55, 528),
    ( 55, 'Rahman', 78, 531),
    ( 56, 'Vakıa', 96, 534),
    ( 57, 'Hadid', 29, 537),
    ( 58, 'Mücadele', 22, 542),
    ( 59, 'Haşr', 24, 545),
    ( 60, 'Mümtehine', 13, 549),
    ( 61, 'Saf', 14, 551),
    ( 62, 'Cuma', 11, 553),
    ( 63, 'Münafikun', 11, 554),
    ( 64, 'Teğabün', 18, 556),
    ( 65, 'Talak', 12, 558),
    ( 66, 'Tahrim', 12, 560),
    ( 67, 'Mülk', 30, 562),
    ( 68, 'Kalem', 52, 564),
    ( 69, 'Hakka', 52, 566),
    ( 70, 'Mearic', 44, 568),
    ( 71, 'Nuh', 28, 570),
    ( 72, 'Cin', 28, 572),
    ( 73, 'Müzzemmil', 20, 574),
    ( 74, 'Müddessir', 56, 575),
    ( 75, 'Kıyame', 40, 577),
    ( 76, 'İnsan', 31, 578),
    ( 77, 'Mürselat', 50, 580),
    ( 78, 'Nebe', 40, 582),
    ( 79, 'Naziat', 46, 583),
    ( 80, 'Abese', 42, 585),
    ( 81, 'Tekvir', 29, 586),
    ( 82, 'İnfitar', 19, 587),
    ( 83, 'Mutaffifin', 36, 587),
    ( 84, 'İnşikak', 25, 589),
    ( 85, 'Büruc', 22, 590),
    ( 86, 'Tarık', 17, 591),
    ( 87, 'Ala', 19, 591),
    ( 88, 'Gaşiye', 26, 592),
    ( 89, 'Fecr', 30, 593),
    ( 90, 'Beled', 20, 594),
    ( 91, 'Şems', 15, 595),
    ( 92, 'Leyl', 21, 595),
    ( 93, 'Duha', 11, 596),
    ( 94, 'İnşirah', 8, 596),
    ( 95, 'Tin', 8, 597),
    ( 96, 'Alak', 19, 597),
    ( 97, 'Kadir', 5, 598),
    ( 98, 'Beyyine', 8, 598),
    ( 99, 'Zilzal', 8, 599),
    (100, 'Adiyat', 11, 599),
    (101, 'Karia', 11, 600),
    (102, 'Tekasür', 8, 600),
    (103, 'Asr', 3, 601),
    (104, 'Hümeze', 9, 601),
    (105, 'Fil', 5, 601),
    (106, 'Kureyş', 4, 602),
    (107, 'Maun', 7, 602),
    (108, 'Kevser', 3, 602),
    (109, 'Kafirun', 6, 603),
    (110, 'Nasr', 3, 603),
    (111, 'Tebbet', 5, 603),
    (112, 'İhlas', 4, 604),
    (113, 'Felak', 5, 604),
    (114, 'Nas', 6, 604)
ON CONFLICT (id) DO UPDATE
    SET name_tr = EXCLUDED.name_tr,
        ayah_count = EXCLUDED.ayah_count,
        start_page = EXCLUDED.start_page;

-- ── 604 sayfa: cüz (formül) ve sayfadaki ilk/son sure ────────────
-- İlk sure: o sayfada başlayan en küçük sıralı sure; sayfada sure başlamıyorsa önceki sayfadan süren sure.
-- Son sure: o sayfaya kadar başlamış en büyük sıralı sure.
DELETE FROM quran_pages;
INSERT INTO quran_pages (page, juz, first_surah_id, last_surah_id)
SELECT p.page,
       CASE WHEN p.page <= 21 THEN 1
            ELSE LEAST(30, (p.page - 2) / 20 + 1) END,
       COALESCE((SELECT min(s.id) FROM quran_surahs s WHERE s.start_page = p.page),
                (SELECT max(s.id) FROM quran_surahs s WHERE s.start_page < p.page)),
       (SELECT max(s.id) FROM quran_surahs s WHERE s.start_page <= p.page)
FROM generate_series(1, 604) AS p(page);