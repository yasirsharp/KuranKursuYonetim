import { useState } from "react";

// ── Renk Paleti ───────────────────────────────────────────────────────
const C = {
    bg: "#F4EEE0",
    card: "#FFFDF7",
    green: "#1B5C3A",
    greenLight: "#2E7D52",
    greenPale: "#E8F5EE",
    gold: "#B8881A",
    goldLight: "#D4A827",
    goldPale: "#FFF8E0",
    text: "#1C1A14",
    muted: "#7A7060",
    border: "#E0D8C0",
    red: "#B03020",
    redPale: "#FFECEC",
    blue: "#1A4A8C",
    bluePale: "#EAF0FF",
    purple: "#5E2D8C",
    purplePale: "#F2EAFF",
};

// ── Örnek Veri ────────────────────────────────────────────────────────
// Mock data removed. Data will be fetched from the backend API.
// ──────────────────────────────────────────────────────────────────────

const TODAY = "2026-05-11";
const TODAY_LABEL = new Date(TODAY).toLocaleDateString("tr-TR", { weekday: "long", day: "numeric", month: "long", year: "numeric" });

// ── SVG İkon Bileşeni ─────────────────────────────────────────────────
function Icon({ name, size = 22, color = "currentColor" }) {
    const icons = {
        home: <><path d="M3 9.5L12 3l9 6.5V20a1 1 0 01-1 1H4a1 1 0 01-1-1V9.5z" /><polyline points="9,21 9,12 15,12 15,21" /></>,
        book: <><path d="M4 19.5A2.5 2.5 0 016.5 17H20" /><path d="M6.5 2H20v20H6.5A2.5 2.5 0 014 19.5v-15A2.5 2.5 0 016.5 2z" /></>,
        users: <><path d="M17 21v-2a4 4 0 00-4-4H5a4 4 0 00-4 4v2" /><circle cx="9" cy="7" r="4" /><path d="M23 21v-2a4 4 0 00-3-3.87" /><path d="M16 3.13a4 4 0 010 7.75" /></>,
        phone: <><path d="M22 16.92v3a2 2 0 01-2.18 2 19.79 19.79 0 01-8.63-3.07A19.5 19.5 0 013.07 10.8 19.79 19.79 0 01.22 2.18 2 2 0 012.22 0h3a2 2 0 012 1.72c.127.96.361 1.903.7 2.81a2 2 0 01-.45 2.11L6.91 7.91a16 16 0 006.1 6.1l1.27-1.27a2 2 0 012.11-.45c.907.339 1.85.573 2.81.7A2 2 0 0122 16.92z" /></>,
        note: <><path d="M14 2H6a2 2 0 00-2 2v16a2 2 0 002 2h12a2 2 0 002-2V8z" /><polyline points="14,2 14,8 20,8" /><line x1="16" y1="13" x2="8" y2="13" /><line x1="16" y1="17" x2="8" y2="17" /></>,
        send: <><line x1="22" y1="2" x2="11" y2="13" /><polygon points="22,2 15,22 11,13 2,9" /></>,
        plus: <><line x1="12" y1="5" x2="12" y2="19" /><line x1="5" y1="12" x2="19" y2="12" /></>,
        back: <><polyline points="15,18 9,12 15,6" /></>,
        check: <><polyline points="20,6 9,17 4,12" /></>,
        warn: <><path d="M10.29 3.86L1.82 18a2 2 0 001.71 3h16.94a2 2 0 001.71-3L13.71 3.86a2 2 0 00-3.42 0z" /><line x1="12" y1="9" x2="12" y2="13" /><line x1="12" y1="17" x2="12.01" y2="17" /></>,
        star: <><polygon points="12,2 15.09,8.26 22,9.27 17,14.14 18.18,21.02 12,17.77 5.82,21.02 7,14.14 2,9.27 8.91,8.26" /></>,
        repeat: <><polyline points="17,1 21,5 17,9" /><path d="M3 11V9a4 4 0 014-4h14" /><polyline points="7,23 3,19 7,15" /><path d="M21 13v2a4 4 0 01-4 4H3" /></>,
        cal: <><rect x="3" y="4" width="18" height="18" rx="2" /><line x1="16" y1="2" x2="16" y2="6" /><line x1="8" y1="2" x2="8" y2="6" /><line x1="3" y1="10" x2="21" y2="10" /></>,
        moon: <><path d="M21 12.79A9 9 0 1111.21 3 7 7 0 0021 12.79z" /></>,
        wa: <><path d="M17.472 14.382c-.297-.149-1.758-.867-2.03-.967-.273-.099-.471-.148-.67.15-.197.297-.767.966-.94 1.164-.173.199-.347.223-.644.075-.297-.15-1.255-.463-2.39-1.475-.883-.788-1.48-1.761-1.653-2.059-.173-.297-.018-.458.13-.606.134-.133.298-.347.446-.52.149-.174.198-.298.298-.497.099-.198.05-.371-.025-.52-.075-.149-.669-1.612-.916-2.207-.242-.579-.487-.5-.669-.51-.173-.008-.371-.01-.57-.01-.198 0-.52.074-.792.372-.272.297-1.04 1.016-1.04 2.479 0 1.462 1.065 2.875 1.213 3.074.149.198 2.096 3.2 5.077 4.487.709.306 1.262.489 1.694.625.712.227 1.36.195 1.871.118.571-.085 1.758-.719 2.006-1.413.248-.694.248-1.289.173-1.413-.074-.124-.272-.198-.57-.347z" /><path d="M11.999 2.007C6.476 2.007 2 6.484 2 12.007c0 1.745.455 3.384 1.25 4.812L2.007 22l5.302-1.225C8.731 21.567 10.336 22 12 22c5.523 0 10-4.477 10-10s-4.477-9.993-10-9.993z" /></>,
    };
    return (
        <svg width={size} height={size} viewBox="0 0 24 24" fill="none" stroke={color}
            strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" style={{ display: "block", flexShrink: 0 }}>
            {icons[name]}
        </svg>
    );
}

