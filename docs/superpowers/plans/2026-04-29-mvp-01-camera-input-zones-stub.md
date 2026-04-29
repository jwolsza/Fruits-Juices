# MVP Step 1: Camera + Input + Joystick + Zones Stub — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Pierwszy grywalny build: gracz może horyzontalnie scrollować kamerą po świecie z 3 strefami-stubami (placeholder boxes ProBuildera), oraz wykorzystać floating joystick w dolnej lewej części ekranu (na razie bez gracza — joystick zwraca tylko wektor input).

**Architecture:** Czysty Unity 6.3 URP. Foldery `Assets/_Project/`. Niezależne skrypty pod `_Project/Scripts/{Core,Input}/` z osobnymi `.asmdef`. ScriptableObject `GameBalanceSO` jako single-source-of-truth dla parametrów. Główna scena `Main.unity` z hierarchią `[Managers]`/`[World]`/`[UI]`/`[Camera]`. Logika input + camera testowalna unitowo (EditMode tests).

**Tech Stack:** Unity 6.3 (6000.3.12f1), URP 17.3, ProBuilder 6.0, Input System 1.19, Test Framework 1.6.

**Plan numer:** 1/8 w sekwencji MVP. Spec: `docs/superpowers/specs/2026-04-29-fruits-juices-design.md`.

---

## File Structure (do utworzenia)

```
Assets/
├── _Project/
│   ├── Scripts/
│   │   ├── Core/
│   │   │   ├── Project.Core.asmdef
│   │   │   └── FruitType.cs                           Enum typów owoców (Apple, Orange, Lemon, ...)
│   │   ├── Data/
│   │   │   ├── Project.Data.asmdef                    Reference: Project.Core
│   │   │   └── GameBalanceSO.cs                       ScriptableObject z parametrami startowymi
│   │   └── Input/
│   │       ├── Project.Input.asmdef                   Reference: Project.Core, Project.Data, com.unity.inputsystem
│   │       ├── InputRouter.cs                         Klasyfikuje gesty: joystick / swipe / tap wg obszaru ekranu
│   │       ├── JoystickArea.cs                        Floating joystick w dolnym lewym ~40%
│   │       ├── CameraScrollController.cs              Pan kamerą po X z rubber band i inertią
│   │       └── ScreenAreaUtils.cs                     Pomocnik: rozróżnia obszary ekranu (joystick vs scroll)
│   ├── Scenes/
│   │   └── Main.unity                                 Główna scena
│   ├── Prefabs/
│   │   └── ZoneStub.prefab                            Placeholder box ProBuilder dla strefy
│   ├── Settings/
│   │   ├── GameBalance.asset                          Instancja GameBalanceSO
│   │   └── Input/
│   │       └── PlayerInputActions.inputactions        Akcje wejściowe (zmigrowane z domyślnego URP template)
│   └── Materials/
│       ├── ZoneStub_Zone1.mat                         Lekkie różnice koloru per strefa
│       ├── ZoneStub_Zone2.mat
│       └── ZoneStub_Zone3.mat
└── _Project/Tests/
    ├── EditMode/
    │   ├── Project.Tests.EditMode.asmdef              Reference: nunit, Project.Core, Project.Data, Project.Input, Test Framework
    │   ├── GameBalanceSOTests.cs
    │   ├── ScreenAreaUtilsTests.cs
    │   ├── CameraScrollControllerTests.cs
    │   ├── JoystickAreaTests.cs
    │   └── InputRouterTests.cs
    └── PlayMode/
        ├── Project.Tests.PlayMode.asmdef              Reference: Project.Core, Project.Data, Project.Input, Test Framework
        └── SceneSmokeTest.cs                          Smoke test: scena ładuje się, ma 3 zone stuby
```

---

## Task 0: Scaffolding — foldery, asmdefs, czyszczenie defaultu

**Files:**
- Create: `Assets/_Project/Scripts/Core/Project.Core.asmdef`
- Create: `Assets/_Project/Scripts/Data/Project.Data.asmdef`
- Create: `Assets/_Project/Scripts/Input/Project.Input.asmdef`
- Create: `Assets/_Project/Tests/EditMode/Project.Tests.EditMode.asmdef`
- Create: `Assets/_Project/Tests/PlayMode/Project.Tests.PlayMode.asmdef`
- Delete: `Assets/Readme.asset`, `Assets/Readme.asset.meta`
- Delete: `Assets/TutorialInfo/` (cały folder)

- [ ] **Step 1: Utwórz strukturę katalogów**

W Unity Editor (lub w shellu z Unity zamkniętym, żeby nie generował metafiles bez identyfikatorów):
```bash
cd "/Users/jakubwolsza/Documents/Fruits&Juices"
mkdir -p Assets/_Project/Scripts/Core
mkdir -p Assets/_Project/Scripts/Data
mkdir -p Assets/_Project/Scripts/Input
mkdir -p Assets/_Project/Scenes
mkdir -p Assets/_Project/Prefabs
mkdir -p Assets/_Project/Materials
mkdir -p Assets/_Project/Settings/Input
mkdir -p Assets/_Project/Tests/EditMode
mkdir -p Assets/_Project/Tests/PlayMode
```

- [ ] **Step 2: Usuń defaultowy URP template**

```bash
cd "/Users/jakubwolsza/Documents/Fruits&Juices"
rm -f Assets/Readme.asset Assets/Readme.asset.meta
rm -rf Assets/TutorialInfo Assets/TutorialInfo.meta
rm -f Assets/InputSystem_Actions.inputactions Assets/InputSystem_Actions.inputactions.meta
rm -rf Assets/Scenes Assets/Scenes.meta
```

- [ ] **Step 3: Otwórz projekt w Unity Editor**

Otwórz `Fruits&Juices.sln` lub dokładnie projekt w Unity Hub (wersja 6000.3.12f1). Poczekaj aż Unity przelici importy. Sprawdź konsolę — nie powinno być błędów (poza ostrzeżeniami o usuniętych plikach, do zignorowania).

- [ ] **Step 4: Utwórz `Project.Core.asmdef`**

Plik: `Assets/_Project/Scripts/Core/Project.Core.asmdef`

