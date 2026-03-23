# OtoRestoran - Legacy Çözümleme ve Oyun Tasarım Belgesi

## 1. Belgenin Amacı
Bu belge, mevcut Unity projesindeki kod ve sistemlerden yola çıkarak oyunun hedeflediği deneyimi, ana oynanış döngüsünü, aktif sistemleri, eksik/taslak kalan modülleri ve yeni projeye taşınırken korunması gereken tasarım niyetini açıklamak için hazırlanmıştır.

Bu doküman özellikle şu kullanım senaryosu için yazıldı:
- mevcut projeyi baştan kurmak,
- mevcut kodu `Legacy` klasörüne taşımak,
- sistemleri tek tek inceleyip temiz bir mimari ile yeniden inşa etmek,
- hangi parçanın gerçekten çalıştığını, hangisinin sadece fikir aşamasında kaldığını karıştırmamak.

---

## 2. Oyunun Kısa Tanımı
`OtoRestoran`, oyuncunun bir restoranı doğrudan karakter kontrolüyle değil, programlanabilir robotlar üzerinden yönettiği bir otomasyon/restoran yönetim oyunudur.

Oyuncu restoranı kurar, masa ve ekipman yerleşimini düzenler, shop üzerinden yeni öğeler satın alır, robotlara kod yazar ve servis akışını optimize ederek daha verimli bir restoran işletmeye çalışır.

Oyunun ayırt edici tarafı klasik restoran yönetimini basit bir programlama/scripting sistemi ile birleştirmesidir.

---

## 3. Hedef Oyuncu Fantezisi
Oyunun vermek istediği temel his:
- "Ben restoranda çalışan biri değil, sistemi kuran kişiyim."
- "Robotlara mantık yazarak restoranı otomatikleştiriyorum."
- "Yerleşim, kapasite, süreç ve kod optimizasyonu ile daha iyi bir akış kuruyorum."
- "Her gün sistemi biraz daha akıllı hale getiriyorum."

Bu nedenle oyun sadece restoran simülasyonu değil, aynı zamanda:
- süreç tasarlama,
- otomasyon kurma,
- kodla davranış tanımlama,
- operasyon iyileştirme
oyunu olarak düşünülmüş.

---

## 4. Çekirdek Oyun Döngüsü
Koddan çıkan ana döngü aşağıdaki gibi görünüyor:

1. `Preparation` fazı
- oyuncu restoran yerleşimini düzenler,
- shop'tan ürün alır,
- robotlara kod/program atar,
- servis öncesi hazırlık yapar.

2. `Service` fazı
- müşteriler gelir,
- boş sandalyelere oturur,
- sipariş vermeye hazırlanır,
- robotlar yazılan program doğrultusunda hareket eder,
- sipariş alır, servis yapar, masa ile etkileşir.

3. `DayEnd` fazı
- gün sonu değerlendirmesi yapılması hedeflenmiştir,
- skor, gelir, memnuniyet, kayıt gibi sistemlerin burada devreye girmesi amaçlanmıştır,
- ancak mevcut kodda bu fazın içeriği henüz büyük ölçüde tamamlanmamıştır.

Bu döngü `PhaseStateMachine` içinde açıkça tanımlanmış durumda:
- `Preparation`
- `Service`
- `DayEnd`

---

## 5. Oyunun Ana Tasarım Sütunları
Kod tabanına göre oyunun ana tasarım sütunları şunlardır:

### 5.1 Robot programlama
Oyunun merkezinde robotların komut dosyalarıyla yönlendirilmesi var.
Oyuncu robotları elle sürmek yerine onlara akış tanımlar.

### 5.2 Restoran düzeni ve kapasite yönetimi
Masa, sandalye, fırın ve diğer objelerin grid üzerinde yerleştirilmesi restoranın kapasitesini ve işleyişini belirliyor.

### 5.3 Müşteri akışı
Müşteriler gelir, oturur, düşünür, sipariş verir, bekler, yer, ayrılır.
Bu durumlar oyunun servis ritmini oluşturur.

### 5.4 Ekonomi ve ilerleme
Para, skor ve restoran puanı gibi sistemler ile yeni ürünler açılması ve işletmenin gelişmesi hedeflenmiş.

### 5.5 Kod + yönetim hibriti
Bu proje doğrudan bir "coding game" ile "management sim" arasında duruyor.
Asıl farkı, işletme kararlarının robot davranış koduna bağlanması.