// ── Performans Rozeti ──────────────────────────────────────────────────
function Badge({ perf }) {
    const m = {
        "Mükemmel": { bg: "#E6F4EA", c: "#1B5E20", t: "⭐ Mükemmel" },
        "Çok İyi": { bg: "#E3F2FD", c: "#0D47A1", t: "✨ Çok İyi" },
        "İyi": { bg: "#FFF8E1", c: "#F57F17", t: "👍 İyi" },
        "Orta": { bg: "#FFF3E0", c: "#E65100", t: "⚠️ Orta" },
        "Zayıf": { bg: "#FFEBEE", c: "#B71C1C", t: "❗ Zayıf" },
    };
    const s = m[perf] || m["İyi"];
    return (
        <span style={{ background: s.bg, color: s.c, borderRadius: 20, padding: "3px 10px", fontSize: 11, fontWeight: 700 }}>
            {s.t}
        </span>
    );
}

// ── Öğrenci Avatarı ───────────────────────────────────────────────────
function Avatar({ student, size = 44 }) {
    return (
        <div style={{
            width: size, height: size, borderRadius: size / 2,
            background: student.color, flexShrink: 0,
            display: "flex", alignItems: "center", justifyContent: "center",
            color: "#fff", fontSize: size * 0.3, fontWeight: 700,
        }}>
            {student.avatar}
        </div>
    );
}

// ════════════════════════════════════════════════════════════════════════
// EKRAN: ANA SAYFA (Dashboard)
// ════════════════════════════════════════════════════════════════════════
function Dashboard({ students, onSelectStudent }) {
    const todayLessons = students.filter(s => s.lessons.some(l => l.date === TODAY));
    const pendingHW = students.filter(s => s.lessons.some(l => l.date === TODAY && l.homework === "yapılmadı"));
    const etutToday = students.filter(s => s.etutNotes.some(n => n.date === TODAY));
    const totalNewPages = todayLessons.reduce((a, s) => a + (s.lessons.find(l => l.date === TODAY)?.newPage || 0), 0);

    return (
        <div style={{ padding: "14px 14px 0" }}>
            {/* Karşılama */}
            <div style={{ marginBottom: 16 }}>
                <p style={{ color: C.muted, fontSize: 12, margin: 0 }}>{TODAY_LABEL}</p>
                <h2 style={{ color: C.green, margin: "2px 0 0", fontSize: 21, fontFamily: "'Playfair Display', serif" }}>
                    Hafız Takip Paneli
                </h2>
            </div>

            {/* İstatistik Kartları */}
            <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: 8, marginBottom: 16 }}>
                {[
                    { label: "Toplam Öğrenci", value: students.length, icon: "users", color: C.green },
                    { label: "Bugün İşlenen", value: `${todayLessons.length}/${students.length}`, icon: "book", color: C.blue },
                    { label: "Eksik Ödev", value: pendingHW.length, icon: "warn", color: C.red },
                    { label: "Yeni Sayfa", value: totalNewPages, icon: "repeat", color: C.purple },
                ].map((s, i) => (
                    <div key={i} style={{ background: C.card, borderRadius: 14, padding: "13px 12px", boxShadow: "0 2px 10px rgba(0,0,0,0.07)", display: "flex", alignItems: "center", gap: 10, border: `1px solid ${C.border}` }}>
                        <div style={{ background: s.color + "18", borderRadius: 10, padding: 9 }}>
                            <Icon name={s.icon} size={18} color={s.color} />
                        </div>
                        <div>
                            <div style={{ fontSize: 21, fontWeight: 800, color: s.color, lineHeight: 1 }}>{s.value}</div>
                            <div style={{ fontSize: 10, color: C.muted, marginTop: 3 }}>{s.label}</div>
                        </div>
                    </div>
                ))}
            </div>

            {/* Etüt Uyarısı */}
            {etutToday.length > 0 && (
                <div style={{ background: "#FFF8DC", border: `1px solid #E8C840`, borderRadius: 12, padding: "10px 14px", marginBottom: 14, display: "flex", alignItems: "center", gap: 10 }}>
                    <Icon name="moon" size={18} color="#B8881A" />
                    <div>
                        <div style={{ fontSize: 12, fontWeight: 700, color: "#7A5C00" }}>Akşam Etüdü Notu</div>
                        <div style={{ fontSize: 11, color: "#9A7A10" }}>{etutToday.map(s => s.name).join(", ")} için not var</div>
                    </div>
                </div>
            )}

            {/* Öğrenci Listesi */}
            <h3 style={{ fontSize: 14, fontWeight: 700, color: C.text, margin: "0 0 8px" }}>Öğrenciler</h3>
            <div style={{ display: "flex", flexDirection: "column", gap: 8, paddingBottom: 100 }}>
                {students.map(student => {
                    const pct = Math.round((student.currentPage / 604) * 100);
                    const todayLesson = student.lessons.find(l => l.date === TODAY);
                    return (
                        <div key={student.id} onClick={() => onSelectStudent(student)}
                            style={{ background: C.card, borderRadius: 14, padding: "13px 14px", boxShadow: "0 2px 8px rgba(0,0,0,0.06)", cursor: "pointer", border: `1px solid ${C.border}`, transition: "box-shadow 0.2s", display: "flex", alignItems: "center", gap: 12 }}>
                            <Avatar student={student} size={44} />
                            <div style={{ flex: 1, minWidth: 0 }}>
                                <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: 2 }}>
                                    <span style={{ fontWeight: 700, fontSize: 14, color: C.text }}>{student.name}</span>
                                    <Badge perf={student.performance} />
                                </div>
                                <div style={{ fontSize: 11, color: C.muted, marginBottom: 5 }}>
                                    {student.currentSurah} · Sayfa {student.currentPage} · {student.currentJuz}. Cüz
                                </div>
                                <div style={{ background: C.border, borderRadius: 6, height: 5, overflow: "hidden" }}>
                                    <div style={{ width: `${pct}%`, background: `linear-gradient(90deg, ${C.green}, ${C.goldLight})`, height: "100%", borderRadius: 6 }} />
                                </div>
                                <div style={{ fontSize: 10, color: C.muted, marginTop: 3 }}>
                                    %{pct} tamamlandı · {todayLesson ? "✅ Bugün ders işlendi" : "⏳ Ders bekleniyor"}
                                </div>
                            </div>
                        </div>
                    );
                })}
            </div>
        </div>
    );
}

