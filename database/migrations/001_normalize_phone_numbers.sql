-- =============================================================
-- 001_normalize_phone_numbers.sql
-- users.phone_number değerlerini kanonik biçime çevirir:
-- ülke kodu ve baştaki 0 olmadan 10 hane (örn. 5321234567).
-- Backend (PhoneNumberHelper.Normalize) numaraları bu biçimde arar ve saklar.
-- Tekrar çalıştırılabilir.
-- =============================================================

UPDATE users
SET phone_number = CASE
        WHEN regexp_replace(phone_number, '\D', '', 'g') ~ '^90\d{10}$'
            THEN substring(regexp_replace(phone_number, '\D', '', 'g') FROM 3)
        WHEN regexp_replace(phone_number, '\D', '', 'g') ~ '^0\d{10}$'
            THEN substring(regexp_replace(phone_number, '\D', '', 'g') FROM 2)
        ELSE regexp_replace(phone_number, '\D', '', 'g')
    END,
    updated_at = now()
WHERE phone_number !~ '^\d{10}$';
