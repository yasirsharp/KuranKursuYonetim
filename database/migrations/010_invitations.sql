-- =============================================================
-- 010_invitations.sql
-- Davetle katılım (hoca / yardımcı hoca):
--   * invitations: kurs yöneticisinin (veya başöğretmenin) oluşturduğu davet bağlantısı/QR.
--     code herkese açık bağlantıdaki koddur (8 karakter); tüm kurslarda benzersizdir.
--     Süresi dolan (expires_at) veya iptal edilen (revoked_at) davetle katılınamaz.
--   * join_requests: davetle gelen kişinin (telefonu SMS koduyla doğrulanmış) katılım isteği.
--     Yönetici onaylayınca users tablosuna personel olarak eklenir (user_id).
--     Bir kursta aynı numaranın yalnızca bir bekleyen isteği olabilir.
-- Tekrar çalıştırılabilir.
-- =============================================================

CREATE TABLE IF NOT EXISTS invitations (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
    code VARCHAR(16) NOT NULL,
    -- 'head_teacher' | 'assistant_teacher'
    role VARCHAR(50) NOT NULL,
    expires_at TIMESTAMPTZ NOT NULL,
    revoked_at TIMESTAMPTZ,
    created_by UUID,

    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ,
    is_deleted BOOLEAN NOT NULL DEFAULT false,
    deleted_at TIMESTAMPTZ,

    CONSTRAINT uq_invitations_tenant_id_id UNIQUE (tenant_id, id),
    CONSTRAINT ck_invitations_role CHECK (role IN ('head_teacher', 'assistant_teacher'))
);

CREATE UNIQUE INDEX IF NOT EXISTS uq_invitations_code ON invitations(code);
CREATE INDEX IF NOT EXISTS idx_invitations_tenant ON invitations(tenant_id, role) WHERE is_deleted = false AND revoked_at IS NULL;

CREATE TABLE IF NOT EXISTS join_requests (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
    invitation_id UUID NOT NULL,
    first_name VARCHAR(100) NOT NULL,
    last_name VARCHAR(100) NOT NULL,
    phone_number VARCHAR(20) NOT NULL,
    email VARCHAR(255),
    role VARCHAR(50) NOT NULL,
    -- 'pending' | 'approved' | 'rejected'
    status VARCHAR(20) NOT NULL DEFAULT 'pending',
    reviewed_at TIMESTAMPTZ,
    reviewed_by UUID,
    rejection_reason VARCHAR(500),
    user_id UUID,

    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ,
    is_deleted BOOLEAN NOT NULL DEFAULT false,
    deleted_at TIMESTAMPTZ,

    CONSTRAINT fk_join_requests_invitation FOREIGN KEY (tenant_id, invitation_id)
        REFERENCES invitations(tenant_id, id) ON DELETE CASCADE,
    CONSTRAINT ck_join_requests_role CHECK (role IN ('head_teacher', 'assistant_teacher')),
    CONSTRAINT ck_join_requests_status CHECK (status IN ('pending', 'approved', 'rejected'))
);

CREATE UNIQUE INDEX IF NOT EXISTS uq_join_requests_pending_phone
    ON join_requests(tenant_id, phone_number) WHERE status = 'pending' AND is_deleted = false;
CREATE INDEX IF NOT EXISTS idx_join_requests_tenant_status
    ON join_requests(tenant_id, status, created_at) WHERE is_deleted = false;
-- Girişte "kaydınız onay bekliyor" bilgisi: numara tüm kurslarda aranır
CREATE INDEX IF NOT EXISTS idx_join_requests_pending_phone
    ON join_requests(phone_number) WHERE status = 'pending' AND is_deleted = false;