// ════════════════════════════════════════════════════════════════════════
// EKRAN: ÖĞRENCİ DETAYI
// ════════════════════════════════════════════════════════════════════════
function StudentDetail({ student, onBack, onUpdate }) {
    const [section, setSection] = useState("dersler");
    const [showForm, setShowForm] = useState(false);
    const [hw, setHw] = useState("yapıldı");
    const [lessonNote, setLessonNote] = useState("");
    const [etutInput, setEtutInput] = useState("");
    const [perf, setPerf] = useState(student.performance);

    const pct = Math.round((student.currentPage / 604) * 100);

    const addLesson = () => {
        const updated = {
            ...student,
            performance: perf,
            lessons: [
                { date: TODAY, homework: hw, note: lessonNote },
                ...student.lessons.filter(l => l.date !== TODAY),
            ]
        };

        // Backend'e gönder
        if (window.apiToken && window.teacherId) {
            fetch("http://localhost:5002/api/lessons", {
                method: "POST",
                headers: { "Content-Type": "application/json", "Authorization": `Bearer ${window.apiToken}` },
                body: JSON.stringify({
                    studentId: student.id,
                    teacherId: window.teacherId,
                    lessonDate: new Date().toISOString(),
                    pageNo: student.currentPage,
                    isNewPage: true, // Şimdilik hep yeni sayfa varsayıyoruz
                    performance: perf,
                    homeworkStatus: hw,
                    teacherNotes: lessonNote
                })
            }).catch(console.error);
        }

        onUpdate(updated);
        setShowForm(false);
        setLessonNote("");
    };

    const addEtutNote = () => {
        if (!etutInput.trim()) return;
        const updated = {
            ...student,
            etutNotes: [{ date: TODAY, note: etutInput, from: "Baş Hoca" }, ...student.etutNotes]
        };
        onUpdate(updated);
        setEtutInput("");
    };

    const todayLesson = student.lessons.find(l => l.date === TODAY);
    const parentMsg = `Sayın ${student.parentName},\n\n${student.name}'in bugünkü ders durumu:\n\n📝 Ödev: ${todayLesson?.homework || "-"}\n📍 Şu an: ${student.currentSurah} Suresi – Sayfa ${student.currentPage} (${student.currentJuz}. Cüz)\n📊 İlerleme: %${pct}${todayLesson?.note ? "\n\n💬 Not: " + todayLesson.note : ""}\n\nHayırlı günler dileriz. 🌙`;
    const waLink = `https://wa.me/${student.parentPhone.replace(/\s/g, "").replace(/^0/, "90")}?text=${encodeURIComponent(parentMsg)}`;

    return (
        <div style={{ paddingBottom: 80 }}>
            {/* Üst Banner */}
            <div style={{ background: `linear-gradient(150deg, ${C.green} 0%, ${C.greenLight} 100%)`, padding: "14px 14px 18px" }}>
                <button onClick={onBack}
                    style={{ background: "rgba(255,255,255,0.18)", border: "none", borderRadius: 10, padding: "6px 12px", cursor: "pointer", color: "#fff", display: "flex", alignItems: "center", gap: 4, fontSize: 13, marginBottom: 14 }}>
                    <Icon name="back" size={16} color="#fff" /> Geri
                </button>
                <div style={{ display: "flex", gap: 12, alignItems: "center" }}>
                    <div style={{ width: 52, height: 52, borderRadius: 26, background: "rgba(255,255,255,0.22)", border: "2px solid rgba(255,255,255,0.5)", display: "flex", alignItems: "center", justifyContent: "center", color: "#fff", fontWeight: 700, fontSize: 17 }}>
                        {student.avatar}
                    </div>
                    <div>
                        <h2 style={{ color: "#fff", margin: 0, fontSize: 19, fontFamily: "'Playfair Display', serif" }}>{student.name}</h2>
                        <p style={{ color: "rgba(255,255,255,0.75)", margin: "2px 0 0", fontSize: 12 }}>
                            {student.age} yaş · Veli: {student.parentName}
                        </p>
                    </div>
                </div>
                {/* İlerleme */}
                <div style={{ marginTop: 12, background: "rgba(255,255,255,0.13)", borderRadius: 12, padding: "10px 12px" }}>
                    <div style={{ display: "flex", justifyContent: "space-between", color: "#fff", fontSize: 12, marginBottom: 5 }}>
                        <span>📖 {student.currentSurah} · Sayfa {student.currentPage}</span>
                        <span style={{ fontWeight: 700 }}>%{pct}</span>
                    </div>
                    <div style={{ background: "rgba(255,255,255,0.2)", borderRadius: 5, height: 7, overflow: "hidden" }}>
                        <div style={{ width: `${pct}%`, background: "linear-gradient(90deg,#FFD700,#FFA520)", height: "100%", borderRadius: 5 }} />
                    </div>
                    <div style={{ color: "rgba(255,255,255,0.65)", fontSize: 10, marginTop: 4 }}>
                        {student.currentPage} / 604 sayfa · {student.currentJuz}. Cüz
                    </div>
                </div>
            </div>

            {/* Sekme Çubuğu */}
            <div style={{ display: "flex", background: "#fff", borderBottom: `1px solid ${C.border}`, position: "sticky", top: 0, zIndex: 10 }}>
                {[{ k: "dersler", l: "📚 Dersler" }, { k: "etut", l: "🌙 Etüt" }, { k: "veli", l: "📲 Veli" }].map(t => (
                    <button key={t.k} onClick={() => setSection(t.k)}
                        style={{ flex: 1, padding: "11px 4px", border: "none", background: "transparent", cursor: "pointer", fontSize: 13, fontWeight: section === t.k ? 700 : 500, color: section === t.k ? C.green : C.muted, borderBottom: section === t.k ? `2.5px solid ${C.green}` : "2.5px solid transparent" }}>
                        {t.l}
                    </button>
                ))}
            </div>

            <div style={{ padding: "14px" }}>

                {/* ── DERSLER ── */}
                {section === "dersler" && (
                    <>
                        <button onClick={() => setShowForm(!showForm)}
                            style={{ width: "100%", background: C.green, color: "#fff", border: "none", borderRadius: 12, padding: "13px", fontSize: 14, fontWeight: 700, cursor: "pointer", display: "flex", alignItems: "center", justifyContent: "center", gap: 6, marginBottom: 14 }}>
                            <Icon name="plus" size={17} color="#fff" />
                            {todayLesson ? "Bugünkü Dersi Düzenle" : "Bugün Ders Ekle"}
                        </button>

                        {showForm && (
                            <div style={{ background: C.card, borderRadius: 14, padding: 14, border: `1px solid ${C.border}`, marginBottom: 14, boxShadow: "0 4px 16px rgba(0,0,0,0.09)" }}>
                                <p style={{ margin: "0 0 12px", fontWeight: 700, fontSize: 13, color: C.green }}>📚 Ders Kaydı – {TODAY}</p>
                                
                                <label style={{ fontSize: 11, color: C.muted, display: "block", marginBottom: 5 }}>Ödev Durumu</label>
                                <div style={{ display: "flex", gap: 6, marginBottom: 10 }}>
                                    {["yapıldı", "eksik", "yapılmadı"].map(h => (
                                        <button key={h} onClick={() => setHw(h)}
                                            style={{ flex: 1, padding: "8px 4px", border: `1.5px solid ${hw === h ? C.green : C.border}`, borderRadius: 8, background: hw === h ? C.green : "#fff", color: hw === h ? "#fff" : C.muted, fontSize: 12, cursor: "pointer", fontWeight: 600 }}>
                                            {h}
                                        </button>
                                    ))}
                                </div>

                                <label style={{ fontSize: 11, color: C.muted, display: "block", marginBottom: 4 }}>Performans</label>
                                <div style={{ display: "flex", gap: 5, marginBottom: 10, flexWrap: "wrap" }}>
                                    {["Mükemmel", "Çok İyi", "İyi", "Orta", "Zayıf"].map(p => (
                                        <button key={p} onClick={() => setPerf(p)}
                                            style={{ padding: "6px 10px", border: `1.5px solid ${perf === p ? C.green : C.border}`, borderRadius: 8, background: perf === p ? C.greenPale : "#fff", color: perf === p ? C.green : C.muted, fontSize: 11, cursor: "pointer", fontWeight: 600 }}>
                                            {p}
                                        </button>
                                    ))}
                                </div>

                                <label style={{ fontSize: 11, color: C.muted, display: "block", marginBottom: 4 }}>Not (isteğe bağlı)</label>
                                <textarea value={lessonNote} onChange={e => setLessonNote(e.target.value)} rows={2}
                                    placeholder="Mahreç hatası, dikkat edilmesi gereken nokta..."
                                    style={{ width: "100%", border: `1.5px solid ${C.border}`, borderRadius: 8, padding: "8px 10px", fontSize: 13, resize: "none", outline: "none", boxSizing: "border-box", marginBottom: 12 }} />

                                <button onClick={addLesson}
                                    style={{ width: "100%", background: C.gold, color: "#fff", border: "none", borderRadius: 10, padding: "12px", fontWeight: 700, cursor: "pointer", fontSize: 14 }}>
                                    ✅ Dersi Kaydet
                                </button>
                            </div>
                        )}

                        {/* Ders Geçmişi */}
                        {student.lessons.length === 0 && (
                            <div style={{ textAlign: "center", padding: "30px", color: C.muted, fontSize: 13 }}>Henüz ders eklenmemiş.</div>
                        )}
                        {student.lessons.map((lesson, i) => (
                            <div key={i} style={{ background: C.card, borderRadius: 13, padding: "13px 14px", marginBottom: 9, border: `1px solid ${lesson.date === TODAY ? C.green + "40" : C.border}` }}>
                                <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: 8 }}>
                                    <span style={{ fontSize: 11, color: C.muted, display: "flex", alignItems: "center", gap: 4 }}>
                                        <Icon name="cal" size={11} color={C.muted} /> {lesson.date}
                                        {lesson.date === TODAY && <span style={{ marginLeft: 4, background: C.greenPale, color: C.green, borderRadius: 6, padding: "1px 6px", fontSize: 10, fontWeight: 700 }}>Bugün</span>}
                                    </span>
                                    <span style={{ fontSize: 11, padding: "2px 9px", borderRadius: 10, background: lesson.homework === "yapıldı" ? "#E8F5E9" : lesson.homework === "eksik" ? "#FFF8E1" : C.redPale, color: lesson.homework === "yapıldı" ? "#2E7D32" : lesson.homework === "eksik" ? "#F57F17" : C.red }}>
                                        Ödev: {lesson.homework}
                                    </span>
                                </div>
                                <div style={{ display: "flex", gap: 8, marginTop: 8 }}>
                                    {lesson.note && (
                                        <div style={{ flex: 1, background: "#FFFDE7", borderRadius: 9, padding: "7px 10px", display: "flex", alignItems: "center" }}>
                                            <span style={{ fontSize: 12, color: "#7A6000" }}>💬 {lesson.note}</span>
                                        </div>
                                    )}
                                </div>
                            </div>
                        ))}
                    </>
                )}

                {/* ── ETÜT ── */}
                {section === "etut" && (
                    <>
                        <div style={{ background: C.bluePale, borderRadius: 12, padding: "11px 13px", marginBottom: 14, border: `1px solid #B3D4FF` }}>
                            <p style={{ margin: 0, fontSize: 12, color: C.blue, lineHeight: 1.6 }}>
                                📋 Akşam etüdünde <b>yardımcı hocaya</b> not bırakın. Bu notlar etüt listesinde görünecektir.
                            </p>
                        </div>
                        <div style={{ display: "flex", gap: 8, marginBottom: 14 }}>
                            <textarea value={etutInput} onChange={e => setEtutInput(e.target.value)} rows={3}
                                placeholder={`${student.name} için etüt notu ekle...`}
                                style={{ flex: 1, border: `1.5px solid ${C.border}`, borderRadius: 10, padding: "10px 12px", fontSize: 13, resize: "none", outline: "none" }} />
                            <button onClick={addEtutNote}
                                style={{ background: C.green, border: "none", borderRadius: 10, padding: "0 14px", cursor: "pointer", display: "flex", alignItems: "center" }}>
                                <Icon name="send" size={18} color="#fff" />
                            </button>
                        </div>

                        {student.etutNotes.length === 0 && (
                            <div style={{ textAlign: "center", padding: "30px", color: C.muted, fontSize: 13 }}>
                                <div style={{ fontSize: 32, marginBottom: 8 }}>🌙</div>
                                Henüz etüt notu eklenmemiş.
                            </div>
                        )}
                        {student.etutNotes.map((en, i) => (
                            <div key={i} style={{ background: C.card, borderRadius: 12, padding: "12px 13px", marginBottom: 8, border: `1px solid ${C.gold}50` }}>
                                <div style={{ display: "flex", justifyContent: "space-between", marginBottom: 5 }}>
                                    <span style={{ fontSize: 11, fontWeight: 700, color: C.gold }}>✍️ {en.from}</span>
                                    <span style={{ fontSize: 11, color: C.muted }}>{en.date}</span>
                                </div>
                                <p style={{ margin: 0, fontSize: 13, color: C.text, lineHeight: 1.6 }}>{en.note}</p>
                            </div>
                        ))}
                    </>
                )}

                {/* ── VELİ ── */}
                {section === "veli" && (
                    <>
                        <div style={{ background: C.card, borderRadius: 14, padding: 14, border: `1px solid ${C.border}`, marginBottom: 12 }}>
                            <p style={{ margin: "0 0 10px", fontWeight: 700, fontSize: 13, color: C.text }}>👤 Veli Bilgileri</p>
                            {[{ l: "Ad Soyad", v: student.parentName }, { l: "Telefon", v: student.parentPhone }].map((r, i) => (
                                <div key={i} style={{ display: "flex", justifyContent: "space-between", padding: "8px 0", borderBottom: i === 0 ? `1px solid ${C.border}` : "none" }}>
                                    <span style={{ fontSize: 13, color: C.muted }}>{r.l}</span>
                                    <span style={{ fontSize: 13, fontWeight: 700, color: C.text }}>{r.v}</span>
                                </div>
                            ))}
                        </div>

                        <div style={{ background: C.card, borderRadius: 14, padding: 14, border: `1px solid ${C.border}`, marginBottom: 12 }}>
                            <p style={{ margin: "0 0 8px", fontWeight: 700, fontSize: 13, color: C.text }}>📄 Günlük Rapor Mesajı</p>
                            <div style={{ background: "#F9F8F4", borderRadius: 10, padding: 12, fontSize: 11.5, color: C.text, lineHeight: 1.8, whiteSpace: "pre-wrap", border: `1px solid ${C.border}` }}>
                                {parentMsg}
                            </div>
                        </div>

                        <a href={waLink} target="_blank" rel="noopener noreferrer"
                            style={{ display: "flex", alignItems: "center", justifyContent: "center", gap: 8, width: "100%", background: "#25D366", color: "#fff", borderRadius: 12, padding: "13px", fontSize: 14, fontWeight: 700, textDecoration: "none", boxSizing: "border-box" }}>
                            <Icon name="wa" size={20} color="#fff" /> WhatsApp'tan Gönder
                        </a>
                    </>
                )}
            </div>
        </div>
    );
}