---

## 6. Mevcut Sistem Envanteri
Aşağıda mevcut projede tespit edilen sistemler yer alıyor.
Her sistem için şu üç durumdan biri kullanıldı:
- `Aktif`: Kodda temel oynanış akışı gerçekten mevcut.
- `Kısmi`: Sistem var ama eksik, bağlantıları tam değil veya sadece temel iskelet çalışıyor.
- `Taslak`: Fikir seviyesi mevcut, sınıf var ama içerik büyük ölçüde boş.

---

## 7. Faz ve Gün Akışı Sistemi
### Durum
- `PhaseStateMachine`: `Aktif`
- `DayManager`: `Taslak`
- `PrepPhase`: `Taslak`
- `ServPhase`: `Taslak`

### Amaçlanan davranış
Oyun gün bazlı ilerliyor. Her gün belirli fazlara ayrılıyor:
- hazırlık,
- servis,
- gün sonu.

`PhaseStateMachine` süre bazlı olarak faz değiştiriyor.
Hazırlık bitince servis başlıyor, servis bitince gün sonu fazına geçiliyor.

### Çalışma şekli
- Faz süresi sayaçla azalıyor.
- Aktif faza göre ilgili component GameObject'i açılıp kapanıyor.
- Faz değişimi ve faz bitişi için event yayınlanıyor.

### Yeni projede korunması gereken niyet
- Gün akışı net bir state machine olmalı.
- Faz mantığı UI, customer spawn, robot execution ve scoring ile entegre olmalı.
- `DayManager`, sadece boş bir sınıf değil, oyunun uygulama katmanı orkestratörü olmalı.

---

## 8. Restoran Yerleşim ve Grid Sistemi
### Durum
- `GridManager`: `Aktif`
- `PlaceableObject`: projede mevcut, detayları bu incelemede doğrudan okunmadı
- `PlacementController`: `Kısmi`
- `PlacementUI`: `Kısmi`

### Amaçlanan davranış
Restorandaki objeler grid üzerinde konumlandırılıyor.
Masa, sandalye ve mutfak ekipmanları hücre işgal ediyor.
Bazı objeler dönüş destekliyor.

### Çalışan davranışlar
`GridManager` üzerinden görülenler:
- grid genişlik/yükseklik ve hücre boyutu tanımlı,
- world pozisyonu ile hücre dönüşümü yapılabiliyor,
- hücre doluluk takibi tutuluyor,
- objelerin yerleştirilebilirliği kontrol ediliyor,
- yerleştirilen obje sayısı tip bazlı sayılıyor,
- masa yerleştirilince otomatik sandalye yerleştirme fikri uygulanmış,
- sandalyenin masaya doğru bakması için rotasyon kontrolü var,
- sahiplik limiti olan placeable objeler shop/build inventory ile ilişkilendiriliyor.

### Tasarım anlamı
Bu sistem restoranın kapasite tasarımını belirliyor.
Masa sayısı, sandalye sayısı ve ekipman yerleşimi doğrudan müşteri akışı ve robot rotaları üzerinde etkili olacak şekilde düşünülmüş.

### Yeni projede öneri
Bu sistemi üç parçaya ayırmak faydalı olur:
- saf grid/domain modeli,
- placement application service,
- Unity scene adapter/visual layer.

---

## 9. Masa ve Sandalye Sistemi
### Durum
- `TableBehavior`: `Aktif`
- `ChairBehavior`: `Aktif`

### Amaçlanan davranış
Müşteriler masalara değil sandalyelere oturuyor.
Sandalyeler uygun masalara bağlanıyor.
Masa, bağlı sandalyeler üzerindeki müşterileri takip ediyor.

### Çalışan davranışlar
#### `ChairBehavior`
- tüm sandalyeleri statik listede tutuyor,
- boş sandalye arayabiliyor,
- yerleştirildiğinde en uygun masaya bağlanıyor,
- mevcut müşteriyi saklıyor,
- müşteri atama/temizleme yapıyor.

#### `TableBehavior`
- servis noktaları tanımlayabiliyor,
- hangi hücrelerin sandalye hücresi olduğunu biliyor,
- bağlı müşterileri bağlı sandalyeler üzerinden topluyor,
- kirli/temiz durumu tutuyor,
- robot için en uygun servis noktasını seçebiliyor.

