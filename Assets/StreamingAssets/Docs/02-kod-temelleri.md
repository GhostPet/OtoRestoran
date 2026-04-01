<!-- doc:id="kod-temelleri"; title="2 - Kod Temelleri"; order=20 -->
# Kod Temelleri

Bu bölüm, DSL içinde en sık kullanacağın temel programlama yapılarını açıklar.

Dil Python’a benzer, ancak tamamen oyun odaklıdır. Amaç:
- güvenli çalışmak
- okunabilir kod yazmak
- kontrol edilebilir robot davranışı üretmektir
<!-- enddoc -->

<!-- doc:id="degiskenler-ve-veri-tipleri"; parent="kod-temelleri"; title="2.1 - Değişkenler ve Veri Tipleri"; order=21 -->
# Değişkenler ve Veri Tipleri

Değişkenler, değerleri saklamak için kullanılır.

```python
def main():
    x = 5
    name = "Robot"
    moving = get_robot().is_moving()
````

Sık kullanılan veri tipleri:

* None → `None`
* sayı → `5`, `3.5`
* metin → `"Merhaba"`
* boolean → `True / False`
* liste → `[1, 2, 3]`
* oyun nesneleri → `robot`, `table`, `order` gibi

Tipler otomatik belirlenir; ayrıca tanımlamaya gerek yoktur.

<!-- enddoc -->

<!-- doc:id="liste-kullanimi"; parent="kod-temelleri"; title="2.2 - Liste Kullanımı"; order=22 -->

# Liste Kullanımı

Listeler birden fazla değeri sıralı şekilde tutar.

```python
def main():
    numbers = [10, 20, 30]
    print(numbers[0])
```

Temel kurallar:

* indeksler `0`’dan başlar
* `numbers[0]` ilk elemandır
* geçersiz indeks hata üretir

Sık kullanılan liste kaynakları:

* `get_tables()`
* `get_orders()`
* `table.customers()`
* `order.items()`

<!-- enddoc -->

<!-- doc:id="operatorler"; parent="kod-temelleri"; title="2.3 - Operatörler"; order=23 -->

# Operatörler

Aritmetik:

* `+`, `-`, `*`, `/`

Karşılaştırma:

* `==`, `!=`, `>`, `<`, `>=`, `<=`

```python
def main():
    a = 5
    b = 3

    print(a + b)
    print(a > b)
```

<!-- enddoc -->

<!-- doc:id="kosul-yapilari"; parent="kod-temelleri"; title="2.4 - Koşul Yapıları"; order=24 -->

# Koşul Yapıları

Koşullar, karar vermek için kullanılır.

```python
def main():
    tables = get_tables()

    if len(tables) == 0:
        print("Masa yok")
    else:
        print("Masa var")
```

## if / elif / else

```python
def main():
    tables = get_tables()

    if len(tables) == 0:
        print("Masa yok")
    elif len(tables[0].customers()) == 0:
        print("Müşteri yok")
    else:
        customer = tables[0].customers()[0]

        if customer.state() == CustomerState.Waiting:
            print("Müşteri bekliyor")
```

İpucu:

* Koşulları küçük ve net yaz
* Önce “var mı?” kontrolü yap, sonra detayına gir

<!-- enddoc -->

<!-- doc:id="donguler"; parent="kod-temelleri"; title="2.5 - Döngüler"; order=25 -->

# Döngüler

Döngüler tekrar eden işlemler için kullanılır.

Ancak dikkat:

* kontrolsüz döngüler performans sorununa yol açar
* çoğu durumda `wait()` gerekir

## while

```python
def main():
    while True:
        print("Çalışıyor")
        wait(1.0)
```

Yanlış kullanım:

```python
while True:
    print("çok hızlı tekrar eder")
```

## for in

```python
def main():
    tables = get_tables()

    for table in tables:
        print(table.position())
```

## range()

```python
for i in range(3):
    print(i)
```

`range(3)` → `0, 1, 2`

<!-- enddoc -->

<!-- doc:id="fonksiyonlar"; parent="kod-temelleri"; title="2.6 - Fonksiyonlar"; order=26 -->

# Fonksiyonlar

Fonksiyonlar kodu bölmek ve tekrar kullanmak için yazılır.

## Tanımlama

```python
def greet():
    print("Merhaba")
```

## Kullanım

```python
def main():
    greet()
```

## Parametre

```python
def go_to_table(table):
    robot = get_robot()
    robot.move(table.position())
```

## return

`return` ile değer döndürülebilir ancak:

* DSL’de kullanım desteği sınırlı olabilir
* karmaşık veri akışları yerine prosedürel kullanım önerilir

Öneri:

* fonksiyonları davranış blokları olarak kullan

<!-- enddoc -->

<!-- doc:id="nesne-kullanimi"; parent="kod-temelleri"; title="2.7 - Nesne Kullanımı"; order=27 -->

# Nesne Kullanımı

DSL, oyun dünyasını **wrapper nesneler** üzerinden sunar.

Örnek:

* robot
* table
* customer
* order

## Method çağrısı

```python
robot = get_robot()
robot.is_moving()
```

## Enum kullanımı

```python
CustomerState.Waiting
FurnaceState.Ready
```

Önemli:

* ana etkileşim şekli **method çağrısıdır**
* property erişimi sınırlıdır

Örnek:

* doğru → `customer.state()`
* doğru → `table.is_dirty()`

<!-- enddoc -->

<!-- doc:id="temel-builtins"; parent="kod-temelleri"; title="2.8 - Temel Built-in Fonksiyonlar"; order=28 -->

# Temel Built-in Fonksiyonlar

En sık kullanılanlar:

* `print()`
* `len()`
* `range()`
* `wait()`

## print()

Debug için kullanılır.

```python
print("Başladı")
```

## len()

```python
len(get_tables())
```

## range()

```python
for i in range(5):
    print(i)
```

## wait()

Scripti kısa süre durdurur.

```python
wait(0.5)
```

Kritik kullanım:

* döngülerde
* hareket beklerken
* polling yaparken

**wait kullanmazsan script çok hızlı çalışır ve sorun çıkarabilir**

<!-- enddoc -->

<!-- doc:id="kod-akisi-ve-zamanlama"; parent="kod-temelleri"; title="2.9 - Kod Akışı ve Zamanlama"; order=29 -->

# Kod Akışı ve Zamanlama

Scriptler anlık değil, zaman içinde çalışır.

İyi bir script şu döngüyü takip eder:

1. kontrol et
2. işlem yap
3. bekle
4. tekrar kontrol et

## Örnek

```python
while True:
    if get_robot().is_moving():
        wait(0.1)
    else:
        print("Robot boşta")
        wait(1.0)
```

## Neden önemli?

* oyunun kilitlenmesini engeller
* davranışları daha stabil yapar
* CPU kullanımını azaltır

## Kritik kural

Her sürekli döngüde `wait()` kullan.

<!-- enddoc -->