// ════════════════════════════════════════════════════════════════════════
// EKRAN: DERSLER (Günlük Özet)
// ════════════════════════════════════════════════════════════════════════
function LessonsScreen({ students }) {
    const todayData = students.map(s => ({
        student: s,
        lesson: s.lessons.find(l => l.date === TODAY)
    }));
    const done = todayData.filter(d => d.lesson);
    const pending = todayData.filter(d => !d.lesson);

    return (
        <div style={{ padding: "14px", paddingBottom: 100 }}>
            <h2 style={{ fontSize: 19, fontFamily: "'Playfair Display', serif", color: C.green, margin: "0 0 2px" }}>Bugünkü Dersler</h2>
            <p style={{ color: C.muted, fontSize: 11, margin: "0 0 14px" }}>{TODAY_LABEL}</p>

            {/* Özet */}
            <div style={{ display: "grid", gridTemplateColumns: "repeat(3,1fr)", gap: 8, marginBottom: 16 }}>
                {[
                    { l: "İşlendi", v: done.length, c: C.green },
                    { l: "Bekliyor", v: pending.length, c: C.red }
                ].map((s, i) => (
                    <div key={i} style={{ background: C.card, borderRadius: 12, padding: "10px 8px", textAlign: "center", border: `1px solid ${C.border}` }}>
                        <div style={{ fontSize: 22, fontWeight: 800, color: s.c }}>{s.v}</div>
                        <div style={{ fontSize: 10, color: C.muted, marginTop: 2 }}>{s.l}</div>
                    </div>
                ))}
            </div>

            {/* Toplam tekrar */}
            <div style={{ background: `linear-gradient(120deg,${C.green},${C.greenLight})`, borderRadius: 12, padding: "10px 16px", marginBottom: 16, display: "flex", alignItems: "center", justifyContent: "space-between", color: "#fff" }}>
                <div style={{ fontSize: 13 }}>🔁 Bugün toplam tekrar</div>
                <div style={{ fontSize: 22, fontWeight: 800 }}>{totalReps}x</div>
            </div>

            {/* İşlenen dersler */}
            {done.length > 0 && (
                <>
                    <h4 style={{ fontSize: 12, fontWeight: 700, color: C.green, margin: "0 0 8px", textTransform: "uppercase", letterSpacing: 1 }}>✅ İşlendi ({done.length})</h4>
                    {done.map(({ student, lesson }, i) => (
                        <div key={i} style={{ background: C.card, borderRadius: 13, padding: "13px 14px", marginBottom: 9, border: `1px solid ${C.border}` }}>
                            <div style={{ display: "flex", alignItems: "center", gap: 10, marginBottom: 9 }}>
                                <Avatar student={student} size={36} />
                                <div style={{ flex: 1 }}>
                                    <div style={{ fontWeight: 700, fontSize: 14, color: C.text }}>{student.name}</div>
                                    <div style={{ fontSize: 11, color: C.muted }}>{student.currentSurah} · S. {student.currentPage}</div>
                                </div>
                                <Badge perf={student.performance} />
                            </div>
                            <div style={{ display: "flex", gap: 7 }}>
                                {[
                                    { e: "📖", v: `${lesson.newPage} sy`, s: "Yeni Sayfa" },
                                    { e: "🔁", v: `${lesson.repetitions}x`, s: "Tekrar" },
                                    { e: lesson.homework === "yapıldı" ? "✅" : "⚠️", v: lesson.homework, s: "Ödev" },
                                ].map((it, j) => (
                                    <div key={j} style={{ flex: 1, background: "#F5F3EE", borderRadius: 8, padding: "6px 5px", textAlign: "center" }}>
                                        <div style={{ fontSize: 14 }}>{it.e}</div>
                                        <div style={{ fontSize: 11, fontWeight: 700, color: C.text }}>{it.v}</div>
                                        <div style={{ fontSize: 9, color: C.muted }}>{it.s}</div>
                                    </div>
                                ))}
                            </div>
                            {lesson.note && (
                                <div style={{ marginTop: 8, background: "#FFFDE7", borderRadius: 8, padding: "6px 10px", fontSize: 11, color: "#7A6000" }}>
                                    💬 {lesson.note}
                                </div>
                            )}
                        </div>
                    ))}
                </>
            )}

            {/* Bekleyenler */}
            {pending.length > 0 && (
                <>
                    <h4 style={{ fontSize: 12, fontWeight: 700, color: C.red, margin: "6px 0 8px", textTransform: "uppercase", letterSpacing: 1 }}>⏳ Bekleniyor ({pending.length})</h4>
                    {pending.map(({ student }, i) => (
                        <div key={i} style={{ background: C.card, borderRadius: 12, padding: "12px 14px", marginBottom: 8, border: `1px dashed ${C.border}`, opacity: 0.8 }}>
                            <div style={{ display: "flex", alignItems: "center", gap: 10 }}>
                                <Avatar student={student} size={36} />
                                <div>
                                    <div style={{ fontWeight: 700, fontSize: 13, color: C.text }}>{student.name}</div>
                                    <div style={{ fontSize: 11, color: C.muted }}>{student.currentSurah} · S. {student.currentPage}</div>
                                </div>
                            </div>
                        </div>
                    ))}
                </>
            )}
        </div>
    );
}

