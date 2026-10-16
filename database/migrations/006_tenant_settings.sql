-- =============================================================
-- 006_tenant_settings.sql
-- Kurs ayarları: her kursa tek satır. Ders programı (çalışma günleri/saatleri),
-- devamsızlık uyarısı, akıllı tekrar ve veli bildirimi tercihleri buradan okunur.
-- Mevcut kurslar için varsayılan ayar satırı oluşturulur.
-- Tekrar çalıştırılabilir.
-- =============================================================

CREATE TABLE IF NOT EXISTS tenant_settings (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,

    -- Çalışma günleri: 1 = Pazartesi … 7 = Pazar
    working_days INTEGER[] NOT NULL DEFAULT '{1,2,3,4,5,6}',
    day_start_time TIME NOT NULL DEFAULT '08:00',
    day_end_time TIME NOT NULL DEFAULT '17:00',
    default_slot_minutes INTEGER NOT NULL DEFAULT 45 CHECK (default_slot_minutes BETWEEN 5 AND 240),
    time_zone VARCHAR(64) NOT NULL DEFAULT 'Europe/Istanbul',

    -- Devamsızlık uyarısı: absence_period_days gün içinde absence_threshold devamsızlık
    absence_threshold INTEGER NOT NULL DEFAULT 5 CHECK (absence_threshold BETWEEN 1 AND 365),
    absence_period_days INTEGER NOT NULL DEFAULT 30 CHECK (absence_period_days BETWEEN 1 AND 365),

    etut_start_time TIME,

    -- Akıllı tekrar: 'spaced' (aralıklı) | 'cyclic' (döngüsel)
    review_mode VARCHAR(20) NOT NULL DEFAULT 'spaced',
    daily_review_target_pages INTEGER NOT NULL DEFAULT 20 CHECK (daily_review_target_pages BETWEEN 1 AND 604),

    notify_parent_on_absence BOOLEAN NOT NULL DEFAULT true,
    notify_parent_on_milestone BOOLEAN NOT NULL DEFAULT true,

    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ,
    is_deleted BOOLEAN NOT NULL DEFAULT false,
    deleted_at TIMESTAMPTZ,

    CONSTRAINT uq_tenant_settings_tenant UNIQUE (tenant_id),
    CONSTRAINT ck_tenant_settings_day_range CHECK (day_start_time < day_end_time)
);

-- Mevcut kurslar için varsayılan ayarlar
INSERT INTO tenant_settings (tenant_id)
SELECT t.id FROM tenants t
WHERE NOT EXISTS (SELECT 1 FROM tenant_settings s WHERE s.tenant_id = t.id);
