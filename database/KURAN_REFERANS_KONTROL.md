# Kur'an Referans Verisi — Hoca Kontrol Listesi

> **TODO (bekliyor):** Bu liste henüz kontrol edilmedi; proje sahibi daha sonra bir hocaya kontrol ettirecek.
> Ezber haritası ve akıllı tekrar (Sprint 6–7) canlıya çıkmadan önce tamamlanmalı.

Uygulama, Türkiye'de hafızlıkta kullanılan **ayetberkenar hafız mushafı (604 sayfa)** düzenini esas alır.
Öğrencinin ilerleme haritası, ders kaydındaki sure bilgisi ve ileride akıllı tekrar planı bu veriye dayanır.

## Neyin kontrol edilmesi gerekiyor?

- **Cüz sayfaları formülle hesaplanır ve kesindir:** 1. cüz s.1–21, sonraki her cüz 20 sayfa (n. cüz s.20n−18 ile başlar), 30. cüz s.582–604.
- **Sure başlangıç sayfaları taslaktır** ve bir hocanın elindeki ayetberkenar mushaftan doğrulaması gerekir. Taslak, Medine mushafı (604 sayfa) listesinden alınmıştır; iki düzen çoğu yerde aynıdır ama bazı surelerde ±1 sayfa fark olabilir.

## Nasıl düzeltilir?

1. Aşağıdaki tabloda her surenin başladığı sayfayı mushaftan kontrol edip ✔ sütununu işaretleyin.
2. Farklı olanları `database/02_quran_reference.sql` içindeki ilgili satırda düzeltin (son sayı başlangıç sayfasıdır).
3. Değişikliği uygulamak için: `.\database\db.ps1 -Migrate -Database HafizTakipDB`. Referans verisi her migrate'te yeniden yüklenir ve mevcut ders kayıtlarının sure/cüz bilgisi de yeniden hesaplanır.

Ayet sayıları (toplam 6236) standarttır; yine de kontrol sırasında gözünüze çarpan bir fark olursa not edin.