// ════════════════════════════════════════════════════════════════════════
// EKRAN: VELİ BİLDİRİMLERİ
// ════════════════════════════════════════════════════════════════════════
function ParentScreen({ students }) {
    const [sent, setSent] = useState([]);

    return (
        <div style={{ padding: "14px", paddingBottom: 100 }}>
            <h2 style={{ fontSize: 19, fontFamily: "'Playfair Display', serif", color: C.green, margin: "0 0 2px" }}>Veli Bildirimleri</h2>
            <p style={{ color: C.muted, fontSize: 11, margin: "0 0 14px" }}>Tüm velilere günlük ders raporu gönderin</p>

            {/* Toplu gönderim uyarısı */}
            <div style={{ background: C.greenPale, borderRadius: 12, padding: "10px 13px", marginBottom: 14, border: `1px solid ${C.green}30` }}>
                <p style={{ margin: 0, fontSize: 12, color: C.green }}>
                    ℹ️ Her öğrenci için bireysel WhatsApp mesajı oluşturulur. Butona tıklayarak doğrudan veliye iletebilirsiniz.
                </p>
            </div>

            {students.map(student => {
                const todayLesson = student.lessons.find(l => l.date === TODAY);
                const pct = Math.round((student.currentPage / 604) * 100);
                const msg = `Sayın ${student.parentName},\n\n${student.name}'in bugünkü ders durumu:\n\n📖 Yeni Sayfa: ${todayLesson?.newPage || "-"} sayfa\n🔁 Tekrar: ${todayLesson?.repetitions || "-"} kez\n📝 Ödev: ${todayLesson?.homework || "-"}\n📍 Şu an: ${student.currentSurah} Suresi – Sayfa ${student.currentPage} (${student.currentJuz}. Cüz)\n📊 İlerleme: %${pct}${todayLesson?.note ? "\n\n💬 Not: " + todayLesson.note : ""}\n\nHayırlı günler dileriz. 🌙`;
                const wa = `https://wa.me/${student.parentPhone.replace(/\s/g, "").replace(/^0/, "90")}?text=${encodeURIComponent(msg)}`;
                const isSent = sent.includes(student.id);

                return (
                    <div key={student.id} style={{ background: C.card, borderRadius: 14, padding: "13px 14px", marginBottom: 10, border: `1px solid ${C.border}`, boxShadow: "0 2px 8px rgba(0,0,0,0.05)" }}>
                        <div style={{ display: "flex", alignItems: "center", gap: 10, marginBottom: 11 }}>
                            <Avatar student={student} size={40} />
                            <div style={{ flex: 1 }}>
                                <div style={{ fontWeight: 700, fontSize: 14, color: C.text }}>{student.name}</div>
                                <div style={{ fontSize: 11, color: C.muted }}>{student.parentName} · {student.parentPhone}</div>
                            </div>
                            {todayLesson
                                ? <span style={{ fontSize: 10, background: C.greenPale, color: C.green, borderRadius: 7, padding: "3px 8px", fontWeight: 700 }}>✅ Ders var</span>
                                : <span style={{ fontSize: 10, background: "#FFF3E0", color: "#E65100", borderRadius: 7, padding: "3px 8px", fontWeight: 700 }}>⏳ Ders yok</span>
                            }
                        </div>
                        <a href={wa} target="_blank" rel="noopener noreferrer"
                            onClick={() => setSent(p => [...p, student.id])}
                            style={{ display: "flex", alignItems: "center", justifyContent: "center", gap: 7, width: "100%", background: isSent ? "#128C7E" : "#25D366", color: "#fff", borderRadius: 10, padding: "11px", fontSize: 13, fontWeight: 700, textDecoration: "none", boxSizing: "border-box" }}>
                            <Icon name="wa" size={16} color="#fff" />
                            {isSent ? "✓ Gönderildi – Tekrar Gönder" : `WhatsApp ile Gönder`}
                        </a>
                    </div>
                );
            })}
        </div>
    );
}

