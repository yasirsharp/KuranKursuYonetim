import { useCallback, useEffect, useRef, useState } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import { useAuth } from '../../context/AuthContext';
import { PATHS } from '../../utils/navigation';
import './welcome-tour.css';

/** Tanıtım bir kez gösterilir; içerik değişirse sürüm artırılarak yeniden gösterilebilir. */
export const WELCOME_TOUR_STORAGE_KEY = 'hafiz_takip_welcome_seen_v1';
const OPEN_EVENT = 'hafiz:welcome-tour';

/** "Tanıtımı tekrar izle" gibi bağlantılardan çağrılır. */
export function openWelcomeTour() {
  window.dispatchEvent(new Event(OPEN_EVENT));
}

function hasSeenTour(): boolean {
  try {
    return localStorage.getItem(WELCOME_TOUR_STORAGE_KEY) === '1';
  } catch {
    return true; // depolama kapalıysa her açılışta göstermeyelim
  }
}

function markTourSeen() {
  try {
    localStorage.setItem(WELCOME_TOUR_STORAGE_KEY, '1');
  } catch {
    /* depolama kapalı: yok say */
  }
}

/** Tanıtımın kendiliğinden açılmayacağı sayfalar (indirme sayfası, platform paneli, kod doğrulama, davetle katılım). */
const SKIP_PATHS = [PATHS.mobileApp, PATHS.platform, PATHS.platformLogin, PATHS.verifyOtp, PATHS.join];

interface Slide {
  eyebrow: string;
  title: string;
  text: string;
  art: React.ReactNode;
}

function buildSlides(firstName?: string): Slide[] {
  return [
    {
      eyebrow: 'Hoş geldiniz',
      title: firstName ? `Hoş geldiniz, ${firstName}` : "Hafız Takip'e hoş geldiniz",
      text: 'Kursunuzun hafızlık takibi, yoklaması ve takvimi tek yerde, sade ve hızlı.',
      art: <MushafArt />,
    },
    {
      eyebrow: 'Ders kaydı',
      title: 'Dersi saniyeler içinde kaydedin',
      text: 'Öğrenciye dokunun; sayfa hazır gelir. Performansı seçin, sıradaki öğrenci kendiliğinden açılır.',
      art: <LessonArt />,
    },
    {
      eyebrow: 'Yoklama ve takvim',
      title: 'Yoklama tek dokunuş, tatiller hazır',
      text: 'Resmî ve dinî tatiller takviminize eklenir; ders günleri kendiliğinden hesaplanır.',
      art: <CalendarArt />,
    },
    {
      eyebrow: 'Telefon uygulaması',
      title: 'Hafız Takip artık cebinizde',
      text: 'Android uygulamasıyla dersleri ve yoklamayı dinleme sırasında telefondan girin.',
      art: <PhoneArt />,
    },
  ];
}

/**
 * İlk açılışta gösterilen animasyonlu tanıtım. Kaydırma, ok tuşları ve Esc desteklenir;
 * "hareketi azalt" tercihinde animasyonlar kapanır (welcome-tour.css).
 */
