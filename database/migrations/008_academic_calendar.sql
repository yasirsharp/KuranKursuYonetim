-- =============================================================
-- 008_academic_calendar.sql
-- Eğitim dönemleri ve kurs takvimi:
--   * academic_terms: kurs müdürünün açtığı dönemler (ör. 14.09.2026–28.08.2027).
--     Aynı kursun silinmemiş dönemleri çakışamaz.
--   * calendar_events: resmi/dini tatil, ara tatil, kurs kapalı günü ve etkinlikler.
--     is_day_off = true olan kayıtlar ders günü hesabından düşülür.
--     holiday_key, otomatik eklenen tatillerin tekrar eklenmesini önler.
-- Tekrar çalıştırılabilir.
-- =============================================================

-- Çakışma kısıtında (tenant_id =) eşitliği için gerekli
CREATE EXTENSION IF NOT EXISTS btree_gist;

CREATE TABLE IF NOT EXISTS academic_terms (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
    name VARCHAR(100) NOT NULL,
    start_date DATE NOT NULL,
    end_date DATE NOT NULL,
    notes VARCHAR(1000),

    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ,
    is_deleted BOOLEAN NOT NULL DEFAULT false,
    deleted_at TIMESTAMPTZ,

    CONSTRAINT uq_academic_terms_tenant_id_id UNIQUE (tenant_id, id),
    CONSTRAINT ck_academic_terms_range CHECK (start_date < end_date),
    CONSTRAINT ex_academic_terms_overlap EXCLUDE USING gist (
        tenant_id WITH =,
        daterange(start_date, end_date, '[]') WITH &&
    ) WHERE (is_deleted = false)
);

CREATE INDEX IF NOT EXISTS idx_academic_terms_tenant ON academic_terms(tenant_id, start_date) WHERE is_deleted = false;

CREATE TABLE IF NOT EXISTS calendar_events (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
    term_id UUID,
    title VARCHAR(200) NOT NULL,
    -- 'official_holiday' | 'religious_holiday' | 'break' | 'closure' | 'event'
    event_type VARCHAR(30) NOT NULL,
    start_date DATE NOT NULL,
    end_date DATE NOT NULL,
    is_day_off BOOLEAN NOT NULL DEFAULT true,
    description VARCHAR(1000),
    holiday_key VARCHAR(60),

    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ,
    is_deleted BOOLEAN NOT NULL DEFAULT false,
    deleted_at TIMESTAMPTZ,

    CONSTRAINT fk_calendar_events_term FOREIGN KEY (tenant_id, term_id)
        REFERENCES academic_terms(tenant_id, id),
    CONSTRAINT ck_calendar_events_range CHECK (start_date <= end_date),
    CONSTRAINT ck_calendar_events_type CHECK (event_type IN ('official_holiday', 'religious_holiday', 'break', 'closure', 'event'))
);

CREATE INDEX IF NOT EXISTS idx_calendar_events_tenant_dates ON calendar_events(tenant_id, start_date, end_date) WHERE is_deleted = false;
CREATE UNIQUE INDEX IF NOT EXISTS uq_calendar_events_holiday_key
    ON calendar_events(tenant_id, holiday_key) WHERE is_deleted = false AND holiday_key IS NOT NULL;