| Sıra | Sure | Ayet | Başlangıç sayfası | Cüz | ✔ | Not |
|---:|---|---:|---:|---:|:---:|---|
| 1 | Fatiha | 7 | 1 | 1 | ☐ | |
| 2 | Bakara | 286 | 2 | 1 | ☐ | |
| 3 | Al-i İmran | 200 | 50 | 3 | ☐ | |
| 4 | Nisa | 176 | 77 | 4 | ☐ | |
| 5 | Maide | 120 | 106 | 6 | ☐ | |
| 6 | Enam | 165 | 128 | 7 | ☐ | |
| 7 | Araf | 206 | 151 | 8 | ☐ | |
| 8 | Enfal | 75 | 177 | 9 | ☐ | |
| 9 | Tevbe | 129 | 187 | 10 | ☐ | |
| 10 | Yunus | 109 | 208 | 11 | ☐ | |
| 11 | Hud | 123 | 221 | 11 | ☐ | |
| 12 | Yusuf | 111 | 235 | 12 | ☐ | |
| 13 | Rad | 43 | 249 | 13 | ☐ | |
| 14 | İbrahim | 52 | 255 | 13 | ☐ | |
| 15 | Hicr | 99 | 262 | 14 | ☐ | |
| 16 | Nahl | 128 | 267 | 14 | ☐ | |
| 17 | İsra | 111 | 282 | 15 | ☐ | |
| 18 | Kehf | 110 | 293 | 15 | ☐ | |
| 19 | Meryem | 98 | 305 | 16 | ☐ | |
| 20 | Taha | 135 | 312 | 16 | ☐ | |
| 21 | Enbiya | 112 | 322 | 17 | ☐ | |
| 22 | Hac | 78 | 332 | 17 | ☐ | |
| 23 | Müminun | 118 | 342 | 18 | ☐ | |
| 24 | Nur | 64 | 350 | 18 | ☐ | |
| 25 | Furkan | 77 | 359 | 18 | ☐ | |
| 26 | Şuara | 227 | 367 | 19 | ☐ | |
| 27 | Neml | 93 | 377 | 19 | ☐ | |
| 28 | Kasas | 88 | 385 | 20 | ☐ | |
| 29 | Ankebut | 69 | 396 | 20 | ☐ | |
| 30 | Rum | 60 | 404 | 21 | ☐ | |
| 31 | Lokman | 34 | 411 | 21 | ☐ | |
| 32 | Secde | 30 | 415 | 21 | ☐ | |
| 33 | Ahzab | 73 | 418 | 21 | ☐ | |
| 34 | Sebe | 54 | 428 | 22 | ☐ | |
| 35 | Fatır | 45 | 434 | 22 | ☐ | |
| 36 | Yasin | 83 | 440 | 22 | ☐ | |
| 37 | Saffat | 182 | 446 | 23 | ☐ | |
| 38 | Sad | 88 | 453 | 23 | ☐ | |
| 39 | Zümer | 75 | 458 | 23 | ☐ | |
| 40 | Mümin | 85 | 467 | 24 | ☐ | |
| 41 | Fussilet | 54 | 477 | 24 | ☐ | |
| 42 | Şura | 53 | 483 | 25 | ☐ | |
| 43 | Zuhruf | 89 | 489 | 25 | ☐ | |
| 44 | Duhan | 59 | 496 | 25 | ☐ | |
| 45 | Casiye | 37 | 499 | 25 | ☐ | |
| 46 | Ahkaf | 35 | 502 | 26 | ☐ | |
| 47 | Muhammed | 38 | 507 | 26 | ☐ | |
| 48 | Fetih | 29 | 511 | 26 | ☐ | |
| 49 | Hucurat | 18 | 515 | 26 | ☐ | |
| 50 | Kaf | 45 | 518 | 26 | ☐ | |
| 51 | Zariyat | 60 | 520 | 26 | ☐ | |
| 52 | Tur | 49 | 523 | 27 | ☐ | |
| 53 | Necm | 62 | 526 | 27 | ☐ | |
| 54 | Kamer | 55 | 528 | 27 | ☐ | |
| 55 | Rahman | 78 | 531 | 27 | ☐ | |
| 56 | Vakıa | 96 | 534 | 27 | ☐ | |
| 57 | Hadid | 29 | 537 | 27 | ☐ | |
| 58 | Mücadele | 22 | 542 | 28 | ☐ | |
| 59 | Haşr | 24 | 545 | 28 | ☐ | |
| 60 | Mümtehine | 13 | 549 | 28 | ☐ | |
| 61 | Saf | 14 | 551 | 28 | ☐ | |
| 62 | Cuma | 11 | 553 | 28 | ☐ | |
| 63 | Münafikun | 11 | 554 | 28 | ☐ | |
| 64 | Teğabün | 18 | 556 | 28 | ☐ | |
| 65 | Talak | 12 | 558 | 28 | ☐ | |
| 66 | Tahrim | 12 | 560 | 28 | ☐ | |
| 67 | Mülk | 30 | 562 | 29 | ☐ | |
| 68 | Kalem | 52 | 564 | 29 | ☐ | |
| 69 | Hakka | 52 | 566 | 29 | ☐ | |
| 70 | Mearic | 44 | 568 | 29 | ☐ | |
| 71 | Nuh | 28 | 570 | 29 | ☐ | |
| 72 | Cin | 28 | 572 | 29 | ☐ | |
| 73 | Müzzemmil | 20 | 574 | 29 | ☐ | |
| 74 | Müddessir | 56 | 575 | 29 | ☐ | |
| 75 | Kıyame | 40 | 577 | 29 | ☐ | |
| 76 | İnsan | 31 | 578 | 29 | ☐ | |
| 77 | Mürselat | 50 | 580 | 29 | ☐ | |
| 78 | Nebe | 40 | 582 | 30 | ☐ | |
| 79 | Naziat | 46 | 583 | 30 | ☐ | |
| 80 | Abese | 42 | 585 | 30 | ☐ | |
| 81 | Tekvir | 29 | 586 | 30 | ☐ | |
| 82 | İnfitar | 19 | 587 | 30 | ☐ | |
| 83 | Mutaffifin | 36 | 587 | 30 | ☐ | |
| 84 | İnşikak | 25 | 589 | 30 | ☐ | |
| 85 | Büruc | 22 | 590 | 30 | ☐ | |
| 86 | Tarık | 17 | 591 | 30 | ☐ | |
| 87 | Ala | 19 | 591 | 30 | ☐ | |
| 88 | Gaşiye | 26 | 592 | 30 | ☐ | |
| 89 | Fecr | 30 | 593 | 30 | ☐ | |
| 90 | Beled | 20 | 594 | 30 | ☐ | |
| 91 | Şems | 15 | 595 | 30 | ☐ | |
| 92 | Leyl | 21 | 595 | 30 | ☐ | |
| 93 | Duha | 11 | 596 | 30 | ☐ | |
| 94 | İnşirah | 8 | 596 | 30 | ☐ | |
| 95 | Tin | 8 | 597 | 30 | ☐ | |
| 96 | Alak | 19 | 597 | 30 | ☐ | |
| 97 | Kadir | 5 | 598 | 30 | ☐ | |
| 98 | Beyyine | 8 | 598 | 30 | ☐ | |
| 99 | Zilzal | 8 | 599 | 30 | ☐ | |
| 100 | Adiyat | 11 | 599 | 30 | ☐ | |
| 101 | Karia | 11 | 600 | 30 | ☐ | |
| 102 | Tekasür | 8 | 600 | 30 | ☐ | |
| 103 | Asr | 3 | 601 | 30 | ☐ | |
| 104 | Hümeze | 9 | 601 | 30 | ☐ | |
| 105 | Fil | 5 | 601 | 30 | ☐ | |
| 106 | Kureyş | 4 | 602 | 30 | ☐ | |
| 107 | Maun | 7 | 602 | 30 | ☐ | |
| 108 | Kevser | 3 | 602 | 30 | ☐ | |
| 109 | Kafirun | 6 | 603 | 30 | ☐ | |
| 110 | Nasr | 3 | 603 | 30 | ☐ | |
| 111 | Tebbet | 5 | 603 | 30 | ☐ | |
| 112 | İhlas | 4 | 604 | 30 | ☐ | |
| 113 | Felak | 5 | 604 | 30 | ☐ | |
| 114 | Nas | 6 | 604 | 30 | ☐ | |
