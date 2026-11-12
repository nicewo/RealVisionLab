# RealVisionLab

**Kamera tabanlı endüstriyel görüntü kontrolü ve makine entegrasyonu**

RealVisionLab, Windows üzerinde çalışan; birden fazla kameradan görüntü almayı, tanımlı bölgeleri (ROI) incelemeyi ve kontrol sonuçlarını makine otomasyonuna aktarmayı amaçlayan endüstriyel bir görüntü kontrol uygulamasıdır.

Uygulama; kamera görüntüleme ve görüntü işleme akışını, Siemens S7 PLC haberleşmesini ve UART üzerinden harici I/O kontrolünü tek bir operatör yazılımında bir araya getirir.

## Temel yetenekler

Kaynak kodda görülen işlev ve entegrasyon alanları:

- **Çoklu kamera erişimi:** AForge.Video ve DirectShow üzerinden bağlı video girişlerini listeleme, kamera seçimi ve çözünürlük yapılandırması.
- **Bölge bazlı görüntü kontrolü:** Kamera görüntüsü üzerinde tanımlanmış dikdörtgen kontrol bölgeleri (ROI), eşik değerleri ve bölge durumlarının görsel olarak işaretlenmesi.
- **Görüntü işleme:** AForge.Imaging filtreleriyle görüntü işleme altyapısı.
- **PLC entegrasyonu:** Sharp7 üzerinden Siemens S7 PLC bağlantısı ve DB alanlarına kontrol biti yazma.
- **Harici I/O haberleşmesi:** Seri port (UART) üzerinden bağlı çıkış kontrol cihazına komut gönderme.
- **Kod okuma altyapısı:** ZXing ve ZBar bağımlılıklarıyla barkod/QR kod iş akışları.
- **Operatör ve ayar ekranları:** Kamera, kontrol bölgeleri, eşikler, PLC ve UART yapılandırmalarını yönetmeye yönelik Windows Forms arayüzü.
- **İşlem kayıtları:** Günlük tarama kayıtlarının tarih bazlı metin dosyalarına yazılması ve hata tanılama çıktıları.

## Teknoloji yığını

| Katman | Teknoloji |
|---|---|
| Uygulama | C# / Windows Forms |
| Runtime | .NET Framework 4.7.2 |
| Kamera | AForge.Video, DirectShow |
| Görüntü işleme | AForge.Imaging |
| PLC haberleşmesi | Sharp7 / Siemens S7 |
| Seri haberleşme | System.IO.Ports |
| Barkod / QR | ZXing.Net, ZBar |
| Yapılandırma | .NET Application Settings |

## Çözüm yapısı

- `RealVisionLab.sln` — Visual Studio çözümü
- `RealVisionLab/RealVisionLab.csproj` — .NET Framework 4.7.2 Windows uygulaması
- `RealVisionLab/MainPage.cs` — ana operatör ekranı, kamera ve kontrol akışları
- `RealVisionLab/Property.cs` — yapılandırma arayüzü
- `RealVisionLab/PasswordForm.cs` — parola ekranı
- `RealVisionLab/Message.cs` — mesaj ekranı
- `RealVisionLab/Settings1.cs` — uygulama ayarları yardımcı sınıfı

## Geliştirme ortamı

- Windows
- Visual Studio ve .NET Framework 4.7.2 Developer Pack / Targeting Pack
- NuGet paketlerinin geri yüklenebilmesi için internet bağlantısı
- Test için DirectShow tarafından görülebilen kamera veya kamera simülatörü
- PLC ve UART işlevleri için ilgili donanım veya test düzeneği

Çözümü Visual Studio'da açıp NuGet paketlerini geri yükledikten sonra `RealVisionLab` projesini derleyin.

## Devreye alma notları

1. Kamera indeksleri ve çözünürlük ayarlarını hedef bilgisayardaki gerçek kamera listesine göre yapılandırın.
2. ROI koordinatlarının kullanılan kamera çözünürlüğü ve görüntü ölçeklemesiyle eşleştiğini doğrulayın.
3. Görüntü eşiklerini gerçek ürün örnekleriyle kalibre edin; farklı ışık, pozlama ve ürün varyasyonlarını test edin.
4. PLC IP, rack/slot ve DB adreslerini hedef makine PLC programıyla karşılaştırın.
5. UART portu, baud rate ve bağlı I/O cihazının protokolünü doğrulayın.
6. Makine çıkışlarını etkinleştirmeden önce kamera, PLC ve seri haberleşme kaybı senaryolarını test edin.

## Güvenlik ve güvenilirlik

- PLC çıkışları makine hareketini veya proses güvenliğini etkileyebilir. Emniyet fonksiyonları yalnızca bu PC uygulamasına bırakılmamalı; PLC ve donanım seviyesinde bağımsız olarak tasarlanıp doğrulanmalıdır.
- Kamera veya haberleşme hatasında sistemin güvenli duruma geçişi gerçek makine üzerinde doğrulanmalıdır.
- Uygulama, eski tip .NET Framework 4.7.2 ve AForge bağımlılıkları kullanır. Yeni Windows sürümleri ve güncel donanımlarla uyumluluk ayrıca test edilmelidir.
- Kaynak kodda makineye özel IP, seri port, dosya yolu veya erişim bilgileri bulunuyorsa dağıtımdan önce gözden geçirilmelidir.

## Proje durumu

Bu depo mevcut bir Windows Forms uygulamasının kaynak kodunu içerir. README'deki açıklamalar kaynak kodda görülen entegrasyonlara dayanır; görüntü kontrol doğruluğu, çevrim süresi, makine emniyeti ve üretim performansı için bağımsız saha doğrulaması gerektiği unutulmamalıdır.

---

**RealVisionLab — Vision inspection meets industrial control.**
