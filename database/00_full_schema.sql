-- =============================================================
-- HafizTakip — 00_full_schema.sql
-- Tüm tabloların ve indekslerin tek bir dosyada birleştirilmiş hali.
-- EF Core ile uyumlu olması için PostgreSQL ENUM'ları kullanılmamış, 
-- yerine VARCHAR(50) kullanılmıştır.
-- =============================================================

-- UUID desteği
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pgcrypto";

-- =============================================================
-- 1. PLATFORM TABLOLARI (Tenant'tan bağımsız)
-- =============================================================

CREATE TABLE IF NOT EXISTS tenants (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(255) NOT NULL,
    slug VARCHAR(100) NOT NULL UNIQUE,
    status VARCHAR(50) NOT NULL DEFAULT 'trial',
    contact_email VARCHAR(255),
    contact_phone VARCHAR(50),
    is_active BOOLEAN NOT NULL DEFAULT true,

    -- Kurs başvurusu ve platform onayı — migrations/007
    city VARCHAR(100),
    district VARCHAR(100),
    address VARCHAR(500),
    submitted_at TIMESTAMPTZ,
    reviewed_at TIMESTAMPTZ,
    reviewed_by UUID,
    rejection_reason VARCHAR(500),
    
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ,
    is_deleted BOOLEAN NOT NULL DEFAULT false,
    deleted_at TIMESTAMPTZ,

    CONSTRAINT ck_tenants_status
        CHECK (status IN ('pending', 'trial', 'active', 'suspended', 'cancelled', 'rejected'))
);

CREATE INDEX IF NOT EXISTS idx_tenants_status ON tenants(status) WHERE is_deleted = false;

CREATE TABLE IF NOT EXISTS platform_users (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    email VARCHAR(255) NOT NULL UNIQUE,
    password_hash VARCHAR(255) NOT NULL,
    role VARCHAR(50) NOT NULL DEFAULT 'super_admin',
    
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ,
    is_deleted BOOLEAN NOT NULL DEFAULT false,
    deleted_at TIMESTAMPTZ
);

ALTER TABLE tenants ADD CONSTRAINT tenants_reviewed_by_fkey
    FOREIGN KEY (reviewed_by) REFERENCES platform_users(id);

CREATE TABLE IF NOT EXISTS subscriptions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
    plan_name VARCHAR(100) NOT NULL,
    start_date DATE NOT NULL,
    end_date DATE NOT NULL,
    status VARCHAR(50) NOT NULL,
    
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ,
    is_deleted BOOLEAN NOT NULL DEFAULT false,
    deleted_at TIMESTAMPTZ
);

CREATE TABLE IF NOT EXISTS invoices (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
    subscription_id UUID NOT NULL REFERENCES subscriptions(id) ON DELETE CASCADE,
    invoice_no VARCHAR(50) NOT NULL,
    amount DECIMAL(10,2) NOT NULL,
    due_date DATE NOT NULL,
    paid_date DATE,
    status VARCHAR(50) NOT NULL,
    
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ,
    is_deleted BOOLEAN NOT NULL DEFAULT false,
    deleted_at TIMESTAMPTZ
);

-- =============================================================
-- 2. TENANT TABLOLARI
-- =============================================================

-- TODO: Tenant tabloları arasındaki ilişkileri tenant_id'yi de içeren composite FK'lerle sınırla; mevcut tek kolonlu FK'ler farklı tenant kayıtlarını ilişkilendirebilir.

CREATE TABLE IF NOT EXISTS users (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL REFERENCES tenants(id),
    first_name VARCHAR(100) NOT NULL,
    last_name VARCHAR(100) NOT NULL,
    phone_number VARCHAR(20) NOT NULL,
    email VARCHAR(255),
    role VARCHAR(50) NOT NULL DEFAULT 'parent',
    password_hash VARCHAR(255),
    
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ,
    is_deleted BOOLEAN NOT NULL DEFAULT false,
    deleted_at TIMESTAMPTZ,
    
    UNIQUE(tenant_id, phone_number)
);

