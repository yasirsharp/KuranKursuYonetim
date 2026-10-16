-- =============================================================
-- HafizTakip — 01_seed_data.sql
-- Detaylı ve anlamlı sahte veriler (Seed Data)
-- =============================================================

-- UUID'leri manuel atıyoruz ki foreign key ilişkilerini kolayca kurabilelim.

-- 1. TENANT Ekleme (Ana Kurum)
INSERT INTO tenants (id, name, slug, status, contact_email, contact_phone, is_active)
VALUES ('11111111-1111-1111-1111-111111111111', 'Merkez Kuran Kursu', 'merkez-kuran-kursu', 'active', 'iletisim@merkezkurs.com', '05550000001', true);

-- Kurs ayarları (varsayılanlar; kurs başına tek satır)
INSERT INTO tenant_settings (tenant_id) VALUES ('11111111-1111-1111-1111-111111111111');

-- 2. PLATFORM KULLANICISI (Süper Admin)
-- YALNIZCA GELİŞTİRME: giriş admin@hafiztakip.com / HafizAdmin!2026  (POST /api/platform/auth/login)
-- Özet biçimi: pbkdf2-sha256$iterasyon$tuz$özet (bkz. PasswordHasher). Üretimde bu satır kullanılmaz.
INSERT INTO platform_users (id, email, password_hash, role)
VALUES ('22222222-2222-2222-2222-222222222222', 'admin@hafiztakip.com', 'pbkdf2-sha256$210000$J2RMcbfYZoEeqpkPMgtBqQ==$cKLMYxjpj7enBwmdyrfEhrVXTc2HZZnPXK+Xg+XV1DA=', 'super_admin');

-- 3. ABONELİKLER VE FATURALAR
INSERT INTO subscriptions (id, tenant_id, plan_name, start_date, end_date, status)
VALUES ('33333333-1111-1111-1111-111111111111', '11111111-1111-1111-1111-111111111111', 'Premium', '2026-01-01', '2026-12-31', 'active');

INSERT INTO invoices (id, tenant_id, subscription_id, invoice_no, amount, due_date, paid_date, status)
VALUES ('44444444-1111-1111-1111-111111111111', '11111111-1111-1111-1111-111111111111', '33333333-1111-1111-1111-111111111111', 'INV-2026-001', 5000.00, '2026-01-15', '2026-01-10', 'paid');

-- 4. KULLANICILAR (Kurs müdürü, öğretmenler ve veliler)
-- Kurs Müdürü (tenant_admin) — giriş: 0500 111 22 30
INSERT INTO users (id, tenant_id, first_name, last_name, phone_number, email, role, password_hash)
VALUES ('55555555-0000-0000-0000-000000000000', '11111111-1111-1111-1111-111111111111', 'Osman', 'Aydın', '5001112230', 'mudur@merkezkurs.com', 'tenant_admin', NULL);

-- Hoca 1 (Başöğretmen)
INSERT INTO users (id, tenant_id, first_name, last_name, phone_number, email, role, password_hash)
VALUES ('55555555-1111-1111-1111-111111111111', '11111111-1111-1111-1111-111111111111', 'Ahmet', 'Yılmaz', '5001112233', 'ahmet.hoca@merkezkurs.com', 'head_teacher', 'hashed_pw');

-- Hoca 2 (Yardımcı Öğretmen)
INSERT INTO users (id, tenant_id, first_name, last_name, phone_number, email, role, password_hash)
VALUES ('55555555-2222-2222-2222-222222222222', '11111111-1111-1111-1111-111111111111', 'Mehmet', 'Kaya', '5001112234', 'mehmet.hoca@merkezkurs.com', 'assistant_teacher', 'hashed_pw');

-- Veli 1
INSERT INTO users (id, tenant_id, first_name, last_name, phone_number, email, role, password_hash)
VALUES ('55555555-3333-3333-3333-333333333333', '11111111-1111-1111-1111-111111111111', 'Hasan', 'Demir', '5001112235', 'hasan.veli@mail.com', 'parent', 'hashed_pw');

