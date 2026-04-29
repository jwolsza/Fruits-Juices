# Fruits & Juices — Design Specification

**Data:** 2026-04-29
**Projekt:** Unity 6.3 (URP) + ProBuilder + Input System
**Lokalizacja:** `/Users/jakubwolsza/Documents/Fruits&Juices`

---

## 1. Wizja gry

Mobilna gra 3D w stylu casual idle. Gracz prowadzi mini-fabrykę soków z owoców: ciężarówki zbierają owoce ze ściany, opróżniają je do dużych butelek, gracz rozlewa sok na małe butelki i dostarcza je klientom.

**Gatunek:** 3D casual idle, mobile-first.
**Cel rozgrywki:** endless / idle z klasyczną pętlą progresji (monety → upgrady).
**Brak fail state.** System ma "soft caps" (pojemności) zamiast kar.

---

## 2. Layout świata

3 strefy ułożone **poziomo obok siebie** (oś X):

| Strefa | Pozycja | Zawartość |
|--------|---------|-----------|
| Strefa 1 | Lewa | Ściana z owocami, conveyor ciężarówek, garaż |
| Strefa 2 | Środek | 3 duże butelki, racki małych butelek |
| Strefa 3 | Prawa | Gracz (kapsuła), kolejka klientów |

### Kamera
- Perspective, FoV ~30°, stały kąt nachylenia ~15° w dół
- Porusza się tylko po osi X (horizontal pan)
- **Bez snap pointów** - swobodny scroll z miękkim ograniczeniem na końcach (rubber band)
- Lewy koniec = pełen widok strefy 1 + ½ strefy 2; prawy koniec = pełen widok strefy 3 + ½ strefy 2
- Domyślny start: kamera wycentrowana na strefie 3

### Input
- **Joystick** (floating): dolny lewy ~40% ekranu, **zawsze aktywny**. Palec kładziesz w dowolnym miejscu obszaru, ten punkt staje się centrum, ruch palca = wektor kierunku.
- **Swipe horizontal**: górne ~60% ekranu, przesuwa kamerą, inertia + rubber band.
- **Tap**: rozpoznawany w górnym ~60%, palec niemal się nie ruszył (`<10px`/`0.2s`). Działa na ciężarówki w strefie 1 i duże butelki w strefie 2.

### Komponenty Unity
- `CameraScrollController` — pan po X, rubber band, inertia.
- `InputRouter` — klasyfikuje gesty wg obszaru ekranu i timing/dystansu.
- `IClickable` — interfejs dla obiektów reagujących na tap.

---

## 3. Strefa 1 — Ściana z owocami i ciężarówki

### 3.1 Ściana (grid + sand-physics + manual refill)

- Grid: **300 kolumn × 300 wierszy** (parametr SO `GameBalanceSO.WallColumns/Rows`). Cała ściana widoczna w kamerze (rendering wszystkich komórek; przy 90 000 instancji warto SpriteRenderer + culling/batching).
- Każda komórka: zajęta (typ owoca) lub pusta.
- Wizualizacja: **2D SpriteRenderer per komórka** z prostą kwadratową grafiką, kolor sprite'a = typ owoca. Cała ściana jest jedną siatką sprite'ów ulokowaną pionowo w przestrzeni 3D (oś Y to wysokość ściany, oś X szerokość). Cell-size konfigurowalny (`CellSizeWorldUnits`, default ~0.05).
- **Brak fizyki**, własna sztuczna grawitacja (sand-style):
  - Tick co `1/GravityRateHz` sekundy (default 10Hz).
  - Iteracja od dołu w górę. Dla każdej zajętej komórki `(x, y)`:
    1. Jeśli `(x, y-1)` pusta → przesuń w dół.
    2. Else jeśli `(x-1, y-1)` pusta → przesuń skos w lewo.
    3. Else jeśli `(x+1, y-1)` pusta → przesuń skos w prawo.
    4. Else zostaje.
  - Parzyste tikki sprawdzają najpierw lewy skos, nieparzyste prawy (eliminacja biasu).