export default function WelcomeTour() {
  const location = useLocation();
  const navigate = useNavigate();
  const { user } = useAuth();
  const [open, setOpen] = useState(false);
  const [index, setIndex] = useState(0);
  const [direction, setDirection] = useState<'next' | 'prev'>('next');
  const dialogRef = useRef<HTMLDivElement>(null);
  const primaryRef = useRef<HTMLButtonElement>(null);
  const returnFocusRef = useRef<HTMLElement | null>(null);
  const touchStartX = useRef<number | null>(null);

  const slides = buildSlides(user?.firstName);
  const isLast = index === slides.length - 1;

  // İlk ziyarette kendiliğinden aç (kısa bir gecikmeyle: sayfa önce görünsün)
  useEffect(() => {
    if (hasSeenTour() || SKIP_PATHS.some((p) => location.pathname.startsWith(p))) return;
    const timer = window.setTimeout(() => setOpen(true), 450);
    return () => window.clearTimeout(timer);
    // Bilerek yalnızca ilk yüklemede çalışır: sayfa değiştikçe yeniden açılmasın
  }, []);

  // "Tanıtımı tekrar izle"
  useEffect(() => {
    const reopen = () => {
      setIndex(0);
      setDirection('next');
      setOpen(true);
    };
    window.addEventListener(OPEN_EVENT, reopen);
    return () => window.removeEventListener(OPEN_EVENT, reopen);
  }, []);

  const close = useCallback(() => {
    markTourSeen();
    setOpen(false);
    returnFocusRef.current?.focus?.();
  }, []);

  const go = useCallback(
    (target: number) => {
      if (target < 0 || target >= slides.length || target === index) return;
      setDirection(target > index ? 'next' : 'prev');
      setIndex(target);
    },
    [index, slides.length]
  );

  // Açılınca odak düğmeye; kapanınca önceki öğeye döner
  useEffect(() => {
    if (!open) return;
    returnFocusRef.current = document.activeElement as HTMLElement | null;
    primaryRef.current?.focus();
  }, [open]);

  useEffect(() => {
    if (open) primaryRef.current?.focus();
  }, [index, open]);

  const onKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Escape') close();
    else if (e.key === 'ArrowRight') go(index + 1);
    else if (e.key === 'ArrowLeft') go(index - 1);
    else if (e.key === 'Tab') {
      // Odak pencere içinde kalsın
      const focusable = dialogRef.current?.querySelectorAll<HTMLElement>('button, a[href]');
      if (!focusable || focusable.length === 0) return;
      const first = focusable[0]!;
      const last = focusable[focusable.length - 1]!;
      if (e.shiftKey && document.activeElement === first) {
        e.preventDefault();
        last.focus();
      } else if (!e.shiftKey && document.activeElement === last) {
        e.preventDefault();
        first.focus();
      }
    }
  };

  if (!open) return null;
  const slide = slides[index]!;

  return (
    <div className="wt-backdrop" onClick={close}>
      <div
        ref={dialogRef}
        className="wt-dialog"
        role="dialog"
        aria-modal="true"
        aria-labelledby="wt-title"
        aria-describedby="wt-text"
        onClick={(e) => e.stopPropagation()}
        onKeyDown={onKeyDown}
        onTouchStart={(e) => { touchStartX.current = e.touches[0]?.clientX ?? null; }}
        onTouchEnd={(e) => {
          const start = touchStartX.current;
          const end = e.changedTouches[0]?.clientX;
          touchStartX.current = null;
          if (start == null || end == null || Math.abs(end - start) < 50) return;
          go(end < start ? index + 1 : index - 1);
        }}
      >
        <div className="wt-hero" aria-hidden="true">
          <StarPattern />
          <div className="wt-sparkles">
            {Array.from({ length: 10 }, (_, i) => <span key={i} style={{ '--i': i } as React.CSSProperties} />)}
          </div>
          <div key={`art-${index}`} className={`wt-art wt-enter-${direction}`}>{slide.art}</div>
        </div>

        <button type="button" className="wt-skip" onClick={close}>
          {isLast ? 'Kapat' : 'Geç'}
        </button>

        <div className="wt-body">
          <div key={`text-${index}`} className={`wt-text wt-enter-${direction}`}>
            <p className="wt-eyebrow">{slide.eyebrow}</p>
            <h2 id="wt-title" className="wt-title">{slide.title}</h2>
            <p id="wt-text" className="wt-desc">{slide.text}</p>
          </div>

          <div className="wt-progress" aria-label={`Adım ${index + 1} / ${slides.length}`}>
            {slides.map((s, i) => (
              <button
                key={s.eyebrow}
                type="button"
                className={`wt-dot ${i === index ? 'active' : ''} ${i < index ? 'done' : ''}`}
                aria-label={`${i + 1}. adım: ${s.eyebrow}`}
                aria-current={i === index ? 'step' : undefined}
                onClick={() => go(i)}
              />
            ))}
          </div>

          <div className="wt-actions">
            {index > 0 && (
              <button type="button" className="wt-btn wt-btn-ghost" onClick={() => go(index - 1)}>
                Geri
              </button>
            )}
            {isLast && (
              <button
                type="button"
                className="wt-btn wt-btn-ghost"
                onClick={() => { close(); navigate(PATHS.mobileApp); }}
              >
                📱 Uygulamayı indir
              </button>
            )}
            <button
              ref={primaryRef}
              type="button"
              className="wt-btn wt-btn-primary"
              onClick={() => (isLast ? close() : go(index + 1))}
            >
              {isLast ? 'Hemen başlayalım' : 'Devam'}
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}

// ── Süslemeler ve illüstrasyonlar (yalnızca SVG; dışarıdan dosya yok) ─────

/** Sekiz köşeli yıldızlardan oluşan, yavaşça dönen geometrik desen. */
function StarPattern() {
  return (
    <svg className="wt-pattern" viewBox="0 0 200 200" aria-hidden="true">
      <defs>
        <pattern id="wt-stars" width="40" height="40" patternUnits="userSpaceOnUse">
          <g fill="none" stroke="rgba(212,168,39,0.28)" strokeWidth="1">
            <rect x="10" y="10" width="20" height="20" />
            <rect x="10" y="10" width="20" height="20" transform="rotate(45 20 20)" />
            <circle cx="20" cy="20" r="4" />
          </g>
        </pattern>
      </defs>
      <rect width="200" height="200" fill="url(#wt-stars)" />
    </svg>
  );
}

