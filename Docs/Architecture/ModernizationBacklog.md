# Modernizasyon Yol Haritası

## Faz 1 — Mimari Temizlik
1. `Programming` runtime için yeni güvenlik katmanını ekle
2. robot başına bağımsız script session modeli kur
3. `ShopManager` içindeki fazla sorumlulukları servis katmanına ayır
4. `InventoryManager` ve `BuildInventoryManager` için ortak abstraction oluştur
5. fiyat ve quantity hesap hatalarını düzelt

## Faz 2 — Veri Modeli Yeniden Kurulumu
1. tüm `ScriptableObject` türlerini tanım verisi odaklı yeniden düzenle
2. `Item`, `PlaceableObject`, `Recipe`, `Milestone`, `ShopProduct` kataloglarını ayır
3. runtime state'i save modellerine ve servislere taşı
4. unlock sistemini milestone tabanlı hale getir

## Faz 3 — Robot İş Akışı
1. robot görevlerini `Order`, `Cook`, `Deliver`, `Clean`, `Refill` olarak ayır
2. script komutlarını görev dispatcher üzerinden çalıştır
3. robot başına hata, iptal ve debug paneli ekle
4. çoklu robot eşzamanlı script yürütmesini doğrula

## Faz 4 — Restoran Simülasyonu
1. sipariş yaşam döngüsünü baştan kur
2. müşteri memnuniyet modelini servis hızı ve doğruluk ile bağla
3. ödeme ve bahşiş hesaplarını ekonomi sistemine taşı
4. restoran puanı ile milestone unlock zincirini bağla

## Faz 5 — Grid ve Obje Etkileşimleri
1. placement kurallarını veri odaklı hale getir
2. appliance runtime sınıflarını ortak etkileşim arayüzü ile birleştir
3. storage, counter, oven ve table davranışlarını ayrı runtime bileşenlerine taşı
4. recipe üretimini station tabanlı hale getir

## Faz 6 — Sahne ve Test Düzeni
1. `Bootstrap`, `Production`, `Sandbox` sahnelerini ayır
2. edit mode testleri ekle
3. play mode testleri ekle
4. kritik gameplay akışları için smoke test senaryoları yaz

## Öncelikli Teknik Borçlar
- `GameCodeRunner` çoklu robot script gereksinimiyle uyumsuz
- `SafeExecutionGuard` boş
- `ScriptRuntime` boş
- `ShopProductDefinitionSO` quantity hesabı yanlış
- `ShopManager` çok fazla alanı biliyor
- envanter sistemleri kopya mantık taşıyor

## Önerilen İlk Uygulama Sırası
1. programlama runtime güvenliği
2. robot başına bağımsız execution session
3. envanter abstraction
4. shop ve progression ayrıştırma
5. grid object runtime ayrıştırma
6. scene bootstrap düzeni
