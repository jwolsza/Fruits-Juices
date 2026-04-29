# MVP Step 2: Fruit Wall (Sand-Physics + Manual Refill) — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Strefa 1 — działająca ściana 300×300 z sand-physics i ręcznym refillem na przycisk. Gracz tappuje "Refill Wall" → ściana zapełnia się losowo od góry, owoce kaskadują w dół przez sand-physics, kończy się gdy wszystkie komórki zajęte. Wizualnie jako siatka 2D SpriteRendererów. Bez ciężarówek (Plan #3).

**Architecture:** Pure-logic `FruitGrid` (2D array `FruitType?`) + `SandPhysicsTick` (deterministyczny krok grawitacji) + `RefillController` (state machine: Idle/Refilling). Unity warstwa: `WallView` MonoBehaviour holduje grid SpriteRendererów i synchronizuje kolory z gridem; `Zone1Manager` orkiestruje, podpina HUD refill button. Eventy SO Channel na refill start/end (na potrzeby Plan #3 - pause ciężarówek).

**Tech Stack:** Unity 6.3 URP, SpriteRenderer / Sprite, ScriptableObject events.

**Plan numer:** 2/8 w sekwencji MVP. Spec: `docs/superpowers/specs/2026-04-29-fruits-juices-design.md` sekcje 3.1, 10. Branch: `feat/mvp-step2`.

---

## File Structure (do utworzenia / zmiany)

```
Assets/_Project/
├── Scripts/
│   ├── Data/
│   │   └── GameBalanceSO.cs                    [MODIFY] dodaj CellSizeWorldUnits, RefillTickRateHz, RefillSpawnsPerTick; zmień WallColumns/Rows na 300
│   ├── Core/
│   │   ├── BoolEventChannelSO.cs               [NEW] generyczny event channel SO bool (np. OnRefillingChanged)
│   │   └── BoolEventChannelSO.cs.meta          [Unity-generated]
│   └── Zone1_FruitWall/
│       ├── Project.Zone1.FruitWall.asmdef      [NEW] reference: Project.Core, Project.Data
│       ├── FruitGrid.cs                        [NEW] 2D array FruitType? + IsFull / GetCell / SetCell / Clear
│       ├── SandPhysicsTick.cs                  [NEW] pure-logic: jeden tick grawitacji, deterministyczny
│       ├── RefillController.cs                 [NEW] state machine sterujący batch refill
│       ├── WallView.cs                         [NEW] MonoBehaviour: instantiate sprite grid + sync from FruitGrid
│       └── Zone1Manager.cs                     [NEW] MonoBehaviour: ties FruitGrid + RefillController + WallView; subscribes refill button
├── Tests/EditMode/
│   ├── GameBalanceSOTests.cs                   [MODIFY] dopasuj asercje do nowych defaultów
│   ├── FruitGridTests.cs                       [NEW]
│   ├── SandPhysicsTickTests.cs                 [NEW]
│   └── RefillControllerTests.cs                [NEW]
└── Settings/
    ├── Events/
    │   └── OnRefillingChanged.asset            [NEW Unity asset, BoolEventChannelSO instance]
    └── GameBalance.asset                       [MODIFY] zaktualizuj wartości w inspectorze po zmianie pól w GameBalanceSO

Assets/_Project/Prefabs/
└── (opt.) FruitCellSprite.prefab               [NEW] prefab pojedynczego sprite renderera (jeśli potrzebny)
```

---

## Task 0: Update `GameBalanceSO` + jej testów

**Files:**
- Modify: `Assets/_Project/Scripts/Data/GameBalanceSO.cs`
- Modify: `Assets/_Project/Tests/EditMode/GameBalanceSOTests.cs`
- Modify: `Assets/_Project/Settings/GameBalance.asset` (po recompile w Unity, wymusić reset w inspectorze)

- [ ] **Step 1: Modify `GameBalanceSO.cs`** - usuń `FruitSpawnRateHz`, zmień defaulty `WallColumns/Rows` na 300, dodaj 3 nowe pola.

```csharp
// GameBalanceSO.cs - sekcja "Wall (sand-physics grid)"
[Header("Wall (sand-physics grid)")]
public int WallColumns = 300;
public int WallRows = 300;
public float CellSizeWorldUnits = 0.05f;
public float GravityRateHz = 10f;

[Header("Wall refill")]
public float RefillTickRateHz = 30f;
public int RefillSpawnsPerTick = 100;

// Usunąć FruitSpawnRateHz (nieużywane od Plan #2 wzwyż)
```

W metodzie `ResetToDefaults()` analogiczne zmiany: ustawić `WallColumns=300`, `WallRows=300`, dodać `CellSizeWorldUnits=0.05f`, `RefillTickRateHz=30f`, `RefillSpawnsPerTick=100`. Usunąć linię `FruitSpawnRateHz = 2f;`.

- [ ] **Step 2: Update `GameBalanceSOTests.cs`** - zmień asercje na nowe wartości

W teście `DefaultInstance_HasExpectedStartingValues`:
- `Assert.AreEqual(300, balance.WallColumns);` (było 1000)
- `Assert.AreEqual(300, balance.WallRows);` (było 1000)
- `Assert.AreEqual(0.05f, balance.CellSizeWorldUnits);` (NEW)
- `Assert.AreEqual(30f, balance.RefillTickRateHz);` (NEW)
- `Assert.AreEqual(100, balance.RefillSpawnsPerTick);` (NEW)
- USUŃ asercję `Assert.AreEqual(2f, balance.FruitSpawnRateHz);`

- [ ] **Step 3: Run EditMode tests in Unity** — wszystkie zielone (GameBalanceSO + reszta).

- [ ] **Step 4: Re-create `GameBalance.asset` (lub reset wartości)** w Unity:
- Otwórz `Assets/_Project/Settings/GameBalance.asset` w inspectorze
- Klikni "..." menu → "Reset" (lub usuń asset i utwórz nowy z menu Create → Project → GameBalance)
- Zweryfikuj że nowe pola mają wartości startowe

- [ ] **Step 5: Commit**

```bash
cd "/Users/jakubwolsza/Documents/Fruits&Juices"
git add Assets/_Project/Scripts/Data/GameBalanceSO.cs Assets/_Project/Tests/EditMode/GameBalanceSOTests.cs Assets/_Project/Settings/GameBalance.asset
git commit -m "feat(data): retune GameBalanceSO for 300x300 sprite wall with manual refill"
```

---

## Task 1: `FruitGrid` data layer (TDD)

Pure C# klasa - 2D `FruitType?` array, podstawowe operacje. Nie wykonuje sand-physics ani refill (te są osobno).

**Files:**
- Create: `Assets/_Project/Scripts/Zone1_FruitWall/Project.Zone1.FruitWall.asmdef`
- Create: `Assets/_Project/Scripts/Zone1_FruitWall/FruitGrid.cs`
- Create: `Assets/_Project/Tests/EditMode/FruitGridTests.cs`

- [ ] **Step 1: Utwórz `Project.Zone1.FruitWall.asmdef`**

```json
{
    "name": "Project.Zone1.FruitWall",
    "rootNamespace": "Project.Zone1.FruitWall",
    "references": [
        "Project.Core",
        "Project.Data"
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

- [ ] **Step 2: Update `Project.Tests.EditMode.asmdef` references**

Dodaj `"Project.Zone1.FruitWall"` do listy `references` w `Assets/_Project/Tests/EditMode/Project.Tests.EditMode.asmdef`.

- [ ] **Step 3: Napisz failing test `FruitGridTests.cs`**

```csharp
using NUnit.Framework;
using Project.Core;
using Project.Zone1.FruitWall;

namespace Project.Tests.EditMode
{
    public class FruitGridTests
    {
        [Test]
        public void NewGrid_AllCellsEmpty()
        {
            var grid = new FruitGrid(columns: 5, rows: 4);

            Assert.AreEqual(5, grid.Columns);
            Assert.AreEqual(4, grid.Rows);
            for (int x = 0; x < 5; x++)
                for (int y = 0; y < 4; y++)
                    Assert.IsNull(grid.GetCell(x, y), $"cell ({x},{y}) should be empty");

            Assert.IsTrue(grid.IsEmpty);
            Assert.IsFalse(grid.IsFull);
            Assert.AreEqual(0, grid.OccupiedCount);
        }

        [Test]
        public void SetCell_StoresValue_AndUpdatesCounters()
        {
            var grid = new FruitGrid(5, 4);
            grid.SetCell(2, 1, FruitType.Apple);

            Assert.AreEqual(FruitType.Apple, grid.GetCell(2, 1));
            Assert.AreEqual(1, grid.OccupiedCount);
            Assert.IsFalse(grid.IsEmpty);
            Assert.IsFalse(grid.IsFull);
        }

        [Test]
        public void ClearCell_RemovesValue()
        {
            var grid = new FruitGrid(3, 3);
            grid.SetCell(0, 0, FruitType.Lemon);
            grid.ClearCell(0, 0);

            Assert.IsNull(grid.GetCell(0, 0));
            Assert.AreEqual(0, grid.OccupiedCount);
        }

        [Test]
        public void IsFull_TrueWhenAllCellsOccupied()
        {
            var grid = new FruitGrid(2, 2);
            grid.SetCell(0, 0, FruitType.Apple);
            grid.SetCell(1, 0, FruitType.Apple);
            grid.SetCell(0, 1, FruitType.Orange);
            grid.SetCell(1, 1, FruitType.Lemon);

            Assert.IsTrue(grid.IsFull);
            Assert.AreEqual(4, grid.OccupiedCount);
        }

        [Test]
        public void OutOfBounds_GetCell_ReturnsNull()
        {
            var grid = new FruitGrid(3, 3);
            Assert.IsNull(grid.GetCell(-1, 0));
            Assert.IsNull(grid.GetCell(3, 0));
            Assert.IsNull(grid.GetCell(0, -1));
            Assert.IsNull(grid.GetCell(0, 3));
        }

        [Test]
        public void IsCellEmpty_HelperMethod()
        {
            var grid = new FruitGrid(3, 3);
            Assert.IsTrue(grid.IsCellEmpty(0, 0));
            grid.SetCell(0, 0, FruitType.Apple);
            Assert.IsFalse(grid.IsCellEmpty(0, 0));
            Assert.IsTrue(grid.IsCellEmpty(1, 1));
            // Out of bounds = NOT empty (treat as wall/blocked)
            Assert.IsFalse(grid.IsCellEmpty(-1, 0));
            Assert.IsFalse(grid.IsCellEmpty(3, 0));
        }
    }
}
```

- [ ] **Step 4: Run test → fails (compile error)**

- [ ] **Step 5: Implementuj `FruitGrid.cs`**

```csharp
using Project.Core;

namespace Project.Zone1.FruitWall
{
    /// <summary>
    /// 2D grid of optional FruitTypes. Pure data, no Unity deps. Indexed (x, y) where
    /// x ∈ [0, Columns), y ∈ [0, Rows). y = 0 is bottom row.
    /// </summary>
    public class FruitGrid
    {
        readonly FruitType?[,] cells;
        public int Columns { get; }
        public int Rows { get; }
        public int OccupiedCount { get; private set; }

        public bool IsEmpty => OccupiedCount == 0;
        public bool IsFull => OccupiedCount == Columns * Rows;

        public FruitGrid(int columns, int rows)
        {
            Columns = columns;
            Rows = rows;
            cells = new FruitType?[columns, rows];
        }

        public FruitType? GetCell(int x, int y)
        {
            if (x < 0 || x >= Columns || y < 0 || y >= Rows) return null;
            return cells[x, y];
        }

        public void SetCell(int x, int y, FruitType type)
        {
            if (x < 0 || x >= Columns || y < 0 || y >= Rows) return;
            if (cells[x, y] == null) OccupiedCount++;
            cells[x, y] = type;
        }

        public void ClearCell(int x, int y)
        {
            if (x < 0 || x >= Columns || y < 0 || y >= Rows) return;
            if (cells[x, y] != null) OccupiedCount--;
            cells[x, y] = null;
        }

        /// <summary>
        /// True jeśli komórka jest w bounds I pusta. Out-of-bounds = false (blocked).
        /// </summary>
        public bool IsCellEmpty(int x, int y)
        {
            if (x < 0 || x >= Columns || y < 0 || y >= Rows) return false;
            return cells[x, y] == null;
        }
    }
}
```

- [ ] **Step 6: Run tests → all pass**

- [ ] **Step 7: Commit**

```bash
git add Assets/_Project/Scripts/Zone1_FruitWall/Project.Zone1.FruitWall.asmdef Assets/_Project/Scripts/Zone1_FruitWall/FruitGrid.cs Assets/_Project/Tests/EditMode/FruitGridTests.cs Assets/_Project/Tests/EditMode/Project.Tests.EditMode.asmdef
git commit -m "feat(zone1): add FruitGrid data layer with tests"
```

---

## Task 2: `SandPhysicsTick` - jeden krok grawitacji (TDD)

Pure-logic. Bierze `FruitGrid` i wykonuje jeden tick: dla każdej zajętej komórki (od dołu w górę) próbuje przesunąć: prosto / skos lewo / skos prawo. Parzyste tikki mają preferencję lewy skos, nieparzyste prawy.

**Files:**
- Create: `Assets/_Project/Scripts/Zone1_FruitWall/SandPhysicsTick.cs`
- Create: `Assets/_Project/Tests/EditMode/SandPhysicsTickTests.cs`

- [ ] **Step 1: Napisz failing tests**

```csharp
using NUnit.Framework;
using Project.Core;
using Project.Zone1.FruitWall;

namespace Project.Tests.EditMode
{
    public class SandPhysicsTickTests
    {
        [Test]
        public void SingleFruit_AboveEmpty_FallsStraightDown()
        {
            var grid = new FruitGrid(3, 3);
            grid.SetCell(1, 2, FruitType.Apple);

            SandPhysicsTick.Step(grid, tickIndex: 0);

            Assert.IsNull(grid.GetCell(1, 2));
            Assert.AreEqual(FruitType.Apple, grid.GetCell(1, 1));
        }

        [Test]
        public void Fruit_OnTopOfPile_StaysWhenNoEmptyBelow()
        {
            var grid = new FruitGrid(3, 3);
            grid.SetCell(1, 0, FruitType.Lemon);
            grid.SetCell(1, 1, FruitType.Apple);

            SandPhysicsTick.Step(grid, tickIndex: 0);

            Assert.AreEqual(FruitType.Lemon, grid.GetCell(1, 0));
            Assert.AreEqual(FruitType.Apple, grid.GetCell(1, 1));
        }

        [Test]
        public void Fruit_OnEdgeOfPile_FallsDiagonallyLeft_TickEven()
        {
            // Pile: (1,0). Fruit on top right edge (1,1). Below-left (0,0) empty.
            var grid = new FruitGrid(3, 3);
            grid.SetCell(1, 0, FruitType.Lemon);
            grid.SetCell(1, 1, FruitType.Apple);

            SandPhysicsTick.Step(grid, tickIndex: 0); // even → prefers left

            // Apple from (1,1) should slide to (0,0) (down-left)
            Assert.IsNull(grid.GetCell(1, 1));
            Assert.AreEqual(FruitType.Apple, grid.GetCell(0, 0));
            Assert.AreEqual(FruitType.Lemon, grid.GetCell(1, 0));
        }

        [Test]
        public void Fruit_OnEdgeOfPile_FallsDiagonallyRight_TickOdd()
        {
            var grid = new FruitGrid(3, 3);
            grid.SetCell(1, 0, FruitType.Lemon);
            grid.SetCell(1, 1, FruitType.Apple);

            SandPhysicsTick.Step(grid, tickIndex: 1); // odd → prefers right

            // Apple from (1,1) should slide to (2,0) (down-right)
            Assert.IsNull(grid.GetCell(1, 1));
            Assert.AreEqual(FruitType.Apple, grid.GetCell(2, 0));
        }

        [Test]
        public void Fruit_BlockedAllSides_DoesNotMove()
        {
            // Pile fills bottom row, fruit on (1,1). Diagonals blocked too.
            var grid = new FruitGrid(3, 3);
            grid.SetCell(0, 0, FruitType.Lemon);
            grid.SetCell(1, 0, FruitType.Lemon);
            grid.SetCell(2, 0, FruitType.Lemon);
            grid.SetCell(1, 1, FruitType.Apple);

            SandPhysicsTick.Step(grid, tickIndex: 0);

            Assert.AreEqual(FruitType.Apple, grid.GetCell(1, 1));
        }

        [Test]
        public void Fruit_AtBottomRow_DoesNotMove()
        {
            var grid = new FruitGrid(3, 3);
            grid.SetCell(1, 0, FruitType.Apple);

            SandPhysicsTick.Step(grid, tickIndex: 0);

            Assert.AreEqual(FruitType.Apple, grid.GetCell(1, 0));
        }

        [Test]
        public void Fruit_OnLeftEdge_OnlyHasRightDiagonalAvailable()
        {
            // Fruit at (0,1), bottom (0,0) blocked, only down-right (1,0) free
            var grid = new FruitGrid(3, 3);
            grid.SetCell(0, 0, FruitType.Lemon);
            grid.SetCell(0, 1, FruitType.Apple);

            SandPhysicsTick.Step(grid, tickIndex: 0); // even prefers left, but left out-of-bounds

            // Should fall down-right to (1,0)
            Assert.IsNull(grid.GetCell(0, 1));
            Assert.AreEqual(FruitType.Apple, grid.GetCell(1, 0));
        }

        [Test]
        public void MultipleFruitsInColumn_BottomUpIteration_NoDoubleProcessing()
        {
            // Tower (0,0..2), all empty space at column 2. Sand should NOT move them all to column 2 in one tick.
            var grid = new FruitGrid(3, 3);
            grid.SetCell(0, 0, FruitType.Apple);
            grid.SetCell(0, 1, FruitType.Apple);
            grid.SetCell(0, 2, FruitType.Apple);

            SandPhysicsTick.Step(grid, tickIndex: 0); // even → left preferred, but left of (0,*) is out of bounds, so try right

            // Each apple should slide down-right one step. Bottom-up: (0,0) blocked (no row below). (0,1) goes to (1,0). (0,2) - now (0,1) empty, falls straight to (0,1).
            Assert.AreEqual(FruitType.Apple, grid.GetCell(0, 0));
            Assert.AreEqual(FruitType.Apple, grid.GetCell(0, 1)); // came from (0,2) straight down
            Assert.AreEqual(FruitType.Apple, grid.GetCell(1, 0)); // came from (0,1) down-right
        }
    }
}
```

- [ ] **Step 2: Run tests → fails**

- [ ] **Step 3: Implementuj `SandPhysicsTick.cs`**

```csharp
namespace Project.Zone1.FruitWall
{
    /// <summary>
    /// Pure-logic sand-physics step. One tick iterates bottom-up; each occupied cell
    /// tries to fall: straight-down, then diagonal toward preferred side, then other diagonal.
    /// Even tickIndex prefers left diagonal; odd prefers right (eliminates directional bias).
    /// </summary>
    public static class SandPhysicsTick
    {
        public static void Step(FruitGrid grid, int tickIndex)
        {
            bool preferLeft = (tickIndex % 2) == 0;

            // Iterate from bottom (y=0) to top.
            // y=0 cells can't fall (no row below); start at y=1.
            for (int y = 1; y < grid.Rows; y++)
            {
                for (int x = 0; x < grid.Columns; x++)
                {
                    var fruit = grid.GetCell(x, y);
                    if (fruit == null) continue;

                    // Try straight down
                    if (grid.IsCellEmpty(x, y - 1))
                    {
                        grid.ClearCell(x, y);
                        grid.SetCell(x, y - 1, fruit.Value);
                        continue;
                    }

                    // Try preferred diagonal first, then the other.
                    int firstDx = preferLeft ? -1 : +1;
                    int secondDx = -firstDx;

                    if (grid.IsCellEmpty(x + firstDx, y - 1))
                    {
                        grid.ClearCell(x, y);
                        grid.SetCell(x + firstDx, y - 1, fruit.Value);
                        continue;
                    }

                    if (grid.IsCellEmpty(x + secondDx, y - 1))
                    {
                        grid.ClearCell(x, y);
                        grid.SetCell(x + secondDx, y - 1, fruit.Value);
                        continue;
                    }
                }
            }
        }
    }
}
```

- [ ] **Step 4: Run tests → pass**

- [ ] **Step 5: Commit**

```bash
git add Assets/_Project/Scripts/Zone1_FruitWall/SandPhysicsTick.cs Assets/_Project/Tests/EditMode/SandPhysicsTickTests.cs
git commit -m "feat(zone1): add SandPhysicsTick for grid gravity simulation with tests"
```

---

## Task 3: `RefillController` - state machine batch refill (TDD)

Steruje stanem refilla. `Start()` przełącza na `Refilling`, każdy `Tick()` spawnuje N owoców w pustych komórkach top row spośród odblokowanych typów, potem zatrzymuje się gdy grid `IsFull`.

Wstrzykiwane: `IRandomSource` (testowalność).

**Files:**
- Create: `Assets/_Project/Scripts/Zone1_FruitWall/RefillController.cs`
- Create: `Assets/_Project/Tests/EditMode/RefillControllerTests.cs`

- [ ] **Step 1: Napisz failing tests**

```csharp
using System.Collections.Generic;
using NUnit.Framework;
using Project.Core;
using Project.Zone1.FruitWall;

namespace Project.Tests.EditMode
{
    public class RefillControllerTests
    {
        // Deterministic random: returns values from a queue.
        class FakeRandom : IRandomSource
        {
            readonly Queue<int> intQueue;
            public FakeRandom(params int[] values)
            {
                intQueue = new Queue<int>(values);
            }
            public int NextInt(int minInclusive, int maxExclusive)
            {
                if (intQueue.Count == 0)
                    return minInclusive;
                int v = intQueue.Dequeue();
                return minInclusive + (((v % (maxExclusive - minInclusive)) + (maxExclusive - minInclusive)) % (maxExclusive - minInclusive));
            }
        }

        FruitType[] DefaultPool() => new[] { FruitType.Apple, FruitType.Orange, FruitType.Lemon };

        [Test]
        public void NewController_IsIdle()
        {
            var grid = new FruitGrid(5, 5);
            var ctrl = new RefillController(grid, DefaultPool(), new FakeRandom(), spawnsPerTick: 10);
            Assert.IsFalse(ctrl.IsRefilling);
        }

        [Test]
        public void Start_PutsControllerInRefillingState()
        {
            var grid = new FruitGrid(5, 5);
            var ctrl = new RefillController(grid, DefaultPool(), new FakeRandom(), spawnsPerTick: 10);
            ctrl.Start();
            Assert.IsTrue(ctrl.IsRefilling);
        }

        [Test]
        public void Tick_NotStarted_Noop()
        {
            var grid = new FruitGrid(3, 3);
            var ctrl = new RefillController(grid, DefaultPool(), new FakeRandom(), spawnsPerTick: 5);
            ctrl.Tick();
            Assert.AreEqual(0, grid.OccupiedCount);
        }

        [Test]
        public void Tick_SpawnsInTopRowEmptyCells_UpToSpawnsPerTick()
        {
            var grid = new FruitGrid(5, 5);
            // Pre-fill some cells in top row to verify it picks empty ones
            grid.SetCell(0, 4, FruitType.Apple);
            grid.SetCell(2, 4, FruitType.Apple);

            // Random returns indices into "empty top row column" list. Empty top columns: [1, 3, 4]
            // Force selection: 0, 0, 0, 0, 0 (always pick first remaining empty)
            // Random for fruit type (3 types): 0, 0, 0, 0, 0 (always Apple)
            var random = new FakeRandom(0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
            var ctrl = new RefillController(grid, DefaultPool(), random, spawnsPerTick: 3);
            ctrl.Start();

            ctrl.Tick();

            // 3 of remaining empty top-row cells (1,3,4) should now be filled
            Assert.AreEqual(2 + 3, grid.OccupiedCount);
            // No more empty cells in top row
            for (int x = 0; x < 5; x++)
                Assert.IsNotNull(grid.GetCell(x, 4));
        }

        [Test]
        public void Tick_TopRowFull_NothingSpawned()
        {
            var grid = new FruitGrid(3, 3);
            for (int x = 0; x < 3; x++) grid.SetCell(x, 2, FruitType.Apple);

            var ctrl = new RefillController(grid, DefaultPool(), new FakeRandom(0), spawnsPerTick: 5);
            ctrl.Start();

            int before = grid.OccupiedCount;
            ctrl.Tick();
            Assert.AreEqual(before, grid.OccupiedCount, "no spawn possible if top row full");
        }

        [Test]
        public void Tick_GridFull_StopsRefilling()
        {
            var grid = new FruitGrid(2, 2);
            // Fill all but 1 in top row
            grid.SetCell(0, 0, FruitType.Apple);
            grid.SetCell(1, 0, FruitType.Apple);
            grid.SetCell(1, 1, FruitType.Apple);
            // (0,1) empty in top row

            var ctrl = new RefillController(grid, DefaultPool(), new FakeRandom(0, 0), spawnsPerTick: 5);
            ctrl.Start();
            ctrl.Tick();

            Assert.IsTrue(grid.IsFull);
            Assert.IsFalse(ctrl.IsRefilling, "should auto-stop when grid full");
        }

        [Test]
        public void Tick_UsesPoolForFruitType()
        {
            var grid = new FruitGrid(3, 1);
            // Random produces sequence: column-pick, fruit-pick, column-pick, fruit-pick, ...
            // For 3 spawns: indices 0,1,2 (columns) and 0,1,2 (fruit indices)
            var random = new FakeRandom(0, 0, 0, 1, 0, 2);
            var ctrl = new RefillController(grid, DefaultPool(), random, spawnsPerTick: 3);
            ctrl.Start();
            ctrl.Tick();

            Assert.AreEqual(3, grid.OccupiedCount);
            // Verify all cells have a value from the pool
            for (int x = 0; x < 3; x++)
            {
                var cell = grid.GetCell(x, 0);
                Assert.IsNotNull(cell);
                CollectionAssert.Contains(DefaultPool(), cell);
            }
        }
    }
}
```

- [ ] **Step 2: Run → fails**

- [ ] **Step 3: Implementuj `IRandomSource` + `RefillController.cs`**

Oba w jednym pliku dla zwięzłości:

```csharp
using System;
using System.Collections.Generic;
using Project.Core;

namespace Project.Zone1.FruitWall
{
    public interface IRandomSource
    {
        int NextInt(int minInclusive, int maxExclusive);
    }

    public class SystemRandomSource : IRandomSource
    {
        readonly Random rng;
        public SystemRandomSource(int? seed = null)
        {
            rng = seed.HasValue ? new Random(seed.Value) : new Random();
        }
        public int NextInt(int min, int max) => rng.Next(min, max);
    }

    /// <summary>
    /// Drives a manual batch refill of the wall. Start() puts controller in Refilling
    /// state; subsequent Tick() calls spawn fruits in random empty top-row cells until
    /// the grid is full.
    /// </summary>
    public class RefillController
    {
        readonly FruitGrid grid;
        readonly FruitType[] fruitPool;
        readonly IRandomSource random;
        readonly int spawnsPerTick;

        readonly List<int> emptyTopColumnsBuffer = new();

        public bool IsRefilling { get; private set; }

        public RefillController(FruitGrid grid, FruitType[] fruitPool, IRandomSource random, int spawnsPerTick)
        {
            this.grid = grid;
            this.fruitPool = fruitPool;
            this.random = random;
            this.spawnsPerTick = spawnsPerTick;
        }

        public void Start()
        {
            if (grid.IsFull) return;
            IsRefilling = true;
        }

        public void Stop()
        {
            IsRefilling = false;
        }

        public void Tick()
        {
            if (!IsRefilling) return;

            int topRowY = grid.Rows - 1;
            int spawned = 0;

            for (int attempt = 0; attempt < spawnsPerTick; attempt++)
            {
                emptyTopColumnsBuffer.Clear();
                for (int x = 0; x < grid.Columns; x++)
                {
                    if (grid.IsCellEmpty(x, topRowY))
                        emptyTopColumnsBuffer.Add(x);
                }

                if (emptyTopColumnsBuffer.Count == 0) break;
                if (fruitPool.Length == 0) break;

                int colIdx = random.NextInt(0, emptyTopColumnsBuffer.Count);
                int chosenX = emptyTopColumnsBuffer[colIdx];

                int fruitIdx = random.NextInt(0, fruitPool.Length);
                grid.SetCell(chosenX, topRowY, fruitPool[fruitIdx]);
                spawned++;
            }

            if (grid.IsFull)
                IsRefilling = false;
        }
    }
}
```

- [ ] **Step 4: Run tests → pass**

- [ ] **Step 5: Commit**

```bash
git add Assets/_Project/Scripts/Zone1_FruitWall/RefillController.cs Assets/_Project/Tests/EditMode/RefillControllerTests.cs
git commit -m "feat(zone1): add RefillController with IRandomSource and tests"
```

---

## Task 4: `BoolEventChannelSO` (refilling pause signal for trucks later)

**Files:**
- Create: `Assets/_Project/Scripts/Core/BoolEventChannelSO.cs`

- [ ] **Step 1: Implementuj `BoolEventChannelSO.cs`**

```csharp
using System;
using UnityEngine;

namespace Project.Core
{
    [CreateAssetMenu(fileName = "OnBoolChanged", menuName = "Project/Events/Bool Event Channel")]
    public class BoolEventChannelSO : ScriptableObject
    {
        public event Action<bool> Raised;
        public bool LastValue { get; private set; }

        public void Raise(bool value)
        {
            LastValue = value;
            Raised?.Invoke(value);
        }
    }
}
```

- [ ] **Step 2: W Unity Editor utwórz instancję**

`Assets/_Project/Settings/Events/` (utwórz folder jeśli nie ma) → prawym → Create → Project → Events → Bool Event Channel → nazwij `OnRefillingChanged.asset`.

- [ ] **Step 3: Commit**

```bash
git add Assets/_Project/Scripts/Core/BoolEventChannelSO.cs Assets/_Project/Settings/Events/
git commit -m "feat(core): add BoolEventChannelSO for cross-system bool signals (e.g. IsRefilling)"
```

---

## Task 5: `WallView` - sprite-based rendering grid 300x300

MonoBehaviour. Przy `Awake` instantiates `Columns × Rows` `SpriteRenderer` GameObjects ułożonych w grid. Każdy sprite to prosty kwadrat (Sprite assigned w inspectorze, np. Unity built-in `Square` lub własny). Co `LateUpdate` synchronizuje kolory: dla każdego sprite'a porównuje aktualny kolor z FruitGrid cellem; jeśli się różni, ustawia.

Mapping FruitType → Color jest w SO `FruitColorPaletteSO` lub po prostu hardcode w WallView na razie.

**Files:**
- Create: `Assets/_Project/Scripts/Zone1_FruitWall/WallView.cs`
- Create: `Assets/_Project/Scripts/Zone1_FruitWall/FruitColorPalette.cs` (helper static)

- [ ] **Step 1: Update `Project.Zone1.FruitWall.asmdef`**

Aby pliki MonoBehaviour mogły używać UnityEngine: nic nie trzeba bo asmdef z noEngineReferences=false (default).

- [ ] **Step 2: Implementuj `FruitColorPalette.cs`**

```csharp
using UnityEngine;
using Project.Core;

namespace Project.Zone1.FruitWall
{
    public static class FruitColorPalette
    {
        public static Color GetColor(FruitType type)
        {
            switch (type)
            {
                case FruitType.Apple:      return new Color(0.85f, 0.10f, 0.10f); // red
                case FruitType.Orange:     return new Color(1.00f, 0.55f, 0.00f); // orange
                case FruitType.Lemon:      return new Color(0.95f, 0.90f, 0.20f); // yellow
                case FruitType.Strawberry: return new Color(0.95f, 0.30f, 0.55f);
                case FruitType.Grape:      return new Color(0.55f, 0.20f, 0.75f);
                case FruitType.Banana:     return new Color(0.95f, 0.80f, 0.30f);
                case FruitType.Kiwi:       return new Color(0.55f, 0.75f, 0.30f);
                case FruitType.Pineapple:  return new Color(0.95f, 0.85f, 0.45f);
                case FruitType.Watermelon: return new Color(0.40f, 0.75f, 0.45f);
                case FruitType.Mango:      return new Color(0.95f, 0.60f, 0.20f);
                default: return Color.gray;
            }
        }

        public static readonly Color EmptyColor = new Color(0.10f, 0.10f, 0.10f, 0.0f); // transparent
    }
}
```

- [ ] **Step 3: Implementuj `WallView.cs`**

```csharp
using UnityEngine;
using Project.Core;

namespace Project.Zone1.FruitWall
{
    /// <summary>
    /// Renderuje FruitGrid jako siatkę SpriteRendererów. Każda komórka = jeden GameObject
    /// z SpriteRenderer. Cache koloru na SpriteRenderer eliminuje niepotrzebne writes.
    /// </summary>
    public class WallView : MonoBehaviour
    {
        [Header("Visual")]
        [SerializeField] Sprite cellSprite;          // Unity built-in "Square" (Sprites/Square) lub custom
        [SerializeField] float cellSize = 0.05f;     // jednostek świata na komórkę
        [SerializeField] string sortingLayerName = "Default";
        [SerializeField] int sortingOrderBase = 0;

        FruitGrid grid;
        SpriteRenderer[,] cellRenderers;
        Color[,] lastColors;

        public void Initialize(FruitGrid grid)
        {
            this.grid = grid;
            cellRenderers = new SpriteRenderer[grid.Columns, grid.Rows];
            lastColors = new Color[grid.Columns, grid.Rows];

            // Origin: bottom-left of wall at this transform position.
            for (int x = 0; x < grid.Columns; x++)
            {
                for (int y = 0; y < grid.Rows; y++)
                {
                    var go = new GameObject($"Cell_{x}_{y}");
                    go.transform.SetParent(transform, worldPositionStays: false);
                    go.transform.localPosition = new Vector3(
                        x * cellSize + cellSize * 0.5f,
                        y * cellSize + cellSize * 0.5f,
                        0f);
                    go.transform.localScale = Vector3.one * cellSize;

                    var sr = go.AddComponent<SpriteRenderer>();
                    sr.sprite = cellSprite;
                    sr.color = FruitColorPalette.EmptyColor;
                    sr.sortingLayerName = sortingLayerName;
                    sr.sortingOrder = sortingOrderBase;

                    cellRenderers[x, y] = sr;
                    lastColors[x, y] = sr.color;
                }
            }
        }

        void LateUpdate()
        {
            if (grid == null || cellRenderers == null) return;

            for (int x = 0; x < grid.Columns; x++)
            {
                for (int y = 0; y < grid.Rows; y++)
                {
                    var cell = grid.GetCell(x, y);
                    Color desired = cell.HasValue
                        ? FruitColorPalette.GetColor(cell.Value)
                        : FruitColorPalette.EmptyColor;

                    if (lastColors[x, y] != desired)
                    {
                        cellRenderers[x, y].color = desired;
                        lastColors[x, y] = desired;
                    }
                }
            }
        }
    }
}
```

- [ ] **Step 4: Sanity check** - Unity should compile without errors. Brak unit testów w MVP (test wizualny w playmode wystarczy).

- [ ] **Step 5: Commit**

```bash
git add Assets/_Project/Scripts/Zone1_FruitWall/WallView.cs Assets/_Project/Scripts/Zone1_FruitWall/FruitColorPalette.cs
git commit -m "feat(zone1): add WallView sprite-grid renderer with color cache"
```

---

## Task 6: `Zone1Manager` - orkiestrator strefy 1

MonoBehaviour który tworzy `FruitGrid`, `RefillController`, podpina `WallView`, eksponuje publiczne `StartRefill()` (do podpięcia z UI buttona), tickuje sand-physics co `1/GravityRateHz` i refill co `1/RefillTickRateHz`.

**Files:**
- Create: `Assets/_Project/Scripts/Zone1_FruitWall/Zone1Manager.cs`

- [ ] **Step 1: Implementuj `Zone1Manager.cs`**

```csharp
using UnityEngine;
using Project.Core;
using Project.Data;

namespace Project.Zone1.FruitWall
{
    public class Zone1Manager : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] GameBalanceSO balance;
        [SerializeField] WallView wallView;

        [Header("Events")]
        [SerializeField] BoolEventChannelSO onRefillingChanged;

        FruitGrid grid;
        RefillController refill;
        SystemRandomSource rng;

        float gravityAccumulator;
        float refillAccumulator;
        int gravityTickIndex;

        void Awake()
        {
            if (balance == null)
            {
                Debug.LogError("[Zone1Manager] GameBalanceSO not assigned");
                enabled = false;
                return;
            }
            if (wallView == null)
            {
                Debug.LogError("[Zone1Manager] WallView not assigned");
                enabled = false;
                return;
            }

            grid = new FruitGrid(balance.WallColumns, balance.WallRows);
            rng = new SystemRandomSource();
            refill = new RefillController(
                grid,
                balance.StartingFruitTypes,
                rng,
                balance.RefillSpawnsPerTick);

            wallView.Initialize(grid);
        }

        void Update()
        {
            float dt = Time.deltaTime;

            // Sand-physics tick.
            float gravityInterval = 1f / Mathf.Max(0.01f, balance.GravityRateHz);
            gravityAccumulator += dt;
            while (gravityAccumulator >= gravityInterval)
            {
                gravityAccumulator -= gravityInterval;
                SandPhysicsTick.Step(grid, gravityTickIndex);
                gravityTickIndex++;
            }

            // Refill tick (only when active).
            if (refill.IsRefilling)
            {
                float refillInterval = 1f / Mathf.Max(0.01f, balance.RefillTickRateHz);
                refillAccumulator += dt;
                while (refillAccumulator >= refillInterval)
                {
                    refillAccumulator -= refillInterval;
                    bool wasRefilling = refill.IsRefilling;
                    refill.Tick();
                    if (wasRefilling && !refill.IsRefilling)
                        EmitRefillingChanged(false);
                }
            }
            else
            {
                refillAccumulator = 0f;
            }
        }

        public void StartRefill()
        {
            if (refill.IsRefilling || grid.IsFull) return;
            refill.Start();
            EmitRefillingChanged(true);
        }

        void EmitRefillingChanged(bool isRefilling)
        {
            onRefillingChanged?.Raise(isRefilling);
        }
    }
}
```

- [ ] **Step 2: Sanity check** w Unity (compile).

- [ ] **Step 3: Commit**

```bash
git add Assets/_Project/Scripts/Zone1_FruitWall/Zone1Manager.cs
git commit -m "feat(zone1): add Zone1Manager orchestrator with sand+refill ticking"
```

---

## Task 7: HUD Refill Button

W scenie `Main.unity` dodaj UI button w `[UI]` Canvas, podepnij do `Zone1Manager.StartRefill()`.

**Files:**
- Modify: `Assets/_Project/Scenes/Main.unity` (manualne w Unity Editor)

- [ ] **Step 1: W `[UI]` Canvas stwórz Button**

`[UI]` → prawym → UI → Button - TextMeshPro (lub legacy Button - wybierz). Nazwij `RefillWallButton`.
Anchor: top-left ekranu (np. position (100, -50) z anchor topLeft).
Size: ~200×60.
Tekst: "Refill Wall".

- [ ] **Step 2: Podepnij OnClick**

Z hierarchii Inspector → Button → On Click() → przeciągnij `[Zone1Manager]` GameObject (utworzysz go w Task 8) → wybierz funkcję `Zone1Manager.StartRefill()`.

(Krok wykonasz po Task 8 wzwyż gdy Zone1Manager będzie w scenie.)

- [ ] **Step 3: Optional - opcja "disable while refilling"**

W przyszłości - Zone1Manager subscribuje OnRefillingChanged i toggluje interactable buttona. Pomijamy dla MVP.

---

## Task 8: Wiring Zone 1 do Main scene

**Files:**
- Modify: `Assets/_Project/Scenes/Main.unity` (manualne)
- (Stworzysz instancję BoolEventChannelSO w Task 4 step 2 jeśli jeszcze nie zrobione)

- [ ] **Step 1: Pod `[World] → Zone1` GameObject dodaj child**

`Zone1` → prawym → Create Empty → nazwij `Wall`. Pozycja:
- localPosition: dostosuj tak żeby siatka 300×300 cellsize=0.05 = 15×15 jednostek była wycentrowana w Zone 1 (lub w widocznym kadrze kamery). Np. `(-7.5, -7.5, 0)` żeby grid rozciągał się 0..15 w X i Y od pivota.
- Dodaj komponent `WallView`. W inspectorze:
  - `Cell Sprite` = drag built-in Unity sprite "Square" (Project window → search "Square" wewnątrz Sprites/UI default lub stwórz Sprite z białej tekstury)
  - `Cell Size` = 0.05
  - `Sorting Layer Name` = "Default"
  - `Sorting Order Base` = 0

> Tip: W URP 2D użyj domyślnego "Square" sprite. Jeśli go brak, w Project window: Create → 2D → Sprites → Square.

- [ ] **Step 2: Pod `Zone1` dodaj GameObject `[Zone1Manager]`**

Zone1 → prawym → Create Empty → nazwij `[Zone1Manager]`. Dodaj komponent `Zone1Manager`. W inspectorze:
- `Balance` = drag&drop `Assets/_Project/Settings/GameBalance.asset`
- `Wall View` = drag&drop child `Wall` z hierarchii
- `On Refilling Changed` = drag&drop `Assets/_Project/Settings/Events/OnRefillingChanged.asset`

- [ ] **Step 3: Dokończ wiring buttona z Task 7**

Wybierz `RefillWallButton` w hierarchii. W komponencie Button: Add OnClick handler → przeciągnij `[Zone1Manager]` → wybierz `Zone1Manager → StartRefill()`.

- [ ] **Step 4: Sprawdź kameę i widoczność ściany**

Wall jest 2D na płaszczyźnie XY (Z=0). Strefa 1 ma worldX=0. Kamera startuje w worldX=12. Żeby zobaczyć ścianę, scrollnij kamerą do strefy 1 (swipe w prawo / drag right). Przy starcie kamera nie pokazuje strefy 1 - to OK.

Alternatywnie: zmień default startX kamery w `MainSceneBootstrap` na `0` (środek strefy 1) jeśli chcesz domyślnego widoku ściany.

- [ ] **Step 5: Commit**

```bash
git add Assets/_Project/Scenes/Main.unity
git commit -m "feat(scene): wire Zone1 wall + Zone1Manager + Refill button in Main scene"
```

---

## Task 9: Manual playtest + tag

- [ ] **Step 1: Otwórz scenę i wciśnij Play**

- [ ] **Step 2: Test refilla**

- Scrolluj kamerą do strefy 1 (drag w prawo) — zobacz ścianę 15×15 (300×300 sprite'ów małych) na środku.
- Tappuj "Refill Wall" button.
- Obserwuj: ściana wypełnia się od góry, owoce kaskadują w dół, kolory różne (3 typy startowe: czerwony jabłko, pomarańczowy, żółty cytryna).
- Po chwili (kilka sekund przy `RefillSpawnsPerTick=100`, `RefillTickRateHz=30`, grid 90000 cells → ~30 sekund teoretycznie) ściana całkowicie pełna.

> Jeśli refill jest zbyt wolny lub szybki, podkręć `RefillSpawnsPerTick` w GameBalance.asset.
> Jeśli sand-physics wygląda dziwnie, zwiększ `GravityRateHz`.

- [ ] **Step 3: Test sand-physics manualnie (opcjonalnie)**

Chwilowo w `Zone1Manager` dodaj test code (nie commituj):
```csharp
void Update() { ... istnejący kod ...
    if (UnityEngine.InputSystem.Keyboard.current?.spaceKey.wasPressedThisFrame == true)
    {
        // Wyczyść środek pełnej ściany - obserwuj jak owoce sypią się
        for (int y = 100; y < 200; y++)
            for (int x = 100; x < 200; x++)
                grid.ClearCell(x, y);
    }
}
```
Gdy ściana pełna i naciśniesz Spację, ubytek w środku, sand-physics powinien zasypać go materiałem z góry. Po teście usuń ten kod.

- [ ] **Step 4: Test EditMode + PlayMode**

Test Runner → EditMode → Run All. Wszystkie zielone (FruitGrid 6 testów + SandPhysicsTick 8 testów + RefillController 7 testów + dotychczasowe 27 testów = ~48).

PlayMode SceneSmokeTest też zielony.

- [ ] **Step 5: Wydajność (opcjonalnie)**

Stats window w Game view: monitoruj FPS przy pełnej ścianie. 90000 SpriteRenderer GameObject powinno być akceptowalne na desktop. Jeśli FPS spada na mobile, optymalizacja w przyszłości (np. shader rendering grida jako jedna mesh).

- [ ] **Step 6: Tag**

```bash
cd "/Users/jakubwolsza/Documents/Fruits&Juices"
git tag -a mvp-step2 -m "MVP Step 2: Fruit Wall sand-physics + manual refill"
```

- [ ] **Step 7: Update spec status**

W `docs/superpowers/specs/2026-04-29-fruits-juices-design.md` sekcja 9 (MVP) zaznacz krok 2 jako ✅ DONE.

---

## Definicja Done dla Plan #2

- ✅ EditMode tests wszystkie zielone (~48 testów).
- ✅ PlayMode SceneSmokeTest zielony.
- ✅ Refill button działa: tap → ściana wypełnia się; po wypełnieniu button przestaje cokolwiek robić (idle).
- ✅ Sand-physics widoczny: gdy zostawisz dziurę w pełnej ścianie, owoce sypią się.
- ✅ FPS akceptowalny na docelowej platformie (>30 fps na desktop, sprawdź samodzielnie w Game view stats).
- ✅ Branch `feat/mvp-step2` mergowany do `main`, tag `mvp-step2` ustawiony.

## Out of Plan #2 (na później)

- Ciężarówki opróżniające ścianę — Plan #3.
- Sloty pod ścianą + magnet → Plan #3.
- Garaż + dispatch trucks → Plan #3.
- Dynamic camera framing strefy 1 (start camera centered on Zone 1 podczas refilla, etc.) → polishing.
- Optymalizacja: 90000 SpriteRendererów może być wolne — alternatywa: SpriteAtlas + jeden mesh + Texture2D z fruit type per pixel + custom shader. Jeśli wydajność nie wystarczy, plan optimization w jednym z kolejnych planów.
