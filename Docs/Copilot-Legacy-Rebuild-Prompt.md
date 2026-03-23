# Copilot Sorgusu - Legacy Koddan Yeni OtoRestoran Mimarisi Çıkarma

Aşağıdaki sorguyu Copilot sohbetinde veya ajan akışında doğrudan kullanabilirsin.

## Kısa Sürüm
`Bu Unity projesini baştan, daha temiz bir mimariyle yeniden kuruyorum. Mevcut projedeki ilgili kodları Legacy klasörüne taşıyacağım. Önce Legacy içindeki sistemi incele, bu kodun amaçladığı oynanış davranışını çıkar, sonra bunu yeni projede temiz ve modüler bir mimariyle nasıl kurmam gerektiğini öner. İncelerken sadece mevcut davranışı kopyalama; çalışan parçalar, eksik parçalar ve placeholder sınıfları ayır. Her incelemede şu formatı kullan: 1) sistemin amacı, 2) legacy sınıflar ve sorumlulukları, 3) şu an gerçekten çalışan davranış, 4) eksik veya riskli kısımlar, 5) yeni mimaride önerilen sınıflar/arayüzler, 6) minimum uygulanabilir sürüm, 7) bir sonraki taşınacak dosyalar. İlk olarak [BURAYA DOSYA VEYA KLASÖR YAZ] üzerinden başla.`

## Ayrıntılı Sürüm
`Bu Unity projesini sıfırdan yeniden inşa ediyorum. Mevcut repodaki eski kodları Legacy klasörüne taşıyıp tek tek analiz ederek yeni bir proje mimarisine geçeceğim.

Görevin:
Legacy içindeki verdiğim dosya veya klasörü inceleyip bunun oyunda ne amaçladığını, hangi davranışları gerçekten sağladığını ve yeni projede bunu nasıl daha temiz kurmam gerektiğini çıkarmak.

Analiz kuralları:
- Unity C# bağlamında düşün.
- Mevcut legacy kodu birebir kopyalamaya çalışma.
- Çalışan sistemler ile placeholder/taslak sınıfları ayır.
- Sınıf isimlerinden çok davranış ve sorumluluk çıkar.
- MonoBehaviour bağımlılıklarını azaltacak bir yapı öner.
- Domain, application ve Unity adapter katmanlarını ayırmaya çalış.
- Eğer sistem oyunun çekirdek oynanış vizyonuna hizmet ediyorsa bunu özellikle belirt.
- Eğer sistem eksikse, bunu tamamlanmış varsayma.
- Gerekirse yeni projede daha küçük MVP adımları öner.

Her cevap şu başlıklarda olsun:
1. Sistem Özeti
2. Legacy Kodda Bulunan Sınıflar
3. Gerçekten Çalışan Davranışlar
4. Eksik/Taslak Kısımlar
5. Oyun Tasarımındaki Rolü
6. Yeni Mimaride Önerilen Yapı
7. Taşıma Sırası
8. İlk Uygulanacak MVP
9. Gerekirse örnek klasör yapısı

Ayrıca sonunda mutlaka şunları da ver:
- Bu parçayı yeni projede hangi klasöre koymalıyım?
- Hangi dosyaları doğrudan legacy referansı olarak saklamalıyım?
- Hangi dosyaları yeniden yazmalıyım?
- Bu sistemden sonra sırada hangi sistemi incelemeliyim?

Bağlam:
Bu oyunun ana fikri, robotlara kod yazarak restoran otomasyonu kurmak. Restoranda müşteri akışı, masa/sandalye yerleşimi, robot hareketi, sipariş-servis döngüsü, shop, ekonomi, skor ve ilerleme sistemleri var. Ben bunları temiz bir mimari ile yeniden kurmak istiyorum.

İlk analiz hedefi: [BURAYA DOSYA, SINIF VEYA KLASÖR YAZ]`

## Kullanım Örnekleri
### Örnek 1
`İlk analiz hedefi: Legacy/Core/Customers`

### Örnek 2
`İlk analiz hedefi: Legacy/Programming/Runtime/AstInterpreter.cs`

### Örnek 3
`İlk analiz hedefi: Legacy/Core/Restaurant/Grid`

## Önerilen Parça Parça İnceleme Sırası
1. `Legacy/Core/GameLoop`
2. `Legacy/Core/Restaurant/Grid`
3. `Legacy/Core/Restaurant/Objects/Furniture`
4. `Legacy/Core/Customers`
5. `Legacy/Core/Robots`
6. `Legacy/Programming/Commands`
7. `Legacy/Programming/Runtime`
8. `Legacy/Programming/Language`
9. `Legacy/Core/Shop`
10. `Legacy/Core/Inventory`
11. `Legacy/Core/Economy`
12. `Legacy/Core/Score`
13. `Legacy/Core/Restaurant/Objects/Kitchen`
14. `Legacy/UI`
15. `Legacy/Infastructure/SaveLoad`

## Ek Mini Sorgular
### Bir sistemi yeniden yazdırmak için
`Bu legacy sistemi yeni projede minimum ama temiz bir mimari ile yeniden tasarla. Önce sorumlulukları ayır, sonra gerekli sınıfları ve interface'leri listele, ardından uygulama sırasını ver. Kod yazmadan önce domain modelini açıkla.`

### Sadece klasör yapısı çıkarmak için
`Bu legacy sistemi yeni projede hangi klasör yapısıyla kurmam gerektiğini çıkar. Domain, Application, UnityAdapters ve Presentation olarak ayır. Her klasör için hangi dosyaların bulunacağını yaz.`

### Sadece risk analizi almak için
`Bu legacy sistemde yeni projeye birebir taşınmaması gereken parçaları tespit et. Placeholder sınıfları, Unity'ye aşırı bağlı kısımları, test edilmesi gereken davranışları ve yeniden tasarlanması gereken noktaları listele.`