function MushafArt() {
  return (
    <svg viewBox="0 0 160 120" className="wt-svg">
      <ellipse cx="80" cy="108" rx="52" ry="6" fill="rgba(0,0,0,0.18)" />
      {/* rahle */}
      <path d="M40 100 L80 70 L120 100" fill="none" stroke="#D4A827" strokeWidth="4" strokeLinecap="round" />
      <path d="M48 100 L80 78 L112 100" fill="none" stroke="#B8881A" strokeWidth="3" strokeLinecap="round" />
      {/* açık mushaf */}
      <path d="M80 34 C64 26 44 26 30 32 L30 78 C44 72 64 72 80 80 Z" fill="#FFFDF7" />
      <path d="M80 34 C96 26 116 26 130 32 L130 78 C116 72 96 72 80 80 Z" fill="#FFF8E0" />
      <path d="M80 34 L80 80" stroke="#D4A827" strokeWidth="2" />
      <g stroke="#2E7D52" strokeWidth="2" strokeLinecap="round" opacity="0.7">
        <path d="M40 44 H70 M40 52 H70 M40 60 H66" />
        <path d="M90 44 H120 M94 52 H120 M90 60 H116" />
      </g>
      <path d="M118 18 a10 10 0 1 0 6 18 a8 8 0 1 1 -6 -18z" fill="#D4A827" className="wt-twinkle" />
    </svg>
  );
}

function LessonArt() {
  return (
    <svg viewBox="0 0 160 120" className="wt-svg">
      <rect x="50" y="8" width="60" height="104" rx="10" fill="#0F3D27" stroke="#D4A827" strokeWidth="2" />
      <rect x="56" y="18" width="48" height="84" rx="4" fill="#FFFDF7" />
      <rect x="62" y="26" width="36" height="12" rx="3" fill="#E8F5EE" />
      <text x="80" y="35" textAnchor="middle" fontSize="8" fontWeight="800" fill="#1B5C3A">Sayfa 41</text>
      <g>
        {[0, 1, 2, 3, 4].map((i) => (
          <rect key={i} x={62 + i * 7.6} y="46" width="6" height="10" rx="2"
            fill={i === 1 ? '#1A4A8C' : '#E0D8C0'} />
        ))}
      </g>
      <rect x="62" y="64" width="36" height="10" rx="3" fill="#1B5C3A" />
      <circle cx="122" cy="40" r="14" fill="#2E7D52" className="wt-pop" />
      <path d="M115 40 l5 5 l9 -10" fill="none" stroke="#FFF" strokeWidth="3" strokeLinecap="round" strokeLinejoin="round" className="wt-pop" />
    </svg>
  );
}

function CalendarArt() {
  return (
    <svg viewBox="0 0 160 120" className="wt-svg">
      <rect x="30" y="18" width="100" height="86" rx="10" fill="#FFFDF7" />
      <rect x="30" y="18" width="100" height="22" rx="10" fill="#1B5C3A" />
      <rect x="30" y="30" width="100" height="10" fill="#1B5C3A" />
      <circle cx="52" cy="18" r="4" fill="#D4A827" />
      <circle cx="108" cy="18" r="4" fill="#D4A827" />
      {Array.from({ length: 15 }, (_, i) => {
        const col = i % 5;
        const row = Math.floor(i / 5);
        const holiday = i === 3 || i === 9;
        return (
          <rect key={i} x={40 + col * 17} y={48 + row * 17} width="12" height="12" rx="3"
            fill={holiday ? '#D4A827' : '#E8F5EE'} className={holiday ? 'wt-twinkle' : undefined} />
        );
      })}
      <path d="M118 74 a12 12 0 1 0 8 21 a9 9 0 1 1 -8 -21z" fill="#D4A827" />
    </svg>
  );
}

function PhoneArt() {
  return (
    <svg viewBox="0 0 160 120" className="wt-svg">
      <rect x="54" y="6" width="52" height="108" rx="12" fill="#0F3D27" stroke="#D4A827" strokeWidth="2" />
      <rect x="59" y="16" width="42" height="88" rx="5" fill="#1B5C3A" />
      <path d="M78 34 a11 11 0 1 0 7 20 a8.5 8.5 0 1 1 -7 -20z" fill="#D4A827" />
      <path d="M90 40 l1.6 3.4 3.7 .4 -2.8 2.5 .8 3.6 -3.3 -1.9 -3.3 1.9 .8 -3.6 -2.8 -2.5 3.7 -.4z" fill="#D4A827" />
      <text x="80" y="72" textAnchor="middle" fontSize="7" fontWeight="800" fill="#FFF8E0">Hafız Takip</text>
      <g className="wt-bounce">
        <circle cx="124" cy="78" r="15" fill="#D4A827" />
        <path d="M124 70 v14 M118 79 l6 6 l6 -6" fill="none" stroke="#1B5C3A" strokeWidth="3" strokeLinecap="round" strokeLinejoin="round" />
      </g>
    </svg>
  );
}
