<!-- doc:id="built-in-ozellikler"; title="3 - Built-in Özellikler"; order=30 -->
# Built-in Özellikler

Bu bölüm, oyun dünyasına erişmek için kullanılan built-in API katmanını açıklar.

Bu API'nin temel amacı:
- güvenli erişim sağlamak
- robot scriptlerini engine ayrıntılarından korumak
- tutarlı method isimleri sunmak
<!-- enddoc -->

<!-- doc:id="dunya-erisim-fonksiyonlari"; parent="built-in-ozellikler"; title="3.1 - Get Fonksiyonları"; order=31 -->
# Get Fonksiyonları

Get fonksiyonları scriptin oyun nesnelerini bulmasını sağlar.

Bu fonksiyonlar genelde wrapper listeleri veya tekil wrapper nesneleri döndürür.

## get_robot()

Aktif scriptin bağlı olduğu robotu döndürür.

```python
robot = get_robot()
print(robot.position())
```

Bu fonksiyon genelde ilk kullanılan built-in'lerden biridir.

## get_tables()

Sahnedeki tüm masa wrapper'larını liste olarak döndürür.

```python
tables = get_tables()
print(len(tables))
```

## get_furnaces()

Sahnedeki tüm fırınları döndürür.

## get_fridges()

Sahnedeki tüm buzdolaplarını döndürür.

## get_trashcans()

Sahnedeki tüm çöp kutularını döndürür.

> Haritada çöp kutusu yoksa boş liste dönmesi normaldir.

## get_orders()

Aktif siparişleri liste olarak döndürür.

```python
orders = get_orders()
for order in orders:
    print(order.is_completed())
```
<!-- enddoc -->

<!-- doc:id="get-nearest-fonksiyonlari"; parent="built-in-ozellikler"; title="3.2 - Get_nearest Fonksiyonları"; order=32 -->
# Get_nearest Fonksiyonları

Desteklenen nearest built-in'ler:
- `get_nearest_robot()`
- `get_nearest_table()`
- `get_nearest_furnace()`
- `get_nearest_fridge()`
- `get_nearest_trashcan()`

Bu fonksiyonlar aktif robotun konumuna göre en yakın nesneyi bulur.

## en yakın nesne bulma mantığı

Genel akış:
1. aktif robot alınır
2. uygun tipteki nesneler toplanır
3. mesafe hesaplanır
4. en yakın nesne döndürülür

Eğer uygun nesne yoksa sonuç boş gelebilir. Bu yüzden mümkün olduğunda önce liste döndüren accessor'larla kontrol yapmak daha güvenlidir.

## performans ve kullanım

`get_nearest_*()` kullanımı pratiktir ama çok sık, çok dar döngü içinde tekrar etmek gereksiz maliyet yaratabilir.

Daha iyi kullanım:

```python
while True:
    tables = get_tables()
    if len(tables) > 0:
        print(get_nearest_table().id())
    wait(0.5)
```
<!-- enddoc -->

<!-- doc:id="robot-wrapper"; parent="built-in-ozellikler"; title="3.3 - Robot"; order=33 -->
# Robot

Robot wrapper'ı hareket, yakınlık ve envanter işlemleri için ana giriş noktandır.

## robot.position()

Robotun konumunu `[x, y]` benzeri bir liste olarak döndürür.

```python
pos = get_robot().position()
print(pos)
```

## robot.is_moving()

Robot hareket ediyorsa `True`, etmiyorsa `False` döner.

## robot.is_near(...)

Hedef nesne veya müşteri robota yeterince yakınsa `True` döner.

```python
robot = get_robot()
tables = get_tables()

if len(tables) > 0:
    table = tables[0]
    if robot.is_near(table):
        print("Robot masaya yakın")
```

## robot.move(...)

Robotu bir konuma yönlendirir.

Konum kaynağı:
- koordinatlar: `x, y`
- koordinat içeren liste veya tuple: `[x, y]` veya `(x, y)`
- başka nesnenin konumu: `table.position()`
- kullanım kolaylığı için direkt nesne: `table`

```python
robot.move([4, 2])
robot.move(table.position())
```

> Not: Hareket işlemi bitene kadar kodun sonraki satırları çalışmaz. Bu yüzden hareket sırasında `wait()` kullanmaya gerek yoktur.

## robot.inventory()

Robotun envanter wrapper'ını döndürür.
<!-- enddoc -->