// ════════════════════════════════════════════════════════════════════════
// EKRAN: AKŞAM ETÜDÜ
// ════════════════════════════════════════════════════════════════════════
function EtutScreen({ students }) {
    const allNotes = students.flatMap(s =>
        s.etutNotes.filter(n => n.date === TODAY).map(n => ({ ...n, student: s }))
    );
    const noNotes = students.filter(s => !s.etutNotes.some(n => n.date === TODAY));

    return (
        <div style={{ padding: "14px", paddingBottom: 100 }}>
            <h2 style={{ fontSize: 19, fontFamily: "'Playfair Display', serif", color: C.green, margin: "0 0 2px" }}>Akşam Etüdü</h2>
            <p style={{ color: C.muted, fontSize: 11, margin: "0 0 14px" }}>Yardımcı hoca için hazırlanan notlar · Etüt: 18:30–20:00</p>

            {/* Banner */}
            <div style={{ background: `linear-gradient(135deg,${C.green},${C.greenLight})`, borderRadius: 14, padding: "14px 16px", marginBottom: 16, color: "#fff", display: "flex", alignItems: "center", justifyContent: "space-between" }}>
                <div>
                    <div style={{ fontSize: 12, opacity: 0.75 }}>Bugün etüt notu olan öğrenci</div>
                    <div style={{ fontSize: 32, fontWeight: 800, lineHeight: 1.1 }}>{allNotes.length}</div>
                </div>
                <div style={{ background: "rgba(255,255,255,0.15)", borderRadius: 10, padding: "10px 14px", textAlign: "center" }}>
                    <div style={{ fontSize: 22 }}>🌙</div>
                    <div style={{ fontSize: 10, marginTop: 2, opacity: 0.8 }}>Etüt</div>
                </div>
            </div>

            {allNotes.length === 0 && (
                <div style={{ textAlign: "center", padding: "30px 20px", color: C.muted }}>
                    <div style={{ fontSize: 36, marginBottom: 8 }}>🌙</div>
                    <p style={{ fontSize: 13 }}>Bugün için etüt notu bulunmuyor.</p>
                    <p style={{ fontSize: 12 }}>Öğrenci detayından not ekleyebilirsiniz.</p>
                </div>
            )}

            {allNotes.length > 0 && (
                <>
                    <h4 style={{ fontSize: 12, fontWeight: 700, color: C.gold, margin: "0 0 8px", textTransform: "uppercase", letterSpacing: 1 }}>📋 Etüt Notları</h4>
                    {allNotes.map((note, i) => (
                        <div key={i} style={{ background: C.card, borderRadius: 14, padding: "13px 14px", marginBottom: 10, border: `2px solid ${C.gold}30`, boxShadow: "0 2px 10px rgba(0,0,0,0.05)" }}>
                            <div style={{ display: "flex", alignItems: "center", gap: 10, marginBottom: 9 }}>
                                <Avatar student={note.student} size={38} />
                                <div style={{ flex: 1 }}>
                                    <div style={{ fontWeight: 700, fontSize: 14, color: C.text }}>{note.student.name}</div>
                                    <div style={{ fontSize: 11, color: C.muted }}>{note.student.currentSurah} · S. {note.student.currentPage}</div>
                                </div>
                                <span style={{ fontSize: 11, color: C.muted, background: "#F5F3EE", borderRadius: 7, padding: "2px 7px" }}>{note.from}</span>
                            </div>
                            <div style={{ background: C.goldPale, borderRadius: 10, padding: "10px 12px", fontSize: 13, color: C.text, border: `1px solid ${C.gold}40`, lineHeight: 1.6 }}>
                                📋 {note.note}
                            </div>
                        </div>
                    ))}
                </>
            )}

            {/* Not olmayan öğrenciler */}
            {noNotes.length > 0 && (
                <>
                    <h4 style={{ fontSize: 12, fontWeight: 700, color: C.muted, margin: "12px 0 8px", textTransform: "uppercase", letterSpacing: 1 }}>Notu olmayan öğrenciler</h4>
                    <div style={{ display: "flex", flexWrap: "wrap", gap: 7 }}>
                        {noNotes.map(s => (
                            <div key={s.id} style={{ display: "flex", alignItems: "center", gap: 6, background: C.card, borderRadius: 10, padding: "7px 10px", border: `1px solid ${C.border}` }}>
                                <Avatar student={s} size={26} />
                                <span style={{ fontSize: 12, color: C.muted }}>{s.name}</span>
                            </div>
                        ))}
                    </div>
                </>
            )}
        </div>
    );
}

