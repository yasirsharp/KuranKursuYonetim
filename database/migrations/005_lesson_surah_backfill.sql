-- =============================================================
-- 005_lesson_surah_backfill.sql
-- Mevcut ders kayıtlarının sure ve cüz bilgisini sayfa numarasından yeniden hesaplar.
-- Önceden ders kaydında sura_id her zaman 0 yazılıyor, cüz ise istemciden olduğu gibi
-- alınıyordu. Artık ikisi de sayfadan türetilir (ayetberkenar düzeni, quran_pages).
--
-- Ön koşul: database/02_quran_reference.sql çalıştırılmış olmalı
-- (database/db.ps1 -Migrate bunu migration'lardan önce otomatik yapar).
-- Tekrar çalıştırılabilir.
-- =============================================================

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM quran_pages) THEN
        RAISE EXCEPTION '005: önce database/02_quran_reference.sql çalıştırılmalı (quran_pages boş).';
    END IF;
END $$;

UPDATE lessons l
SET juz_number = qp.juz,
    sura_id = qp.first_surah_id,
    updated_at = now()
FROM quran_pages qp
WHERE qp.page = l.page_number
  AND (l.juz_number IS DISTINCT FROM qp.juz OR l.sura_id IS DISTINCT FROM qp.first_surah_id);

DO $$
DECLARE
    invalid_count integer;
BEGIN
    SELECT count(*) INTO invalid_count FROM lessons WHERE page_number NOT BETWEEN 1 AND 604;
    IF invalid_count > 0 THEN
        RAISE NOTICE '005: % ders kaydında geçersiz sayfa numarası var (1-604 dışında); sure/cüz hesaplanamadı.', invalid_count;
    END IF;
END $$;