### Tasarım anlamı
Masalar sadece dekoratif değil; müşteri kümelenmesi, servis hedefi ve temizlik hedefi olarak kullanılıyor.

### Eksik taraflar
- masa kirliliği var ama bu durumun oynanış sonuçları zayıf,
- masanın tam servis/turnover ekonomisine etkisi henüz bağlanmamış.

---

## 10. Müşteri ve Sipariş Akışı
### Durum
- `Customer`: `Aktif`
- `CustomerSpawner`: `Aktif`
- `Order`, `OrderItem`, `OrderQueue`: `Kısmi`
- `ActiveOrders`: kullanılıyor, ancak bu incelemede doğrudan okunmadı

### Amaçlanan davranış
Müşteriler restorana gelir, boş sandalyeye oturur ve bir durum makinesi içinde ilerler.

### `Customer` state akışı
Koddan çıkan akış:
- `Seating`
- `Thinking`
- `Ordering`
- `Waiting`
- `Eating`
- `Leaving`

### Akış detayı
1. Müşteri spawn olur.
2. Boş sandalye bulunur.
3. Müşteri o sandalyeye yürür.
4. Oturunca düşünme süresi başlar.
5. Düşünme bitince sipariş hazır olur.
6. Robot siparişi alır.
7. Müşteri bekler.
8. Sipariş servis edilince yemeye başlar.
9. Yeme bitince masa kirlenir.
10. Müşteri ayrılır ve obje yok edilir.

### `CustomerSpawner`
- belirli aralıklarla müşteri üretir,
- boş sandalye yoksa spawn atlanır,
- UI butonu ile aç/kapat yapılabilir.

### Tasarım anlamı
Bu oyunda müşteri akışı tamamen servis otomasyonu için hedef nesneleri üretir.
Müşteri bir ekonomik veri kaynağı olmanın yanında robot programlarının doğruluğunu test eden canlı görev nesnesidir.

### Eksik taraflar
- müşteri sabrı, memnuniyet, terk etme riski gibi sistemler henüz görünmüyor,
- farklı sipariş tipleri ve masa başına çoklu sipariş akışı sınırlı,
- servis başarısının gelir/skor/rating etkisi tam bağlanmamış.

---

## 11. Robot Sistemi
### Durum
- `RobotController`: `Aktif`
- `RobotExecutor`: `Aktif`
- `RobotCommandQueue`: `Aktif`
- `Robot`, `RobotStats`, `RobotUpgrades`: `Taslak`
- `RobotInventory`: projede mevcut, bu incelemede ayrıntılı okunmadı
- `RobotTasks/*`: `Kısmi` veya fikir aşamasında

### Amaçlanan davranış
Robotlar restoranın gerçek çalışanlarıdır.
Oyuncu robotlara komut verir veya program yazar; robotlar bunları sırayla uygular.

### `RobotController`
- `NavMeshAgent` kullanıyor,
- dünya pozisyonu bilgisi veriyor,
- hareket ediyor mu bilgisini veriyor,
- hedefe yürüme veya anında warp etme desteği var.

### `RobotExecutor`
- komut kuyruğunu işler,
- aktif komutu frame frame tick eder,
- komut tamamlandığında sıradakine geçer,
- çalışma anında aktif robotu execution context içine koyar.

### `RobotCommandQueue`
- basit FIFO kuyruk.

### Tasarım anlamı
Robot sistemi aslında oyunun ana ajan sistemi.
İleride aynı altyapı ile farklı görev uzmanlıkları olan robot tipleri üretmek mümkün.

### Yeni projede öneri
Robot davranışını üç katmana bölmek uygun olur:
- robot domain state,
- robot action/task execution,
- movement adapter.

---

## 12. Kodlama / DSL / Robot Programlama Sistemi
### Durum
- `GameCodeRunner`: `Aktif`
- `AstInterpreter`: `Aktif`
- `Lexer`, `Parser`, AST node'ları: `Aktif`
- `BuiltinCommandRegistry`: `Aktif`
- `BuiltinFunctions`: `Aktif`
- `Validator`: `Kısmi`
- `CodeEditorUI`, `AutoCompleteProvider`: `Taslak`
- syntax highlighting/editor tool sınıfları: `Kısmi`

### Oyunun en kritik fark yaratan sistemi
Bu proje sıradan restoran oyunundan burada ayrılıyor.
Oyuncu robotlara muhtemelen özel bir script dili ile davranış tanımlıyor.