// ════════════════════════════════════════════════════════════════════════
// ALT NAVİGASYON
// ════════════════════════════════════════════════════════════════════════
function BottomNav({ active, onSelect }) {
    const tabs = [
        { key: "dashboard", icon: "home", label: "Ana Sayfa" },
        { key: "lessons", icon: "book", label: "Dersler" },
        { key: "parent", icon: "phone", label: "Veli" },
        { key: "etut", icon: "users", label: "Etüt" },
    ];
    return (
        <div style={{ position: "fixed", bottom: 0, left: "50%", transform: "translateX(-50%)", width: "100%", maxWidth: 430, background: "#fff", borderTop: `1px solid ${C.border}`, display: "flex", zIndex: 100, paddingBottom: 6, boxShadow: "0 -4px 20px rgba(0,0,0,0.10)" }}>
            {tabs.map(t => (
                <button key={t.key} onClick={() => onSelect(t.key)}
                    style={{ flex: 1, display: "flex", flexDirection: "column", alignItems: "center", padding: "9px 0 4px", border: "none", background: "transparent", cursor: "pointer", gap: 3, position: "relative" }}>
                    {active === t.key && (
                        <div style={{ position: "absolute", top: 0, left: "30%", right: "30%", height: 2.5, background: C.green, borderRadius: "0 0 3px 3px" }} />
                    )}
                    <Icon name={t.icon} size={21} color={active === t.key ? C.green : C.muted} />
                    <span style={{ fontSize: 9.5, color: active === t.key ? C.green : C.muted, fontWeight: active === t.key ? 700 : 500 }}>{t.label}</span>
                </button>
            ))}
        </div>
    );
}