-- Veli 2
INSERT INTO users (id, tenant_id, first_name, last_name, phone_number, email, role, password_hash)
VALUES ('55555555-4444-4444-4444-444444444444', '11111111-1111-1111-1111-111111111111', 'Ayşe', 'Çelik', '5001112236', 'ayse.veli@mail.com', 'parent', 'hashed_pw');

-- 4b. ONAY BEKLEYEN KURS BAŞVURUSU (platform onay ekranını denemek için)
-- Başvuran kurs müdürü giriş yapabilir ama yalnızca "kursunuz onay bekliyor" durumunu görür — giriş: 0500 111 22 40
INSERT INTO tenants (id, name, slug, status, contact_email, contact_phone, is_active, city, district, address, submitted_at)
VALUES ('11111111-2222-2222-2222-222222222222', 'Yeşilvadi Kur''an Kursu', 'yesilvadi-kuran-kursu', 'pending', NULL, '5001112240', true,
        'Konya', 'Selçuklu', 'Yeşilvadi Mah. Cami Sok. No: 3', now() - INTERVAL '2 hours');

INSERT INTO tenant_settings (tenant_id) VALUES ('11111111-2222-2222-2222-222222222222');

INSERT INTO users (id, tenant_id, first_name, last_name, phone_number, email, role, password_hash)
VALUES ('55555555-9999-9999-9999-999999999999', '11111111-2222-2222-2222-222222222222', 'Kemal', 'Arslan', '5001112240', NULL, 'tenant_admin', NULL);

-- 5. GRUPLAR (Sınıflar)
INSERT INTO groups (id, tenant_id, teacher_id, name, description)
VALUES ('66666666-1111-1111-1111-111111111111', '11111111-1111-1111-1111-111111111111', '55555555-1111-1111-1111-111111111111', 'A Sınıfı (Hafızlık İleri)', 'Hafızlığa geçmiş tecrübeli öğrenciler.');

INSERT INTO groups (id, tenant_id, teacher_id, name, description)
VALUES ('66666666-2222-2222-2222-222222222222', '11111111-1111-1111-1111-111111111111', '55555555-2222-2222-2222-222222222222', 'B Sınıfı (Hazırlık)', 'Yüzüne okuma ve hafızlık hazırlık sınıfı.');

-- 6. ÖĞRENCİLER
-- A Sınıfı Öğrencileri (Ahmet Hoca)
INSERT INTO students (id, tenant_id, group_id, first_name, last_name, identification_number, current_page, completed_pages, total_pages_to_memorize, is_hafiz, is_boarder, status)
VALUES ('77777777-1111-1111-1111-111111111111', '11111111-1111-1111-1111-111111111111', '66666666-1111-1111-1111-111111111111', 'Yusuf', 'Demir', '12345678901', 10, 150, 604, false, true, 'active');

INSERT INTO students (id, tenant_id, group_id, first_name, last_name, identification_number, current_page, completed_pages, total_pages_to_memorize, is_hafiz, is_boarder, status)
VALUES ('77777777-2222-2222-2222-222222222222', '11111111-1111-1111-1111-111111111111', '66666666-1111-1111-1111-111111111111', 'Mustafa', 'Çelik', '12345678902', 15, 300, 604, false, false, 'active');

-- B Sınıfı Öğrencileri (Mehmet Hoca)
INSERT INTO students (id, tenant_id, group_id, first_name, last_name, identification_number, current_page, completed_pages, total_pages_to_memorize, is_hafiz, is_boarder, status)
VALUES ('77777777-3333-3333-3333-333333333333', '11111111-1111-1111-1111-111111111111', '66666666-2222-2222-2222-222222222222', 'Ali', 'Yıldız', '12345678903', 1, 20, 604, false, true, 'active');

-- 7. ÖĞRENCİ - VELİ BAĞLANTILARI
INSERT INTO student_parents (id, tenant_id, student_id, parent_id)
VALUES (gen_random_uuid(), '11111111-1111-1111-1111-111111111111', '77777777-1111-1111-1111-111111111111', '55555555-3333-3333-3333-333333333333'); -- Yusuf'un velisi Hasan

INSERT INTO student_parents (id, tenant_id, student_id, parent_id)
VALUES (gen_random_uuid(), '11111111-1111-1111-1111-111111111111', '77777777-2222-2222-2222-222222222222', '55555555-4444-4444-4444-444444444444'); -- Mustafa'nın velisi Ayşe