<!-- doc:id="robot-inventory-wrapper"; parent="built-in-ozellikler"; title="3.4 - Robot Inventory"; order=34 -->
# Robot Inventory

Robot inventory wrapper'ı, robotun taşıdığı item'ları kontrol etmek için kullanılır.

## has_item()

Belirli bir item envanterde var mı kontrol eder.

## has_empty_slot()

Boş slot olup olmadığını döndürür.

## is_empty()

Envanter tamamen boşsa `True` döndürür.

## slot_count()

Toplam slot sayısını verir.

## quantity(item)

Belirli item'ın toplam adedini döndürür.
<!-- enddoc -->

<!-- doc:id="table-wrapper"; parent="built-in-ozellikler"; title="3.5 - Table"; order=35 -->
# Table

Masa wrapper'ı müşteri ve temizlik akışında kullanılır.

## table.position()

Masanın konumunu döndürür.

## table.customers()

Masadaki müşterileri liste olarak döndürür.

## table.is_dirty()

Masa kirliyse `True` döndürür.

## table.clean()

Masa kirliyse temizlemeyi dener.

Temel kullanım:

```python
if table.is_dirty():
    table.clean()
```
<!-- enddoc -->

<!-- doc:id="customer-wrapper"; parent="built-in-ozellikler"; title="3.6 - Customer"; order=36 -->
# Customer

Customer wrapper'ı sipariş ve durum kontrolü için kullanılır.

## customer.table()

Müşterinin bağlı olduğu masayı döndürür.

## customer.state()

Müşteri durumunu string olarak döndürür.

Genelde `CustomerState.*` ile karşılaştırılır:

```python
if customer.state() == CustomerState.Waiting:
    print("Müşteri bekliyor")
```

### müşteri durumları (state açıklamaları)

- `Seating`: masaya yerleşiyor
- `Thinking`: sipariş düşünme aşaması
- `Ordering`: sipariş almaya hazır
- `Waiting`: siparişini bekliyor
- `Eating`: yemeğini yiyiyor
- `Leaving`: ayrılıyor

## customer.has_order()

Müşteride aktif veya alınabilir sipariş olup olmadığını söyler.

## customer.take_order()

Müşteri uygun durumdaysa siparişi alır ve `Order` döndürür.

Genelde önce state kontrolü yapılır:

```python
if customer.state() == CustomerState.Ordering:
    order = customer.take_order()
```

## customer.order()

Müşterinin mevcut siparişini döndürür. Sipariş yoksa sonuç gelmeyebilir; bu yüzden önce `customer.has_order()` kontrolü yapmak iyi pratiktir.
<!-- enddoc -->

<!-- doc:id="order-wrapper"; parent="built-in-ozellikler"; title="3.7 - Order (Sipariş Sistemi)"; order=37 -->
# Order (Sipariş Sistemi)

Order wrapper'ı siparişin içeriğini ve ilerleme durumunu temsil eder.

## order.items()

Siparişteki item'ları liste olarak döndürür.

## order.customer()

Siparişin hangi müşteriye ait olduğunu döndürür.

## order.is_completed()

Sipariş tamamlandıysa `True` döndürür.

## sipariş akışı

Tipik sipariş akışı:
1. müşteri `Ordering` durumuna gelir
2. robot siparişi alır
3. gerekli malzemeler hazırlanır
4. pişirme gerekiyorsa furnace kullanılır
5. uygun item'lar müşteriye ulaştırılır
6. sipariş tamamlanır
<!-- enddoc -->

<!-- doc:id="furnace-wrapper"; parent="built-in-ozellikler"; title="3.8 - Furnace (Pişirme Sistemi)"; order=38 -->
# Furnace (Pişirme Sistemi)

Furnace wrapper'ı pişirme akışını temsil eder.

## furnace.position()

Fırının konumunu döndürür.

## furnace.state()

Fırının durumunu döndürür.

## furnace.is_ready()

Pişirme tamamlandıysa `True` döndürür.

## furnace.is_busy()

Fırın meşgulse `True` döndürür.

## furnace.is_dirty()

Fırın kirli veya yanmış durumdaysa `True` döndürür.

## furnace.place(...)

Item veya item listesi ile yerleştirme yapar.

```python
furnace.place(item)
furnace.place(item, quantity)
```

## furnace.cook()

Pişirme sürecini başlatır.

## furnace.take()

Hazır çıktı varsa almaya çalışır.

## furnace.clean()