### Koddan çıkan yetenekler
#### Dil seviyesi
Sistemde şu yapılar var:
- fonksiyon tanımı,
- fonksiyon çağrısı,
- değişken atama,
- `if`, `while`, `for`,
- liste literal'leri,
- sayı/string identifier expression'ları,
- indeks erişimi,
- member access,
- ikili operatörler.

#### Built-in komutlar
Kayıtlı komutlar:
- `move_to`
- `serve`
- `clean`
- `wait`
- `pickup`
- `print`
- `drop`

#### Built-in fonksiyonlar
Kayıtlı fonksiyonlar:
- `range`
- `len`
- `type`
- `int`
- `float`
- `str`
- `get_tables`
- `get_furnaces`
- `get_orders`

### Çalışma şekli
1. Oyuncu bir editörde kod yazar.
2. `GameCodeRunner` kodu alır.
3. `Lexer` token üretir.
4. `Parser` AST üretir.
5. `AstInterpreter` fonksiyonları yürütür.
6. Komutlar doğrudan çalıştırılabilir veya bir `RobotExecutor` kuyruğuna enqueued edilebilir.
7. Böylece script ile gerçek robot hareketi bağlanır.

### Tasarım anlamı
Bu sistem oyuncuya şu gücü vermek istiyor:
- masaları tara,
- siparişleri al,
- uygun masaya git,
- fırınlarla etkileş,
- koşullu mantık kur,
- döngü yazarak tekrar eden işleri otomatikleştir.

Yani oyunun asıl "ustalık eğrisi" restoran yönetiminden çok otomasyon mantığı kurmak olabilir.

### Mevcut sınırlamalar
- editör UX tarafı tamamlanmamış,
- komutların bir kısmı gerçek dünya etkisi yerine şimdilik log atıyor,
- güvenlik, zaman dilimleme ve sandbox altyapısı var ama ürünleşmemiş,
- kullanıcıya sunulacak gerçek script deneyimi henüz yarı tamamlanmış durumda.

---

## 13. Komut Bazlı Oynanış Davranışları
### `move_to`
- robotu belirli hedefe gönderiyor,
- hedef `Vector3`, `Transform`, `GameObject`, `TableBehavior` veya koordinat listesi olabilir,
- robot hedefe varana kadar komut bitmiyor.

### `serve`
- bir `Order` nesnesi bekliyor,
- robotun ilgili masaya yakın olması gerekiyor,
- müşteri durumunu `served` olarak ilerletiyor.

### `clean`
- şu anda davranış olarak çok yüzeysel,
- temizlik fikrinin oyunda olduğunu gösteriyor ama uygulama eksik.

### `pickup` / `drop`
- taşıma/elde tutma akışının hedeflendiğini gösteriyor,
- şu an daha çok iskelet düzeyinde.

### Sonuç
Komut sistemi final oyunda şu davranışlara dönüşebilir:
- sipariş al,
- pişirme istasyonuna git,
- item al,
- müşteriye servis yap,
- kirli masayı temizle,
- stok yönetimi yap.

---

## 14. Mutfak ve Pişirme Sistemi
### Durum
- `OvenBehavior`: `Aktif`
- `RecipeExecutor`: `Taslak`
- `CookingSlot`: projede mevcut, ayrıntı okunmadı
- `RecipeSO`: veri tarafı mevcut

### Amaçlanan davranış
Yemek hazırlama sürecinin istasyon bazlı olması planlanmış.
Fırın/ocak gibi objeler belirli bir state machine ile çalışıyor.

### `OvenBehavior` state akışı
- `Empty`
- `Placed`
- `Cooking`
- `Ready`
- `BurnedDirty`

### Davranış
- içerik yerleştiriliyor,
- pişirme başlatılıyor,
- süre dolunca hazır oluyor,
- uzun süre alınmazsa yanıyor,
- yanarsa temizlenmesi gerekiyor.

### Tasarım anlamı
Bu sistem restoran otomasyonuna zaman baskısı ekliyor.
Robot yalnızca müşteriye gitmekle kalmıyor; pişirme sürecini de doğru zamanda yönetmek zorunda.

### Eksik taraflar
- tarif/malzeme tüketimi tam bağlı değil,
- inventory ile mutfak çıktısı arasındaki bağ görünür değil,
- robot komutları ile oven etkileşimi henüz zayıf.

---

## 15. Ekonomi Sistemi
### Durum
- `EconomyManager`: `Aktif`