-- 8. DERSLER (Geçmiş Sabak / Sabki Kayıtları)
INSERT INTO lessons (id, tenant_id, student_id, teacher_id, lesson_date, sura_id, juz_number, page_number, performance, is_new_page, homework_status, notes)
VALUES (gen_random_uuid(), '11111111-1111-1111-1111-111111111111', '77777777-1111-1111-1111-111111111111', '55555555-1111-1111-1111-111111111111', CURRENT_DATE - INTERVAL '1 day', 2, 1, 10, 'cok_iyi', true, 'yapildi', 'Maşallah, çok güzel ezberlemiş.');

INSERT INTO lessons (id, tenant_id, student_id, teacher_id, lesson_date, sura_id, juz_number, page_number, performance, is_new_page, homework_status, notes)
VALUES (gen_random_uuid(), '11111111-1111-1111-1111-111111111111', '77777777-1111-1111-1111-111111111111', '55555555-1111-1111-1111-111111111111', CURRENT_DATE - INTERVAL '2 days', 2, 1, 1, 'iyi', false, 'yapildi', 'Tekrarlarını aksatmamalı.');

-- 9. YOKLAMA (Devamsızlık Kayıtları)
INSERT INTO attendances (id, tenant_id, student_id, date, status, notes)
VALUES (gen_random_uuid(), '11111111-1111-1111-1111-111111111111', '77777777-1111-1111-1111-111111111111', CURRENT_DATE - INTERVAL '1 day', 'present', NULL);

INSERT INTO attendances (id, tenant_id, student_id, date, status, notes)
VALUES (gen_random_uuid(), '11111111-1111-1111-1111-111111111111', '77777777-2222-2222-2222-222222222222', CURRENT_DATE - INTERVAL '1 day', 'excused', 'Hastaneye gittiği için izinli.');

-- 10. SINAVLAR VE SONUÇLAR
INSERT INTO exams (id, tenant_id, group_id, exam_type, exam_date, title, description)
VALUES ('88888888-1111-1111-1111-111111111111', '11111111-1111-1111-1111-111111111111', '66666666-1111-1111-1111-111111111111', 'deneme', CURRENT_DATE - INTERVAL '7 days', 'Ekim Ayı 1. Dönem Hafızlık Denemesi', '1-5 Cüz arası değerlendirme.');

INSERT INTO exam_results (id, tenant_id, exam_id, student_id, score, grade, teacher_notes)
VALUES (gen_random_uuid(), '11111111-1111-1111-1111-111111111111', '88888888-1111-1111-1111-111111111111', '77777777-1111-1111-1111-111111111111', 85.50, 'B', 'Hatalar az, mahreçlere dikkat etmeli.');

-- 11. GÜNLÜK ETÜT GÖREVLERİ (Akşam Etüdü)
INSERT INTO daily_assignments (id, tenant_id, student_id, teacher_id, assignment_date, is_boarder_today, assigned_tasks, completed_in_etut, completed_at_home, status, etut_teacher_note)
VALUES (
    gen_random_uuid(), 
    '11111111-1111-1111-1111-111111111111', 
    '77777777-1111-1111-1111-111111111111', 
    '55555555-1111-1111-1111-111111111111', 
    CURRENT_DATE, 
    true, 
    '[{"id":"t1","type":"ezber","cuz":2,"page":11}, {"id":"t2","type":"has","cuz":1,"page":1}]'::jsonb, 
    '["t1"]'::jsonb, 
    '[]'::jsonb, 
    'in_progress', 
    'Ezber tamam, has sayfası ev ödevine kaldı.'
);

-- 12. DUYURULAR
INSERT INTO announcements (id, tenant_id, author_id, title, content, target_audience, is_pinned, published_at)
VALUES (gen_random_uuid(), '11111111-1111-1111-1111-111111111111', '55555555-1111-1111-1111-111111111111', 'Kış Saati Uygulaması', 'Pazartesi gününden itibaren ders başlangıç saati 08:30 olarak güncellenmiştir.', 'all', true, CURRENT_TIMESTAMP);
