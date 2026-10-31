import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import QRCode from 'qrcode';
import { ANDROID_APK_URL } from '../../utils/constants';
import { PATHS } from '../../utils/navigation';

const showTestHints = import.meta.env.VITE_SHOW_DEV_HINTS === 'true';

interface Step {
  title: string;
  text: React.ReactNode;
}

const STEPS: Step[] = [
  {
    title: 'İndirin',
    text: (
      <>
        Yukarıdaki yeşil <strong>Uygulamayı indir</strong> düğmesine dokunun. İndirme birkaç dakika sürebilir;
        mümkünse Wi-Fi'ye bağlıyken yapın.
      </>
    ),
  },
  {
    title: 'Dosyayı açın',
    text: (
      <>
        İndirme bitince ekranın altında <strong>“hafiz-takip.apk”</strong> yazısı çıkar, yanındaki{' '}
        <strong>Aç</strong>'a dokunun.
        <br />
        Kaçırdıysanız: telefonunuzda <strong>Dosyalar</strong> (veya <strong>İndirilenler</strong>) uygulamasını
        açın ve <strong>hafiz-takip.apk</strong> dosyasına dokunun.
      </>
    ),
  },
  {
    title: 'İzin verin',
    text: (
      <>
        <strong>“Bu kaynaktan uygulama yüklenmesine izin verilmiyor”</strong> gibi bir uyarı çıkarsa{' '}
        <strong>Ayarlar</strong>'a dokunun, <strong>Bu kaynaktan izin ver</strong> düğmesini açın, sonra telefonun{' '}
        <strong>geri</strong> tuşuna basın.
      </>
    ),
  },
  {
    title: 'Yükleyin',
    text: (
      <>
        <strong>Yükle</strong>'ye dokunun. <strong>“Play Protect”</strong> uyarısı çıkarsa{' '}
        <strong>Yine de yükle</strong>'ye dokunun. Uygulama Play Store'dan gelmediği için bu uyarı normaldir.
      </>
    ),
  },
  {
    title: 'Giriş yapın',
    text: (
      <>
        <strong>Aç</strong>'a dokunun. Kursa kayıtlı <strong>e-posta adresinizi</strong> yazın, e-postanıza gelen{' '}
        <strong>6 haneli kodu</strong> girin. Bir kez giriş yapmanız yeterli; uygulama sizi hatırlar.
      </>
    ),
  },
];

/** Herkese açık: Android uygulamasını indirme ve kurma anlatımı (yaşça büyük kullanıcılar için büyük yazı). */
export default function MobileAppPage() {
  const [qr, setQr] = useState('');
  const pageUrl = typeof window !== 'undefined' ? window.location.origin + window.location.pathname : '';

  useEffect(() => {
    // Bilgisayardan bakan, telefonunun kamerasıyla okutup bu sayfayı telefonda açabilsin
    QRCode.toDataURL(pageUrl, { width: 220, margin: 1, color: { dark: '#1B5C3A', light: '#FFFDF7' } })
      .then(setQr)
      .catch(() => setQr(''));
  }, [pageUrl]);

  return (
    <div className="mobile-app-page">
      <header className="mobile-app-hero">
        <div className="mobile-app-icon" aria-hidden="true">☪️</div>
        <h1>Hafız Takip telefon uygulaması</h1>
        <p>Ders ve yoklamayı telefonunuzdan girin. Android telefonlar içindir.</p>
        <a className="mobile-app-download" href={ANDROID_APK_URL} download="hafiz-takip.apk">
          ⬇️ Uygulamayı indir
        </a>
        <p className="mobile-app-small">Dosya büyüktür; mümkünse Wi-Fi'ye bağlıyken indirin.</p>
      </header>

      <section aria-labelledby="kurulum">
        <h2 id="kurulum">Nasıl kurulur?</h2>
        <ol className="mobile-app-steps">
          {STEPS.map((step, i) => (
            <li key={step.title}>
              <span className="mobile-app-step-no" aria-hidden="true">{i + 1}</span>
              <div>
                <h3>{step.title}</h3>
                <p>{step.text}</p>
              </div>
            </li>
          ))}
        </ol>
        {showTestHints && (
          <p className="mobile-app-note">Test sunucusu: e-posta gönderilmez, giriş kodu <strong>123456</strong>.</p>
        )}
      </section>

      {qr && (
        <section className="mobile-app-qr" aria-labelledby="qr">
          <h2 id="qr">Bilgisayardan mı bakıyorsunuz?</h2>
          <p>Telefonunuzun kamerasını bu kareye tutun; çıkan bağlantıya dokunarak bu sayfayı telefonda açın.</p>
          <img src={qr} alt="Bu sayfanın adresini içeren karekod" width={220} height={220} />
        </section>
      )}

      <section aria-labelledby="sorular">
        <h2 id="sorular">Sık sorulanlar</h2>
        <details>
          <summary>“Uygulama yüklenmedi” diyor</summary>
          <p>
            Telefonda eski bir Hafız Takip varsa önce onu kaldırın: simgesine parmağınızla basılı tutun,{' '}
            <strong>Kaldır</strong>'a dokunun. Sonra yeniden yükleyin.
          </p>
        </details>
        <details>
          <summary>Telefonum iPhone</summary>
          <p>
            iPhone uygulaması hazırlanıyor. Şimdilik telefonunuzun internet tarayıcısından bu siteye girip aynı
            işleri yapabilirsiniz.
          </p>
        </details>
        <details>
          <summary>Yeni sürüm çıkınca ne yapmalıyım?</summary>
          <p>Bu sayfaya tekrar girip aynı adımları uygulayın. Bilgileriniz silinmez; hepsi sunucuda saklanır.</p>
        </details>
        <details>
          <summary>Kod gelmiyor veya giriş yapamıyorum</summary>
          <p>
            E-postanızın gereksiz (spam) klasörüne de bakın. Olmazsa kurs müdürünüzden e-posta adresinizin sisteme
            doğru yazıldığını kontrol etmesini isteyin.
          </p>
        </details>
      </section>

      <p className="mobile-app-back">
        <Link to={PATHS.login}>← Web sitesinden giriş yap</Link>
      </p>
    </div>
  );
}
