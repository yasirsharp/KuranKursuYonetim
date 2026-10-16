-- =============================================================
-- 007_course_applications.sql
-- Kurs başvurusu ve platform onayı:
--   * Başvuruyla açılan kurs 'pending' durumunda başlar; platform yöneticisi
--     onaylayınca 'active', reddederse 'rejected' olur (gerekçesiyle).
--   * Başvuru bilgileri: il, ilçe, adres, başvuru zamanı; inceleme bilgileri.
--   * tenants.status yalnızca bilinen değerleri alabilir.
-- Tekrar çalıştırılabilir.
-- =============================================================

ALTER TABLE tenants ADD COLUMN IF NOT EXISTS city VARCHAR(100);
ALTER TABLE tenants ADD COLUMN IF NOT EXISTS district VARCHAR(100);
ALTER TABLE tenants ADD COLUMN IF NOT EXISTS address VARCHAR(500);
ALTER TABLE tenants ADD COLUMN IF NOT EXISTS submitted_at TIMESTAMPTZ;
ALTER TABLE tenants ADD COLUMN IF NOT EXISTS reviewed_at TIMESTAMPTZ;
ALTER TABLE tenants ADD COLUMN IF NOT EXISTS reviewed_by UUID REFERENCES platform_users(id);
ALTER TABLE tenants ADD COLUMN IF NOT EXISTS rejection_reason VARCHAR(500);

-- Bilinmeyen durum değerleri (varsa) askıya alınmış sayılır; kontrol kısıtı ancak bundan sonra eklenebilir
UPDATE tenants SET status = 'suspended'
WHERE status NOT IN ('pending', 'trial', 'active', 'suspended', 'cancelled', 'rejected');

ALTER TABLE tenants DROP CONSTRAINT IF EXISTS ck_tenants_status;
ALTER TABLE tenants ADD CONSTRAINT ck_tenants_status
    CHECK (status IN ('pending', 'trial', 'active', 'suspended', 'cancelled', 'rejected'));

-- Platform panelinde onay bekleyen başvuruların listesi
CREATE INDEX IF NOT EXISTS idx_tenants_status ON tenants(status) WHERE is_deleted = false;
