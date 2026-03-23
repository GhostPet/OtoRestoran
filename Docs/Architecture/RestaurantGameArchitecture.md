# OtoRestoran Hedef Oyun Mimarisi

## Amaç
Bu doküman, robot programlama odaklı izometrik restoran simülasyonu için hedef klasör yapısını, veri modelini ve runtime mimarisini tanımlar.

## Mevcut Mimari Problemleri
Aşağıdaki noktalar mevcut kod tabanında doğrudan görülüyor:

1. `Assets/Scripts/Programming/Runtime/GameCodeRunner.cs`
   - `ToggleRunForIndex` içinde yeni script başlatırken `StopAllExecution()` çağrılıyor.
   - Bu tasarım, aynı anda birden fazla robotun farklı script çalıştırması gereksinimiyle çelişiyor.
2. `Assets/Scripts/Core/Shop/ShopProductDefinitionSO.cs`
   - `GetBuyTotalPrice` ve `GetSellTotalPrice` metotları miktarı dikkate almıyor.
   - Fiyat hesabı veri katmanında hatalı.
3. `Assets/Scripts/Core/Inventory/InventoryManager.cs`
   ve `Assets/Scripts/Core/Inventory/BuildInventoryManager.cs`
   - Benzer mantık iki kez yazılmış.
   - Her iki sistem de doğrudan `ShopManager` event'lerine bağlı.
   - Ortak kurallar servis katmanına taşınmalı.
4. `Assets/Scripts/Core/Shop/ShopManager.cs`
   - Shop, ekonomi, skor, grid ve sahiplik mantığını aynı sınıfta topluyor.
   - Tek sorumluluk ilkesini bozuyor.
5. `Assets/Scripts/Programming/Runtime/SafeExecutionGuard.cs`
   ve `Assets/Scripts/Programming/Runtime/ScriptRuntime.cs`
   - Kritik runtime güvenlik katmanı boş durumda.
   - Oyuncu kodu hata verdiğinde oyunun çökmesi riski burada çözülmeli.

## Mimari İlkeler

### 1. ScriptableObject sadece tanım verisi tutar
`ScriptableObject` nesneleri sadece içerik tanımı için kullanılmalı:
- item tanımı
- obje tanımı
- tarif tanımı
- market ürün tanımı
- milestone tanımı
- robot modeli ve upgrade tanımı

Runtime state `ScriptableObject` içinde tutulmamalı.
Runtime state servislerde, save modellerinde ve entity bileşenlerinde tutulmalı.

### 2. Robot script çalıştırma izole olmalı
Her robotun bağımsız bir script oturumu olmalı:
- kendi execution context'i
- kendi variable scope'u
- kendi hata kaydı
- kendi iptal durumu
- kendi komut kuyruğu

Bir robotun script hatası diğer robotları veya ana oyun döngüsünü durdurmamalı.

### 3. Object storage ve item inventory ayrı kalmalı
İki sistem ayrılmalı ama ortak arayüz üzerinden konuşmalı:
- `ObjectInventory`: grid'e yerleştirilecek dünya nesneleri
- `ItemInventory`: robotun taşıdığı ve üretimde kullandığı malzemeler

### 4. Oyun akışları servis tabanlı olmalı
`MonoBehaviour` bileşenleri yalnızca bağlama ve görselleştirme yapmalı.
Kural ve iş mantığı saf C# servislerinde toplanmalı.

### 5. İçerik veri tabanlı olmalı
Yeni eşya, recipe, robot yeteneği veya milestone eklemek için mevcut koda minimum dokunuş gerekmeli.

## Hedef Klasör Yapısı