CREATE TABLE IF NOT EXISTS groups (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL REFERENCES tenants(id),
    teacher_id UUID NOT NULL REFERENCES users(id),
    name VARCHAR(100) NOT NULL,
    description TEXT,
    
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ,
    is_deleted BOOLEAN NOT NULL DEFAULT false,
    deleted_at TIMESTAMPTZ
);

CREATE TABLE IF NOT EXISTS students (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL REFERENCES tenants(id),
    group_id UUID REFERENCES groups(id),
    first_name VARCHAR(100) NOT NULL,
    last_name VARCHAR(100) NOT NULL,
    identification_number VARCHAR(20),
    current_page INT NOT NULL DEFAULT 0,
    completed_pages INT NOT NULL DEFAULT 0,
    total_pages_to_memorize INT NOT NULL DEFAULT 604,
    is_hafiz BOOLEAN NOT NULL DEFAULT false,
    is_boarder BOOLEAN NOT NULL DEFAULT false,
    status VARCHAR(50) NOT NULL DEFAULT 'active',
    
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ,
    is_deleted BOOLEAN NOT NULL DEFAULT false,
    deleted_at TIMESTAMPTZ
);

CREATE TABLE IF NOT EXISTS student_parents (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
    student_id UUID NOT NULL REFERENCES students(id) ON DELETE CASCADE,
    parent_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ,
    is_deleted BOOLEAN NOT NULL DEFAULT false,
    deleted_at TIMESTAMPTZ
);

CREATE TABLE IF NOT EXISTS lessons (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL REFERENCES tenants(id),
    student_id UUID NOT NULL REFERENCES students(id),
    teacher_id UUID NOT NULL REFERENCES users(id),
    lesson_date DATE NOT NULL,
    sura_id INT NOT NULL,
    juz_number INT NOT NULL,
    page_number INT NOT NULL,
    performance VARCHAR(50) NOT NULL DEFAULT 'iyi',
    is_new_page BOOLEAN NOT NULL DEFAULT true,
    homework_status VARCHAR(50) NOT NULL DEFAULT 'yapilmadi',
    notes TEXT,
    
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ,
    is_deleted BOOLEAN NOT NULL DEFAULT false,
    deleted_at TIMESTAMPTZ,
    
    UNIQUE(tenant_id, student_id, lesson_date)
);

CREATE TABLE IF NOT EXISTS attendances (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL REFERENCES tenants(id),
    student_id UUID NOT NULL REFERENCES students(id),
    date DATE NOT NULL,
    status VARCHAR(50) NOT NULL,
    notes TEXT,
    
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ,
    is_deleted BOOLEAN NOT NULL DEFAULT false,
    deleted_at TIMESTAMPTZ,
    
    CONSTRAINT uq_attendances_student_date UNIQUE (student_id, date)
);

CREATE TABLE IF NOT EXISTS repetition_sessions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL REFERENCES tenants(id),
    student_id UUID NOT NULL REFERENCES students(id),
    teacher_id UUID NOT NULL REFERENCES users(id),
    session_date DATE NOT NULL,
    start_cuz INTEGER,
    end_cuz INTEGER,
    start_page INTEGER,
    end_page INTEGER,
    performance VARCHAR(50), 
    teacher_notes TEXT,
    
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ,
    is_deleted BOOLEAN NOT NULL DEFAULT false,
    deleted_at TIMESTAMPTZ
);

CREATE TABLE IF NOT EXISTS etut_notes (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL REFERENCES tenants(id),
    student_id UUID NOT NULL REFERENCES students(id),
    teacher_id UUID NOT NULL REFERENCES users(id),
    note_date DATE NOT NULL,
    homework_status VARCHAR(50) NOT NULL,
    teacher_notes TEXT,
    
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ,
    is_deleted BOOLEAN NOT NULL DEFAULT false,
    deleted_at TIMESTAMPTZ
);