Kirli veya yanmış fırını temizler.
<!-- enddoc -->

<!-- doc:id="fridge-wrapper"; parent="built-in-ozellikler"; title="3.9 - Fridge (Malzeme Sistemi)"; order=39 -->
# Fridge (Malzeme Sistemi)

Fridge wrapper'ı malzeme alma ve bırakma işlemleri için kullanılır.

## fridge.position()

Buzdolabının konumunu döndürür.

## fridge.has_item(...)

Belirli item'ın yeterli adette bulunup bulunmadığını kontrol eder.

```python
if fridge.has_item(item, 2):
    print("Yeterli malzeme var")
```

## fridge.quantity(...)

Belirli item'ın adedini döndürür.

## fridge.take(...)

Buzdolabından item almaya çalışır.

## fridge.place(...)

Robot inventory'den buzdolabına item yerleştirmeye çalışır.
<!-- enddoc -->

<!-- doc:id="trashcan-wrapper"; parent="built-in-ozellikler"; title="3.10 - Trashcan"; order=40 -->
# Trashcan

Trashcan wrapper'ı istenmeyen item'ları atmak için kullanılır.

## trashcan.position()

Çöp kutusunun konumunu döndürür.

## trashcan.throw(...)

Tek item veya item listesi atmak için tasarlanmıştır.

> Eğer sahnede çöp kutusu yoksa önce `get_trashcans()` veya `get_nearest_trashcan()` sonucunu kontrol et.
<!-- enddoc -->

<!-- doc:id="enum-yapilari"; parent="built-in-ozellikler"; title="3.11 - Enum Yapıları"; order=41 -->
# Enum Yapıları

Enum scope'ları okunabilir state kontrolü sağlar.

## CustomerState

Kullanım:

```python
if customer.state() == CustomerState.Waiting:
    print("Servise hazır")
```

## FurnaceState

Kullanım:

```python
if furnace.state() == FurnaceState.Ready:
    print("Ürün alınabilir")
```
<!-- enddoc -->

<!-- doc:id="find-fonksiyonlari"; parent="built-in-ozellikler"; title="3.12 - Find fonksiyonları"; order=42 -->
# Find fonksiyonları

Find fonksiyonları, belirli kriterlere göre nesne aramak için kullanılır.

## find_item("item_name")

Pişirilmek istenen eşyayı bulmak için kullanılır.
```python
def main():
    item = find_item("meatball")
    if item != None:
        print("Item bulundu:", item)
    else:
        print("Item bulunamadı")
```

<!-- enddoc -->

<!-- doc:id="genel-kullanim-kurallari"; parent="built-in-ozellikler"; title="3.13 - Genel Kullanım Kuralları"; order=43 -->
# Genel Kullanım Kuralları

Built-in API güçlüdür ama doğru sırada kullanılmalıdır.

## Hangi fonksiyon ne zaman çağrılmalı

Örnek akışlar:
- masaya gitmeden önce `get_nearest_table()`
- sipariş almadan önce `customer.state()`
- pişirme başlamadan önce `furnace.place(...)`
- pişmiş ürünü almadan önce `furnace.is_ready()`
- malzeme almadan önce `fridge.has_item(...)`

## None / boş değer kontrolü

Her sorgu sonuç vermeyebilir.

Güvenli kullanım:

```python
tables = get_tables()
if len(tables) > 0:
    print(tables[0].position())
```

## is_* fonksiyonlarının önemi

Kontrol method'ları scripti güvenli hale getirir:
- `robot.is_moving()`
- `robot.is_near(...)`
- `table.is_dirty()`
- `customer.has_order()`
- `furnace.is_ready()`
- `furnace.is_busy()`
- `furnace.is_dirty()`

Bir eylemden önce durum kontrol etmek, hata riskini ciddi biçimde azaltır.

## Yanlış kullanım örnekleri

Yanlış:

```python
customer = get_tables()[0].customers()[0]
order = customer.take_order()
```

Neden yanlış olabilir?
- masa listesi boş olabilir
- müşteri olmayabilir
- robot masaya uzak olabilir
- müşteri sipariş durumunda olmayabilir

Daha güvenli:

```python
robot = get_robot()
tables = get_tables()
if len(tables) > 0:
    customers = tables[0].customers()
    if len(customers) > 0:
        customer = customers[0]
        if customer.state() == CustomerState.Ordering:
            robot.move(tables[0])
            order = customer.take_order()
```
<!-- enddoc -->
