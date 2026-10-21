-- =============================================================
-- 009_user_email_login.sql
-- E-posta ile giriş:
--   * users.email boşluksuz ve küçük harfle saklanır; boş metin NULL olur.
--   * E-postası olmayan kurs yöneticisine kursun iletişim e-postası atanır
--     (kurs başına en eski yönetici; adres o kursta başka kullanıcıda yoksa).
--   * Bir kursta aynı e-posta iki kullanıcıda olamaz; girişte adres tüm kurslarda aranır.
-- Aynı kursta aynı e-postayı kullanan birden fazla kullanıcı varsa migration durur
-- ve hiçbir değişiklik yapılmaz (kayıtların elle düzeltilmesi gerekir).
-- Tekrar çalıştırılabilir.
-- =============================================================

UPDATE users
SET email = NULLIF(lower(btrim(email)), '')
WHERE email IS DISTINCT FROM NULLIF(lower(btrim(email)), '');

UPDATE users u
SET email = lower(btrim(t.contact_email))
FROM tenants t
WHERE u.tenant_id = t.id
  AND u.email IS NULL
  AND NULLIF(btrim(t.contact_email), '') IS NOT NULL
  AND u.id = (
      SELECT a.id FROM users a
      WHERE a.tenant_id = t.id AND a.role = 'tenant_admin' AND a.is_deleted = false
      ORDER BY a.created_at, a.id
      LIMIT 1)
  AND NOT EXISTS (
      SELECT 1 FROM users o
      WHERE o.tenant_id = t.id AND o.is_deleted = false AND o.email = lower(btrim(t.contact_email)));

DO $$
DECLARE duplicate_count INTEGER;
BEGIN
    SELECT count(*) INTO duplicate_count FROM (
        SELECT tenant_id, email FROM users
        WHERE email IS NOT NULL AND is_deleted = false
        GROUP BY tenant_id, email HAVING count(*) > 1) d;
    IF duplicate_count > 0 THEN
        RAISE EXCEPTION '009: % kursta aynı e-postayı kullanan birden fazla kullanıcı var; önce bu kayıtları düzeltin.', duplicate_count;
    END IF;
END $$;

CREATE UNIQUE INDEX IF NOT EXISTS uq_users_tenant_email
    ON users(tenant_id, lower(email)) WHERE email IS NOT NULL AND is_deleted = false;

-- Giriş: adres tüm kurslarda aranır
CREATE INDEX IF NOT EXISTS idx_users_email_lower
    ON users(lower(email)) WHERE email IS NOT NULL AND is_deleted = false;