CREATE TABLE IF NOT EXISTS exams (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL REFERENCES tenants(id),
    group_id UUID NOT NULL REFERENCES groups(id),
    exam_type VARCHAR(50) NOT NULL,
    exam_date DATE NOT NULL,
    title VARCHAR(200) NOT NULL,
    description TEXT,
    
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ,
    is_deleted BOOLEAN NOT NULL DEFAULT false,
    deleted_at TIMESTAMPTZ
);

CREATE TABLE IF NOT EXISTS exam_results (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL REFERENCES tenants(id),
    exam_id UUID NOT NULL REFERENCES exams(id),
    student_id UUID NOT NULL REFERENCES students(id),
    score DECIMAL(5,2),
    grade VARCHAR(50),
    teacher_notes TEXT,
    
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ,
    is_deleted BOOLEAN NOT NULL DEFAULT false,
    deleted_at TIMESTAMPTZ,

    CONSTRAINT uq_exam_results_exam_student UNIQUE (exam_id, student_id)
);

CREATE TABLE IF NOT EXISTS announcements (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL REFERENCES tenants(id),
    author_id UUID NOT NULL REFERENCES users(id),
    title VARCHAR(200) NOT NULL,
    content TEXT NOT NULL,
    target_audience VARCHAR(100) NOT NULL DEFAULT 'all',
    is_pinned BOOLEAN NOT NULL DEFAULT false,
    published_at TIMESTAMPTZ,
    
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ,
    is_deleted BOOLEAN NOT NULL DEFAULT false,
    deleted_at TIMESTAMPTZ
);

CREATE TABLE IF NOT EXISTS parent_notifications (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
    student_id UUID NOT NULL REFERENCES students(id) ON DELETE CASCADE,
    notification_type VARCHAR(50) NOT NULL,
    title VARCHAR(200) NOT NULL,
    message TEXT NOT NULL,
    recipient_phone VARCHAR(20) NOT NULL,
    delivery_channel VARCHAR(50) NOT NULL,
    delivery_status VARCHAR(50) NOT NULL,
    sent_at TIMESTAMPTZ,
    
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ,
    is_deleted BOOLEAN NOT NULL DEFAULT false,
    deleted_at TIMESTAMPTZ
);

CREATE TABLE IF NOT EXISTS daily_assignments (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
    student_id UUID NOT NULL REFERENCES students(id) ON DELETE CASCADE,
    teacher_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    assignment_date DATE NOT NULL,
    is_boarder_today BOOLEAN NOT NULL DEFAULT FALSE,
    assigned_tasks JSONB NOT NULL DEFAULT '[]',
    completed_in_etut JSONB NOT NULL DEFAULT '[]',
    completed_at_home JSONB NOT NULL DEFAULT '[]',
    status VARCHAR(50) NOT NULL DEFAULT 'assigned',
    etut_teacher_note TEXT,
    parent_note TEXT,
    
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ,
    is_deleted BOOLEAN NOT NULL DEFAULT false,
    deleted_at TIMESTAMPTZ
);

-- Kurs ayarları (kurs başına tek satır) — migrations/006
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

-- Oturum yenileme token'ları (yalnızca SHA-256 özeti saklanır) — migrations/002
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

-- =============================================================
-- 3. INDEKSLER
-- =============================================================

