-- =============================================================
-- 003_tenant_composite_fks.sql
-- Kiracı izolasyonu için ikinci savunma hattı.
-- Tenant tabloları arasındaki tek kolonlu FK'ler, tenant_id'yi de içeren
-- bileşik FK'lerle değiştirilir: bir kayıt yalnızca AYNI kursa ait kayıtlara
-- bağlanabilir. Uygulama katmanı bir kontrolü atlasa bile veritabanı reddeder.
--
-- Not: Mevcut veride kurslar arası bir bağlantı varsa ilgili ADD CONSTRAINT
-- hata verir ve migration durur; önce o kayıtların düzeltilmesi gerekir.
-- Tekrar çalıştırılabilir.
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
SELECT pg_temp.add_constraint_if_missing('users',    'uq_users_tenant_id_id',    'UNIQUE (tenant_id, id)');
SELECT pg_temp.add_constraint_if_missing('groups',   'uq_groups_tenant_id_id',   'UNIQUE (tenant_id, id)');
SELECT pg_temp.add_constraint_if_missing('students', 'uq_students_tenant_id_id', 'UNIQUE (tenant_id, id)');
SELECT pg_temp.add_constraint_if_missing('exams',    'uq_exams_tenant_id_id',    'UNIQUE (tenant_id, id)');

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
SELECT pg_temp.add_constraint_if_missing('groups', 'fk_groups_teacher',
    'FOREIGN KEY (tenant_id, teacher_id) REFERENCES users (tenant_id, id)');
SELECT pg_temp.add_constraint_if_missing('students', 'fk_students_group',
    'FOREIGN KEY (tenant_id, group_id) REFERENCES groups (tenant_id, id)');
SELECT pg_temp.add_constraint_if_missing('student_parents', 'fk_student_parents_student',
    'FOREIGN KEY (tenant_id, student_id) REFERENCES students (tenant_id, id) ON DELETE CASCADE');
SELECT pg_temp.add_constraint_if_missing('student_parents', 'fk_student_parents_parent',
    'FOREIGN KEY (tenant_id, parent_id) REFERENCES users (tenant_id, id) ON DELETE CASCADE');
SELECT pg_temp.add_constraint_if_missing('lessons', 'fk_lessons_student',
    'FOREIGN KEY (tenant_id, student_id) REFERENCES students (tenant_id, id)');
SELECT pg_temp.add_constraint_if_missing('lessons', 'fk_lessons_teacher',
    'FOREIGN KEY (tenant_id, teacher_id) REFERENCES users (tenant_id, id)');
SELECT pg_temp.add_constraint_if_missing('attendances', 'fk_attendances_student',
    'FOREIGN KEY (tenant_id, student_id) REFERENCES students (tenant_id, id)');
SELECT pg_temp.add_constraint_if_missing('repetition_sessions', 'fk_repetition_sessions_student',
    'FOREIGN KEY (tenant_id, student_id) REFERENCES students (tenant_id, id)');
SELECT pg_temp.add_constraint_if_missing('repetition_sessions', 'fk_repetition_sessions_teacher',
    'FOREIGN KEY (tenant_id, teacher_id) REFERENCES users (tenant_id, id)');
SELECT pg_temp.add_constraint_if_missing('etut_notes', 'fk_etut_notes_student',
    'FOREIGN KEY (tenant_id, student_id) REFERENCES students (tenant_id, id)');
SELECT pg_temp.add_constraint_if_missing('etut_notes', 'fk_etut_notes_teacher',
    'FOREIGN KEY (tenant_id, teacher_id) REFERENCES users (tenant_id, id)');
SELECT pg_temp.add_constraint_if_missing('exams', 'fk_exams_group',
    'FOREIGN KEY (tenant_id, group_id) REFERENCES groups (tenant_id, id)');
SELECT pg_temp.add_constraint_if_missing('exam_results', 'fk_exam_results_exam',
    'FOREIGN KEY (tenant_id, exam_id) REFERENCES exams (tenant_id, id)');
SELECT pg_temp.add_constraint_if_missing('exam_results', 'fk_exam_results_student',
    'FOREIGN KEY (tenant_id, student_id) REFERENCES students (tenant_id, id)');
SELECT pg_temp.add_constraint_if_missing('announcements', 'fk_announcements_author',
    'FOREIGN KEY (tenant_id, author_id) REFERENCES users (tenant_id, id)');
SELECT pg_temp.add_constraint_if_missing('parent_notifications', 'fk_parent_notifications_student',
    'FOREIGN KEY (tenant_id, student_id) REFERENCES students (tenant_id, id) ON DELETE CASCADE');
SELECT pg_temp.add_constraint_if_missing('daily_assignments', 'fk_daily_assignments_student',
    'FOREIGN KEY (tenant_id, student_id) REFERENCES students (tenant_id, id) ON DELETE CASCADE');
SELECT pg_temp.add_constraint_if_missing('daily_assignments', 'fk_daily_assignments_teacher',
    'FOREIGN KEY (tenant_id, teacher_id) REFERENCES users (tenant_id, id) ON DELETE CASCADE');
SELECT pg_temp.add_constraint_if_missing('refresh_tokens', 'fk_refresh_tokens_user',
    'FOREIGN KEY (tenant_id, user_id) REFERENCES users (tenant_id, id) ON DELETE CASCADE');
