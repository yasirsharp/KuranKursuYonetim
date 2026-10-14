-- =============================================================
-- 002_refresh_tokens.sql
-- Oturum yenileme (refresh) token'ları. Token'ın kendisi saklanmaz,
-- yalnızca SHA-256 özeti (token_hash) tutulur. Her kullanımda token
-- döndürülür (rotation); iptal edilmiş bir token tekrar kullanılırsa
-- kullanıcının tüm aktif token'ları iptal edilir.
-- Tekrar çalıştırılabilir.
-- =============================================================

CREATE TABLE IF NOT EXISTS refresh_tokens (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    token_hash VARCHAR(128) NOT NULL,
    expires_at TIMESTAMPTZ NOT NULL,
    revoked_at TIMESTAMPTZ,
    replaced_by_token_hash VARCHAR(128),

    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ,
    is_deleted BOOLEAN NOT NULL DEFAULT false,
    deleted_at TIMESTAMPTZ
);

CREATE UNIQUE INDEX IF NOT EXISTS UQ_refresh_tokens_token_hash ON refresh_tokens(token_hash);
CREATE INDEX IF NOT EXISTS IX_refresh_tokens_user_id ON refresh_tokens(user_id);
CREATE INDEX IF NOT EXISTS IX_refresh_tokens_tenant_id ON refresh_tokens(tenant_id);