```text
Assets/
├─ Scenes/
│  ├─ Bootstrap/
│  │  └─ Bootstrap.unity
│  ├─ Frontend/
│  │  ├─ MainMenu.unity
│  │  └─ SaveSelect.unity
│  ├─ Gameplay/
│  │  ├─ RestaurantSandbox.unity
│  │  └─ RestaurantProduction.unity
│  └─ Testbeds/
│     ├─ ProgrammingSandbox.unity
│     ├─ GridSandbox.unity
│     └─ EconomySandbox.unity
├─ Scripts/
│  ├─ Bootstrap/
│  │  ├─ GameBootstrapper.cs
│  │  ├─ SceneReferenceRegistry.cs
│  │  └─ ServiceInstaller.cs
│  ├─ Shared/
│  │  ├─ Constants/
│  │  ├─ Extensions/
│  │  ├─ Utility/
│  │  ├─ Results/
│  │  └─ Events/
│  ├─ Core/
│  │  ├─ Time/
│  │  ├─ SaveLoad/
│  │  ├─ Economy/
│  │  ├─ Milestones/
│  │  ├─ Progression/
│  │  └─ GameLoop/
│  ├─ Programming/
│  │  ├─ Language/
│  │  │  ├─ Lexer/
│  │  │  ├─ Parser/
│  │  │  ├─ Ast/
│  │  │  ├─ Validation/
│  │  │  └─ Diagnostics/
│  │  ├─ Runtime/
│  │  │  ├─ Sessions/
│  │  │  ├─ Execution/
│  │  │  ├─ Scheduling/
│  │  │  ├─ Safety/
│  │  │  └─ Bindings/
│  │  ├─ Commands/
│  │  ├─ Functions/
│  │  ├─ Editor/
│  │  └─ UI/
│  ├─ Robots/
│  │  ├─ Domain/
│  │  ├─ Runtime/
│  │  ├─ Tasks/
│  │  ├─ Navigation/
│  │  ├─ Interactions/
│  │  └─ Presentation/
│  ├─ Restaurant/
│  │  ├─ Grid/
│  │  ├─ Placement/
│  │  ├─ Objects/
│  │  │  ├─ Appliances/
│  │  │  ├─ Furniture/
│  │  │  ├─ Storage/
│  │  │  └─ Decor/
│  │  ├─ Stations/
│  │  └─ Layout/
│  ├─ Inventory/
│  │  ├─ Abstractions/
│  │  ├─ ItemInventory/
│  │  ├─ ObjectInventory/
│  │  ├─ Transfers/
│  │  └─ UI/
│  ├─ Items/
│  │  ├─ Definitions/
│  │  ├─ Runtime/
│  │  └─ Recipes/
│  ├─ Orders/
│  │  ├─ Domain/
│  │  ├─ Runtime/
│  │  └─ UI/
│  ├─ Customers/
│  │  ├─ Domain/
│  │  ├─ Runtime/
│  │  └─ Satisfaction/
│  ├─ Shop/
│  │  ├─ Catalog/
│  │  ├─ Runtime/
│  │  ├─ Unlocks/
│  │  └─ UI/
│  └─ UI/
│     ├─ Common/
│     ├─ HUD/
│     ├─ Windows/
│     └─ Debug/
├─ ScriptableObjects/
│  ├─ Catalogs/
│  │  ├─ Shop/
│  │  ├─ Items/
│  │  ├─ Objects/
│  │  ├─ Recipes/
│  │  └─ Milestones/
│  ├─ Definitions/
│  │  ├─ Items/
│  │  ├─ Objects/
│  │  ├─ Appliances/
│  │  ├─ Robots/
│  │  └─ Customers/
│  ├─ Balance/
│  │  ├─ Economy/
│  │  ├─ Ratings/
│  │  └─ Satisfaction/
│  └─ RuntimeTemplates/
│     └─ Programming/
├─ Prefabs/
│  ├─ Robots/
│  ├─ Restaurant/
│  ├─ UI/
│  └─ Customers/
└─ Tests/
   ├─ EditMode/
   └─ PlayMode/
```

## Önerilen Assembly Definition Yapısı
Mümkünse klasör bazlı `asmdef` ayrımı yapılmalı:

- `OtoRestoran.Shared`
- `OtoRestoran.Core`
- `OtoRestoran.Programming`
- `OtoRestoran.Robots`
- `OtoRestoran.Restaurant`
- `OtoRestoran.Inventory`
- `OtoRestoran.Shop`
- `OtoRestoran.UI`
- `OtoRestoran.Editor`
- `OtoRestoran.Tests`

Bu ayrım derleme süresini düşürür ve bağımlılıkları görünür hale getirir.

## Hedef Veri Modeli

### ScriptableObject tanımları

#### `ItemDefinitionSO`
- `itemId`
- `displayName`
- `icon`
- `stackLimit`
- `itemTags`
- `baseValue`

#### `PlaceableObjectDefinitionSO`
- `objectId`
- `displayName`
- `prefab`
- `footprint`
- `pivot`
- `placementRules`
- `interactionProfile`
- `marketUnlockId`

#### `ApplianceDefinitionSO`
- `objectDefinition`
- `supportedRecipeTags`
- `inputSlotCount`
- `outputSlotCount`
- `processingDuration`

#### `RecipeDefinitionSO`
- `recipeId`
- `stationType`
- `requiredInputs`
- `requiredToolTags`
- `output`
- `cookDuration`
- `qualityModifiers`

#### `ShopCatalogSO`
- sadece kategori ve ürün listesi tutar
- ürün fiyatı, unlock koşulu ve hedef envanter tipini referanslar

#### `MilestoneDefinitionSO`
- `milestoneId`
- `requiredRestaurantScore`
- `unlockActions`
- `rewardText`

## Runtime Sistemleri

### Programlama runtime

#### `RobotScriptSession`
Tek bir robotun çalışan script oturumudur.
- `SessionId`
- `RobotId`
- `ExecutionStatus`
- `Diagnostics`
- `CancellationToken`
- `VariableStore`

