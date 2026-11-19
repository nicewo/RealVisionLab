# RealVisionLab — Phase 0 Denetim Raporu

Tarih: 2026-11-19 · Denetlenen commit: `0532507` (main) · Kapsam: tüm repo, salt-okunur denetim.
Çalışma ağacında denetimden önce de var olan değişiklikler: `.gitignore` (`logs/` eklenmiş, commitlenmemiş), `logs/T00-audit.log` (izlenmiyor). Bunlara dokunulmadı.

## 1. Proje gerçekte ne?

Tek projeli, **.NET Framework 4.7.2 WinForms** masaüstü uygulaması (`RealVisionLab/RealVisionLab.csproj`). Neredeyse tüm iş mantığı iki dosyada:

- `RealVisionLab/MainPage.cs` (2860 satır): kamera açma, canlı görüntü, 100 ms timer'lı durum makinesi, 21 adet piksel tabanlı "tarama fonksiyonu", UART/S7 çıkış yazma, log.
- `RealVisionLab/Property.cs` (1740 satır): ayar ekranı; ROI/kamera/PLC/UART/bekleme ayarlarını `Properties.Settings` içinde **virgül/slash ile ayrılmış string** olarak saklar.

Gerçek akış (koddan):
1. Başlat sinyali: UART giriş biti (`Uart_to_IO_DataReceived`, `MainPage.cs:2504`), PLC `DB1.DBX0.x` (`MainPage.cs:822`) veya **firma logosuna tıklama** (`pb_RealTekno_Click`, `MainPage.cs:2459`).
2. `Islemler()` (`MainPage.cs:840`): Lamba+Piston çıkışı açılır → `Bekleme[0]` ms bekle → her kamera için kare al → her ROI için `Rect` alanını kırp → `islem_id`'ye göre fonksiyonu `Task.Run` içinde çalıştır → `Ofset[min] <= ölçüm <= Ofset[max]` ise OK.
3. Tümü OK ise "ONAYLANDI", Kaşe+boş bit çıkışı verilir, sonra Reset. NOK ise kırmızı çarpı çizilir, ROI'nin RST bayrağına göre otomatik Reset ya da operatör şifresiyle Reset beklenir.
4. Her tarama `Values/yyyy_MM_dd.txt` dosyasına tek satır, `/` ayraçlı kayıt olarak eklenir.

AForge **yalnızca kamera yakalama** için kullanılıyor; görüntü işleme tamamen elle yazılmış `GetPixel/SetPixel` ve `LockBits` döngüleri. Kodda ürüne özel (Audi parçası, kol uzunluğu, mm kalibrasyonu) sabitler var. Bu repo tek bir makineye göre yazılmış bir üretim uygulamasının kaynak dökümü; README'deki "genel amaçlı vision platformu" anlatımından daha dar kapsamlı.

## 2. Özellik tablosu

