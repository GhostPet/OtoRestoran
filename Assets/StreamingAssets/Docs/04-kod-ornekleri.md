<!-- doc:id="kod-ornekleri"; title="4 - Kod Örnekleri"; order=50 -->
# Kod Örnekleri

Bu bölümde, temel ve orta seviye kullanım için örnek scriptler bulunur.

Örnekler birebir tek çözüm değildir. Amaç, düşünme biçimini göstermektir.
<!-- enddoc -->


<!-- doc:id="temel-oyun-senaryolari"; parent="kod-ornekleri"; title="4.1 - Başlangıç Seviyesi"; order=51 -->
# Başlangıç Seviyesi

İlk örneklerde `print()` ve `wait()` kullanımına odaklan.

## print ile debug örneği

```python
def main():
    robot = get_robot()
    print(robot.id(), "ID'sine sahip bir robot bulundu, konumu:", robot.position())
```

## Basit hareket (robot.move)

```python
def main():
    robot = get_robot()
    robot.move(3, 2)
```

## wait kullanımı

```python
def main():
    while True:
        print("Robot durumu:", get_robot().is_moving())
        wait(1.0)
```
<!-- enddoc -->

<!-- doc:id="temel-oyun-senaryolari"; parent="kod-ornekleri"; title="4.2 - Temel Oyun Senaryoları"; order=52 -->
# Temel Oyun Senaryoları

Bu örnekler günlük restoran akışına daha yakındır.

## Tüm Masaları Dolaşma

```python
def main():
    robot = get_robot()
    tables = get_tables()

    for table in tables:
        robot.move(table.position())
        print("Masa ID:", table.id(), "Kirli mi:", table.is_dirty())
```

## Masalardaki Müşteri Sayılarını Kontrol Etme

```python
def main():
    tables = get_tables()
    robot = get_robot()
    for table in tables:
        robot.move(table.position())
        customers = table.customers()
        print("Masa ID:", table.id(), "Müşteri sayısı:", len(customers))
```

## Sipariş alma

```python
def main():
    tables = get_tables()
    robot = get_robot()
    for table in tables:
        robot.move(table.position())
        customers = table.customers()
        for customer in customers:
            if customer.state() == CustomerState.Ordering:
                customer.take_order()
                print("Sipariş alındı")
```

> Not: Müşteri `Ordering` durumunda değilken `take_order()` çağırmak hata üretmez, ancak sipariş alınmaz. Bu yüzden state kontrolü önemlidir.
<!-- enddoc -->

<!-- doc:id="orta-seviye"; parent="kod-ornekleri"; title="4.3 - Orta Seviye"; order=53 -->
# Orta Seviye

Bu bölüm, birden fazla sistem kullanan örnekleri içerir.

## Furnace kullanımı (place + cook + take)

```python
def main():
    robot = get_robot()
    furnace = get_nearest_furnace()
    meatball = find_item("meatball_raw")

    if meatball != None:
        robot.move(furnace)
        furnace.place(meatball, 1) // Köfteyi furnace'e koy
        furnace.cook() // Pişirme işlemini başlat

        while furnace.is_busy(): // Pişirme bitene kadar bekle
            wait(0.2)

        if furnace.is_ready():
            cooked = furnace.take() // Pişmiş ürünü al
            print("Alınan ürünler:", cooked)
```

> Bu örnek, sipariş item'larının furnace ile uyumlu olduğunu varsayar.

## Fridge’den malzeme alma

```python
def main():
    fridge = get_fridges()[0] // İlk fridge'i al
    get_robot().move(fridge) // Fridge'e hareket et
    item = find_item("meatball_raw") // Pişmemiş köfte item'ını bul
    if item != None:
        fridge.take(item, 1) // Fridge'den 1 adet pişmemiş köfte al
```
<!-- enddoc -->