CREATE INDEX IX_users_tenant_id ON users(tenant_id);
CREATE INDEX IX_groups_tenant_id ON groups(tenant_id);
CREATE INDEX IX_students_tenant_id ON students(tenant_id);
CREATE INDEX IX_students_tenant_id_group_id ON students(tenant_id, group_id);
CREATE INDEX IX_lessons_tenant_id ON lessons(tenant_id);
CREATE INDEX IX_lessons_student_id ON lessons(student_id);
CREATE INDEX IX_lessons_tenant_student_date ON lessons(tenant_id, student_id, lesson_date);
CREATE INDEX IX_attendances_tenant_id ON attendances(tenant_id);
CREATE INDEX IX_attendances_tenant_date ON attendances(tenant_id, date);
CREATE INDEX IX_attendances_student_id ON attendances(student_id);
CREATE INDEX IX_repetition_sessions_tenant_id ON repetition_sessions(tenant_id);
CREATE INDEX IX_repetition_sessions_student_id ON repetition_sessions(student_id);
CREATE INDEX IX_repetition_sessions_session_date ON repetition_sessions(session_date);
CREATE INDEX IX_etut_notes_tenant_id ON etut_notes(tenant_id);
CREATE INDEX IX_etut_notes_student_id ON etut_notes(student_id);
CREATE INDEX IX_etut_notes_note_date ON etut_notes(tenant_id, note_date);
CREATE INDEX IX_exams_tenant_id ON exams(tenant_id);
CREATE INDEX IX_exams_group_id ON exams(group_id);
CREATE INDEX IX_exams_exam_date ON exams(tenant_id, exam_date);
CREATE INDEX IX_exam_results_tenant_id ON exam_results(tenant_id);
CREATE INDEX IX_exam_results_exam_id ON exam_results(exam_id);
CREATE INDEX IX_exam_results_student_id ON exam_results(student_id);
CREATE INDEX IX_announcements_tenant_id ON announcements(tenant_id);
CREATE INDEX IX_announcements_author_id ON announcements(author_id);
CREATE UNIQUE INDEX UQ_refresh_tokens_token_hash ON refresh_tokens(token_hash);
CREATE INDEX IX_refresh_tokens_user_id ON refresh_tokens(user_id);
CREATE INDEX IX_refresh_tokens_tenant_id ON refresh_tokens(tenant_id);

-- =============================================================
-- 4. KİRACI İZOLASYONU — bileşik (tenant_id, x_id) FK'ler (migrations/003)
-- Bir kayıt yalnızca aynı kursa ait kayıtlara bağlanabilir.
-- =============================================================

CREATE OR REPLACE FUNCTION pg_temp.add_constraint_if_missing(p_table text, p_name text, p_definition text)
RETURNS void AS $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = p_name) THEN
        EXECUTE format('ALTER TABLE %I ADD CONSTRAINT %I %s', p_table, p_name, p_definition);
    END IF;
END;
$$ LANGUAGE plpgsql;

-- 1. Referans verilen tablolarda (tenant_id, id) benzersizliği
DO $$ BEGIN PERFORM pg_temp.add_constraint_if_missing('users',    'uq_users_tenant_id_id',    'UNIQUE (tenant_id, id)'); END $$;
DO $$ BEGIN PERFORM pg_temp.add_constraint_if_missing('groups',   'uq_groups_tenant_id_id',   'UNIQUE (tenant_id, id)'); END $$;
DO $$ BEGIN PERFORM pg_temp.add_constraint_if_missing('students', 'uq_students_tenant_id_id', 'UNIQUE (tenant_id, id)'); END $$;
DO $$ BEGIN PERFORM pg_temp.add_constraint_if_missing('exams',    'uq_exams_tenant_id_id',    'UNIQUE (tenant_id, id)'); END $$;