| Özellik | Durum | Kanıt |
|---|---|---|
| Çoklu kamera yakalama (AForge DirectShow) | implemented | `MainPage.cs:359-430` `Cam_adjust`, `Property.cs:602` |
| Kamera keşfi / indeks seçimi | partial | Kamera indeks numarasıyla seçiliyor (cihaz adı/moniker değil); USB sırası değişince yanlış kamera açılır. `debug.txt` içinde `Cam_adjust` ve `Cam_cmboxAdjust` IndexOutOfRange kayıtları var |
| Kamera özelliklerini XML'e kaydetme/yükleme | implemented | `MainPage.cs:2694-2843` (`CameraSettings.xml`, hatalar sessizce yutuluyor) |
| ROI tanımlama (mouse ile konumlama) | implemented | `Property.cs:674-693`, `MainPage.cs:2608`; X/Y ölçek oranları ters kullanılıyor (`MainPage.cs:2612` ile `:300` karşılaştır) |
| ROI sınır doğrulaması | **broken** | `bitmap[c].Clone(Rect[i], …)` (`MainPage.cs:931`) kare dışına taşan ROI'de GDI+ `OutOfMemoryException` fırlatır; `debug.txt` 29.07.2024 kaydı tam olarak bu |
| Tarama fonksiyonları 0-5 (renk/siyah/beyaz sayma, genişlik, konum) | implemented, unverified | `MainPage.cs:1239-1518` |
| 6-7 Açı hesapla yatay/dikey | **broken** | `sagalt` hiç güncellenmiyor, sağ kenar dalı `solust/solalt`'ın üzerine yazıyor (`MainPage.cs:1569-1573`, `1636-1640`) |
| 8, 10, 11, 12 Audi/mesafe fonksiyonları | partial | Sabit kalibrasyon: `hassasiyet = 78`, `turuncuUst = 116000` (`:1899-1908`), `75px/17mm`, `198 mm`, `okumaOfset = 32` (`:1997-2005`) |
| 9 Çizgi ile lokasyon bul | partial | Diğer ROI'leri kaydırıyor ama `hata[]` değerini hiç atamıyor (`:1712-1778`) |
| 13/14 L/R yatay mesafe | partial | Yorumlar ile dağıtım ters (`//13- L_…`, ama `islem_id 13 → R_…`, `:981-983`); `R_…` overlay'i yanlış kolona çiziyor (`:2074`) |
| 15-16 dikey açı | implemented, unverified | `:2102-2166` |
| 17 İki ölçüm arası fark | partial | Referans ROI indeksleri ROI'nin **genişlik/yüksekliğinden** okunuyor (`:2170-2173`); hatalı boyutta IndexOutOfRange |
| 18 QR okuma (ZXing) | implemented | `:2182-2212`; `find_text.Split('-')[1]` parça adında `-` yoksa exception |
| 19 Buton bekle | partial | PLC'de bit **0 sabit** okunuyor, `plc_buton` ayarı yok sayılıyor (`:2249`); thread-pool'da `Thread.Sleep` döngüsü |
| 20 Harici QR (klavye wedge) | partial | Thread-pool thread'inden `Application.DoEvents()` (`:2300`) |
| ZBar barkod | **placeholder/dead** | `packages.config` ve `zbar/*.dll` var, kodda hiç kullanım yok (grep: 0 sonuç) |
| "AForge.Imaging filtre hattı" (README) | **yok** | Sadece `using AForge.Imaging.Filters;` (`MainPage.cs:1`), hiç filtre sınıfı kullanılmıyor |
| Siemens S7 (Sharp7) | partial | Sadece `DB1.DBB0` okunup yazılıyor (`MainPage.cs:468,599,822,872…`); DB numarası/offset sabit |
| PLC "Fonksiyonlar" ayar sekmesi (`PlcFonk`) | **placeholder** | Ayar ekranında düzenleniyor (`Property.cs:353-492, 903-971`), runtime'da hiç okunmuyor |
| UART çıkış/giriş | partial | Port olarak **son COM port** seçiliyor (`MainPage.cs:506`), `Uart` ayarı ("3-3") kullanılmıyor; timeout yok |
| UART kurtarma (`C:\UartReset\UartReset.exe` runas) | unverified / risk | `MainPage.cs:520, 558`; repoda böyle bir araç yok |
| Şifre ekranı | partial | Sadece NOK sonrası Reset için (`MainPage.cs:2550-2598`). **Ayar ekranı şifresiz açılıyor** (`Pb_companyLogo_Click`, `:2465`) |
| Admin modu | implemented, güvensiz | Ayar ekranındaki "gönder" kutusuna `realtekno` veya `2580` yazılınca (`Property.cs:1473,1561`) |
| Günlük tarama logu | implemented | `MainPage.cs:1130-1161`; boş dosyada başlangıç parse hatası (`:190`, `debug.txt` "Load Error") |
| Hata logu `debug.txt` | implemented | `Tx_debug_TextChanged` (`:2487`) + `Program.cs:30`; döndürme/limit yok, çalışma dizinine göre göreli yol |
| Yazıcı seçimi | placeholder | Ayar kaydediliyor (`Property.cs:1340`), hiçbir yerde yazdırılmıyor |
| `Message` formu | dead | Constructor içinde `Dispose()` (`Message.cs:20`), kullanılmıyor |
| `Settings1.cs` | dead | Boş şablon |
| ClickOnce yayın | partial | `InstallUrl/UpdateUrl` GitHub **tree** sayfasına (eski org) işaret ediyor (`csproj:28-29`); güncelleme bu URL'den çalışmaz |
| Testler | yok | Repoda test projesi yok |

