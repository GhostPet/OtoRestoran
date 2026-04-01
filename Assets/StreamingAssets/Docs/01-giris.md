<!-- doc:id="giris"; title="1 - Giriş"; order=10 -->
# Giriş

Bu bölüm, robot programlama DSL'inin ne olduğunu ve oyunda nasıl çalıştığını açıklar.

Bu dokümandaki örnekler Python benzeri bir sözdizimi kullanır. Ancak bu dil gerçek Python değildir. Kodlar doğrudan Unity nesnelerine erişemez; oyun dünyasıyla iletişim her zaman güvenli built-in API üzerinden gerçekleştirilir.

## DSL Nedir?

Bu sistem, robotlara davranış tanımlaman için hazırlanmış **oyuna özel bir programlama dili**dir.

Amaç:
- robotlara tekrar eden işleri öğretmek
- görev akışlarını otomatikleştirmek
- koşullara göre karar veren davranışlar oluşturmak
- aynı problemi farklı stratejilerle çözebilmek

Dil, Python’a benzer ancak birebir Python değildir. Yalnızca oyunun desteklediği sözdizimi ve built-in API kullanılabilir.

## Bu Sistemle Ne Yapabilirsin?

Script yazarak aşağıdaki işlemleri gerçekleştirebilirsin:
- robotu belirli noktalara veya nesnelere yönlendirmek
- masaları dolaşmak
- müşterileri kontrol etmek
- sipariş almak
- fırın ve buzdolabı ile etkileşim kurmak
- en yakın nesneyi bulup buna göre karar vermek
- sürekli çalışan otomasyon davranışları kurmak

Doğru yazılmış bir script, robotun davranışını senin yerine sürekli ve tutarlı şekilde yönetir.

## Script Ne Zaman Çalışır / Çalışmaz

Bir script aşağıdaki koşullarda çalışır:
- kod derlenebilir durumdaysa
- script içinde `main()` fonksiyonu varsa
- script bir robota bağlıysa
- oyun o anda script çalıştırmaya izin veriyorsa

Script şu durumlarda başlamaz veya durdurulur:
- restoran kapalıysa
- aktif robot yoksa
- sözdizimi (syntax) hatası varsa
- çalışma sırasında runtime hatası oluşursa
- aynı robota yeni bir script atanırsa

Özetle: script, oyuncu tarafından başlatılır ve interpreter tarafından kontrollü şekilde yürütülür.

## Robot – Script İlişkisi

Her script mantıksal olarak tek bir robota bağlıdır.

Bu nedenle:
- `get_robot()` fonksiyonu, scriptin bağlı olduğu robotu döndürür
- `get_nearest_*()` hesaplamaları genellikle bu robotun konumuna göre yapılır
- inventory işlemleri aynı robot üzerinden yürütülür

Aynı anda farklı robotlarda farklı scriptler çalışabilir. Ancak bir robot üzerinde aynı anda birden fazla aktif script çalıştırılması desteklenmez.

## Oyun Döngüsü ve Kod Çalışma Mantığı

Scriptler tek bir frame içinde kesintisiz şekilde çalışmaz.

Interpreter şu şekilde ilerler:
1. `main()` fonksiyonu çağrılır
2. Kod satır satır işlenir
3. Döngüler ve fonksiyon çağrıları kontrollü şekilde yürütülür
4. `wait()` çağrıları scriptin belirli bir süre duraklamasını sağlar
5. Güvenlik sınırları aşılırsa script durdurulabilir

Bu modelin amacı:
- oyunun donmasını engellemek
- scriptleri kontrollü ve güvenli şekilde çalıştırmaktır

## İlk Script Nasıl Yazılır?

Her scriptin başlangıç noktası `main()` fonksiyonudur.

```python
def main():
    print("Merhaba robot")
````

Temel adımlar:

1. `def main():` ile giriş fonksiyonunu tanımla
2. İçine çalıştırılacak komutları ekle
3. Scripti çalıştır
4. Çıktıyı ve robot davranışını gözlemle

İlk denemelerde `print()` kullanarak debug yapmak en güvenli yöntemdir.

## Kısa Örnek: Basit Robot Davranışı

```python
def main():
    tables = get_tables()

    if len(tables) > 0:
        robot = get_robot()
        table = tables[0]

        wait(0.5)
        robot.move(table)

        print("Robot masaya yönlendirildi")
```

Bu örnekte:

* masalar alınır
* en az bir masa varsa devam edilir
* aktif robot alınır
* listedeki ilk masa seçilir
* kısa bir bekleme eklenir
* robot masaya yönlendirilir
* debug mesajı yazdırılır

> Not: `wait()` kullanılmadan yazılan hızlı döngüler veya sürekli tekrar eden işlemler scriptin dengesiz çalışmasına neden olabilir.

<!-- enddoc -->