-- 2. Tek kolonlu FK'leri kaldır
ALTER TABLE groups               DROP CONSTRAINT IF EXISTS groups_teacher_id_fkey;
ALTER TABLE students             DROP CONSTRAINT IF EXISTS students_group_id_fkey;
ALTER TABLE student_parents      DROP CONSTRAINT IF EXISTS student_parents_student_id_fkey;
ALTER TABLE student_parents      DROP CONSTRAINT IF EXISTS student_parents_parent_id_fkey;
ALTER TABLE lessons              DROP CONSTRAINT IF EXISTS lessons_student_id_fkey;
ALTER TABLE lessons              DROP CONSTRAINT IF EXISTS lessons_teacher_id_fkey;
ALTER TABLE attendances          DROP CONSTRAINT IF EXISTS attendances_student_id_fkey;
ALTER TABLE repetition_sessions  DROP CONSTRAINT IF EXISTS repetition_sessions_student_id_fkey;
ALTER TABLE repetition_sessions  DROP CONSTRAINT IF EXISTS repetition_sessions_teacher_id_fkey;
ALTER TABLE etut_notes           DROP CONSTRAINT IF EXISTS etut_notes_student_id_fkey;
ALTER TABLE etut_notes           DROP CONSTRAINT IF EXISTS etut_notes_teacher_id_fkey;
ALTER TABLE exams                DROP CONSTRAINT IF EXISTS exams_group_id_fkey;
ALTER TABLE exam_results         DROP CONSTRAINT IF EXISTS exam_results_exam_id_fkey;
ALTER TABLE exam_results         DROP CONSTRAINT IF EXISTS exam_results_student_id_fkey;
ALTER TABLE announcements        DROP CONSTRAINT IF EXISTS announcements_author_id_fkey;
ALTER TABLE parent_notifications DROP CONSTRAINT IF EXISTS parent_notifications_student_id_fkey;
ALTER TABLE daily_assignments    DROP CONSTRAINT IF EXISTS daily_assignments_student_id_fkey;
ALTER TABLE daily_assignments    DROP CONSTRAINT IF EXISTS daily_assignments_teacher_id_fkey;
ALTER TABLE refresh_tokens       DROP CONSTRAINT IF EXISTS refresh_tokens_user_id_fkey;

-- 3. Bileşik (tenant_id, x_id) FK'ler — ON DELETE davranışları korunmuştur
DO $$ BEGIN PERFORM pg_temp.add_constraint_if_missing('groups', 'fk_groups_teacher',
    'FOREIGN KEY (tenant_id, teacher_id) REFERENCES users (tenant_id, id)'); END $$;
DO $$ BEGIN PERFORM pg_temp.add_constraint_if_missing('students', 'fk_students_group',
    'FOREIGN KEY (tenant_id, group_id) REFERENCES groups (tenant_id, id)'); END $$;
DO $$ BEGIN PERFORM pg_temp.add_constraint_if_missing('student_parents', 'fk_student_parents_student',
    'FOREIGN KEY (tenant_id, student_id) REFERENCES students (tenant_id, id) ON DELETE CASCADE'); END $$;
DO $$ BEGIN PERFORM pg_temp.add_constraint_if_missing('student_parents', 'fk_student_parents_parent',
    'FOREIGN KEY (tenant_id, parent_id) REFERENCES users (tenant_id, id) ON DELETE CASCADE'); END $$;
DO $$ BEGIN PERFORM pg_temp.add_constraint_if_missing('lessons', 'fk_lessons_student',
    'FOREIGN KEY (tenant_id, student_id) REFERENCES students (tenant_id, id)'); END $$;
DO $$ BEGIN PERFORM pg_temp.add_constraint_if_missing('lessons', 'fk_lessons_teacher',
    'FOREIGN KEY (tenant_id, teacher_id) REFERENCES users (tenant_id, id)'); END $$;
DO $$ BEGIN PERFORM pg_temp.add_constraint_if_missing('attendances', 'fk_attendances_student',
    'FOREIGN KEY (tenant_id, student_id) REFERENCES students (tenant_id, id)'); END $$;
DO $$ BEGIN PERFORM pg_temp.add_constraint_if_missing('repetition_sessions', 'fk_repetition_sessions_student',
    'FOREIGN KEY (tenant_id, student_id) REFERENCES students (tenant_id, id)'); END $$;