### Amaçlanan davranış
Oyuncunun para akışını yönetmek.
Shop satın alımları ve satışları bunun üzerinden geçiyor.

### Çalışan davranışlar
- başlangıç bakiyesi ile initialize,
- harcama kontrolü,
- gelir ekleme,
- manuel bakiye ayarlama,
- transaction result event'leri yayınlama.

### Tasarım anlamı
Ekonomi sistemi shop ve işletme büyümesini destekleyen temel kaynak sistemi.
Şu aşamada güçlü bir altyapı sınıfı gibi duruyor.

### Eksik taraflar
- müşteri servisinden doğrudan gelir üretimi görünmüyor,
- gün sonu finans raporu bağlı değil.

---

## 16. İlerleme Skoru ve Restoran Puanı
### Durum
- `GameScoreManager`: `Aktif`
- `RestaurantRatingManager`: `Aktif`

### `GameScoreManager`
Bu sistem doğrudan para değil, ilerleme puanı tutuyor.
Kod yorumlarından çıkan niyet:
- yeni sistem açma,
- shop ürün kilidi,
- içerik progression.

### `RestaurantRatingManager`
Bu sistem 1-10 arası restoran puanı tutuyor.
Kod yorumlarından çıkan muhtemel kullanım:
- müşteri sıklığı,
- fiyat toleransı,
- talep çarpanı,
- memnuniyet tabanlı denge.

### Tasarım anlamı
Projede iki ayrı meta kaynak düşünülmüş:
- para = operasyonel kaynak,
- game score = uzun vadeli ilerleme,
- rating = itibar/talep dengesi.

Bu ayrım iyi bir tasarım niyeti gösteriyor.
Yeni projede kesinlikle korunması önerilir.

---

## 17. Shop Sistemi
### Durum
- `ShopManager`: `Aktif`
- `ShopUIController`: `Aktif`
- `ShopCatalogSO`, `ShopProductDefinitionSO`, `ShopTabDefinition`: `Aktif`

### Amaçlanan davranış
Shop veri odaklı tasarlanmış.
Sekmeler, ürünler, satın alma ve satış kuralları katalog üzerinden tanımlanıyor.

### Çalışan davranışlar
- sekme seçimi,
- seçili sekmeye göre ürün listeleme,
- satın alma doğrulama,
- satış doğrulama,
- ekonomi entegrasyonu,
- game score kilidi,
- inventory/build inventory entegrasyonu,
- sahip olunan ve yerleştirilmiş miktar sorguları,
- event tabanlı transaction yayınlama.

### Tasarım anlamı
Bu sistem yeni projede büyük ölçüde korunabilir.
Çünkü veri odaklı shop yaklaşımı temiz ve genişlemeye uygun.

### Güçlü yönler
- ürün bazlı kurallar ayrı,
- shop UI ile manager ayrılmış,
- `ConsumableInventory` ve `BuildInventory` ayrımı net.

---

## 18. Envanter Sistemleri
### Durum
- `InventoryManager`: `Aktif`
- `BuildInventoryManager`: `Aktif`

### Tasarım ayrımı
Projede iki ayrı envanter katmanı var:

#### 1. Tüketilebilir envanter
Örnek:
- malzeme,
- gıda girdileri,
- üretimde harcanacak öğeler.

#### 2. Build envanteri
Örnek:
- masa,
- sandalye,
- fırın,
- yerleştirilebilir sahne objeleri.

### Tasarım anlamı
Bu ayrım doğru.
Yeni projede de korunmalı.
Çünkü restoranın operasyon stoğu ile dekor/altyapı stoğu aynı şey değil.

### Shop entegrasyonu
Her iki manager da shop transaction event'lerini dinleyip ilgili stoğu güncelliyor.
Bu event tabanlı bağ yapısı yeni projede service katmanına taşınabilir.

---

## 19. UI ve Editör Tarafı
### Durum
- `WindowController`: `Aktif`
- `ShopUIController`: `Aktif`
- `ProgramsPanelManager`: `Kısmi`
- `RobotProgramsPanelController`: `Kısmi`
- diğer UI toggle/controller sınıfları: `Kısmi`

### Amaçlanan davranış
Kod editörü pencereleri, shop penceresi ve robot program panelleri üzerinden oyuncu çok pencereli bir üretim/otomasyon arayüzü kullanacak gibi görünüyor.

### `WindowController`
- pencere minimize/restore,
- runtime resize,
- pencere kapatma.