```json
{
    "name": "Project.Core",
    "rootNamespace": "Project.Core",
    "references": [],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": false,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

- [ ] **Step 5: Utwórz `Project.Data.asmdef`**

Plik: `Assets/_Project/Scripts/Data/Project.Data.asmdef`

```json
{
    "name": "Project.Data",
    "rootNamespace": "Project.Data",
    "references": [
        "Project.Core"
    ],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": false,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

- [ ] **Step 6: Utwórz `Project.Input.asmdef`**

Plik: `Assets/_Project/Scripts/Input/Project.Input.asmdef`

```json
{
    "name": "Project.Input",
    "rootNamespace": "Project.Input",
    "references": [
        "Project.Core",
        "Project.Data",
        "Unity.InputSystem"
    ],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": false,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

- [ ] **Step 7: Utwórz `Project.Tests.EditMode.asmdef`**

Plik: `Assets/_Project/Tests/EditMode/Project.Tests.EditMode.asmdef`

```json
{
    "name": "Project.Tests.EditMode",
    "rootNamespace": "Project.Tests.EditMode",
    "references": [
        "Project.Core",
        "Project.Data",
        "Project.Input",
        "UnityEngine.TestRunner",
        "UnityEditor.TestRunner",
        "Unity.InputSystem",
        "Unity.InputSystem.TestFramework"
    ],
    "includePlatforms": [
        "Editor"
    ],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": true,
    "precompiledReferences": [
        "nunit.framework.dll"
    ],
    "autoReferenced": false,
    "defineConstraints": [
        "UNITY_INCLUDE_TESTS"
    ],
    "versionDefines": [],
    "noEngineReferences": false
}
```

- [ ] **Step 8: Utwórz `Project.Tests.PlayMode.asmdef`**

Plik: `Assets/_Project/Tests/PlayMode/Project.Tests.PlayMode.asmdef`

```json
{
    "name": "Project.Tests.PlayMode",
    "rootNamespace": "Project.Tests.PlayMode",
    "references": [
        "Project.Core",
        "Project.Data",
        "Project.Input",
        "UnityEngine.TestRunner"
    ],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": true,
    "precompiledReferences": [
        "nunit.framework.dll"
    ],
    "autoReferenced": false,
    "defineConstraints": [
        "UNITY_INCLUDE_TESTS"
    ],
    "versionDefines": [],
    "noEngineReferences": false
}
```

- [ ] **Step 9: Wróć do Unity Editor i poczekaj na recompile**

Unity przebuduje wszystkie assembly. Sprawdź konsolę — nie powinno być błędów. W oknie `Project` powinieneś widzieć ikony assembly definition (czerwone moduły).

- [ ] **Step 10: Commit**

```bash
cd "/Users/jakubwolsza/Documents/Fruits&Juices"
git add Assets/_Project/ Assets/Readme.asset Assets/Readme.asset.meta Assets/TutorialInfo Assets/TutorialInfo.meta Assets/InputSystem_Actions.inputactions Assets/InputSystem_Actions.inputactions.meta Assets/Scenes Assets/Scenes.meta 2>/dev/null
git commit -m "chore: scaffold _Project structure, asmdefs, remove URP template defaults"
```

---

## Task 1: `FruitType` enum

**Files:**
- Create: `Assets/_Project/Scripts/Core/FruitType.cs`
- Test: brak (enum bez logiki — pominięcie testu)

- [ ] **Step 1: Utwórz `FruitType.cs`**

Plik: `Assets/_Project/Scripts/Core/FruitType.cs`

```csharp
namespace Project.Core
{
    public enum FruitType
    {
        // Starter (3 typy odblokowane od początku)
        Apple = 0,
        Orange = 1,
        Lemon = 2,

        // Locked (do odblokowania przez upgrady)
        Strawberry = 10,
        Grape = 11,
        Banana = 12,
        Kiwi = 13,
        Pineapple = 14,
        Watermelon = 15,
        Mango = 16,
    }
}
```

Numerowanie z przerwą zostawia miejsce na nowe owoce w starter / locked grupach bez kolizji z save'ami w przyszłości (mimo że MVP nie ma save, dobry hak na przyszłość).

- [ ] **Step 2: Sprawdź kompilację**

Wróć do Unity. Konsola powinna być bez błędów.

- [ ] **Step 3: Commit**

```bash
git add Assets/_Project/Scripts/Core/FruitType.cs Assets/_Project/Scripts/Core/FruitType.cs.meta
git commit -m "feat(core): add FruitType enum with starter and locked fruit types"
```

---

## Task 2: `GameBalanceSO` — ScriptableObject z parametrami startowymi (TDD)

**Files:**
- Create: `Assets/_Project/Scripts/Data/GameBalanceSO.cs`
- Test: `Assets/_Project/Tests/EditMode/GameBalanceSOTests.cs`

- [ ] **Step 1: Napisz failing test**

Plik: `Assets/_Project/Tests/EditMode/GameBalanceSOTests.cs`

```csharp
using NUnit.Framework;
using UnityEngine;
using Project.Core;
using Project.Data;

namespace Project.Tests.EditMode
{
    public class GameBalanceSOTests
    {
        [Test]
        public void DefaultInstance_HasExpectedStartingValues()
        {
            var balance = ScriptableObject.CreateInstance<GameBalanceSO>();
            balance.ResetToDefaults();

            Assert.AreEqual(1000, balance.WallColumns);
            Assert.AreEqual(1000, balance.WallRows);
            Assert.AreEqual(10f, balance.GravityRateHz);
            Assert.AreEqual(2f, balance.FruitSpawnRateHz);
            Assert.AreEqual(5f, balance.MagnetRateHz);
            Assert.AreEqual(4, balance.ConveyorSlotCount);
            Assert.AreEqual(100, balance.TruckCapacity);
            Assert.AreEqual(200, balance.BigBottleCapacity);
            Assert.AreEqual(5, balance.FruitsPerSmallBottle);
            Assert.AreEqual(30, balance.RackCapacity);
            Assert.AreEqual(6f, balance.PourSpeed);
            Assert.AreEqual(5f, balance.PlayerSpeed);
            Assert.AreEqual(10, balance.PlayerCapacity);
            Assert.AreEqual(1.5f, balance.PickupRadius);
            Assert.AreEqual(1.5f, balance.DeliverRadius);
            Assert.AreEqual(10f, balance.PickupRateHz);
            Assert.AreEqual(10f, balance.DeliverRateHz);
            Assert.AreEqual(5, balance.CustomerQueueLength);
            Assert.AreEqual(0.25f, balance.CustomerSpawnRateHz);
            Assert.AreEqual(10, balance.CoinsPerCustomerBase);

            CollectionAssert.AreEqual(
                new[] { FruitType.Apple, FruitType.Orange, FruitType.Lemon },
                balance.StartingFruitTypes);
        }
    }
}
```

- [ ] **Step 2: Uruchom test — verify fail**

W Unity: `Window → General → Test Runner → EditMode → Run All`. Test `DefaultInstance_HasExpectedStartingValues` powinien failować bo `GameBalanceSO` jeszcze nie istnieje (compile error).

- [ ] **Step 3: Implementuj `GameBalanceSO.cs`**

Plik: `Assets/_Project/Scripts/Data/GameBalanceSO.cs`

```csharp
using UnityEngine;
using Project.Core;

namespace Project.Data
{
    [CreateAssetMenu(fileName = "GameBalance", menuName = "Project/GameBalance", order = 0)]
    public class GameBalanceSO : ScriptableObject
    {
        [Header("Wall (sand-physics grid)")]
        public int WallColumns = 1000;
        public int WallRows = 1000;
        public float GravityRateHz = 10f;
        public float FruitSpawnRateHz = 2f;

        [Header("Trucks / Conveyor")]
        public float MagnetRateHz = 5f;
        public int ConveyorSlotCount = 4;
        public int TruckCapacity = 100;

        [Header("Big bottles & racks")]
        public int BigBottleCapacity = 200;
        public int FruitsPerSmallBottle = 5;
        public int RackCapacity = 30;
        public float PourSpeed = 6f;

        [Header("Player")]
        public float PlayerSpeed = 5f;
        public int PlayerCapacity = 10;
        public float PickupRadius = 1.5f;
        public float DeliverRadius = 1.5f;
        public float PickupRateHz = 10f;
        public float DeliverRateHz = 10f;

        [Header("Customers")]
        public int CustomerQueueLength = 5;
        public float CustomerSpawnRateHz = 0.25f;
        public int CoinsPerCustomerBase = 10;

        [Header("Fruits")]
        public FruitType[] StartingFruitTypes = new[]
        {
            FruitType.Apple, FruitType.Orange, FruitType.Lemon,
        };

        public FruitType[] LockedFruitTypes = new[]
        {
            FruitType.Strawberry, FruitType.Grape, FruitType.Banana,
            FruitType.Kiwi, FruitType.Pineapple, FruitType.Watermelon, FruitType.Mango,
        };

        public void ResetToDefaults()
        {
            WallColumns = 1000;
            WallRows = 1000;
            GravityRateHz = 10f;
            FruitSpawnRateHz = 2f;

            MagnetRateHz = 5f;
            ConveyorSlotCount = 4;
            TruckCapacity = 100;

            BigBottleCapacity = 200;
            FruitsPerSmallBottle = 5;
            RackCapacity = 30;
            PourSpeed = 6f;

            PlayerSpeed = 5f;
            PlayerCapacity = 10;
            PickupRadius = 1.5f;
            DeliverRadius = 1.5f;
            PickupRateHz = 10f;
            DeliverRateHz = 10f;

            CustomerQueueLength = 5;
            CustomerSpawnRateHz = 0.25f;
            CoinsPerCustomerBase = 10;

            StartingFruitTypes = new[] { FruitType.Apple, FruitType.Orange, FruitType.Lemon };
            LockedFruitTypes = new[]
            {
                FruitType.Strawberry, FruitType.Grape, FruitType.Banana,
                FruitType.Kiwi, FruitType.Pineapple, FruitType.Watermelon, FruitType.Mango,
            };
        }
    }
}
```

- [ ] **Step 4: Uruchom test — verify pass**

`Test Runner → EditMode → Run All`. Powinien być zielony.

- [ ] **Step 5: Utwórz instancję `GameBalance.asset`**

W Unity Editor: `Project window → prawym → Create → Project → GameBalance`. Zapisz w `Assets/_Project/Settings/GameBalance.asset`. Otwórz w inspectorze, zweryfikuj że ma poprawne wartości startowe.

- [ ] **Step 6: Commit**

```bash
git add Assets/_Project/Scripts/Data/ Assets/_Project/Tests/EditMode/GameBalanceSOTests.cs Assets/_Project/Tests/EditMode/GameBalanceSOTests.cs.meta Assets/_Project/Settings/GameBalance.asset Assets/_Project/Settings/GameBalance.asset.meta
git commit -m "feat(data): add GameBalanceSO with starting parameters and tests"
```

---

## Task 3: `ScreenAreaUtils` — pomocnik klasyfikujący obszary ekranu (TDD)

Klasa pure-function: dany punkt (px coords) + wymiar ekranu → kategoria obszaru: `JoystickArea` (dolny lewy 40%) lub `ScrollArea` (górne 60%) lub `OutsideAll`.

**Files:**
- Create: `Assets/_Project/Scripts/Input/ScreenAreaUtils.cs`
- Test: `Assets/_Project/Tests/EditMode/ScreenAreaUtilsTests.cs`

- [ ] **Step 1: Napisz failing test**

Plik: `Assets/_Project/Tests/EditMode/ScreenAreaUtilsTests.cs`

```csharp
using NUnit.Framework;
using UnityEngine;
using Project.Input;

namespace Project.Tests.EditMode
{
    public class ScreenAreaUtilsTests
    {
        const int W = 1080;
        const int H = 1920;

        [Test]
        public void Classify_PointInBottomLeft40Percent_ReturnsJoystickArea()
        {
            var area = ScreenAreaUtils.Classify(new Vector2(100, 200), new Vector2(W, H));
            Assert.AreEqual(ScreenArea.Joystick, area);
        }

        [Test]
        public void Classify_PointInTopHalf_ReturnsScrollArea()
        {
            var area = ScreenAreaUtils.Classify(new Vector2(W / 2f, H * 0.8f), new Vector2(W, H));
            Assert.AreEqual(ScreenArea.Scroll, area);
        }

        [Test]
        public void Classify_PointInBottomRight_ReturnsScrollArea()
        {
            // Joystick is bottom LEFT 40% width × bottom 40% height.
            // bottom-right (right 60% width, bottom 40% height) is scroll.
            var area = ScreenAreaUtils.Classify(new Vector2(W * 0.8f, 200), new Vector2(W, H));
            Assert.AreEqual(ScreenArea.Scroll, area);
        }

        [Test]
        public void Classify_PointAtJoystickBoundary_TopRightCorner_StillJoystick()
        {
            // Joystick area: x ∈ [0, 0.4*W], y ∈ [0, 0.4*H]. Boundary should be inclusive on inner side.
            var area = ScreenAreaUtils.Classify(new Vector2(W * 0.4f - 1, H * 0.4f - 1), new Vector2(W, H));
            Assert.AreEqual(ScreenArea.Joystick, area);
        }

        [Test]
        public void Classify_PointJustOutsideJoystick_IsScroll()
        {
            var area = ScreenAreaUtils.Classify(new Vector2(W * 0.4f + 1, H * 0.4f - 1), new Vector2(W, H));
            Assert.AreEqual(ScreenArea.Scroll, area);
        }

        [Test]
        public void Classify_PointOutsideScreen_ReturnsOutsideAll()
        {
            var area = ScreenAreaUtils.Classify(new Vector2(-10, -10), new Vector2(W, H));
            Assert.AreEqual(ScreenArea.OutsideAll, area);

            var area2 = ScreenAreaUtils.Classify(new Vector2(W + 10, H / 2f), new Vector2(W, H));
            Assert.AreEqual(ScreenArea.OutsideAll, area2);
        }
    }
}
```

- [ ] **Step 2: Uruchom test — verify fail**

`Test Runner → EditMode → Run All`. Compile error (typy nie istnieją).

- [ ] **Step 3: Implementuj `ScreenAreaUtils`**

Plik: `Assets/_Project/Scripts/Input/ScreenAreaUtils.cs`

```csharp
using UnityEngine;

namespace Project.Input
{
    public enum ScreenArea
    {
        OutsideAll,
        Joystick,
        Scroll,
    }

    public static class ScreenAreaUtils
    {
        public const float JoystickWidthFraction = 0.4f;
        public const float JoystickHeightFraction = 0.4f;

        public static ScreenArea Classify(Vector2 point, Vector2 screenSize)
        {
            if (point.x < 0f || point.y < 0f || point.x > screenSize.x || point.y > screenSize.y)
                return ScreenArea.OutsideAll;

            bool inJoystick =
                point.x < screenSize.x * JoystickWidthFraction &&
                point.y < screenSize.y * JoystickHeightFraction;

            return inJoystick ? ScreenArea.Joystick : ScreenArea.Scroll;
        }
    }
}
```

- [ ] **Step 4: Uruchom testy — verify pass**

Wszystkie testy zielone.

- [ ] **Step 5: Commit**

```bash
git add Assets/_Project/Scripts/Input/ScreenAreaUtils.cs Assets/_Project/Scripts/Input/ScreenAreaUtils.cs.meta Assets/_Project/Tests/EditMode/ScreenAreaUtilsTests.cs Assets/_Project/Tests/EditMode/ScreenAreaUtilsTests.cs.meta
git commit -m "feat(input): add ScreenAreaUtils for input area classification with tests"
```

---

## Task 4: `JoystickArea` — floating joystick (TDD)

Klasa logiczna: stan (idle / active), pozycja palca, wynik = znormalizowany wektor 2D (`Vector2.zero` gdy idle, `magnitude ≤ 1` gdy active). Bez Unity-zależnych komponentów w testach.

**Files:**
- Create: `Assets/_Project/Scripts/Input/JoystickArea.cs`
- Test: `Assets/_Project/Tests/EditMode/JoystickAreaTests.cs`

- [ ] **Step 1: Napisz failing test**

Plik: `Assets/_Project/Tests/EditMode/JoystickAreaTests.cs`

```csharp
using NUnit.Framework;
using UnityEngine;
using Project.Input;

namespace Project.Tests.EditMode
{
    public class JoystickAreaTests
    {
        const float MaxRadius = 100f;

        [Test]
        public void NewJoystick_StartsIdle_OutputIsZero()
        {
            var j = new JoystickArea(MaxRadius);
            Assert.IsFalse(j.IsActive);
            Assert.AreEqual(Vector2.zero, j.Output);
        }

        [Test]
        public void OnPress_BecomesActive_OutputStillZeroBecauseAtCenter()
        {
            var j = new JoystickArea(MaxRadius);
            j.OnPress(new Vector2(500, 300));
            Assert.IsTrue(j.IsActive);
            Assert.AreEqual(Vector2.zero, j.Output);
            Assert.AreEqual(new Vector2(500, 300), j.Center);
        }

        [Test]
        public void OnDrag_ReturnsNormalizedVector_WithinUnitMagnitude()
        {
            var j = new JoystickArea(MaxRadius);
            j.OnPress(new Vector2(500, 300));
            j.OnDrag(new Vector2(550, 300)); // 50px right; ratio 0.5
            Assert.That(j.Output.x, Is.EqualTo(0.5f).Within(0.001f));
            Assert.That(j.Output.y, Is.EqualTo(0f).Within(0.001f));
        }

        [Test]
        public void OnDrag_BeyondMaxRadius_ClampsToMagnitudeOne()
        {
            var j = new JoystickArea(MaxRadius);
            j.OnPress(new Vector2(500, 300));
            j.OnDrag(new Vector2(900, 300)); // 400px right, well beyond maxRadius=100
            Assert.That(j.Output.magnitude, Is.EqualTo(1f).Within(0.001f));
            Assert.That(j.Output.x, Is.EqualTo(1f).Within(0.001f));
        }

        [Test]
        public void OnRelease_BecomesIdle_OutputZero()
        {
            var j = new JoystickArea(MaxRadius);
            j.OnPress(new Vector2(500, 300));
            j.OnDrag(new Vector2(550, 350));
            j.OnRelease();
            Assert.IsFalse(j.IsActive);
            Assert.AreEqual(Vector2.zero, j.Output);
        }

        [Test]
        public void OnDrag_DiagonalMovement_NormalizedCorrectly()
        {
            var j = new JoystickArea(MaxRadius);
            j.OnPress(new Vector2(500, 300));
            j.OnDrag(new Vector2(560, 380)); // Δ = (60,80), magnitude 100, exactly at maxRadius
            Assert.That(j.Output.magnitude, Is.EqualTo(1f).Within(0.01f));
            Assert.That(j.Output.x, Is.EqualTo(0.6f).Within(0.01f));
            Assert.That(j.Output.y, Is.EqualTo(0.8f).Within(0.01f));
        }
    }
}
```

- [ ] **Step 2: Uruchom test — verify fail**

`Test Runner → EditMode`. Compile error.

- [ ] **Step 3: Implementuj `JoystickArea.cs`**

Plik: `Assets/_Project/Scripts/Input/JoystickArea.cs`

```csharp
using UnityEngine;

namespace Project.Input
{
    /// <summary>
    /// Floating joystick logic. Pure C# class — testowalna bez Unity Editor.
    /// Caller dostarcza eventy (OnPress / OnDrag / OnRelease), klasa zwraca znormalizowany Output 2D.
    /// </summary>
    public class JoystickArea
    {
        public float MaxRadius { get; }
        public bool IsActive { get; private set; }
        public Vector2 Center { get; private set; }
        public Vector2 Output { get; private set; }

        public JoystickArea(float maxRadius)
        {
            MaxRadius = maxRadius;
            Reset();
        }

        public void OnPress(Vector2 screenPosition)
        {
            IsActive = true;
            Center = screenPosition;
            Output = Vector2.zero;
        }

        public void OnDrag(Vector2 screenPosition)
        {
            if (!IsActive) return;

            Vector2 delta = screenPosition - Center;
            if (delta.magnitude > MaxRadius)
                Output = delta.normalized;
            else
                Output = delta / MaxRadius;
        }

        public void OnRelease()
        {
            Reset();
        }

        void Reset()
        {
            IsActive = false;
            Center = Vector2.zero;
            Output = Vector2.zero;
        }
    }
}
```

- [ ] **Step 4: Uruchom testy — verify pass**

Wszystkie zielone.

- [ ] **Step 5: Commit**

```bash
git add Assets/_Project/Scripts/Input/JoystickArea.cs Assets/_Project/Scripts/Input/JoystickArea.cs.meta Assets/_Project/Tests/EditMode/JoystickAreaTests.cs Assets/_Project/Tests/EditMode/JoystickAreaTests.cs.meta
git commit -m "feat(input): add JoystickArea pure-logic class with tests"
```

---

## Task 5: `CameraScrollController` — logika pan kamerą po X (TDD)

Klasa logiczna obliczająca pozycję kamery na podstawie inputu (delta X palca w pikselach), z rubber band na końcach. Pure C# klasa, w Unity owinięta w `MonoBehaviour` w późniejszym tasku.

Wymagania:
- `targetX` — aktualna pozycja kamery (worldX)
- `minX`, `maxX` — granice
- `pixelsToWorld` — skala konwersji (np. 0.01)
- `rubberStrength` — jak mocno wraca z poza granic (0..1)
- `OnDragDelta(deltaPx)` — przesuwa pozycję, klamp z rubber band poza granicami
- `OnRelease()` — ustawia stan "released"
- `Update(dtSec)` — gdy released i pozycja poza granicami, interpoluje powrót do najbliższej granicy

**Files:**
- Create: `Assets/_Project/Scripts/Input/CameraScrollController.cs`
- Test: `Assets/_Project/Tests/EditMode/CameraScrollControllerTests.cs`

- [ ] **Step 1: Napisz failing test**

Plik: `Assets/_Project/Tests/EditMode/CameraScrollControllerTests.cs`

```csharp
using NUnit.Framework;
using Project.Input;

namespace Project.Tests.EditMode
{
    public class CameraScrollControllerTests
    {
        CameraScrollController NewCtrl(float min = 0f, float max = 10f, float startX = 5f)
        {
            return new CameraScrollController(
                minX: min,
                maxX: max,
                pixelsToWorld: 0.01f,
                rubberStrength: 0.5f,
                snapBackSpeed: 10f,
                startX: startX);
        }

        [Test]
        public void NewController_HasStartXAsTargetX()
        {
            var c = NewCtrl(startX: 5f);
            Assert.AreEqual(5f, c.TargetX);
        }

        [Test]
        public void OnDragDelta_WithinBounds_TranslatesPixelsToWorld()
        {
            var c = NewCtrl(startX: 5f);
            c.OnDragDelta(100f); // +100 px → +1 world (przy pixelsToWorld 0.01)
            Assert.That(c.TargetX, Is.EqualTo(6f).Within(0.001f));
        }

        [Test]
        public void OnDragDelta_NegativeDelta_GoesLeft()
        {
            var c = NewCtrl(startX: 5f);
            c.OnDragDelta(-200f); // -2 world
            Assert.That(c.TargetX, Is.EqualTo(3f).Within(0.001f));
        }

        [Test]
        public void OnDragDelta_PastMaxBound_RubberBandSlowsMovement()
        {
            var c = NewCtrl(min: 0f, max: 10f, startX: 9.5f);
            c.OnDragDelta(100f); // +1 world if no rubber, but past bound (10) we get rubber-attenuated
            Assert.That(c.TargetX, Is.GreaterThan(10f), "should pass bound");
            Assert.That(c.TargetX, Is.LessThan(10.5f), "rubber should attenuate compared to free 1.0 increment past 10");
        }

        [Test]
        public void OnRelease_PositionOutsideBounds_SnapsBackOverTime()
        {
            var c = NewCtrl(min: 0f, max: 10f, startX: 11f);
            c.OnRelease();

            // simulate update for 1s with snapBackSpeed 10
            c.Update(0.05f);
            Assert.That(c.TargetX, Is.LessThan(11f));

            // After enough time, must converge to maxX
            for (int i = 0; i < 100; i++) c.Update(0.05f);
            Assert.That(c.TargetX, Is.EqualTo(10f).Within(0.01f));
        }

        [Test]
        public void OnRelease_PositionWithinBounds_DoesNotMove()
        {
            var c = NewCtrl(min: 0f, max: 10f, startX: 5f);
            c.OnRelease();
            c.Update(1.0f);
            Assert.AreEqual(5f, c.TargetX);
        }
    }
}
```

- [ ] **Step 2: Uruchom test — verify fail**

`Test Runner → EditMode`. Compile error.

- [ ] **Step 3: Implementuj `CameraScrollController.cs`**

Plik: `Assets/_Project/Scripts/Input/CameraScrollController.cs`

```csharp
using UnityEngine;

namespace Project.Input
{
    /// <summary>
    /// Pure-logic kontroler pan kamery po osi X z rubber band na granicach.
    /// MonoBehaviour wrapper to add later (Task 9).
    /// </summary>
    public class CameraScrollController
    {
        public float MinX { get; }
        public float MaxX { get; }
        public float PixelsToWorld { get; }
        public float RubberStrength { get; }
        public float SnapBackSpeed { get; }

        public float TargetX { get; private set; }

        bool isDragging;

        public CameraScrollController(
            float minX, float maxX, float pixelsToWorld,
            float rubberStrength, float snapBackSpeed, float startX)
        {
            MinX = minX;
            MaxX = maxX;
            PixelsToWorld = pixelsToWorld;
            RubberStrength = rubberStrength;
            SnapBackSpeed = snapBackSpeed;
            TargetX = Mathf.Clamp(startX, minX, maxX);
        }

        public void OnDragStart()
        {
            isDragging = true;
        }

        public void OnDragDelta(float pixelDeltaX)
        {
            isDragging = true;
            float worldDelta = pixelDeltaX * PixelsToWorld;
            float newX = TargetX + worldDelta;

            // Rubber band poza granicami: tłumimy o (1 - rubberStrength).
            if (newX > MaxX)
            {
                float overshoot = newX - MaxX;
                newX = MaxX + overshoot * (1f - RubberStrength);
            }
            else if (newX < MinX)
            {
                float undershoot = MinX - newX;
                newX = MinX - undershoot * (1f - RubberStrength);
            }

            TargetX = newX;
        }

        public void OnRelease()
        {
            isDragging = false;
        }

        public void Update(float deltaTime)
        {
            if (isDragging) return;

            if (TargetX > MaxX)
            {
                TargetX = Mathf.MoveTowards(TargetX, MaxX, SnapBackSpeed * deltaTime);
            }
            else if (TargetX < MinX)
            {
                TargetX = Mathf.MoveTowards(TargetX, MinX, SnapBackSpeed * deltaTime);
            }
        }
    }
}
```

- [ ] **Step 4: Uruchom testy — verify pass**

Wszystkie zielone.

- [ ] **Step 5: Commit**

```bash
git add Assets/_Project/Scripts/Input/CameraScrollController.cs Assets/_Project/Scripts/Input/CameraScrollController.cs.meta Assets/_Project/Tests/EditMode/CameraScrollControllerTests.cs Assets/_Project/Tests/EditMode/CameraScrollControllerTests.cs.meta
git commit -m "feat(input): add CameraScrollController with rubber-band logic and tests"
```

---

## Task 6: `InputRouter` — klasyfikuje gesty (joystick / scroll / tap) (TDD)

State machine reagująca na press/drag/release i przekierowująca eventy:
- Press w joystick area → joystick `OnPress`
- Press w scroll area → możliwy tap; jeśli ruch > thresholdu → scroll mode
- Release w scroll area:
  - Jeśli ruch < tap threshold (10px / 0.2s) → emit Tap
  - Inaczej → emit Scroll release

Klasa pure-logic, `InputRouter` przyjmuje delegaty / event handlers przez konstruktor.

**Files:**
- Create: `Assets/_Project/Scripts/Input/InputRouter.cs`
- Test: `Assets/_Project/Tests/EditMode/InputRouterTests.cs`

- [ ] **Step 1: Napisz failing test**

Plik: `Assets/_Project/Tests/EditMode/InputRouterTests.cs`

```csharp
using NUnit.Framework;
using UnityEngine;
using Project.Input;
using System.Collections.Generic;

namespace Project.Tests.EditMode
{
    public class InputRouterTests
    {
        const int W = 1080;
        const int H = 1920;

        class FakeJoystick
        {
            public List<string> Events = new();
            public void OnPress(Vector2 p) => Events.Add($"press({p.x},{p.y})");
            public void OnDrag(Vector2 p) => Events.Add($"drag({p.x},{p.y})");
            public void OnRelease() => Events.Add("release");
        }

        class FakeScroll
        {
            public List<string> Events = new();
            public void OnDragDelta(float d) => Events.Add($"delta({d:F1})");
            public void OnRelease() => Events.Add("release");
        }

        InputRouter NewRouter(FakeJoystick j, FakeScroll s, out List<Vector2> taps)
        {
            var capturedTaps = new List<Vector2>();
            var router = new InputRouter(
                screenSize: new Vector2(W, H),
                tapDistanceThresholdPx: 10f,
                tapTimeThresholdSec: 0.2f,
                onJoystickPress: j.OnPress,
                onJoystickDrag: j.OnDrag,
                onJoystickRelease: j.OnRelease,
                onScrollDragDelta: s.OnDragDelta,
                onScrollRelease: s.OnRelease,
                onTap: capturedTaps.Add);
            taps = capturedTaps;
            return router;
        }

        [Test]
        public void PressInJoystickArea_RoutesToJoystick()
        {
            var j = new FakeJoystick(); var s = new FakeScroll();
            var r = NewRouter(j, s, out var taps);

            r.OnPointerDown(new Vector2(100, 100), 0f);

            CollectionAssert.Contains(j.Events, "press(100,100)");
            Assert.IsEmpty(s.Events);
            Assert.IsEmpty(taps);
        }

        [Test]
        public void TapInScrollArea_NoMovement_FastRelease_EmitsTap()
        {
            var j = new FakeJoystick(); var s = new FakeScroll();
            var r = NewRouter(j, s, out var taps);

            var pos = new Vector2(W / 2f, H / 2f);
            r.OnPointerDown(pos, 0f);
            r.OnPointerUp(pos + new Vector2(2, 2), 0.1f); // tiny move within tap window

            Assert.AreEqual(1, taps.Count);
            Assert.That(taps[0].x, Is.EqualTo(pos.x).Within(0.001f));
            Assert.IsEmpty(s.Events);
        }

        [Test]
        public void DragInScrollArea_BeyondThreshold_RoutesToScroll_NoTap()
        {
            var j = new FakeJoystick(); var s = new FakeScroll();
            var r = NewRouter(j, s, out var taps);

            var start = new Vector2(W / 2f, H / 2f);
            r.OnPointerDown(start, 0f);
            r.OnPointerMove(start + new Vector2(50, 0), 0.1f);
            r.OnPointerMove(start + new Vector2(80, 0), 0.2f);
            r.OnPointerUp(start + new Vector2(80, 0), 0.3f);

            Assert.IsEmpty(taps);
            // Two delta events: 50, then 30
            CollectionAssert.Contains(s.Events, "delta(50.0)");
            CollectionAssert.Contains(s.Events, "delta(30.0)");
            CollectionAssert.Contains(s.Events, "release");
        }

        [Test]
        public void HoldInScrollArea_BeyondTapTimeButStillStill_DoesNotEmitTap()
        {
            var j = new FakeJoystick(); var s = new FakeScroll();
            var r = NewRouter(j, s, out var taps);

            var pos = new Vector2(W / 2f, H / 2f);
            r.OnPointerDown(pos, 0f);
            r.OnPointerUp(pos, 1.0f); // long hold, no movement

            // Long press not classified as tap (>0.2s threshold)
            Assert.IsEmpty(taps);
        }

        [Test]
        public void DragJoystick_ForwardsAllMovesToJoystick()
        {
            var j = new FakeJoystick(); var s = new FakeScroll();
            var r = NewRouter(j, s, out var taps);

            r.OnPointerDown(new Vector2(100, 100), 0f);
            r.OnPointerMove(new Vector2(150, 150), 0.05f);
            r.OnPointerUp(new Vector2(160, 160), 0.1f);

            CollectionAssert.Contains(j.Events, "press(100,100)");
            CollectionAssert.Contains(j.Events, "drag(150,150)");
            CollectionAssert.Contains(j.Events, "drag(160,160)");
            CollectionAssert.Contains(j.Events, "release");
            Assert.IsEmpty(s.Events);
            Assert.IsEmpty(taps);
        }
    }
}
```

- [ ] **Step 2: Uruchom test — verify fail**

`Test Runner → EditMode`. Compile error.

- [ ] **Step 3: Implementuj `InputRouter.cs`**

Plik: `Assets/_Project/Scripts/Input/InputRouter.cs`

```csharp
using System;
using UnityEngine;

namespace Project.Input
{
    /// <summary>
    /// Klasyfikuje events typu pointer i przekierowuje je do odpowiedniego
    /// systemu (JoystickArea / CameraScrollController) albo emituje Tap.
    /// Pure logic — Unity-agnostic, dostaje pozycje w px i czas w sekundach.
    /// </summary>
    public class InputRouter
    {
        readonly Vector2 screenSize;
        readonly float tapDistanceThresholdPx;
        readonly float tapTimeThresholdSec;

        readonly Action<Vector2> onJoystickPress;
        readonly Action<Vector2> onJoystickDrag;
        readonly Action onJoystickRelease;
        readonly Action<float> onScrollDragDelta;
        readonly Action onScrollRelease;
        readonly Action<Vector2> onTap;

        ScreenArea downArea;
        Vector2 downPosition;
        Vector2 lastPosition;
        float downTime;
        bool exceededMovementThreshold;

        public InputRouter(
            Vector2 screenSize,
            float tapDistanceThresholdPx,
            float tapTimeThresholdSec,
            Action<Vector2> onJoystickPress,
            Action<Vector2> onJoystickDrag,
            Action onJoystickRelease,
            Action<float> onScrollDragDelta,
            Action onScrollRelease,
            Action<Vector2> onTap)
        {
            this.screenSize = screenSize;
            this.tapDistanceThresholdPx = tapDistanceThresholdPx;
            this.tapTimeThresholdSec = tapTimeThresholdSec;
            this.onJoystickPress = onJoystickPress;
            this.onJoystickDrag = onJoystickDrag;
            this.onJoystickRelease = onJoystickRelease;
            this.onScrollDragDelta = onScrollDragDelta;
            this.onScrollRelease = onScrollRelease;
            this.onTap = onTap;
            downArea = ScreenArea.OutsideAll;
        }

        public void OnPointerDown(Vector2 position, float timeSec)
        {
            downArea = ScreenAreaUtils.Classify(position, screenSize);
            downPosition = position;
            lastPosition = position;
            downTime = timeSec;
            exceededMovementThreshold = false;

            if (downArea == ScreenArea.Joystick)
                onJoystickPress?.Invoke(position);
        }

        public void OnPointerMove(Vector2 position, float timeSec)
        {
            if (downArea == ScreenArea.OutsideAll) return;

            if (downArea == ScreenArea.Joystick)
            {
                onJoystickDrag?.Invoke(position);
                lastPosition = position;
                return;
            }

            // Scroll area
            float distFromDown = (position - downPosition).magnitude;
            if (distFromDown >= tapDistanceThresholdPx)
                exceededMovementThreshold = true;

            if (exceededMovementThreshold)
            {
                float deltaX = position.x - lastPosition.x;
                onScrollDragDelta?.Invoke(deltaX);
            }

            lastPosition = position;
        }

        public void OnPointerUp(Vector2 position, float timeSec)
        {
            if (downArea == ScreenArea.OutsideAll) return;

            if (downArea == ScreenArea.Joystick)
            {
                onJoystickRelease?.Invoke();
                downArea = ScreenArea.OutsideAll;
                return;
            }

            // Scroll area
            float distFromDown = (position - downPosition).magnitude;
            float duration = timeSec - downTime;
            bool isTap = !exceededMovementThreshold
                         && distFromDown < tapDistanceThresholdPx
                         && duration < tapTimeThresholdSec;

            if (isTap)
            {
                onTap?.Invoke(position);
            }
            else
            {
                // Final delta if there was movement
                if (exceededMovementThreshold)
                {
                    float deltaX = position.x - lastPosition.x;
                    if (Mathf.Abs(deltaX) > 0.0001f)
                        onScrollDragDelta?.Invoke(deltaX);
                }
                onScrollRelease?.Invoke();
            }

            downArea = ScreenArea.OutsideAll;
        }
    }
}
```

- [ ] **Step 4: Uruchom testy — verify pass**

Wszystkie zielone.

- [ ] **Step 5: Commit**

```bash
git add Assets/_Project/Scripts/Input/InputRouter.cs Assets/_Project/Scripts/Input/InputRouter.cs.meta Assets/_Project/Tests/EditMode/InputRouterTests.cs Assets/_Project/Tests/EditMode/InputRouterTests.cs.meta
git commit -m "feat(input): add InputRouter with gesture classification and tests"
```

---

## Task 7: Utwórz scenę `Main.unity` z hierarchią i ProBuilder zone stubami

**Files:**
- Create: `Assets/_Project/Scenes/Main.unity`
- Create: `Assets/_Project/Materials/ZoneStub_Zone1.mat`, `ZoneStub_Zone2.mat`, `ZoneStub_Zone3.mat`
- Create: `Assets/_Project/Prefabs/ZoneStub.prefab` (opcjonalnie — można też trzymać po prostu w scenie)

To jest task ręczny w Unity Editor. Każdy "step" to czynność w edytorze.

- [ ] **Step 1: Stwórz nową scenę**

Unity → `File → New Scene → Basic (URP)`. Zapisz jako `Assets/_Project/Scenes/Main.unity`.

- [ ] **Step 2: Ustaw scenę jako default w Build Settings**

Unity → `File → Build Profiles` (lub Build Settings) → Scenes In Build → Add Open Scenes. Powinna być jedyna scena na liście.

- [ ] **Step 3: Utwórz hierarchię root GameObjectów**

W oknie Hierarchy stwórz puste GameObjecty:
- `[Managers]` — pusty, root
- `[World]` — pusty, root, z dziećmi:
  - `Zone1` — pusty
  - `Zone2` — pusty
  - `Zone3` — pusty
- `[UI]` — Canvas (`UI → Canvas`), Render Mode = Screen Space - Overlay
- `[Camera]` — Main Camera (już istnieje z domyślnej sceny URP) — przesuń pod ten root

- [ ] **Step 4: Utwórz placeholder ProBuilder boxy dla każdej strefy**

Dla każdej strefy:
- Wybierz `Zone1` w Hierarchy.
- `Tools → ProBuilder → ProBuilder Window → New Shape → Cube`. Domyślne wymiary 1×1×1.
- Skaluj do 10×3×10 (X×Y×Z), pozycja `(0, 0, 0)` w stosunku do parenta `Zone1`.
- Powtórz dla `Zone2` (parent), pozycja worldX = 12.
- Powtórz dla `Zone3` (parent), pozycja worldX = 24.

(Odstęp 2 jednostki między strefami pozwala wizualnie rozróżnić granice; możesz dostosować w mockupie.)

- [ ] **Step 5: Utwórz materiały i przypisz**

W `Assets/_Project/Materials/` stwórz 3 nowe materiały (Right-click → Create → Material):
- `ZoneStub_Zone1.mat` — Albedo np. jasnoniebieski (`#7AB6E0`)
- `ZoneStub_Zone2.mat` — jasnozielony (`#7AE0B6`)
- `ZoneStub_Zone3.mat` — jasnopomarańczowy (`#E0B67A`)

Drag-and-drop każdy materiał na odpowiedni cube w scenie.

- [ ] **Step 6: Ustaw kamerę**

`[Camera] → Main Camera`:
- Position: `(12, 8, -15)` (worldX=12 to środek strefy 2, czyli środkowa)
- Rotation: `(15, 0, 0)` (lekkie nachylenie w dół)
- Field of View: `30`
- Projection: Perspective
- Background: Solid Color, jasnoszary `#CCCCCC`
- Clipping planes: Near `0.1`, Far `100`

- [ ] **Step 7: Sprawdź wizualnie**

Wciśnij Play. Widok powinien pokazywać 3 kolorowe boxy obok siebie (niebieski/zielony/pomarańczowy). Kamera nie rusza się jeszcze — to OK.

- [ ] **Step 8: Commit**

```bash
git add Assets/_Project/Scenes/ Assets/_Project/Materials/ Assets/ProjectSettings/EditorBuildSettings.asset
git commit -m "feat(scene): add Main scene with 3 zone stub boxes (ProBuilder) and camera setup"
```

---

## Task 8: `MainSceneBootstrap` — MonoBehaviour wiążący wszystkie systemy

Wpinamy nasze pure-C# klasy do scene MonoBehaviour który:
- Czyta inputy (touches / mouse — Input System)
- Inicjalizuje `JoystickArea`, `CameraScrollController`, `InputRouter`
- Aplikuje `CameraScrollController.TargetX` na transform kamery
- Renderuje joystick jako prosty UI (kropka centrum + kropka palca) gdy aktywny

**Files:**
- Create: `Assets/_Project/Scripts/Input/MainSceneBootstrap.cs`
- Create: `Assets/_Project/Scripts/Input/JoystickVisual.cs` (UI dla joysticka)
- Create: `Assets/_Project/Prefabs/JoystickHandle.prefab` (UI handle)

- [ ] **Step 1: Utwórz `JoystickVisual.cs`**

Plik: `Assets/_Project/Scripts/Input/JoystickVisual.cs`

```csharp
using UnityEngine;
using UnityEngine.UI;

namespace Project.Input
{
    /// <summary>
    /// Prosty wizualizator joysticka: 2 obrazki (centrum + handle) widoczne tylko gdy aktywny.
    /// Pozycje screen-space (px). Bind do JoystickArea.
    /// </summary>
    public class JoystickVisual : MonoBehaviour
    {
        [SerializeField] RectTransform centerHandle;
        [SerializeField] RectTransform thumbHandle;
        [SerializeField] CanvasGroup canvasGroup;

        public void Show(Vector2 screenCenter, Vector2 screenThumb)
        {
            canvasGroup.alpha = 1f;
            centerHandle.position = screenCenter;
            thumbHandle.position = screenThumb;
        }

        public void Hide()
        {
            canvasGroup.alpha = 0f;
        }
    }
}
```

- [ ] **Step 2: Utwórz `MainSceneBootstrap.cs`**

Plik: `Assets/_Project/Scripts/Input/MainSceneBootstrap.cs`

```csharp
using UnityEngine;
using UnityEngine.InputSystem;

namespace Project.Input
{
    /// <summary>
    /// Wiąże pure-logic klasy (JoystickArea, CameraScrollController, InputRouter)
    /// z Unity Input System i transformą kamery. Single-touch MVP.
    /// </summary>
    public class MainSceneBootstrap : MonoBehaviour
    {
        [Header("Camera")]
        [SerializeField] Camera mainCamera;
        [SerializeField] float minX = 0f;
        [SerializeField] float maxX = 24f;
        [SerializeField] float pixelsToWorld = 0.01f;
        [SerializeField] float rubberStrength = 0.5f;
        [SerializeField] float snapBackSpeed = 10f;

        [Header("Joystick")]
        [SerializeField] float joystickMaxRadiusPx = 100f;
        [SerializeField] JoystickVisual joystickVisual;

        [Header("Input thresholds")]
        [SerializeField] float tapDistancePx = 10f;
        [SerializeField] float tapTimeSec = 0.2f;

        JoystickArea joystick;
        CameraScrollController scroll;
        InputRouter router;

        bool pointerDown;
        int activePointerId = -1;

        void Awake()
        {
            joystick = new JoystickArea(joystickMaxRadiusPx);
            float startX = mainCamera != null ? mainCamera.transform.position.x : 12f;
            scroll = new CameraScrollController(
                minX, maxX, pixelsToWorld, rubberStrength, snapBackSpeed, startX);

            router = new InputRouter(
                screenSize: new Vector2(Screen.width, Screen.height),
                tapDistanceThresholdPx: tapDistancePx,
                tapTimeThresholdSec: tapTimeSec,
                onJoystickPress: p => joystick.OnPress(p),
                onJoystickDrag: p => joystick.OnDrag(p),
                onJoystickRelease: () => joystick.OnRelease(),
                onScrollDragDelta: d => scroll.OnDragDelta(d),
                onScrollRelease: () => scroll.OnRelease(),
                onTap: HandleTap);
        }

        void HandleTap(Vector2 screenPos)
        {
            // MVP step 1: nikt jeszcze nie reaguje na tap
            Debug.Log($"[Tap] {screenPos}");
        }

        void Update()
        {
            HandlePointerInput();

            scroll.Update(Time.deltaTime);
            ApplyCameraPosition();
            UpdateJoystickVisual();
        }

        void HandlePointerInput()
        {
            // Single-touch / mouse
            // Touchscreen.current dla mobilnych, Mouse.current jako fallback w editorze.
            Vector2? pointerPos = null;
            bool pressedThisFrame = false;
            bool releasedThisFrame = false;

            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
            {
                pointerPos = Touchscreen.current.primaryTouch.position.ReadValue();
                pressedThisFrame = Touchscreen.current.primaryTouch.press.wasPressedThisFrame;
            }
            else if (Mouse.current != null && Mouse.current.leftButton.isPressed)
            {
                pointerPos = Mouse.current.position.ReadValue();
                pressedThisFrame = Mouse.current.leftButton.wasPressedThisFrame;
            }

            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasReleasedThisFrame)
                releasedThisFrame = true;
            else if (Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame)
                releasedThisFrame = true;

            float now = Time.time;

            if (pressedThisFrame && pointerPos.HasValue)
            {
                pointerDown = true;
                router.OnPointerDown(pointerPos.Value, now);
            }
            else if (pointerDown && releasedThisFrame)
            {
                Vector2 lastPos = pointerPos ?? GetLastKnownPointerPos();
                router.OnPointerUp(lastPos, now);
                pointerDown = false;
            }
            else if (pointerDown && pointerPos.HasValue)
            {
                router.OnPointerMove(pointerPos.Value, now);
            }
        }

        Vector2 GetLastKnownPointerPos()
        {
            if (Touchscreen.current != null)
                return Touchscreen.current.primaryTouch.position.ReadValue();
            if (Mouse.current != null)
                return Mouse.current.position.ReadValue();
            return Vector2.zero;
        }

        void ApplyCameraPosition()
        {
            if (mainCamera == null) return;
            var pos = mainCamera.transform.position;
            pos.x = scroll.TargetX;
            mainCamera.transform.position = pos;
        }

        void UpdateJoystickVisual()
        {
            if (joystickVisual == null) return;

            if (joystick.IsActive)
            {
                Vector2 center = joystick.Center;
                Vector2 thumb = center + joystick.Output * joystickMaxRadiusPx;
                joystickVisual.Show(center, thumb);
            }
            else
            {
                joystickVisual.Hide();
            }
        }
    }
}
```

- [ ] **Step 3: Stwórz UI joysticka w scenie**

W `[UI]` Canvas:
- Stwórz pusty GameObject `JoystickRoot` (RectTransform anchored full screen)
- Pod nim stwórz 2 Image (`UI → Image`):
  - `Center` — obrazek koła, `RectTransform.sizeDelta = (60, 60)`, kolor półprzezroczysty biały
  - `Thumb` — obrazek koła, `sizeDelta = (40, 40)`, kolor biały
- Dodaj `CanvasGroup` na `JoystickRoot`
- Dodaj `JoystickVisual` MonoBehaviour na `JoystickRoot`, w inspectorze przypisz: `centerHandle = Center`, `thumbHandle = Thumb`, `canvasGroup = JoystickRoot.CanvasGroup`
- Set `CanvasGroup.alpha = 0` na start (joystick niewidoczny)

- [ ] **Step 4: Wpinanie `MainSceneBootstrap`**

W `[Managers]` GameObject dodaj komponent `MainSceneBootstrap`. W inspectorze:
- `Main Camera` = drag&drop main camera z hierarchii
- `Min X` = 0 (lewy koniec - środek strefy 1)
- `Max X` = 24 (prawy koniec - środek strefy 3)
- `Pixels To World` = 0.01
- `Rubber Strength` = 0.5
- `Snap Back Speed` = 10
- `Joystick Max Radius Px` = 100
- `Joystick Visual` = drag&drop `JoystickRoot` z UI
- `Tap Distance Px` = 10
- `Tap Time Sec` = 0.2

- [ ] **Step 5: Sprawdź wizualnie - playmode test**

Play. Test ręczny:
1. Wciśnij i przytrzymaj LMB w dolnej-lewej części Game view i przesuwaj — powinieneś widzieć rysowany joystick (2 kółka).
2. Puść — joystick znika.
3. Wciśnij i przesuń LMB w górnej części Game view → kamera się przesuwa po X.
4. Tap krótki w górnej części — Console powinno wypisać `[Tap] (x, y)`.
5. Po przesunięciu kamery poza granice (próbuj scrollować mocno) — powinno widocznie wracać po puszczeniu (rubber band snap).

Jeśli któryś krok nie działa — rebuild manualnie (Ctrl+Shift+R) i ponownie sprawdź. Jeśli błąd w konsoli — fix przed dalej.

- [ ] **Step 6: Commit**

```bash
git add Assets/_Project/Scripts/Input/MainSceneBootstrap.cs Assets/_Project/Scripts/Input/JoystickVisual.cs Assets/_Project/Scripts/Input/MainSceneBootstrap.cs.meta Assets/_Project/Scripts/Input/JoystickVisual.cs.meta Assets/_Project/Scenes/Main.unity
git commit -m "feat(input): wire MainSceneBootstrap, joystick UI, camera scroll in scene"
```

---

## Task 9: PlayMode smoke test — scena ładuje się i ma 3 strefy

**Files:**
- Create: `Assets/_Project/Tests/PlayMode/SceneSmokeTest.cs`

- [ ] **Step 1: Napisz test**

Plik: `Assets/_Project/Tests/PlayMode/SceneSmokeTest.cs`

```csharp
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Project.Tests.PlayMode
{
    public class SceneSmokeTest
    {
        const string ScenePath = "Assets/_Project/Scenes/Main.unity";

        [UnityTest]
        public IEnumerator MainScene_LoadsAndContainsThreeZones()
        {
            yield return SceneManager.LoadSceneAsync(ScenePath, LoadSceneMode.Single);

            var world = GameObject.Find("[World]");
            Assert.NotNull(world, "[World] root must exist in scene");

            var zone1 = world.transform.Find("Zone1");
            var zone2 = world.transform.Find("Zone2");
            var zone3 = world.transform.Find("Zone3");
            Assert.NotNull(zone1, "Zone1 missing");
            Assert.NotNull(zone2, "Zone2 missing");
            Assert.NotNull(zone3, "Zone3 missing");

            // Zone3 powinno mieć większy worldX niż Zone1 (poziomy układ)
            Assert.That(zone3.position.x, Is.GreaterThan(zone1.position.x));
        }

        [UnityTest]
        public IEnumerator MainScene_HasMainCameraAndBootstrap()
        {
            yield return SceneManager.LoadSceneAsync(ScenePath, LoadSceneMode.Single);

            var cam = Camera.main;
            Assert.NotNull(cam, "Main camera missing");

            var bootstrap = Object.FindFirstObjectByType<Project.Input.MainSceneBootstrap>();
            Assert.NotNull(bootstrap, "MainSceneBootstrap must be present");
        }
    }
}
```

- [ ] **Step 2: Uruchom test — verify pass**

Unity → `Test Runner → PlayMode → Run All`. Test powinien przejść (scena istnieje, ma elementy, bootstrap obecny).

- [ ] **Step 3: Commit**

```bash
git add Assets/_Project/Tests/PlayMode/SceneSmokeTest.cs Assets/_Project/Tests/PlayMode/SceneSmokeTest.cs.meta
git commit -m "test(scene): add PlayMode smoke test for Main scene"
```

---

## Task 10: Final manual playtest checklist + tag/release

- [ ] **Step 1: Uruchom wszystkie testy**

Unity → `Test Runner`:
- EditMode → Run All — wszystkie zielone
- PlayMode → Run All — wszystkie zielone

Jeśli któryś czerwony — fix przed dalej.

- [ ] **Step 2: Manualny playtest (mobile-like)**

Build na platformę Mac/Win (lub uruchom w editorze) i przetestuj:

✓ Joystick w dolnej lewej części reaguje na nacisk i przesunięcie palca.
✓ Joystick znika po puszczeniu.
✓ Joystick centrum = miejsce pierwszego dotyku (floating).
✓ Joystick clamp do max radius (nie wychodzi poza okrąg).
✓ Swipe w górnej części przesuwa kamerą po X.
✓ Kamera ma rubber band na granicach (zwalnia poza min/max).
✓ Po puszczeniu poza granicami kamera wraca.
✓ Tap krótki w górnej części → log "[Tap] (x, y)".
✓ Tap długi (>0.2s) → brak loga (poprawnie - hold).
✓ Drag krótki (<10px) i krótkotrwały → tap.
✓ Drag długi → kamera scroll, brak tap.
✓ Konsola czysta — żadnych runtime errors.

- [ ] **Step 3: Tag commitu jako MVP-Step1-done**

```bash
cd "/Users/jakubwolsza/Documents/Fruits&Juices"
git tag -a mvp-step1 -m "MVP Step 1: Camera + Input + Joystick + zones stub"
```

- [ ] **Step 4: Update spec status**

W pliku `docs/superpowers/specs/2026-04-29-fruits-juices-design.md` w sekcji 9 (MVP — kolejność implementacji) dopisz przy kroku 1: ✅ DONE 2026-XX-XX (uzupełnij datą).

```bash
git add docs/superpowers/specs/2026-04-29-fruits-juices-design.md
git commit -m "docs: mark MVP step 1 as done"
```

---

## Definicja "Done" dla Plan #1

Wszystkie poniższe muszą być spełnione przed przejściem do Plan #2:

- ✅ Wszystkie EditMode testy zielone (`GameBalanceSO`, `ScreenAreaUtils`, `JoystickArea`, `CameraScrollController`, `InputRouter`).
- ✅ PlayMode smoke test zielony.
- ✅ Manualny playtest checklist (Task 10 Step 2) wszystkie ✓.
- ✅ Konsola Unity bez błędów / warningów (poza ew. ostrzeżeniami od ProBuilder).
- ✅ Build kompiluje się bez błędów.
- ✅ Wszystkie commity wypchnięte (lokalnie — repo nie ma remote w MVP).

## Co NIE jest w Plan #1 (rezerwowane na kolejne plany)

- Spawn owoców na ścianie i sand-physics — **Plan #2**.
- Ciężarówki / conveyor / magnet — **Plan #3**.
- Duże butelki / racki — **Plan #4**.
- Gracz capsule + auto-pickup — **Plan #5**.
- Klienci + auto-deliver + monety — **Plan #6**.
- UI upgradów — **Plan #7**.
- Balans i polishing — **Plan #8**.