- **Spawn owoców — batch refill na przycisk**, nie continuous:
  - Gracz tappuje "Refill Wall" button (UI w HUD).
  - Wszystkie ciężarówki w tym czasie wstrzymują się (`IsRefilling = true` flaga konsumowana przez systemy ciężarówek; w Plan #2 brak ciężarówek, flaga gotowa do odpinki w Plan #3).
  - Każdy tick refilla (`1/RefillTickRateHz`, default 30Hz) spawnuje `RefillSpawnsPerTick` (default ~100) owoców w losowych pustych komórkach top row z puli odblokowanych typów; sand-physics co tick przesuwa stos w dół, zwalniając miejsce w top row.
  - Refill kończy się gdy każda komórka zajęta — flaga `IsRefilling = false`.
- **Wskaźniki:** brak top cap / fail state — refill po prostu kończy się gdy grid pełen. Gracz uruchamia kolejny refill kiedy ściana zostanie wystarczająco opróżniona przez ciężarówki.

### 3.2 Sloty pod ścianą (3 aktywne, parallel)

- 3 punkty pod dolną krawędzią ściany (lewy / środkowy / prawy), w stałych pozycjach X.
- Każdy slot to "active spot" z magnetem na **całą bottom row** ściany (slot **NIE** ogranicza zasięgu do 3 kolumn).
- 3 ciężarówki mogą zbierać **jednocześnie** (po 1 na slot).

### 3.3 Conveyor (wężyk)

- Zamknięta pętla pod ścianą: prosta linia pod 3 slotami → zakręt → tył pętli → drugi zakręt → wraca do slotu lewego.
- Lista waypointów; ciężarówki podążają z fixed spacing.
- Max ciężarówek na torze: parametr SO `ConveyorSlotCount` (start: 4, upgradowalny).
- **Pause logic**: cały conveyor zatrzymuje się tylko gdy ciężarówka stojąca w slocie 3 (ostatnim) zbiera. Ciężarówki za nią stoją (formacja). Ciężarówki PRZED nią (na zakręcie / tyle pętli) jadą dalej normalnie.

### 3.4 Logika ciężarówki

**Stany:** `Idle (in garage)` → `OnConveyor (driving)` → `Collecting` (tylko w slocie 3) → `OnConveyor` → `Full` → `DrivingToBottle` → `Dumping` → `ReturningToGarage` → `Idle`.

- Kolor ciężarówki = stały, odpowiada typowi owoca (enum `FruitType`).
- Pojemność: **100 owoców** start (parametr SO `TruckCapacity`).
- Podczas przejazdu przez sloty 1 i 2 ciężarówka collectuje "w locie" (magnet) **bez zatrzymywania się**.
- W slocie 3 zatrzymuje się **tylko jeśli** wciąż może zbierać:
  - W bottom row ściany jest co najmniej 1 owoc jej koloru.
  - Ciężarówka nie jest pełna.
- Po wyczerpaniu warunków → opuszcza slot 3 i jedzie dalej (zakręt → tył pętli → wraca do slotu 1).
- Jak zostanie pełna na trasie → opuszcza conveyor w punkcie dispatch (po zakręcie za slotem 3), jedzie do strefy 2.

### 3.5 Magnet — przydział owoców

Tick magneta `1/MagnetRateHz` (default 5Hz).
1. Zbierz listę aktywnych ciężarówek w slotach (max 3).
2. Zbierz listę owoców w bottom row ściany.
3. Dla każdej ciężarówki (kolejność: slot 1 → 2 → 3):
   - Znajdź najbliższego pasującego owoca w bottom row względem jej pozycji X.
   - Jeśli istnieje i ciężarówka nie pełna → przypisz, animuj łuk Bezier ~0.3s. Owoc usuwany z grida w momencie startu animacji (eliminacja double-assignment). Animacja śledzi aktualną pozycję ciężarówki (tracking moving target — ciężarówki w slotach 1 i 2 cały czas się poruszają).
4. Po przypisaniu — sand-physics przy następnym tiku przesuwa stack w dół, kolejne owoce trafiają do bottom row.

### 3.6 Garaż

- Garaż znajduje się po lewej stronie strefy 1, przy punkcie wejścia conveyora (od tyłu pętli). Ciężarówki z garażu wjeżdżają na lewy skraj conveyora, jadą w prawo przez sloty 1 → 2 → 3, zakręcają, wracają tyłem pętli; jeśli pełne — opuszczają conveyor w punkcie dispatch zaraz za slotem 3 i kierują się do strefy 2; jeśli nie pełne — wracają na początek pętli.
- Każda ciężarówka ma slot w garażu, oznaczona kolorem swojego owoca (paka pomalowana).
- **Tap** na ciężarówkę → wjeżdża na conveyor (jeśli wolny slot na torze), animacja wjazdu od wejścia toru.
- Pełen conveyor → tap = visual shake / negative feedback (brak akcji).

### 3.7 Eventy
- `OnFruitSpawned(FruitType, columnIndex)` — opcjonalny.
- `OnTruckCollectedFruit(truckId, FruitType, count)`.
- `OnTruckFull(truckId, FruitType, fruitCount)` — startuje routing do strefy 2.

---

## 4. Strefa 2 — Duże butelki, rozlewanie, racki

### 4.1 Duże butelki (3 sztuki)

- Stałe pozycje, rząd w strefie 2.
- Pojemność: **200 owoców** start (parametr SO `BigBottleCapacity`).
- Stany: `Empty (unreserved)` → `Filling (reserved type X)` → `TapAble` (zawiera sok) → po rozlaniu wraca do `Empty`.
- **Reserved type**: po pierwszym wrzuceniu owocu, butelka lockuje typ aż do pełnego opróżnienia.
- Wizualizacja: szklana butelka, sok jako wewnętrzny cylinder z `scaleY` animowanym wraz z `fillAmount`. Kolor zgodnie z typem.
- Komponent: `BigBottle` z polami `currentType : FruitType?`, `fillAmount : int`, `capacity : int`.

### 4.2 Routing pełnej ciężarówki

Po `OnTruckFull` ciężarówka szuka butelki w priorytecie:
1. Pasująca typu i z miejscem (`fillAmount + truckLoad ≤ capacity`).
2. Pusta (`Empty/unreserved`) — po dotarciu zarezerwuje swój typ.
3. Brak żadnej z (1)/(2) → ciężarówka kieruje się do **buffer queue** (parking obok strefy 2), czeka aż jakaś butelka zwolni miejsce.

Po dotarciu: animacja zsypywania (~1s), `fillAmount += truckLoad`, ciężarówka opróżniona, **wraca do garażu** jako `Idle`.

### 4.3 Tap → rozlewanie

- Tap (rozpoznany przez `InputRouter` jako tap, nie swipe) → `BigBottle.Pour()`.
- **Konwersja: 5 owoców = 1 mała butelka** (parametr SO `FruitsPerSmallBottle`).
- N butelek do rozlania = `floor(fillAmount / 5)`, ograniczone przez wolne miejsce w racku.
- Animacja: butelka przechyla się, sok leje się do racka, małe butelki "wyrastają" jedna po drugiej w slotach racka. Tempo: **6 butelek/sek** (parametr SO `PourSpeed`).
- Po rozlaniu: `fillAmount` pomniejszane o `5 * spawnedSmallBottles`. Jeśli pozostałość `< 5` (i co najmniej 1 mała butelka została rozlana w tej akcji) — `fillAmount := 0`, butelka wraca do `Empty/unreserved` (uproszczenie: leftover < 5 nie kumuluje się).
- Jeśli rack był pełny i nawet 1 mała butelka się nie zmieściła — butelka zostaje nietknięta (`fillAmount` bez zmian, typ nadal zarezerwowany), tap wyzwala visual shake.
- Jeśli butelka opróżniona w pełni (`fillAmount == 0` po rozlaniu) — `Empty/unreserved`.

### 4.4 Racki małych butelek

- 3 racki, jeden przy każdej dużej butelce.
- Ulokowane na **granicy strefy 2 i 3** — gracz dochodzi do nich z dołu (z pozycji w strefie 3).
- Każdy rack: trójwymiarowy stojak (model ProBuilder) z gridem slotów (np. 5×6 = 30 slotów).
- Pojemność: **30 małych butelek** (parametr SO `RackCapacity`, upgradowalna).
- Rack przejmuje typ od dużej butelki w momencie spawnu pierwszej małej butelki tej rundy rozlewania. Tracimy "typ racka" gdy rack jest pusty.
- Rack pełny + tap na butelkę → rozlewanie częściowe (do zapełnienia racka) lub brak akcji (jeśli rack już pełny i butelka nieskonwertowalna na nawet 1 dodatkową) + visual shake.

### 4.5 Eventy
- `OnBigBottleReserved(bottleId, FruitType)`.
- `OnBigBottleFull(bottleId)`.
- `OnBigBottlePoured(bottleId, FruitType, smallBottleCount)`.
- `OnSmallBottlesSpawned(rackId, FruitType, count)`.

---

## 5. Strefa 3 — Gracz, klienci, dostarczanie

### 5.1 Gracz (kapsuła)

- Capsule mesh (ProBuilder) z prostą animacją bobbing.
- Movement: kinematic `CharacterController`, transform sterowany inputem.
- Joystick `direction` (znormalizowany) → `velocity = direction * MoveSpeed`.
- `MoveSpeed`: **5 jednostek/s** start (parametr SO `PlayerSpeed`, upgradowalny).
- Pole gry gracza: cała strefa 3 + pasek przy granicy strefy 2 (żeby dosięgnąć racków).
- **Pojemność**: 10 małych butelek start (parametr SO `PlayerCapacity`, upgradowalna), **mieszane typy**.
- Wizualizacja niesionego: pionowy stack małych butelek nad głową gracza, każda widoczna jako mini-prefab z kolorem typu.

### 5.2 Auto-pickup z racków

- Trigger collider wokół gracza, radius ~1.5m (parametr SO `PickupRadius`).
- Wewnątrz triggera racka → tick co `1/PickupRateHz` (default 10Hz):
  - Jeśli gracz ma wolne miejsce **i** rack ma butelki → przenieś 1 butelkę z racka do gracza, animacja "wystrzelenia" (~0.15s).
- Powtarza dopóki: gracz pełny / rack pusty / gracz wyjdzie ze strefy triggera.
- FIFO — gracz bierze cokolwiek jest w racku.

### 5.3 Kolejka klientów

- **5 slotów** w kolejce (parametr SO `CustomerQueueLength`, upgradowalny do ~10).
- Sloty w linii (np. od prawej krawędzi strefy 3, klienci czekają, kolejny za nim).
- Spawn klienta: nowy klient pojawia się na końcu kolejki co `1/CustomerSpawnRateHz` (start: 1/4s) jeśli wolny slot.
- Ruch klienta do slotu: prosty waypoint follow.
- **Każdy klient ma 1 zamówienie**: konkretny typ soku (1 mała butelka).
- Typ losowany z aktualnie odblokowanych typów.
- Wizualizacja zamówienia: bubble nad głową klienta z ikoną owocu i kolorową obwódką.

### 5.4 Auto-deliver

- Trigger collider wokół gracza wykrywa klientów w kolejce, radius ~1.5m (parametr `DeliverRadius`).
- Tick co `1/DeliverRateHz` (default 10Hz):
  - Dla każdego klienta w trigger zone (preferencja: najbliższy):
    - Jeśli gracz ma w stacku butelkę pasującą do jego zamówienia → animacja podania (butelka leci z stacku do klienta, ~0.3s).
    - `OnCustomerServed(customerId, coins, FruitType)`.
    - Klient odchodzi (animacja: w bok / w dół ekranu), slot się zwalnia.
    - Pozostali klienci shuffle do przodu.

### 5.5 Brak fail state
- Klient czeka w nieskończoność (brak timera cierpliwości w MVP).
- Jeśli gracz nie ma jego typu — przejdzie obok bez podania.
- Kolejka pełna → kolejny klient nie spawnuje (utracony potencjalny dochód, nie kara).

### 5.6 Eventy
- `OnPlayerPickedBottle(rackId, FruitType)`.
- `OnCustomerSpawned(customerId, orderType)`.
- `OnCustomerServed(customerId, coins, FruitType)` → `EconomyManager`.

---

## 6. Progresja, ekonomia, upgrady

### 6.1 Waluta
- Jedna waluta: **monety**. Start: 0.
- Źródło: `OnCustomerServed` → `+coinsPerCustomer * fruitTypeMultiplier`.
- Wydatek: tylko zakupy upgradów.
- Komponent: `EconomyManager` (singleton), emituje `OnCoinsChanged`.

### 6.2 Upgrady

Każdy upgrade definiowany w `UpgradeConfigSO`:
- `id : string`
- `displayName : string`
- `description : string`
- `level : int` (start 0)
- `maxLevel : int?`
- `baseCost : int`
- `costMultiplier : float` (default 1.5)
- `effectPerLevel : float` (interpretowany przez konkretny upgrade handler)

**Lista upgradów MVP:**

**Strefa 1:**
- `TruckCapacity` — +20 owoców / level (start: 100).
- `TruckCount_<Color>` — dodaj kolejną ciężarówkę danego koloru do garażu (per typ owocu, start: 1 ciężarówka per kolor).
- `ConveyorSlots` — max ciężarówek na torze (start: 4, +1/level, max 8).
- `MagnetRateHz` — start 5Hz, +1Hz/level (max 15).
- `FruitSpawnRateHz` — start 2Hz, +0.5Hz/level (max 10).

**Strefa 2:**
- `BigBottleCapacity` — start 200, +50/level.
- `RackCapacity` — start 30, +5/level.
- `PourSpeed` — start 6/sek, +2/level.

**Strefa 3:**
- `PlayerSpeed` — start 5 j/s, +0.5/level (max 12).
- `PlayerCapacity` — start 10, +2/level (max ~50).
- `CustomerQueueLength` — start 5, +1/level (max 10).
- `CustomerSpawnRateHz` — start 0.25Hz (1/4s), +0.05Hz/level (max 1Hz).
- `CoinsPerCustomer` — start 1.0x, +0.1x/level.

### 6.3 Odblokowywanie typów owoców

- Start z 3 typów: jabłko, pomarańcza, cytryna.
- Upgrade `UnlockFruit_<type>` — jednorazowy zakup za stały koszt (niezależnie od poziomu).
- Po zakupie: typ wchodzi do spawn pool ściany, w garażu pojawia się początkowa ciężarówka tego typu, klienci mogą zamawiać ten typ.
- Pula do odblokowania: truskawka, winogrono, banan, kiwi, ananas, arbuz, mango (7 dodatkowych typów).

### 6.4 UI panelu upgradów

- Mały button na stałe (dolny prawy róg ekranu).
- Panel slide-in z listą upgradów.
- Zakładki / filtry: Zone 1 / Zone 2 / Zone 3 / Unlocks.
- Każdy item pokazuje: nazwę, level, opis efektu, koszt (z aktualizacją multiplikatora), przycisk Buy (disabled jeśli za mało monet).
- HUD: licznik monet w lewym górnym rogu.
- Komponenty: `UpgradePanelUI`, `UpgradeItem` prefab, `CoinsHUD`.

### 6.5 Bez save/load i offline progress w MVP

- Stan gry żyje w pamięci sesji.
- Restart aplikacji = fresh start (monety: 0, upgrady: 0, typy: początkowe 3).
- Save/load + offline progress można dodać poza MVP (już teraz architektura z `EconomyManager` + `UpgradeManager` ułatwia późniejsze dodanie serializacji JSON).

---

## 7. Architektura kodu

### 7.1 Struktura folderów

```
Assets/
├── _Project/
│   ├── Scenes/
│   │   └── Main.unity
│   ├── Scripts/
│   │   ├── Core/                  GameManager, EventChannels (SO base), Singletons
│   │   ├── Input/                 InputRouter, JoystickArea, CameraScrollController
│   │   ├── Zone1_FruitWall/       FruitWall, FruitGrid, FruitGravityTick, FruitSpawner, WallView
│   │   ├── Zone1_Trucks/          Truck, TruckStateMachine, ConveyorTrack, Garage, WallSlot, MagnetSystem
│   │   ├── Zone2_Bottling/        BigBottle, BottleRouter, SmallBottleRack, PourAnimation
│   │   ├── Zone3_Player/          Player, PlayerCarry, AutoPickup, AutoDeliver
│   │   ├── Zone3_Customers/       Customer, CustomerQueue, CustomerSpawner, OrderBubble
│   │   ├── Economy/               EconomyManager, UpgradeManager
│   │   ├── UI/                    UpgradePanelUI, CoinsHUD, UpgradeItem
│   │   └── Data/                  ScriptableObjects (FruitTypeSO, UpgradeConfigSO, GameBalanceSO, EventChannelSOs)
│   ├── Prefabs/
│   │   ├── Fruits/                Per typ owocu
│   │   ├── Trucks/                Generic truck prefab + color material variants
│   │   ├── Bottles/               BigBottle, SmallBottle
│   │   ├── Customer/              Customer prefab + skin variants
│   │   └── UI/                    UpgradeItem, JoystickHandle, OrderBubble
│   ├── Materials/                 Per kolor owocu + ogólne (metal, wood, glass)
│   ├── Models/                    ProBuilder eksporty (jeśli zapisane jako .asset)
│   └── Settings/
│       ├── GameBalanceSO.asset    Wartości startowe wszystkich parametrów
│       ├── Upgrades/              UpgradeConfigSO assets
│       └── Events/                EventChannel SO assets
```

### 7.2 Namespace convention
- `Project.Core`, `Project.Input`, `Project.Zone1.FruitWall`, `Project.Zone1.Trucks`, `Project.Zone2`, `Project.Zone3.Player`, `Project.Zone3.Customers`, `Project.Economy`, `Project.UI`, `Project.Data`.
- Każdy folder w `Scripts/` ma swój `.asmdef`. Reguły referencji:
  - `Core` i `Data` mogą być referencowane przez wszystkich.
  - `Zone1.*`, `Zone2`, `Zone3.*`, `Economy`, `UI` referencują `Core`/`Data` i komunikują się tylko przez Event Channels.
  - Strefy NIE odwołują się do siebie bezpośrednio.

### 7.3 Scena (Main.unity)
- `[Managers]` — root z singletonami: `EconomyManager`, `UpgradeManager`, `InputRouter`, `CameraScrollController`.
- `[World]` — root z dziećmi `Zone1`, `Zone2`, `Zone3`, każde ze swoim managerem i contentem.
- `[UI]` — Canvas z HUD (coins, upgrade button) i panel slide-in.
- `[Camera]` — Main Camera z URP.

### 7.4 ScriptableObject Event Channels
Wzorzec:
```csharp
public class FruitTypeEventChannel : ScriptableObject {
    public event Action<FruitType> Raised;
    public void Raise(FruitType type) => Raised?.Invoke(type);
}
```
Subscriberzy w `OnEnable` / `OnDisable`. Lista kanałów w `Settings/Events/`:
- `OnTruckFull`, `OnTruckCollectedFruit`
- `OnBigBottleReserved`, `OnBigBottlePoured`
- `OnSmallBottlesSpawned`
- `OnCustomerSpawned`, `OnCustomerServed`
- `OnCoinsChanged`, `OnUpgradePurchased`, `OnFruitUnlocked`

### 7.5 GameManager bootstrap
- `GameManager.Awake()` ładuje `GameBalanceSO`, inicjalizuje `EconomyManager` (coins=0), `UpgradeManager` (wszystkie levele=0), spawnuje początkowe ciężarówki dla 3 startowych typów, budzi 3 strefy.
- Brak ekranu loading / main menu w MVP — od razu gameplay.

---

## 8. Modele 3D (ProBuilder)

Wszystkie modele tworzone w ProBuilder, zero zewnętrznych assetów:

| Obiekt | Konstrukcja |
|--------|-------------|
| Owoc | Sześcian z fazami (mała kostka), kolor per typ |
| Ciężarówka | Sklejka 2-3 sześcianów (cab + paka + spłaszczone walce jako koła), paka w kolorze typu |
| Duża butelka | Cylinder z węższą szyjką (extrude), materiał glass + wewnętrzny cylinder soku z animowanym `scaleY` |
| Mała butelka | Mini wersja big bottle |
| Rack | Konstrukcja z prętów (cienkie boxy), grid slotów |
| Klient | Capsule + sześcian "głowa", inne proporcje niż gracz |
| Gracz | Capsule (kontrastowy kolor wobec klientów) |
| Ściana / podłoga / przepierzenia | Prostokątne boxy z procedural materialami |

Geometrię można ewentualnie freeze do mesha później (po balansowaniu). MVP używa surowych ProBuilder objects jako prefabów.

---

## 9. MVP — kolejność implementacji

Każdy krok kończy się grywalnym (choć niekompletnym) buildem do testu, z review przed kolejnym.

1. **Kamera + Input + Joystick + scena z 3 strefami stub** — puste boxy oznaczające granice, scrollowanie kamerą, joystick odpalany.
2. **Strefa 1 — ściana + sand-physics + spawn owoców** — bez ciężarówek, samo grid się napełnia owocami z grawitacją.
3. **Strefa 1 — ciężarówki + conveyor + magnet pickup** — 3 typy owoców, 1 ciężarówka per kolor, garaż, dispatch tap, collect w slotach.
4. **Strefa 2 — duże butelki + dump z ciężarówki + rack + tap rozlewania**.
5. **Strefa 3 — gracz + ruch + auto-pickup z racków**.
6. **Strefa 3 — klienci + spawn + auto-deliver + monety**.
7. **UI — HUD coins + panel upgradów + 3-5 podstawowych upgradów** (TruckCapacity, PlayerCapacity, ConveyorSlots, BigBottleCapacity, CustomerSpawnRate).
8. **Balans i polishing** — animacje, dodatkowe upgrady, kolory, kamera tweaks.

### Out of MVP
- Save / load.
- Offline progress / idle reward.
- Cutscene / start menu / tutorial.
- SFX / music.
- Particle effects (juice splash, magnet sparkle, confetti).
- Reklamy / IAP.
- VIP klienci, multiple worlds.

---

## 10. Parametry balansowe (start)

Wszystkie wartości definiowane w `GameBalanceSO` (jeden asset):

| Parametr | Wartość |
|----------|---------|
| `WallColumns` | 42 |
| `WallRows` | 42 |
| `WallWidthWorldUnits` | 5 |
| `WallHeightWorldUnits` | 5 |
| `GravityRateHz` | 10 |
| `RefillTickRateHz` | 30 |
| `RefillSpawnsPerTick` | 100 |
| `MagnetRateHz` | 5 |
| `MagnetAnimDurationSec` | 0.3 |
| `ConveyorSlotCount` | 4 |
| `TruckCapacity` | 100 |
| `BigBottleCapacity` | 200 |
| `FruitsPerSmallBottle` | 5 |
| `RackCapacity` | 30 |
| `PourSpeed` | 6 |
| `PlayerSpeed` | 5 |
| `PlayerCapacity` | 10 |
| `PickupRadius` | 1.5 |
| `DeliverRadius` | 1.5 |
| `PickupRateHz` | 10 |
| `DeliverRateHz` | 10 |
| `CustomerQueueLength` | 5 |
| `CustomerSpawnRateHz` | 0.25 |
| `CoinsPerCustomer_Base` | 10 |
| `StartingFruitTypes` | [Apple, Orange, Lemon] |
| `LockedFruitTypes` | [Strawberry, Grape, Banana, Kiwi, Pineapple, Watermelon, Mango] |

---

## 11. Glossary / definicje pojęć

- **Strefa** — jeden z 3 obszarów gry (ściana / butelki / gracz), ułożone poziomo.
- **Ciężarówka** — pojazd zbierający owoce ze ściany. Stały kolor/typ. Pojemność limitowana, upgradowalna.
- **Conveyor / wężyk** — zamknięta pętla pod ścianą po której jeżdżą ciężarówki.
- **Slot (ściana)** — punkt zatrzymania ciężarówki pod ścianą. 3 sloty równolegle aktywne, magnet sięga całej dolnej krawędzi.
- **Magnet** — mechanizm przyciągania owoców z bottom row do ciężarówki w slocie.
- **Garaż** — miejsce parkowania ciężarówek czekających na dispatch.
- **Duża butelka** — pojemnik w strefie 2, gromadzi sok z ciężarówek tego samego typu.
- **Rack** — stojak z małymi butelkami, output rozlewania, pickup point dla gracza.
- **Mała butelka** — produkt finalny dostarczany klientowi.
- **Klient** — NPC w kolejce z konkretnym zamówieniem (typ soku).
- **Sand-physics** — własna grawitacja grid-based, bez Rigidbody.
- **Active spot / collecting spot** — slot pod ścianą w którym ciężarówka aktywnie zbiera.