#### `RobotScriptRunner`
- parse
- validate
- compile/prepare
- tick bazlı yürütme
- hata yalıtımı

#### `ScriptSafetyGuard`
- max instruction per tick
- max loop iterations
- timeout
- illegal command filtering
- runtime exception yakalama

#### `RobotCommandBridge`
Script komutlarını robot görevlerine çevirir.
Örnek:
- `move_to(counter)`
- `pickup(item)`
- `cook(recipe_id)`
- `serve(table_2)`
- `clean(table_4)`

### Robot domain

#### `RobotBrain`
- aktif görev
- script session referansı
- task queue
- fail state

#### `RobotTaskDispatcher`
Script veya otomatik AI tarafından gelen görevleri çalıştırır.

#### `RobotBlackboard`
Robotun dünyadan bildiği verileri tutar:
- hedef istasyon
- eldeki item
- aktif sipariş
- şarj seviyesi
- bloklanma nedeni

### Envanter domain

#### `IInventoryService`
Ortak arayüz:
- `CanAdd`
- `CanRemove`
- `TryAdd`
- `TryRemove`
- `GetQuantity`

#### `ItemInventoryService`
Tüketilebilir item ve araç gereçler için.

#### `ObjectInventoryService`
Yerleştirilebilir obje hakları için.

#### `InventoryTransferService`
- robot ↔ storage
- appliance ↔ storage
- storage ↔ order prep

### Restoran ve obje etkileşimleri

Her obje davranışı iki parçalı olmalı:
1. `DefinitionSO`
2. `RuntimeBehaviour`

Örnekler:
- `OvenDefinitionSO` + `OvenRuntime`
- `StorageRackDefinitionSO` + `StorageRackRuntime`
- `CounterDefinitionSO` + `CounterRuntime`
- `TableDefinitionSO` + `DiningTableRuntime`

Bu sayede aynı runtime mantığı farklı veri setleriyle tekrar kullanılabilir.

## Sipariş ve ekonomi akışı

### Sipariş akışı
1. müşteri gelir
2. masa veya sıra atanır
3. sipariş oluşur
4. sipariş görev parçalarına bölünür
5. robot(lar) görevleri üstlenir
6. ürün teslim edilir
7. müşteri memnuniyeti hesaplanır
8. ödeme ve bahşiş işlenir
9. restoran puanı güncellenir
10. milestone sistemi unlock kontrolü yapar

### Ekonomi formülü
Önerilen ödeme modeli:
- `basePrice`
- `serviceSpeedModifier`
- `orderAccuracyModifier`
- `cleanlinessModifier`
- `moodModifier`
- `tipAmount`

`finalPayment = basePrice * satisfactionMultiplier + tipAmount`

## Sahne Düzeni

### `Bootstrap.unity`
Kalıcı sistemler:
- save/load
- content registry
- service installer
- audio bootstrap
- scene loader

### `RestaurantProduction.unity`
Ana oynanış:
- grid root
- placeable roots
- robot spawners
- customer spawners
- runtime HUD
- code editor windows

### `ProgrammingSandbox.unity`
Sadece dil ve robot programlama testleri için.
Bu sahne ana oyundan bağımsız tutulmalı.

### `GridSandbox.unity`
Yerleştirme, footprint, pivot ve rotation testleri için.

## Uygulama Kuralları

1. Unity `MonoBehaviour` sınıfları servis locator gibi davranmamalı.
2. `FindAnyObjectByType` ve `FindFirstObjectByType` kullanımı bootstrap ve composition root dışına taşmamalı.
3. Runtime state `ScriptableObject` içine yazılmamalı.
4. Oyuncu script hataları `Result` veya `Diagnostic` nesnesi olarak UI'ye dönmeli.
5. Robot script sistemi frame'i kilitlememeli; tick bazlı çalışmalı.
6. Market unlock sistemi doğrudan score manager içine gömülmemeli; progression servisi üzerinden çözülmeli.

## İlk Refactor Hedefleri

1. `Programming` runtime katmanını güvenli hale getirmek
2. `Shop`, `Inventory`, `Economy`, `Milestone` bağımlılıklarını ayrıştırmak
3. `ObjectInventory` ve `ItemInventory` için ortak abstraction eklemek
4. `PlaceableObject` davranışlarını definition/runtime yapısına taşımak
5. sahneleri testbed ve production olarak ayırmak

## Beklenen Sonuç
Bu yapı ile proje:
- daha modüler olur
- SO bağımlılığı temizlenir
- robot başına bağımsız script çalıştırabilir
- oyuncu kod hatalarında oyun çökmez
- yeni eşya ve recipe eklemek kolaylaşır
- test yazmak mümkün hale gelir