DO $$ BEGIN PERFORM pg_temp.add_constraint_if_missing('repetition_sessions', 'fk_repetition_sessions_teacher',
    'FOREIGN KEY (tenant_id, teacher_id) REFERENCES users (tenant_id, id)'); END $$;
DO $$ BEGIN PERFORM pg_temp.add_constraint_if_missing('etut_notes', 'fk_etut_notes_student',
    'FOREIGN KEY (tenant_id, student_id) REFERENCES students (tenant_id, id)'); END $$;
DO $$ BEGIN PERFORM pg_temp.add_constraint_if_missing('etut_notes', 'fk_etut_notes_teacher',
    'FOREIGN KEY (tenant_id, teacher_id) REFERENCES users (tenant_id, id)'); END $$;
DO $$ BEGIN PERFORM pg_temp.add_constraint_if_missing('exams', 'fk_exams_group',
    'FOREIGN KEY (tenant_id, group_id) REFERENCES groups (tenant_id, id)'); END $$;
DO $$ BEGIN PERFORM pg_temp.add_constraint_if_missing('exam_results', 'fk_exam_results_exam',
    'FOREIGN KEY (tenant_id, exam_id) REFERENCES exams (tenant_id, id)'); END $$;
DO $$ BEGIN PERFORM pg_temp.add_constraint_if_missing('exam_results', 'fk_exam_results_student',
    'FOREIGN KEY (tenant_id, student_id) REFERENCES students (tenant_id, id)'); END $$;
DO $$ BEGIN PERFORM pg_temp.add_constraint_if_missing('announcements', 'fk_announcements_author',
    'FOREIGN KEY (tenant_id, author_id) REFERENCES users (tenant_id, id)'); END $$;
DO $$ BEGIN PERFORM pg_temp.add_constraint_if_missing('parent_notifications', 'fk_parent_notifications_student',
    'FOREIGN KEY (tenant_id, student_id) REFERENCES students (tenant_id, id) ON DELETE CASCADE'); END $$;
DO $$ BEGIN PERFORM pg_temp.add_constraint_if_missing('daily_assignments', 'fk_daily_assignments_student',
    'FOREIGN KEY (tenant_id, student_id) REFERENCES students (tenant_id, id) ON DELETE CASCADE'); END $$;
DO $$ BEGIN PERFORM pg_temp.add_constraint_if_missing('daily_assignments', 'fk_daily_assignments_teacher',
    'FOREIGN KEY (tenant_id, teacher_id) REFERENCES users (tenant_id, id) ON DELETE CASCADE'); END $$;
DO $$ BEGIN PERFORM pg_temp.add_constraint_if_missing('refresh_tokens', 'fk_refresh_tokens_user',
    'FOREIGN KEY (tenant_id, user_id) REFERENCES users (tenant_id, id) ON DELETE CASCADE'); END $$;

-- =============================================================
-- 5. KUR'AN REFERANS TABLOLARI (veri: 02_quran_reference.sql)
-- =============================================================

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

-- =============================================================
-- 6. MIGRATION TAKİBİ
-- Bu dosya şemanın son halidir; içerdiği migration'lar uygulanmış sayılır.
-- database/db.ps1 -Migrate yalnızca burada kayıtlı olmayan migration'ları çalıştırır.
-- Yeni bir migration eklendiğinde sürümü aşağıdaki listeye de eklenmelidir.
-- =============================================================

CREATE TABLE IF NOT EXISTS schema_migrations (
    version VARCHAR(100) PRIMARY KEY,
    applied_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

INSERT INTO schema_migrations (version) VALUES
    ('001_normalize_phone_numbers'),
    ('002_refresh_tokens'),
    ('003_tenant_composite_fks'),
    ('004_normalize_status_values'),
    ('005_lesson_surah_backfill'),
    ('006_tenant_settings'),
    ('007_course_applications')
ON CONFLICT (version) DO NOTHING;