// ════════════════════════════════════════════════════════════════════════
// ANA UYGULAMA
// ════════════════════════════════════════════════════════════════════════
export default function App() {
    const [tab, setTab] = useState("dashboard");
    const [students, setStudents] = useState([]);
    const [selectedStudent, setSelectedStudent] = useState(null);
    const [token, setToken] = useState(null);
    const [loading, setLoading] = useState(false);

    const loginAndFetch = async () => {
        setLoading(true);
        try {
            // Seed ve test token al
            const res = await fetch("http://localhost:5002/api/dev/seed", { method: "POST" });
            const data = await res.json();
            if (data.token) {
                setToken(data.token);
                window.apiToken = data.token;
                window.teacherId = data.teacherId;

                // Öğrencileri getir
                const studRes = await fetch("http://localhost:5002/api/students?page=1&pageSize=100", {
                    headers: { "Authorization": `Bearer ${data.token}` }
                });
                const studData = await studRes.json();
                
                if (studData.success) {
                    const mapped = studData.data.items.map(s => ({
                        id: s.id,
                        name: `${s.firstName} ${s.lastName}`,
                        age: 12,
                        parentName: "Örnek Veli",
                        parentPhone: "5551234567",
                        currentPage: s.currentPage,
                        currentJuz: Math.ceil(s.currentPage / 20) || 1,
                        currentSurah: "Sûre",
                        performance: "İyi",
                        avatar: `${s.firstName[0]}${s.lastName[0]}`,
                        color: "#1B5C3A",
                        lessons: [],
                        etutNotes: []
                    }));
                    
                    // Örnek dersleri ekle (görsellik için, gerçekte /api/lessons'dan çekilmeli)
                    mapped.forEach(m => {
                        if (m.name === "Yusuf Demir") {
                            m.lessons.push({ date: TODAY, newPage: 2, repetitions: 5, homework: "yapıldı", note: "Test verisi" });
                        }
                    });
                    
                    setStudents(mapped);
                }
            }
        } catch (e) {
            alert("API bağlantı hatası. Lütfen backend'in çalıştığından emin olun (http://localhost:5002).");
            console.error(e);
        }
        setLoading(false);
    };

    const updateStudent = (updated) => {
        setStudents(prev => prev.map(s => s.id === updated.id ? updated : s));
        setSelectedStudent(updated);
    };

    if (!token) {
        return (
            <div style={{ maxWidth: 430, margin: "0 auto", height: "100vh", display: "flex", flexDirection: "column", justifyContent: "center", alignItems: "center", background: C.bg }}>
                <div style={{ fontSize: 40, marginBottom: 10 }}>🕌</div>
                <h1 style={{ color: C.green, fontFamily: "'Playfair Display', serif", margin: "0 0 10px" }}>Hafız Takip</h1>
                <p style={{ color: C.muted, textAlign: "center", padding: "0 20px", marginBottom: 30, fontSize: 14 }}>
                    Mock veriler kaldırıldı.<br/>Gerçek ASP.NET Core backend'e bağlanmak için devam edin.
                </p>
                <button onClick={loginAndFetch} disabled={loading} 
                    style={{ background: C.green, color: "#fff", padding: "14px 28px", border: "none", borderRadius: 12, fontSize: 16, cursor: "pointer", fontWeight: 700, boxShadow: "0 4px 14px rgba(27,92,58,0.3)" }}>
                    {loading ? "Bağlanıyor..." : "Backend'e Bağlan"}
                </button>
            </div>
        );
    }

    return (
        <>
            <style>{`
        @import url('https://fonts.googleapis.com/css2?family=Playfair+Display:wght@600;700&family=Nunito:wght@400;500;600;700;800&display=swap');
        *, *::before, *::after { box-sizing: border-box; -webkit-tap-highlight-color: transparent; }
        html, body { margin: 0; padding: 0; background: #E0D8C8; }
        * { font-family: 'Nunito', sans-serif; }
        input, textarea, button, select { font-family: 'Nunito', sans-serif; }
        ::-webkit-scrollbar { display: none; }
      `}</style>

            <div style={{ maxWidth: 430, margin: "0 auto", minHeight: "100vh", background: C.bg, position: "relative", overflowX: "hidden", boxShadow: "0 0 40px rgba(0,0,0,0.18)" }}>

                {/* BAŞLIK ÇUBUĞU */}
                {!selectedStudent && (
                    <div style={{ background: C.green, padding: "13px 16px 10px", display: "flex", alignItems: "center", justifyContent: "space-between", position: "sticky", top: 0, zIndex: 50 }}>
                        <div style={{ display: "flex", alignItems: "center", gap: 8 }}>
                            <span style={{ fontSize: 20 }}>☪️</span>
                            <span style={{ color: "#fff", fontFamily: "'Playfair Display', serif", fontSize: 17, fontWeight: 700 }}>Hafız Takip</span>
                        </div>
                        <div style={{ background: "rgba(255,255,255,0.15)", borderRadius: 9, padding: "4px 10px" }}>
                            <span style={{ color: "rgba(255,255,255,0.9)", fontSize: 11, fontWeight: 600 }}>
                                {new Date(TODAY).toLocaleDateString("tr-TR", { day: "numeric", month: "short" })}
                            </span>
                        </div>
                    </div>
                )}

                {/* İÇERİK */}
                <div style={{ overflowY: "auto", height: selectedStudent ? "100vh" : "calc(100vh - 52px)" }}>
                    {selectedStudent ? (
                        <StudentDetail
                            student={selectedStudent}
                            onBack={() => setSelectedStudent(null)}
                            onUpdate={updateStudent}
                        />
                    ) : tab === "dashboard" ? (
                        <Dashboard students={students} onSelectStudent={setSelectedStudent} />
                    ) : tab === "lessons" ? (
                        <LessonsScreen students={students} />
                    ) : tab === "parent" ? (
                        <ParentScreen students={students} />
                    ) : (
                        <EtutScreen students={students} />
                    )}
                </div>

                {/* ALT NAVİGASYON */}
                {!selectedStudent && <BottomNav active={tab} onSelect={setTab} />}
            </div>
        </>
    );
}