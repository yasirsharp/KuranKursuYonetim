-- =============================================================
-- 004_normalize_status_values.sql
-- Durum alanlarını uygulamanın kod sözlüğüne (Core/Enums) çevirir.
-- Önceden aynı anlam için farklı değerler kullanılıyordu
-- (ör. yoklama: 'var' / 'geldi' / 'present'); raporlar yanlış sayıyordu.
--
--   Yoklama   : present | absent | late | excused
--   Performans: mukemmel | cok_iyi | iyi | orta | zayif
--   Ödev      : yapildi | eksik | yapilmadi | mazeretli
--   Sınav türü: hafizlik | tecvid | okuma | deneme
--
-- Tanınmayan değerler değiştirilmez; sayıları NOTICE olarak raporlanır.
-- Tekrar çalıştırılabilir.
-- =============================================================

SET client_encoding = 'UTF8';

-- Türkçe karakterleri ASCII'ye katlayıp küçük harfe çevirir ('Çok İyi' → 'cok iyi')
CREATE OR REPLACE FUNCTION pg_temp.fold(value text) RETURNS text AS $$
    SELECT lower(translate(trim(value), 'İIıŞşĞğÜüÖöÇç', 'iiissgguuoocc'));
$$ LANGUAGE sql IMMUTABLE;

-- ── Yoklama ──────────────────────────────────────────────────────
UPDATE attendances SET status = CASE pg_temp.fold(status)
        WHEN 'present' THEN 'present' WHEN 'var' THEN 'present' WHEN 'geldi' THEN 'present'
        WHEN 'absent' THEN 'absent' WHEN 'yok' THEN 'absent' WHEN 'gelmedi' THEN 'absent' WHEN 'devamsiz' THEN 'absent'
        WHEN 'late' THEN 'late' WHEN 'gec' THEN 'late' WHEN 'gec geldi' THEN 'late'
        WHEN 'excused' THEN 'excused' WHEN 'izinli' THEN 'excused' WHEN 'mazeretli' THEN 'excused' WHEN 'raporlu' THEN 'excused'
        ELSE status END,
    updated_at = now()
WHERE status NOT IN ('present', 'absent', 'late', 'excused');

-- ── Performans (ders ve tekrar oturumu) ──────────────────────────
UPDATE lessons SET performance = CASE pg_temp.fold(performance)
        WHEN 'mukemmel' THEN 'mukemmel'
        WHEN 'cok iyi' THEN 'cok_iyi' WHEN 'cok_iyi' THEN 'cok_iyi' WHEN 'pekiyi' THEN 'cok_iyi'
        WHEN 'iyi' THEN 'iyi'
        WHEN 'orta' THEN 'orta' WHEN 'hasne' THEN 'orta' WHEN 'hasen' THEN 'orta'
        WHEN 'zayif' THEN 'zayif'
        ELSE performance END,
    updated_at = now()
WHERE performance NOT IN ('mukemmel', 'cok_iyi', 'iyi', 'orta', 'zayif');

UPDATE repetition_sessions SET performance = CASE pg_temp.fold(performance)
        WHEN 'mukemmel' THEN 'mukemmel'
        WHEN 'cok iyi' THEN 'cok_iyi' WHEN 'cok_iyi' THEN 'cok_iyi' WHEN 'pekiyi' THEN 'cok_iyi'
        WHEN 'iyi' THEN 'iyi'
        WHEN 'orta' THEN 'orta' WHEN 'hasne' THEN 'orta' WHEN 'hasen' THEN 'orta'
        WHEN 'zayif' THEN 'zayif'
        ELSE performance END,
    updated_at = now()
WHERE performance IS NOT NULL
  AND performance NOT IN ('mukemmel', 'cok_iyi', 'iyi', 'orta', 'zayif');

-- ── Ödev durumu (ders ve etüt notu) ──────────────────────────────
UPDATE lessons SET homework_status = CASE pg_temp.fold(homework_status)
        WHEN 'yapildi' THEN 'yapildi'
        WHEN 'eksik' THEN 'eksik'
        WHEN 'yapilmadi' THEN 'yapilmadi'
        WHEN 'mazeretli' THEN 'mazeretli' WHEN 'mazeret' THEN 'mazeretli'
        ELSE homework_status END,
    updated_at = now()
WHERE homework_status NOT IN ('yapildi', 'eksik', 'yapilmadi', 'mazeretli');

UPDATE etut_notes SET homework_status = CASE pg_temp.fold(homework_status)
        WHEN 'yapildi' THEN 'yapildi'
        WHEN 'eksik' THEN 'eksik'
        WHEN 'yapilmadi' THEN 'yapilmadi'
        WHEN 'mazeretli' THEN 'mazeretli' WHEN 'mazeret' THEN 'mazeretli'
        ELSE homework_status END,
    updated_at = now()
WHERE homework_status NOT IN ('yapildi', 'eksik', 'yapilmadi', 'mazeretli');

-- ── Sınav türü ───────────────────────────────────────────────────
UPDATE exams SET exam_type = pg_temp.fold(exam_type), updated_at = now()
WHERE exam_type NOT IN ('hafizlik', 'tecvid', 'okuma', 'deneme')
  AND pg_temp.fold(exam_type) IN ('hafizlik', 'tecvid', 'okuma', 'deneme');

-- ── Kolon varsayılanları ─────────────────────────────────────────
ALTER TABLE lessons ALTER COLUMN performance SET DEFAULT 'iyi';
ALTER TABLE lessons ALTER COLUMN homework_status SET DEFAULT 'yapilmadi';

-- ── Tanınmayan değer raporu ──────────────────────────────────────
DO $$
DECLARE
    unknown_count integer;
BEGIN
    SELECT
        (SELECT count(*) FROM attendances WHERE status NOT IN ('present', 'absent', 'late', 'excused')) +
        (SELECT count(*) FROM lessons WHERE performance NOT IN ('mukemmel', 'cok_iyi', 'iyi', 'orta', 'zayif')) +
        (SELECT count(*) FROM lessons WHERE homework_status NOT IN ('yapildi', 'eksik', 'yapilmadi', 'mazeretli')) +
        (SELECT count(*) FROM etut_notes WHERE homework_status NOT IN ('yapildi', 'eksik', 'yapilmadi', 'mazeretli')) +
        (SELECT count(*) FROM exams WHERE exam_type NOT IN ('hafizlik', 'tecvid', 'okuma', 'deneme'))
    INTO unknown_count;

    IF unknown_count > 0 THEN
        RAISE NOTICE '004: % kayıtta tanınmayan durum değeri kaldı; elle kontrol edin.', unknown_count;
    END IF;
END $$;