## 3. Build / test

| Komut | Sonuç |
|---|---|
| `which msbuild nuget dotnet csc` (Git Bash) | `msbuild`, `nuget`, `csc` PATH'te **yok**; `dotnet` var |
| `dotnet --version` / `--list-sdks` | `10.0.404` |
| vswhere / Visual Studio MSBuild tespiti | **Çalıştırılamadı**: oturum izinleri çalışma dizini dışındaki `C:\Program Files (x86)\…` yollarına erişimi engelledi |
| `msbuild RealVisionLab.sln` | **Çalıştırılmadı**. Nedenler: (1) MSBuild PATH'te yok; (2) `packages/` klasörü yok ve proje `packages.config` kullanıyor, `dotnet restore` bunu geri yüklemez; (3) build `obj/`, `Bin/` üretirdi, bu denetimde dosya oluşturmak yasak |
| Testler | Test projesi yok, çalıştırılacak test yok |

Statik build riskleri:
- `EnsureNuGetPackageBuildImports` hedefi `..\packages\NETStandard.Library.2.0.0\…` yoksa build'i hata ile durdurur (`csproj:271-276`). Önce restore gerekir: `msbuild -t:restore -p:RestorePackagesConfig=true` veya `nuget restore`.
- `System` referansında 6 seviye yukarı çıkan mutlak `HintPath` var (`csproj:126`); kırılgan.
- `RealVision-Lab` konfigürasyonu çıktıyı repo içindeki izlenen `Program\RealVision-Lab\` klasörüne yazıyor (`csproj:74`). Build, izlenen dosyaları değiştirir.
- `ManifestKeyFile RealVisionLab_TemporaryKey.pfx` repoda yok (iyi). `SignManifests=false`, normal build'i etkilemez; publish'i etkileyebilir.
- Repodaki `Program/RealVision-Lab/RealVisionLab.exe` ile kaynağın eşleştiği **doğrulanamaz**.

## 4. Bulgular

### 4.1 Güvenlik / gizli bilgiler
- **Hardcoded master şifre `2580`**: `PasswordForm.cs:62,74`, `Property.cs:1473,1561`. Admin anahtar kelimesi `realtekno`. Public repoda olduğu için ifşa olmuş durumda.
- **Varsayılan kullanıcı şifresi `1903`** düz metin olarak: `Settings.settings:15`, `App.config`, izlenen `Program/RealVision-Lab/RealVisionLab.exe.config`. Şifre `user.config` içinde düz metin saklanıyor (`PasswordForm.cs:40`).
- Ayar ekranı (PLC IP, çıkış bitleri, ROI'ler) şifresiz açılıyor.
- **Gizli 5×5 px etiket** `Reset_setting` (`MainPage.Designer.cs:166-175`) tıklanınca onay sormadan `Settings.Default.Reset()` çalıştırıyor: tüm ROI/PLC ayarları silinir.
- `UartReset.exe` sabit yoldan **yönetici olarak (`runas`)** başlatılıyor (`MainPage.cs:519-526, 557-567`). O yola yazabilen biri yetki yükseltebilir.
- Repoda API anahtarı, token veya bağlantı dizesi **bulunmadı**. PLC IP varsayılanı `192.168.000.000`.

### 4.2 Fonksiyonel güvenlik / güvenilirlik (makine çıkışları)
- **Fail-open tarama**: 0-11 numaralı fonksiyonlar exception'ı yakalayıp sadece logluyor, `hata[c,r]` değerini atamıyor (örn. `Kırmızı_say` catch `:1293`). `Reset()` her ROI'yi `false` (OK) yapıyor (`:443-449`). Fonksiyon patlarsa ROI **OK sayılır** ve Kaşe çıkışı verilebilir.
- **Logoya tıklamak çevrimi başlatıyor** (`:2459`): Lamba ve **Piston** çıkışı sürülüyor.
- **Sonsuz bekleme**: kare gelmezse `do { … } while (bitmap[c] == null)` döngüsü (`:891-902`) bitmez, çevrim takılı kalır (çıkışlar açık kalabilir).
- Kamera/PLC/UART kopunca güvenli duruma geçen merkezi bir mekanizma yok. Timer sadece yeniden bağlanmayı deniyor (`:790-831`).
- `Program.cs:39` ThreadException'da `Application.Restart()`: ne çıkışları sıfırlıyor ne de restart döngüsünü sınırlıyor. `AppDomain.UnhandledException` yok; kamera thread'indeki exception süreci düşürür.
- **Thread güvenliği**: `CheckForIllegalCrossThreadCalls = false` (`:138`). Kamera thread'i `pb_scene.Image`'e yazıyor, `Task.Run` içindeki fonksiyonlar UI kontrollerine yazıyor, `S7Client` UI timer'ı ve thread-pool'dan **kilitsiz** eşzamanlı kullanılıyor.
- `Uart_connect`: `Open()` başarısız olsa da `finally` içinde `Write` yapılıyor (`:544-550`). Ardından UI thread'inde `Thread.Sleep(1000)` var (`:551`). S7 `ConnectTo` da UI thread'inde blokluyor.

### 4.3 Kaynak / bellek
- Her çevrimde `bitmap[c]`, `Pen`, `Kırmızı_say` sonuç bitmap'i dispose edilmiyor. `Threshold_Apply` her karede yeni Bitmap üretiyor, eskisini bırakmıyor (`:2357, 2432`). `New_Frame`, `Islemler`'in o anda kullandığı `pb_scene.Image`'i dispose edebilir (yarış).
- `debug.txt` sınırsız büyüyor. Tarama logu için 1 GB eşiği var ama ikinci dosya da sınırsız (`:1133-1146`).

### 4.4 Konfigürasyon
- ROI tanımı 11 alanlı CSV string: `X,Y,W,H,Min,Max,CamId,İşlem,Thrs,RST,ParçaAdı`. Her erişimde `Split` + `int.Parse` yapılıyor, şema doğrulaması yok. Parça adına `,` veya `/` yazılırsa tüm ayar bozulur.
- `btn_delCam_Click`, `Cam` ayarını `','` ile bölüyor; doğrusu `'/'` (`Property.cs:1179, 1199`). Kamera silmek `Cam` ayarını bozar.
- `btn_bit_Click` geçersiz değerde hep `nud_Lamba`'yı düzeltiyor (`Property.cs:1654-1656`, kopyala-yapıştır hatası).
- UART bit numaraları tutarsız: constructor 1 tabanlı yüklüyor (`Property.cs:95-98`), `Property_Load` 0 tabanlı tekrar yüklüyor (`:1311-1314`), `btn_bit_Click` `-1` ile kaydediyor. Her kaydetmede bit numarası **1 azalabilir** (unverified, UI akışına bağlı).
- `Bekleme` 11 elemanlı `-` ayraçlı liste; anlamları kodda belgelenmemiş.

### 4.5 Repo hijyeni
- İzlenen derleme çıktıları: `Program/RealVision-Lab/*.dll|exe`, `app.publish/`, `Program/Setup/` (4 ClickOnce sürümü, `setup.exe`). Toplam ~25 MB ikili dosya.
- İzlenen **üretim verisi**: `degerler.txt` (~400 KB tarama logu), `Program/RealVision-Lab/debug.txt`, `Values/*.txt`.
- `.gitignore` şunları kapsamıyor: `packages/`, `debug.txt`, `Values/`, `CameraSettings.xml`, `*.pfx`, `app.publish/`. `.gitattributes` sadece `* text=auto`; ikili/CRLF kuralı yok.
- `Program/RealVision-Lab/Properties/Settings.*` kaynak dosyalarının birebir kopyası (diff: aynı).

### 4.6 Bağımlılıklar (`packages.config`)
- AForge.* 2.2.5 (2013, bakımı yok), DirectShowLib.Standard 2.1.0, Sharp7 1.1.84, ZXing.Net 0.16.9 + Bindings.Windows.Compatibility 0.16.12, System.Drawing.Common 7.0.0 (net472 için gereksiz), ZBar 1.0.2 (kullanılmıyor).
- `zxing.presentation` (WPF bağlaması) referanslı ama kullanılmıyor (unverified, grep'e göre).
- Sürümler pinli (packages.config), lock dosyası yok.

### 4.7 TODO/FIXME, ölü ve tekrar eden kod
- TODO/FIXME bulunmadı.
- 21 fonksiyonluk `if/else` dağıtımı iki kez birebir kopyalanmış (`MainPage.cs:934-996` ve `1002-1058`).
- `Property.cs` içinde `btn_send_Click` ile `txt_send_KeyDown` birebir aynı (`:1469-1553`, `:1555-1642`). `Value_changed`'in 3 dalı aynı serileştirmeyi tekrar ediyor.
- Ölü kod: `Message.cs`, `Settings1.cs`, `MainPage.cs:1065-1068` (iki dal aynı), yorum satırına alınmış kalibrasyon blokları, kullanılmayan `qr`, `alarm`, `newFileIndex`, `qrCodeText` alanları, `Uart`/`QrFind`/`Printer`/`PlcFonk` ayarları.

## 5. Plan'daki odak maddeleri

| Madde | Durum |
|---|---|
| Legacy .NET Framework build | Kaynak var, build bu ortamda doğrulanamadı (MSBuild yok, packages restore edilmemiş). Restore + build script'i yok |
| Kamera keşfi | İndeks tabanlı, kırılgan. Kamera yoksa "Kamera N Bulunamadı" gösteriliyor ama tarama başlatılabiliyor ve sonsuz beklemeye giriyor |
| ROI | Çalışıyor; sınır doğrulaması yok (OOM), X/Y ölçek oranları ters, CSV ayar formatı kırılgan |
| Görüntü hattı | Elle yazılmış piksel döngüleri; 2 fonksiyon bozuk, birkaçında sabit kalibrasyon; fail-open hata yönetimi; bellek sızıntısı |
| PLC S7 | Sadece DB1.DBB0; kilitsiz eşzamanlı erişim; bağlantı kopmasında güvenli durum yok; `PlcFonk` ekranı işlevsiz |
| UART | Son COM port varsayımı, timeout yok, `finally` hatası, harici runas aracı |
| Barkod | ZXing QR çalışıyor (unverified); ZBar ölü |
| Config | `Properties.Settings` CSV string'leri; doğrulama yok; kamera silmede bozulma hatası |
| Logging | Günlük tarama logu ve `debug.txt` var; rotasyon yok, göreli yol |
| Hatada güvenli davranış | **Yetersiz**: fail-open, logo tetiklemesi, sonsuz döngü, restart'ta çıkış sıfırlama yok |
| Donanımdan bağımsız testler | Yok; mantık UI formuna gömülü olduğu için önce ayrıştırma gerekiyor |
| Devreye alma checklist | README'de 6 maddelik özet var; ayrıntılı checklist yok |

## 6. Riskler, bilinmeyenler, engeller
- **Engel:** Bu makinede MSBuild/VS Build Tools PATH'te yok. Erişim izni olmadığı için kurulu olup olmadığı da doğrulanamadı. Phase 1 build task'ı VS 2022 Build Tools + .NET 4.7.2 Developer Pack gerektirir.
- **Bilinmeyen:** Sahada hangi fonksiyonların (`islem_id`) ve hangi `Rect` konfigürasyonlarının kullanıldığı. Ölçüm sonucunu değiştirecek algoritma düzeltmeleri (açı fonksiyonları) sahadaki kalibre edilmiş Min/Max değerlerini geçersiz kılabilir. Bu yüzden önce characterization test, sonra sahip kararı gerekiyor.
- **Bilinmeyen:** UART I/O kartının protokolü (1 byte bit maskesi varsayılıyor), `UartReset.exe`'nin ne yaptığı, PLC programındaki DB1 yerleşimi.
- **Bilinmeyen:** Repodaki exe'nin hangi commit'ten derlendiği.
- **Risk:** Kod çıkışları (Piston, Kaşe) sürüyor. Bu PC uygulaması makine emniyet fonksiyonu yerine geçemez. Tüm güvenli durum değişiklikleri **donanımda doğrulanmadı** olarak kalacak.
- **Risk:** Büyük `MainPage.cs` refactor'ı davranış değiştirebilir. Ayrıştırma davranışı koruyan, küçük adımlarla yapılmalı.

## 7. Yol haritası (task dosyaları)

| ID | Başlık | Model | Gerekçe |
|---|---|---|---|
| T01 | Repo hygiene: ignore/attributes | haiku | Mekanik; artefakt ve veri sızıntısını durdurur |
| T02 | Reproducible build baseline (`build.ps1`, csproj düzeltmeleri) | sonnet | Sonraki tüm task'ların doğrulama temeli |
| T03 | Fail-closed inspection & ROI bounds | opus | Hatalı parçanın onaylanması riski |
| T04 | Machine I/O abstraction (S7/UART/Sim + kilit) | opus | Eşzamanlı S7 erişimi, mock katmanı için ön koşul |
| T05 | Safe outputs & unintended actuation | opus | Logo tetiklemesi, gizli reset, kopmada/çökmede güvenli durum |
| T06 | Thread-safety: frames & UI marshaling | opus | `CheckForIllegalCrossThreadCalls=false` yarışları |
| T07 | UART connection robustness | sonnet | Port seçimi, timeout, runas aracı |
| T08 | Password & admin access hardening | sonnet | Hardcoded master şifre, şifresiz ayar ekranı |
| T09 | Settings parsing/validation & Property bugs | sonnet | CSV ayar bozulmaları, kamera silme ve bit numarası hataları |
| T10 | Resource leaks & log rotation | sonnet | Saha OOM kaydı, sınırsız log |
| T11 | Extract inspection core library | opus | Test edilebilirlik için davranışı koruyan ayrıştırma |
| T12 | Unit & characterization tests | sonnet | Algoritma/config regresyon güvencesi |
| T13 | Simulated cycle integration tests | sonnet | Donanımsız çevrim/çıkış sırası doğrulaması |
| T14 | CI workflow (Windows build+test) | haiku | Tekrarlanabilirlik |
| T15 | Documentation & release readiness | haiku | README düzeltmeleri, CONFIG/BUILD/COMMISSIONING/RELEASE |
| T16 | Final verification review | opus | Kabul kriterlerinin kodla doğrulanması, plan checklist'i |

Bağımlılıklar: T02 → T03…; T04 → T05, T13; T11 → T12, T13; T12 → T14.

## 8. Kullanıcıya kalanlar (agent'ların YAPMAYACAĞI işler)
1. Roadmap onayı (plan madde 3). Onay gelmeden T01+ çalıştırılmamalı.
2. VS 2022 Build Tools (MSBuild) + .NET Framework 4.7.2 Developer Pack kurulumu. Kurulu değilse T02 doğrulanamaz.
3. **Şifre rotasyonu**: `2580`/`realtekno`/`1903` public repoda ifşa olmuş. Sahadaki makinelerde şifreyi değiştirin. T08 sonrası servis kodu hash'ini kendiniz üretip ayarlayın; değeri repoya koymayın.
4. İzlenen ikili dosyaların ve üretim verisinin (`Program/`, `degerler.txt`, `debug.txt`, `Values/`) repodan çıkarılması (`git rm --cached`), gerekirse **git geçmişinin temizlenmesi**. Yıkıcı ve geri dönüşsüz bir karar; agent'lar yapmaz.
5. Tüm commit/push/PR işlemleri; GitHub repo ayarları (branch protection, Actions'ı açma).
6. ClickOnce `InstallUrl/UpdateUrl` için doğru yayın konumunun belirlenmesi ve yeniden publish/deploy.
7. **Donanım testleri**: gerçek kameralar, S7 PLC (DB1 yerleşimi), UART I/O kartı. Kopma senaryoları: kamera çekme, Ethernet çekme, USB-UART çekme. Çıkışların güvenli duruma geçtiğinin makinede doğrulanması. Emniyet fonksiyonlarının PLC/donanım seviyesinde bağımsız olduğunun teyidi.
8. Ölçüm sonucunu değiştiren algoritma düzeltmelerine (açı fonksiyonları 6-7, L/R eşlemesi) karar vermek ve sahada yeniden kalibrasyon yapmak.
9. `C:\UartReset\UartReset.exe` aracının kaynağı/gerekliliği hakkında karar.
10. Sahadaki gerçek `user.config` (ROI/Bekleme) değerlerinin yedeklenmesi. Ayar formatı değişikliklerinden önce gerekli.