### Tasarım anlamı
Oyunda IDE benzeri çok pencereli bir yönetim deneyimi hedeflenmiş.
Bu çok önemli bir ürün kimliği olabilir.
Yani oyuncu sadece butonlara basmıyor; adeta restoran otomasyon konsolunu kullanıyor.

---

## 20. Save/Load Sistemi
### Durum
- `SaveManager`: `Taslak`
- `SaveDataModels`: `Taslak`

### Niyet
Gün sonu kayıt veya genel progress save sistemi planlanmış.
Ancak mevcut projede henüz uygulanmamış.

### Yeni projede öneri
Save kapsamı en başta net tanımlanmalı:
- ekonomi,
- game score,
- rating,
- restoran yerleşimi,
- build inventory,
- robot scriptleri,
- unlock durumu,
- gün/faz bilgisi.

---

## 21. Test ve Geliştirici Araçları
### Durum
- `Assets/Tests` klasörü mevcut
- çeşitli debug scriptleri mevcut
- editor tool ve syntax araçları mevcut ama parçalı durumda

### Anlamı
Proje deneysel şekilde iteratif geliştirilmiş.
Bazı sistemler oynanıştan önce prototiplenmiş, bazıları ise ürünleştirilmeden repo içinde kalmış.

Bu yüzden yeni projede:
- domain testleri,
- play mode testleri,
- komut/DSL testleri,
- placement testleri
ayrı ele alınmalı.

---

## 22. Legacy Koddan Çıkan Gerçek Oyun Vizyonu
Mevcut koda bakınca oyunun asıl vizyonu şu şekilde özetlenebilir:

> Oyuncu, restoranı fiziksel olarak yönetmek yerine robotlara program yazarak yöneten bir otomasyon mimarıdır. Amaç daha fazla müşteriyi daha verimli şekilde ağırlamak, restoran düzenini optimize etmek, yeni ekipman ve içeriklerin kilidini açmak ve gittikçe daha karmaşık servis süreçlerini otomatikleştirmektir.

Bu vizyonun alt başlıkları:
- restoran kur,
- oturma kapasitesini artır,
- robot davranışı yaz,
- mutfak akışını çöz,
- siparişleri aksatmadan yönet,
- temizlik ve servis döngüsünü optimize et,
- para, puan ve rating ile büyü.

---

## 23. Yeni Proje İçin Korunması Gereken Çekirdekler
Yeni projede mutlaka korunması önerilen ana fikirler:

### Korunmalı
- robotlara kod yazma fikri,
- faz bazlı gün akışı,
- grid tabanlı restoran kurulumu,
- müşteri state machine,
- shop + build inventory ayrımı,
- game score ve money ayrımı,
- rating sisteminin meta ilerlemeye etkisi.

### Yeniden tasarlanmalı
- `DayManager` ve faz orkestrasyonu,
- save/load,
- recipe/cooking pipeline,
- robot inventory ve task sistemi,
- editor UX,
- command güvenliği ve sandbox,
- customer satisfaction/economy bağları.

### Legacy olarak referans alınmalı ama doğrudan taşınmamalı
- placeholder MonoBehaviour sınıfları,
- log bazlı kalan komutlar,
- tam bağlanmamış debug scriptleri,
- yarım kalan UI prototipleri.

---

## 24. Yeni Mimari İçin Önerilen Yüksek Seviye Katmanlar
Bu belge esasen tasarım belgesi olsa da legacy yeniden kurulum için aşağıdaki mimari ayrım faydalı olur:

### Domain
- `RestaurantDay`
- `PhaseState`
- `CustomerFlow`
- `Order`
- `RobotProgram`
- `Economy`
- `Rating`
- `Inventory`
- `BuildInventory`
- `PlacementGrid`

### Application
- `DayFlowService`
- `CustomerSpawnService`
- `RobotProgramExecutionService`
- `ShopService`
- `PlacementService`
- `SaveLoadService`
- `CookingService`

### Infrastructure / Unity Adapters
- navmesh movement adapter,
- scene object lookup,
- save backend,
- UI presenters,
- script editor bindings.

### Presentation
- shop screens,
- code editor windows,
- robot assignment panels,
- day phase HUD,
- score/rating/balance HUD.

---

## 25. Legacy'den Yeni Projeye Taşıma Önceliği
Önerilen sırayla ilerlemek mantıklı olur:

1. Faz sistemi
2. Grid ve placement
3. Table/chair ilişkisi
4. Customer state machine
5. Order sistemi
6. Robot movement + executor
7. DSL runtime minimum sürüm
8. `move_to`, `serve`, `wait` komutları
9. Economy + shop
10. Inventory + build inventory
11. Cooking
12. Rating + score + day end
13. Save/load
14. Gelişmiş UI/editör

Bu sıranın sebebi şudur:
Önce restoran sahnesinin yaşayan minimum simülasyonu kurulmalı, sonra otomasyon ve meta sistemler eklenmeli.

---

## 26. Legacy Kodda Tespit Edilen Riskler
### 1. Tamamlanmış gibi görünen ama aslında taslak olan sınıflar
Örnek:
- `DayManager`
- `PrepPhase`
- `ServPhase`
- `SaveManager`
- `SaveDataModels`
- `RecipeExecutor`
- `Robot`
- `RobotStats`
- `RobotUpgrades`
- `CodeEditorUI`
- `AutoCompleteProvider`

### 2. Tasarım niyeti ile çalışma durumu arasındaki fark
Bazı sistemler yorumlarda geniş hedef anlatıyor ama oynanış bağları henüz kurulmamış.
Bu yüzden yeni projede sadece sınıf adına bakarak karar verilmemeli.

### 3. MonoBehaviour yoğunluğu
Birçok sınıf doğrudan Unity component olarak yazılmış.
Yeni projede domain mantığını `MonoBehaviour` dışına almak bakım maliyetini ciddi azaltır.

---

## 27. Sonuç
Bu legacy projeden çıkan en net sonuç şudur:

`OtoRestoran`, robotlara kod yazılarak işletilen bir restoran otomasyon oyunu olarak tasarlanmış.
Restoran düzenleme, müşteri akışı, robot komut sistemi, shop ve ilerleme mekanikleri bunun etrafında şekillenmiş.

Mevcut repo tamamen bitmiş bir oyun değil; çalışan prototip parçaları ile taslak fikirlerin karışımı.
Bu yüzden yeni projede en doğru yaklaşım:
- legacy kodu referans almak,
- çalışan sistemleri davranış bazında yeniden kurmak,
- sınıf isimlerini değil tasarım niyetini taşımak,
- placeholder yapıları yeni mimariye doğrudan kopyalamamak.

Bu belge yeni projede hangi sistemin neden var olacağını ve hangi davranışın korunması gerektiğini sabitlemek için temel referans olarak kullanılabilir.

---

## 28. İnceleme Sırasında Referans Alınan Temel Kaynaklar
Bu belge hazırlanırken özellikle şu kaynaklar incelendi:
- `Assets/Scripts/Core/GameLoop/PhaseStateMachine.cs`
- `Assets/Scripts/Core/Customers/Customer.cs`
- `Assets/Scripts/Core/Customers/CustomerSpawner.cs`
- `Assets/Scripts/Core/Robots/RobotController.cs`
- `Assets/Scripts/Core/Robots/RobotExecutor.cs`
- `Assets/Scripts/Core/Robots/RobotCommandQueue.cs`
- `Assets/Scripts/Programming/Runtime/GameCodeRunner.cs`
- `Assets/Scripts/Programming/Runtime/AstInterpreter.cs`
- `Assets/Scripts/Programming/Commands/BuiltinCommandRegistry.cs`
- `Assets/Scripts/Programming/Language/BuiltinFunctions.cs`
- `Assets/Scripts/Core/Restaurant/Grid/GridManager.cs`
- `Assets/Scripts/Core/Restaurant/Objects/Furniture/TableBehavior.cs`
- `Assets/Scripts/Core/Restaurant/Objects/Furniture/ChairBehavior.cs`
- `Assets/Scripts/Core/Restaurant/Objects/Kitchen/OvenBehavior.cs`
- `Assets/Scripts/Core/Economy/EconomyManager.cs`
- `Assets/Scripts/Core/Score/GameScoreManager.cs`
- `Assets/Scripts/Core/Score/RestaurantRatingManager.cs`
- `Assets/Scripts/Core/Shop/ShopManager.cs`
- `Assets/Scripts/Core/Inventory/InventoryManager.cs`
- `Assets/Scripts/Core/Inventory/BuildInventoryManager.cs`
- `Assets/UI/ShopUIController.cs`
- `Assets/UI/WindowController.cs`
