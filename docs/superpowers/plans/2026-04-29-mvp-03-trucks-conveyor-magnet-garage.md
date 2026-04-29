# MVP Step 3: Trucks + Conveyor + Magnet + Garage — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Strefa 1 wzbogacona o ciężarówki. Gracz tappuje ciężarówkę w garażu (przed ścianą, pod kamerą) → wjeżdża na conveyor (widoczny tor pętlący pod ścianą) → zbiera owoce magnetem z bottom row gridu w 3 aktywnych slotach → pełna jedzie z toru → na razie wraca do garażu (Plan #4 zastąpi to wizytą u dużej butelki). Conveyor pauzuje gdy `OnRefillingChanged.LastValue == true`.

**Architecture:** Pure-logic warstwa (`Truck`, `TruckStateMachine`, `ConveyorTrack` z waypointami, `WallSlot`, `MagnetSystem`, `Garage`) testowalna unitowo. Unity warstwa: `TruckView` (ProBuilder cab+paka+koła), `ConveyorView` (widoczny tor — line renderer lub ProBuilder taśma), `GarageView` (parking + tap pickup), `Zone1TrucksManager` (orkiestrator strefy + integracja z `Zone1Manager` z Plan #2). Subscribe `OnRefillingChanged` → conveyor pause.

**Tech Stack:** Unity 6.3 URP, ProBuilder, SpriteRenderer (z Plan #2), Physics raycast (tap detection).

**Plan numer:** 3/8 w sekwencji MVP. Spec: `docs/superpowers/specs/2026-04-29-fruits-juices-design.md` sekcje 3.2–3.7. Branch: `feat/mvp-step3` (utworzysz po merge'u step2).

---

## File Structure

```
Assets/_Project/
├── Scripts/
│   └── Zone1_Trucks/
│       ├── Project.Zone1.Trucks.asmdef           [NEW] ref: Project.Core, Project.Data, Project.Zone1.FruitWall
│       ├── TruckState.cs                          [NEW] enum: Idle, OnConveyor, Collecting, Full, ReturningToGarage
│       ├── Truck.cs                               [NEW] data: FruitType color, capacity, current load, state
│       ├── ConveyorWaypoint.cs                    [NEW] position + isActiveSlot bool
│       ├── ConveyorTrack.cs                      [NEW] list of waypoints, MoveAlong with formation, Pause/Resume
│       ├── WallSlot.cs                            [NEW] position + slotIndex (0,1,2 — slot 3 is "stop slot")
│       ├── MagnetSystem.cs                       [NEW] per-tick: assign fruits from wall bottom row to trucks at slots
│       ├── Garage.cs                              [NEW] list of parked Truck refs, Dispatch(truckId)
│       ├── TruckView.cs                           [NEW] MonoBehaviour: holds Truck ref, syncs transform from Truck position
│       ├── ConveyorView.cs                       [NEW] MonoBehaviour: builds visible track from waypoints
│       ├── GarageView.cs                          [NEW] MonoBehaviour: lays out parked TruckViews + raycast tap detection
│       └── Zone1TrucksManager.cs                  [NEW] orchestrator: ticks conveyor + magnet, owns Garage, integrates with Zone1Manager
└── Tests/EditMode/
    ├── TruckStateMachineTests.cs                  [NEW]
    ├── ConveyorTrackTests.cs                      [NEW]
    ├── MagnetSystemTests.cs                       [NEW]
    └── GarageTests.cs                             [NEW]

Assets/_Project/Tests/EditMode/Project.Tests.EditMode.asmdef    [MODIFY] add "Project.Zone1.Trucks" reference

Assets/_Project/Scenes/Main.unity                  [MODIFY] add Zone1.Trucks GameObjects
Assets/_Project/Settings/GameBalance.asset         [no changes needed; existing TruckCapacity, ConveyorSlotCount, MagnetRateHz used]
```

---

## Task 0: Branch + asmdef setup

- [ ] **Step 1: Create feature branch from current main (after step2 merged)**

```bash
cd "/Users/jakubwolsza/Documents/Fruits&Juices"
git checkout main
git merge --no-ff feat/mvp-step2 -m "merge: feat/mvp-step2 (MVP Step 2: Fruit Wall sand-physics + manual refill)"
git checkout -b feat/mvp-step3
```

- [ ] **Step 2: Create folder + asmdef**

```bash
mkdir -p "/Users/jakubwolsza/Documents/Fruits&Juices/Assets/_Project/Scripts/Zone1_Trucks"
```

`Assets/_Project/Scripts/Zone1_Trucks/Project.Zone1.Trucks.asmdef`:

```json
{
    "name": "Project.Zone1.Trucks",
    "rootNamespace": "Project.Zone1.Trucks",
    "references": [
        "Project.Core",
        "Project.Data",
        "Project.Zone1.FruitWall"
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

- [ ] **Step 3: Update test asmdef references**

W `Assets/_Project/Tests/EditMode/Project.Tests.EditMode.asmdef` dodaj `"Project.Zone1.Trucks"` do `references`.

- [ ] **Step 4: Commit**

```bash
git add Assets/_Project/Scripts/Zone1_Trucks/Project.Zone1.Trucks.asmdef Assets/_Project/Tests/EditMode/Project.Tests.EditMode.asmdef
git commit -m "chore: scaffold Project.Zone1.Trucks asmdef"
```

---

## Task 1: `TruckState` enum + `Truck` data class

**Files:**
- Create: `Assets/_Project/Scripts/Zone1_Trucks/TruckState.cs`
- Create: `Assets/_Project/Scripts/Zone1_Trucks/Truck.cs`

- [ ] **Step 1: `TruckState.cs`**

```csharp
namespace Project.Zone1.Trucks
{
    public enum TruckState
    {
        InGarage,
        EnteringConveyor,
        OnConveyor,
        StoppedAtSlot,
        Full,
        ReturningToGarage,
    }
}
```

- [ ] **Step 2: `Truck.cs`**

```csharp
using Project.Core;

namespace Project.Zone1.Trucks
{
    public class Truck
    {
        public int Id { get; }
        public FruitType FruitColor { get; }
        public int Capacity { get; private set; }
        public int Load { get; private set; }
        public TruckState State { get; set; } = TruckState.InGarage;

        // Position along the conveyor track parameterized as [0..1] (loop). Owned by ConveyorTrack.
        public float TrackPosition { get; set; }

        public bool IsFull => Load >= Capacity;

        public Truck(int id, FruitType color, int capacity)
        {
            Id = id;
            FruitColor = color;
            Capacity = capacity;
            Load = 0;
        }

        public void AddFruit() { if (Load < Capacity) Load++; }
        public void EmptyLoad() { Load = 0; }
        public void SetCapacity(int newCapacity) { Capacity = newCapacity; }
    }
}
```

Brak testów dla samego `Truck` (trivial getters/setters). Testy w state machine i magnet system go pokryją.

- [ ] **Step 3: Commit**

```bash
git add Assets/_Project/Scripts/Zone1_Trucks/TruckState.cs Assets/_Project/Scripts/Zone1_Trucks/Truck.cs
git commit -m "feat(zone1): add TruckState enum and Truck data class"
```

---

## Task 2: `ConveyorWaypoint` + `ConveyorTrack` (TDD)

`ConveyorTrack` przechowuje listę waypointów i porusza ciężarówkami w formacji. Każda ciężarówka ma `TrackPosition ∈ [0..1)`. Track ma metodę `Tick(deltaTime, baseSpeed)` która przesuwa wszystkie ciężarówki o `baseSpeed * deltaTime / TrackLength`. Ale jeśli któraś ciężarówka jest w stanie `StoppedAtSlot`, to wszystkie ZA NIĄ (idące do tej ciężarówki) zatrzymują się. Ciężarówki PRZED nią (po active slot 3) jadą dalej.

Dla MVP: prosta lista waypointów `Vector3[]` ułożonych w pętlę. `GetWorldPositionAtTrackParam(float t)` interpoluje liniowo między waypointami.

**Files:**
- Create: `Assets/_Project/Scripts/Zone1_Trucks/ConveyorWaypoint.cs`
- Create: `Assets/_Project/Scripts/Zone1_Trucks/ConveyorTrack.cs`
- Create: `Assets/_Project/Tests/EditMode/ConveyorTrackTests.cs`

- [ ] **Step 1: `ConveyorWaypoint.cs`**

```csharp
using UnityEngine;

namespace Project.Zone1.Trucks
{
    [System.Serializable]
    public struct ConveyorWaypoint
    {
        public Vector3 Position;
        public bool IsActiveSlot;     // true for the 3 slots under the wall
        public int SlotIndex;         // -1 for non-slot waypoints, 0/1/2 for slots
    }
}
```

- [ ] **Step 2: Tests `ConveyorTrackTests.cs`**

```csharp
using NUnit.Framework;
using UnityEngine;
using Project.Zone1.Trucks;
using Project.Core;
using System.Collections.Generic;

namespace Project.Tests.EditMode
{
    public class ConveyorTrackTests
    {
        ConveyorTrack BuildSimpleSquareTrack()
        {
            var waypoints = new List<ConveyorWaypoint>
            {
                new() { Position = new Vector3(0, 0, 0), IsActiveSlot = false, SlotIndex = -1 },
                new() { Position = new Vector3(10, 0, 0), IsActiveSlot = false, SlotIndex = -1 },
                new() { Position = new Vector3(10, 0, 10), IsActiveSlot = false, SlotIndex = -1 },
                new() { Position = new Vector3(0, 0, 10), IsActiveSlot = false, SlotIndex = -1 },
            };
            return new ConveyorTrack(waypoints);
        }

        [Test]
        public void GetWorldPositionAtTrackParam_AtZero_ReturnsFirstWaypoint()
        {
            var t = BuildSimpleSquareTrack();
            var pos = t.GetWorldPositionAtTrackParam(0f);
            Assert.That(pos, Is.EqualTo(new Vector3(0, 0, 0)));
        }

        [Test]
        public void GetWorldPositionAtTrackParam_AtQuarter_OnFirstSegment()
        {
            // Total length = 4 * 10 = 40. Quarter = position 10 along path = (10,0,0) — second waypoint.
            var t = BuildSimpleSquareTrack();
            var pos = t.GetWorldPositionAtTrackParam(0.25f);
            Assert.That(pos.x, Is.EqualTo(10f).Within(0.001f));
            Assert.That(pos.z, Is.EqualTo(0f).Within(0.001f));
        }

        [Test]
        public void GetWorldPositionAtTrackParam_LoopsAtOne()
        {
            var t = BuildSimpleSquareTrack();
            var posZero = t.GetWorldPositionAtTrackParam(0f);
            var posOne = t.GetWorldPositionAtTrackParam(1f);
            Assert.That(posOne, Is.EqualTo(posZero));
        }

        [Test]
        public void Tick_AdvancesAllTrucks_WhenNoneStopped()
        {
            var track = BuildSimpleSquareTrack();
            var truck1 = new Truck(1, FruitType.Apple, 100);
            truck1.TrackPosition = 0f;
            truck1.State = TruckState.OnConveyor;

            var truck2 = new Truck(2, FruitType.Orange, 100);
            truck2.TrackPosition = 0.25f;
            truck2.State = TruckState.OnConveyor;

            track.Tick(new[] { truck1, truck2 }, deltaTime: 1f, speedUnitsPerSec: 4f); // advance 4 units, total length 40 → 0.1

            Assert.That(truck1.TrackPosition, Is.EqualTo(0.1f).Within(0.001f));
            Assert.That(truck2.TrackPosition, Is.EqualTo(0.35f).Within(0.001f));
        }

        [Test]
        public void Tick_TruckStoppedAtSlot_TrucksBehindFreeze()
        {
            // Stopped truck at TrackPosition=0.5; truck behind at 0.4 should NOT advance past 0.5 (formation).
            // Trucks ahead (>0.5) advance.
            var track = BuildSimpleSquareTrack();
            var stopped = new Truck(1, FruitType.Apple, 100);
            stopped.TrackPosition = 0.5f;
            stopped.State = TruckState.StoppedAtSlot;

            var behind = new Truck(2, FruitType.Apple, 100);
            behind.TrackPosition = 0.45f;
            behind.State = TruckState.OnConveyor;

            var ahead = new Truck(3, FruitType.Apple, 100);
            ahead.TrackPosition = 0.6f;
            ahead.State = TruckState.OnConveyor;

            track.Tick(new[] { stopped, behind, ahead }, deltaTime: 1f, speedUnitsPerSec: 4f);

            Assert.That(stopped.TrackPosition, Is.EqualTo(0.5f).Within(0.001f), "stopped stays");
            Assert.That(behind.TrackPosition, Is.LessThanOrEqualTo(0.5f), "behind cannot pass stopped");
            Assert.That(ahead.TrackPosition, Is.EqualTo(0.7f).Within(0.001f), "ahead advances normally");
        }
    }
}
```

- [ ] **Step 3: Implementuj `ConveyorTrack.cs`**

```csharp
using System.Collections.Generic;
using UnityEngine;

namespace Project.Zone1.Trucks
{
    public class ConveyorTrack
    {
        readonly List<ConveyorWaypoint> waypoints;
        readonly float[] segmentLengths;
        readonly float totalLength;

        public IReadOnlyList<ConveyorWaypoint> Waypoints => waypoints;
        public float TotalLength => totalLength;

        public ConveyorTrack(IList<ConveyorWaypoint> waypoints)
        {
            this.waypoints = new List<ConveyorWaypoint>(waypoints);
            int n = this.waypoints.Count;
            segmentLengths = new float[n];
            float total = 0f;
            for (int i = 0; i < n; i++)
            {
                int next = (i + 1) % n;
                float len = Vector3.Distance(this.waypoints[i].Position, this.waypoints[next].Position);
                segmentLengths[i] = len;
                total += len;
            }
            totalLength = total;
        }

        public Vector3 GetWorldPositionAtTrackParam(float t)
        {
            if (totalLength <= 0f) return waypoints.Count > 0 ? waypoints[0].Position : Vector3.zero;
            t = ((t % 1f) + 1f) % 1f; // wrap [0..1)
            float distance = t * totalLength;
            for (int i = 0; i < segmentLengths.Length; i++)
            {
                if (distance <= segmentLengths[i])
                {
                    int next = (i + 1) % waypoints.Count;
                    float frac = segmentLengths[i] > 0f ? distance / segmentLengths[i] : 0f;
                    return Vector3.Lerp(waypoints[i].Position, waypoints[next].Position, frac);
                }
                distance -= segmentLengths[i];
            }
            return waypoints[0].Position;
        }

        /// <summary>
        /// Advance all trucks. A truck in StoppedAtSlot state holds its position;
        /// trucks immediately behind it (within "formation") freeze at the stopped truck's position.
        /// Trucks ahead of the stopped one advance freely.
        /// </summary>
        public void Tick(IReadOnlyList<Truck> trucks, float deltaTime, float speedUnitsPerSec)
        {
            if (trucks.Count == 0 || totalLength <= 0f) return;

            float deltaParam = (speedUnitsPerSec * deltaTime) / totalLength;

            // Collect TrackPositions of stopped trucks for formation check.
            var stoppedPositions = new List<float>();
            foreach (var t in trucks)
                if (t.State == TruckState.StoppedAtSlot)
                    stoppedPositions.Add(t.TrackPosition);

            foreach (var truck in trucks)
            {
                if (truck.State == TruckState.StoppedAtSlot) continue;
                if (truck.State == TruckState.InGarage) continue;
                if (truck.State == TruckState.ReturningToGarage) continue;
                // OnConveyor / EnteringConveyor / Full: advance, but check formation against stopped trucks ahead.

                float current = truck.TrackPosition;
                float desired = current + deltaParam;

                // Find nearest stopped truck ahead (mod 1).
                float minBlocker = float.PositiveInfinity;
                foreach (float sp in stoppedPositions)
                {
                    float distAhead = (sp - current + 1f) % 1f;
                    if (distAhead > 0f && distAhead < minBlocker)
                        minBlocker = distAhead;
                }

                if (minBlocker < float.PositiveInfinity && deltaParam >= minBlocker)
                {
                    // Clamp behind the blocker (small epsilon to avoid overlap).
                    desired = current + minBlocker - 0.001f;
                }

                truck.TrackPosition = ((desired % 1f) + 1f) % 1f;
            }
        }
    }
}
```

- [ ] **Step 4: Run tests → all pass**

- [ ] **Step 5: Commit**

```bash
git add Assets/_Project/Scripts/Zone1_Trucks/ConveyorWaypoint.cs Assets/_Project/Scripts/Zone1_Trucks/ConveyorTrack.cs Assets/_Project/Tests/EditMode/ConveyorTrackTests.cs
git commit -m "feat(zone1): add ConveyorTrack with waypoint loop and formation logic + tests"
```

---

## Task 3: `WallSlot` + `MagnetSystem` (TDD)

3 sloty parallel — każdy jest "active spot" w którym ciężarówka się zatrzymuje (slot 3 = ostatni, ten powoduje pause). Slot ma pozycję world i `slotIndex`. `MagnetSystem` co tick: dla każdej ciężarówki w slocie znajduje najbliższy pasujący kolor w bottom row gridu, usuwa z gridu, dodaje load do ciężarówki, emituje event animacji.

**Files:**
- Create: `Assets/_Project/Scripts/Zone1_Trucks/WallSlot.cs`
- Create: `Assets/_Project/Scripts/Zone1_Trucks/MagnetSystem.cs`
- Create: `Assets/_Project/Tests/EditMode/MagnetSystemTests.cs`

- [ ] **Step 1: `WallSlot.cs`**

```csharp
using UnityEngine;

namespace Project.Zone1.Trucks
{
    [System.Serializable]
    public struct WallSlot
    {
        public Vector3 WorldPosition;
        public int SlotIndex;        // 0..2
        public bool IsStopSlot;      // true for the last slot (slot 2 by default) where trucks pause
    }
}
```

- [ ] **Step 2: Tests `MagnetSystemTests.cs`**

```csharp
using NUnit.Framework;
using UnityEngine;
using Project.Core;
using Project.Zone1.FruitWall;
using Project.Zone1.Trucks;

namespace Project.Tests.EditMode
{
    public class MagnetSystemTests
    {
        [Test]
        public void Magnet_WithMatchingFruitInBottomRow_RemovesFromGridAndIncrementsLoad()
        {
            var grid = new FruitGrid(10, 10);
            grid.SetCell(5, 0, FruitType.Apple);

            var truck = new Truck(1, FruitType.Apple, 100);
            truck.State = TruckState.StoppedAtSlot;

            var assignments = MagnetSystem.AssignFruitsToTrucksAtSlots(
                grid,
                trucksAtSlots: new[] { (truck, slotWorldX: 0f) },
                wallLeftX: 0f,
                wallWidth: 10f);

            Assert.AreEqual(1, assignments.Count);
            Assert.AreEqual(1, truck.Load);
            Assert.IsNull(grid.GetCell(5, 0), "fruit removed from grid");
        }

        [Test]
        public void Magnet_NoMatchingFruit_NoAssignment()
        {
            var grid = new FruitGrid(10, 10);
            grid.SetCell(5, 0, FruitType.Orange);

            var truck = new Truck(1, FruitType.Apple, 100);
            truck.State = TruckState.StoppedAtSlot;

            var assignments = MagnetSystem.AssignFruitsToTrucksAtSlots(
                grid,
                trucksAtSlots: new[] { (truck, slotWorldX: 0f) },
                wallLeftX: 0f,
                wallWidth: 10f);

            Assert.AreEqual(0, assignments.Count);
            Assert.AreEqual(0, truck.Load);
            Assert.AreEqual(FruitType.Orange, grid.GetCell(5, 0), "fruit still in grid");
        }

        [Test]
        public void Magnet_FullTruck_NoAssignment()
        {
            var grid = new FruitGrid(10, 10);
            grid.SetCell(5, 0, FruitType.Apple);

            var truck = new Truck(1, FruitType.Apple, 1);
            truck.AddFruit(); // already at capacity 1
            truck.State = TruckState.StoppedAtSlot;

            var assignments = MagnetSystem.AssignFruitsToTrucksAtSlots(
                grid,
                trucksAtSlots: new[] { (truck, slotWorldX: 0f) },
                wallLeftX: 0f,
                wallWidth: 10f);

            Assert.AreEqual(0, assignments.Count);
            Assert.IsNotNull(grid.GetCell(5, 0));
        }

        [Test]
        public void Magnet_MultipleTrucks_AssignsClosestFruitPerTruckByX()
        {
            var grid = new FruitGrid(10, 10);
            grid.SetCell(2, 0, FruitType.Apple);
            grid.SetCell(8, 0, FruitType.Apple);

            var truckLeft = new Truck(1, FruitType.Apple, 100);
            truckLeft.State = TruckState.StoppedAtSlot;
            var truckRight = new Truck(2, FruitType.Apple, 100);
            truckRight.State = TruckState.StoppedAtSlot;

            // wallLeftX=0, wallWidth=10 → cell x=2 at worldX=2.5 (cell center), cell x=8 at worldX=8.5.
            // truckLeft at slotWorldX=2 → closest fruit is x=2 (worldX 2.5). truckRight at slotWorldX=8 → x=8.
            var assignments = MagnetSystem.AssignFruitsToTrucksAtSlots(
                grid,
                trucksAtSlots: new[] { (truckLeft, 2f), (truckRight, 8f) },
                wallLeftX: 0f,
                wallWidth: 10f);

            Assert.AreEqual(2, assignments.Count);
            Assert.AreEqual(1, truckLeft.Load);
            Assert.AreEqual(1, truckRight.Load);
            Assert.IsTrue(grid.IsCellEmpty(2, 0));
            Assert.IsTrue(grid.IsCellEmpty(8, 0));
        }

        [Test]
        public void Magnet_TwoTrucksSameColor_SecondGetsNextNearest()
        {
            var grid = new FruitGrid(10, 10);
            grid.SetCell(4, 0, FruitType.Apple);
            grid.SetCell(6, 0, FruitType.Apple);

            var truckA = new Truck(1, FruitType.Apple, 100);
            truckA.State = TruckState.StoppedAtSlot;
            var truckB = new Truck(2, FruitType.Apple, 100);
            truckB.State = TruckState.StoppedAtSlot;

            // Both trucks same color, both at the same slot worldX.
            // First truck takes nearest, second takes next-nearest.
            var assignments = MagnetSystem.AssignFruitsToTrucksAtSlots(
                grid,
                trucksAtSlots: new[] { (truckA, 5f), (truckB, 5f) },
                wallLeftX: 0f,
                wallWidth: 10f);

            Assert.AreEqual(2, assignments.Count);
            Assert.AreEqual(1, truckA.Load);
            Assert.AreEqual(1, truckB.Load);
        }
    }
}
```

- [ ] **Step 3: Implementuj `MagnetSystem.cs`**

```csharp
using System.Collections.Generic;
using UnityEngine;
using Project.Core;
using Project.Zone1.FruitWall;

namespace Project.Zone1.Trucks
{
    public struct MagnetAssignment
    {
        public Truck Truck;
        public Vector2Int GridCellRemoved;
        public FruitType FruitType;
        public Vector3 FruitWorldPosition;
    }

    public static class MagnetSystem
    {
        /// <summary>
        /// For each truck at a slot, find the nearest matching-color fruit in the bottom row of the grid
        /// (by world X distance). Assign 1 fruit per call per truck, remove it from grid, increment truck Load.
        /// Returns list of assignments (for view-side animation triggering).
        /// </summary>
        public static List<MagnetAssignment> AssignFruitsToTrucksAtSlots(
            FruitGrid grid,
            IReadOnlyList<(Truck truck, float slotWorldX)> trucksAtSlots,
            float wallLeftX,
            float wallWidth)
        {
            var result = new List<MagnetAssignment>();
            if (grid == null || trucksAtSlots == null || trucksAtSlots.Count == 0) return result;
            if (grid.Columns <= 0) return result;

            float cellWidth = wallWidth / grid.Columns;

            // Build mutable list of available bottom-row fruits (cellX → FruitType).
            var available = new List<(int cellX, FruitType type)>();
            for (int x = 0; x < grid.Columns; x++)
            {
                var cell = grid.GetCell(x, 0);
                if (cell.HasValue) available.Add((x, cell.Value));
            }

            foreach (var (truck, slotWorldX) in trucksAtSlots)
            {
                if (truck.IsFull) continue;

                int bestIdx = -1;
                float bestDist = float.PositiveInfinity;
                for (int i = 0; i < available.Count; i++)
                {
                    var (cellX, type) = available[i];
                    if (type != truck.FruitColor) continue;
                    float worldX = wallLeftX + cellX * cellWidth + cellWidth * 0.5f;
                    float dist = Mathf.Abs(worldX - slotWorldX);
                    if (dist < bestDist)
                    {
                        bestDist = dist;
                        bestIdx = i;
                    }
                }

                if (bestIdx >= 0)
                {
                    var (cellX, type) = available[bestIdx];
                    grid.ClearCell(cellX, 0);
                    truck.AddFruit();
                    available.RemoveAt(bestIdx);

                    result.Add(new MagnetAssignment
                    {
                        Truck = truck,
                        GridCellRemoved = new Vector2Int(cellX, 0),
                        FruitType = type,
                        FruitWorldPosition = new Vector3(
                            wallLeftX + cellX * cellWidth + cellWidth * 0.5f,
                            0f, // caller fills in actual Y from wallBottomY
                            0f),
                    });
                }
            }

            return result;
        }
    }
}
```

- [ ] **Step 4: Run tests → all pass**

- [ ] **Step 5: Commit**

```bash
git add Assets/_Project/Scripts/Zone1_Trucks/WallSlot.cs Assets/_Project/Scripts/Zone1_Trucks/MagnetSystem.cs Assets/_Project/Tests/EditMode/MagnetSystemTests.cs
git commit -m "feat(zone1): add WallSlot and MagnetSystem with closest-X assignment + tests"
```

---

## Task 4: `Garage` (TDD)

Trzyma listę ciężarówek (per kolor — 1 startowa per kolor). Ma metodę `Dispatch(int truckId)` która próbuje wpuścić ciężarówkę na conveyor (jeśli wolne miejsce na torze). Limit ciężarówek na conveyorze = `ConveyorSlotCount` z `GameBalanceSO`.

**Files:**
- Create: `Assets/_Project/Scripts/Zone1_Trucks/Garage.cs`
- Create: `Assets/_Project/Tests/EditMode/GarageTests.cs`

- [ ] **Step 1: Tests**

```csharp
using NUnit.Framework;
using Project.Core;
using Project.Zone1.Trucks;

namespace Project.Tests.EditMode
{
    public class GarageTests
    {
        [Test]
        public void NewGarage_WithStarterTrucks_AllInGarageState()
        {
            var garage = new Garage(maxOnConveyor: 4);
            var t1 = new Truck(1, FruitType.Apple, 100);
            var t2 = new Truck(2, FruitType.Orange, 100);
            garage.AddStarterTruck(t1);
            garage.AddStarterTruck(t2);

            Assert.AreEqual(2, garage.TruckCount);
            Assert.AreEqual(0, garage.OnConveyorCount);
            Assert.AreEqual(TruckState.InGarage, t1.State);
            Assert.AreEqual(TruckState.InGarage, t2.State);
        }

        [Test]
        public void Dispatch_FromGarage_ChangesStateToEnteringConveyor()
        {
            var garage = new Garage(maxOnConveyor: 4);
            var t = new Truck(1, FruitType.Apple, 100);
            garage.AddStarterTruck(t);

            bool ok = garage.Dispatch(1);

            Assert.IsTrue(ok);
            Assert.AreEqual(TruckState.EnteringConveyor, t.State);
            Assert.AreEqual(0f, t.TrackPosition);
            Assert.AreEqual(1, garage.OnConveyorCount);
        }

        [Test]
        public void Dispatch_ConveyorFull_ReturnsFalse()
        {
            var garage = new Garage(maxOnConveyor: 1);
            var t1 = new Truck(1, FruitType.Apple, 100);
            var t2 = new Truck(2, FruitType.Apple, 100);
            garage.AddStarterTruck(t1);
            garage.AddStarterTruck(t2);

            Assert.IsTrue(garage.Dispatch(1));
            Assert.IsFalse(garage.Dispatch(2), "conveyor full");
            Assert.AreEqual(TruckState.InGarage, t2.State);
        }

        [Test]
        public void Dispatch_TruckNotInGarage_ReturnsFalse()
        {
            var garage = new Garage(maxOnConveyor: 4);
            var t = new Truck(1, FruitType.Apple, 100);
            garage.AddStarterTruck(t);

            Assert.IsTrue(garage.Dispatch(1));
            Assert.IsFalse(garage.Dispatch(1), "already on conveyor");
        }

        [Test]
        public void ReturnToGarage_RestoresState()
        {
            var garage = new Garage(maxOnConveyor: 4);
            var t = new Truck(1, FruitType.Apple, 100);
            garage.AddStarterTruck(t);

            garage.Dispatch(1);
            t.AddFruit();
            t.State = TruckState.ReturningToGarage;

            garage.ReturnToGarage(1);

            Assert.AreEqual(TruckState.InGarage, t.State);
            Assert.AreEqual(0, t.Load);
            Assert.AreEqual(0, garage.OnConveyorCount);
        }
    }
}
```

- [ ] **Step 2: Implementuj `Garage.cs`**

```csharp
using System.Collections.Generic;

namespace Project.Zone1.Trucks
{
    public class Garage
    {
        readonly Dictionary<int, Truck> trucksById = new();
        readonly HashSet<int> onConveyorIds = new();
        public int MaxOnConveyor { get; }

        public int TruckCount => trucksById.Count;
        public int OnConveyorCount => onConveyorIds.Count;
        public IReadOnlyDictionary<int, Truck> TrucksById => trucksById;

        public Garage(int maxOnConveyor)
        {
            MaxOnConveyor = maxOnConveyor;
        }

        public void AddStarterTruck(Truck truck)
        {
            trucksById[truck.Id] = truck;
            truck.State = TruckState.InGarage;
        }

        public bool Dispatch(int truckId)
        {
            if (!trucksById.TryGetValue(truckId, out var truck)) return false;
            if (truck.State != TruckState.InGarage) return false;
            if (onConveyorIds.Count >= MaxOnConveyor) return false;

            truck.State = TruckState.EnteringConveyor;
            truck.TrackPosition = 0f;
            onConveyorIds.Add(truckId);
            return true;
        }

        public void ReturnToGarage(int truckId)
        {
            if (!trucksById.TryGetValue(truckId, out var truck)) return;
            truck.State = TruckState.InGarage;
            truck.EmptyLoad();
            onConveyorIds.Remove(truckId);
        }
    }
}
```

- [ ] **Step 3: Run tests → pass**

- [ ] **Step 4: Commit**

```bash
git add Assets/_Project/Scripts/Zone1_Trucks/Garage.cs Assets/_Project/Tests/EditMode/GarageTests.cs
git commit -m "feat(zone1): add Garage with dispatch and return-to-garage logic + tests"
```

---

## Task 5: `TruckView` (3D ProBuilder model + position sync)

MonoBehaviour. Ma referencję do `Truck` (data). Co frame: ustawia transform.position na `track.GetWorldPositionAtTrackParam(truck.TrackPosition)` lub na garage parking position. Również orientuje się tangencjalnie wzdłuż toru (forward = pochodna pozycji wzdłuż t).

Wizualizacja: child GameObjects z ProBuilder cubes:
- `Cab` (mała kostka 0.5×0.5×0.5)
- `Box` (większa kostka 1.5×0.8×1.0 z materiałem koloru `FruitType`)
- 4 `Wheels` (spłaszczone walce 0.3×0.1×0.3)

**Files:**
- Create: `Assets/_Project/Scripts/Zone1_Trucks/TruckView.cs`

- [ ] **Step 1: `TruckView.cs`**

```csharp
using UnityEngine;
using Project.Zone1.FruitWall;

namespace Project.Zone1.Trucks
{
    public class TruckView : MonoBehaviour
    {
        [SerializeField] Renderer boxRenderer; // assign in inspector — paka do colorowania

        Truck truck;
        ConveyorTrack track;
        Vector3 garageParkPosition;
        Material boxMaterial;

        public void Bind(Truck truck, ConveyorTrack track, Vector3 garageParkPosition)
        {
            this.truck = truck;
            this.track = track;
            this.garageParkPosition = garageParkPosition;
            ApplyColor();
            UpdateTransform();
        }

        void ApplyColor()
        {
            if (boxRenderer == null) return;
            if (boxMaterial == null)
            {
                boxMaterial = new Material(boxRenderer.sharedMaterial);
                boxRenderer.material = boxMaterial;
            }
            boxMaterial.color = FruitColorPalette.GetColor(truck.FruitColor);
        }

        public void UpdateTransform()
        {
            if (truck == null) return;

            switch (truck.State)
            {
                case TruckState.InGarage:
                    transform.position = garageParkPosition;
                    break;
                case TruckState.ReturningToGarage:
                    // Lerp toward garage; for MVP — instant teleport in Tick() of Zone1TrucksManager
                    transform.position = garageParkPosition;
                    break;
                default:
                    if (track != null)
                    {
                        transform.position = track.GetWorldPositionAtTrackParam(truck.TrackPosition);
                        // Tangent for orientation
                        float dt = 0.001f;
                        Vector3 ahead = track.GetWorldPositionAtTrackParam(
                            (truck.TrackPosition + dt) % 1f);
                        Vector3 forward = (ahead - transform.position).normalized;
                        if (forward != Vector3.zero)
                            transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
                    }
                    break;
            }
        }

        void LateUpdate()
        {
            UpdateTransform();
        }
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add Assets/_Project/Scripts/Zone1_Trucks/TruckView.cs
git commit -m "feat(zone1): add TruckView 3D MonoBehaviour with track-position sync"
```

---

## Task 6: `ConveyorView` — widoczny tor

LineRenderer rysujący zamkniętą pętlę z waypointów. Materiał metalowy szary, stała szerokość ~0.3 jednostek.

**Files:**
- Create: `Assets/_Project/Scripts/Zone1_Trucks/ConveyorView.cs`

- [ ] **Step 1: `ConveyorView.cs`**

```csharp
using UnityEngine;
using System.Collections.Generic;

namespace Project.Zone1.Trucks
{
    [RequireComponent(typeof(LineRenderer))]
    public class ConveyorView : MonoBehaviour
    {
        [SerializeField] float lineWidth = 0.3f;
        [SerializeField] Color lineColor = new(0.4f, 0.4f, 0.45f);

        public void Build(IReadOnlyList<ConveyorWaypoint> waypoints)
        {
            var lr = GetComponent<LineRenderer>();
            lr.useWorldSpace = false;
            lr.loop = true;
            lr.startWidth = lineWidth;
            lr.endWidth = lineWidth;
            lr.startColor = lineColor;
            lr.endColor = lineColor;

            var points = new Vector3[waypoints.Count];
            for (int i = 0; i < waypoints.Count; i++)
                points[i] = transform.InverseTransformPoint(waypoints[i].Position);

            lr.positionCount = points.Length;
            lr.SetPositions(points);
        }
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add Assets/_Project/Scripts/Zone1_Trucks/ConveyorView.cs
git commit -m "feat(zone1): add ConveyorView (LineRenderer-based visible track)"
```

---

## Task 7: `GarageView` — parking + tap detection

Layouts truck views w grid w garage area. Na każdą ciężarówkę raycasts dla tap.

**Files:**
- Create: `Assets/_Project/Scripts/Zone1_Trucks/GarageView.cs`

- [ ] **Step 1: `GarageView.cs`**

```csharp
using UnityEngine;
using System;
using System.Collections.Generic;

namespace Project.Zone1.Trucks
{
    public class GarageView : MonoBehaviour
    {
        [Tooltip("Local positions where parked trucks are placed, indexed by parking slot.")]
        [SerializeField] Vector3[] parkingSlots;

        readonly Dictionary<int, TruckView> truckViewsById = new();
        readonly List<int> orderedTruckIds = new();

        public Vector3 GetParkPositionFor(int truckId)
        {
            int idx = orderedTruckIds.IndexOf(truckId);
            if (idx < 0 || parkingSlots == null || parkingSlots.Length == 0) return transform.position;
            int slotIdx = idx % parkingSlots.Length;
            return transform.TransformPoint(parkingSlots[slotIdx]);
        }

        public void RegisterTruckView(int truckId, TruckView view)
        {
            truckViewsById[truckId] = view;
            if (!orderedTruckIds.Contains(truckId)) orderedTruckIds.Add(truckId);
        }

        /// <summary>
        /// Try to detect a truck under the given screen tap position via 3D raycast.
        /// Returns truck ID if hit, otherwise -1.
        /// </summary>
        public int TryGetTappedTruckId(Camera cam, Vector2 screenPos)
        {
            if (cam == null) return -1;
            Ray ray = cam.ScreenPointToRay(screenPos);
            if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
            {
                var view = hit.collider.GetComponentInParent<TruckView>();
                if (view != null)
                {
                    foreach (var kv in truckViewsById)
                        if (kv.Value == view) return kv.Key;
                }
            }
            return -1;
        }
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add Assets/_Project/Scripts/Zone1_Trucks/GarageView.cs
git commit -m "feat(zone1): add GarageView with parking layout and tap raycast"
```

---

## Task 8: `Zone1TrucksManager` — orkiestrator

MonoBehaviour. Przy `Awake` (odpalany po `Zone1Manager` w Plan #2):
- Czyta `GameBalanceSO` (TruckCapacity, ConveyorSlotCount, MagnetRateHz)
- Subskrybuje `OnRefillingChanged` event channel — pause conveyor przy refilling=true
- Buduje conveyor track z waypointów (serializowane w inspectorze)
- Tworzy 3 startowe ciężarówki (Apple, Orange, Lemon), instancjonuje TruckView prefaby
- Tworzy garaż, paruje ciężarówki w GarageView slotach
- Konfiguruje subscriber dla input router tap → GarageView.TryGetTappedTruckId → Garage.Dispatch
- W Update: tickuje conveyor + magnet (z FruitGrid z Zone1Manager)

**Files:**
- Create: `Assets/_Project/Scripts/Zone1_Trucks/Zone1TrucksManager.cs`

- [ ] **Step 1: `Zone1TrucksManager.cs`**

```csharp
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Project.Core;
using Project.Data;
using Project.Zone1.FruitWall;

namespace Project.Zone1.Trucks
{
    public class Zone1TrucksManager : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] GameBalanceSO balance;
        [SerializeField] Zone1Manager zone1Manager; // for FruitGrid access
        [SerializeField] BoolEventChannelSO onRefillingChanged;

        [Header("Conveyor")]
        [SerializeField] List<ConveyorWaypoint> conveyorWaypoints;
        [SerializeField] ConveyorView conveyorView;
        [SerializeField] float truckSpeedUnitsPerSec = 3f;

        [Header("Wall slots (active spots)")]
        [SerializeField] List<WallSlot> wallSlots; // 3 slots, last one IsStopSlot=true
        [SerializeField] float wallLeftX = -18f;
        [SerializeField] float wallWidthWorldUnits = 36f;

        [Header("Garage")]
        [SerializeField] GarageView garageView;
        [SerializeField] GameObject truckViewPrefab; // prefab z TruckView komponentem + ProBuilder modelem

        [Header("Camera (for tap raycast)")]
        [SerializeField] Camera mainCamera;

        ConveyorTrack track;
        Garage garage;
        readonly List<Truck> trucks = new();
        readonly Dictionary<int, TruckView> truckViews = new();

        float magnetAccumulator;
        bool isRefilling;

        void OnEnable()
        {
            if (onRefillingChanged != null)
                onRefillingChanged.Raised += OnRefillingChangedHandler;
        }
        void OnDisable()
        {
            if (onRefillingChanged != null)
                onRefillingChanged.Raised -= OnRefillingChangedHandler;
        }

        void OnRefillingChangedHandler(bool value)
        {
            isRefilling = value;
        }

        void Start()
        {
            if (balance == null || zone1Manager == null || conveyorView == null
                || garageView == null || truckViewPrefab == null)
            {
                Debug.LogError("[Zone1TrucksManager] missing references in inspector");
                enabled = false;
                return;
            }

            track = new ConveyorTrack(conveyorWaypoints);
            conveyorView.Build(track.Waypoints);

            garage = new Garage(balance.ConveyorSlotCount);

            // Create starter trucks: 1 per starting fruit type.
            int idCounter = 1;
            foreach (var fruit in balance.StartingFruitTypes)
            {
                var truck = new Truck(idCounter++, fruit, balance.TruckCapacity);
                garage.AddStarterTruck(truck);
                trucks.Add(truck);

                var go = Instantiate(truckViewPrefab, transform);
                go.name = $"TruckView_{fruit}";
                var view = go.GetComponent<TruckView>();
                Vector3 parkPos = garageView.GetParkPositionFor(truck.Id);
                view.Bind(truck, track, parkPos);
                garageView.RegisterTruckView(truck.Id, view);
                truckViews[truck.Id] = view;
            }
        }

        void Update()
        {
            float dt = Time.deltaTime;

            // Tap dispatch from garage
            HandleTapDispatch();

            // Conveyor pause during refill
            if (!isRefilling)
            {
                track.Tick(trucks, dt, truckSpeedUnitsPerSec);
            }

            // Stop logic at slot 3 (last wall slot with IsStopSlot=true) — for trucks that can collect
            ApplyStopAtSlotIfShouldCollect();

            // Magnet ticks
            float magnetInterval = 1f / Mathf.Max(0.01f, balance.MagnetRateHz);
            magnetAccumulator += dt;
            while (magnetAccumulator >= magnetInterval)
            {
                magnetAccumulator -= magnetInterval;
                if (!isRefilling) RunMagnetTick();
            }

            // Trucks that became Full → return to garage immediately (placeholder; Plan #4 will route to bottle)
            HandleFullTrucks();
        }

        void HandleTapDispatch()
        {
            // Use Mouse for editor / Touchscreen for mobile. Single tap only.
            Vector2? tapPos = null;
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
                tapPos = Touchscreen.current.primaryTouch.position.ReadValue();
            else if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                tapPos = Mouse.current.position.ReadValue();

            if (!tapPos.HasValue) return;
            int tappedId = garageView.TryGetTappedTruckId(mainCamera, tapPos.Value);
            if (tappedId >= 0)
                garage.Dispatch(tappedId);
        }

        void ApplyStopAtSlotIfShouldCollect()
        {
            // A truck near the stop slot transitions to StoppedAtSlot if it can still collect.
            int stopSlotIdx = -1;
            for (int i = 0; i < wallSlots.Count; i++)
                if (wallSlots[i].IsStopSlot) { stopSlotIdx = i; break; }
            if (stopSlotIdx < 0) return;

            // Convert stop slot world position to track param: linear scan for closest waypoint param.
            float stopSlotParam = ApproximateTrackParamForWorldPos(wallSlots[stopSlotIdx].WorldPosition);
            const float stopWindow = 0.02f; // 2% of track length

            foreach (var truck in trucks)
            {
                if (truck.State != TruckState.OnConveyor && truck.State != TruckState.EnteringConveyor) continue;
                float dist = Mathf.Abs(((truck.TrackPosition - stopSlotParam) + 1f) % 1f);
                dist = Mathf.Min(dist, 1f - dist);
                if (dist <= stopWindow)
                {
                    if (CanTruckStillCollect(truck))
                        truck.State = TruckState.StoppedAtSlot;
                }
            }

            // Trucks at StoppedAtSlot whose collect criteria no longer hold → resume.
            foreach (var truck in trucks)
            {
                if (truck.State != TruckState.StoppedAtSlot) continue;
                if (!CanTruckStillCollect(truck))
                    truck.State = TruckState.OnConveyor;
            }
        }

        bool CanTruckStillCollect(Truck truck)
        {
            if (truck.IsFull) return false;
            // Check: any matching fruit in bottom row?
            var grid = zone1Manager != null ? zone1Manager.Grid : null;
            if (grid == null) return false;
            for (int x = 0; x < grid.Columns; x++)
                if (grid.GetCell(x, 0) == truck.FruitColor) return true;
            return false;
        }

        void RunMagnetTick()
        {
            var grid = zone1Manager != null ? zone1Manager.Grid : null;
            if (grid == null) return;

            // Build list of trucks at active slots with their slotWorldX.
            var trucksAtSlots = new List<(Truck, float)>();
            foreach (var slot in wallSlots)
            {
                Truck nearest = FindTruckNearWaypointWorld(slot.WorldPosition);
                if (nearest != null) trucksAtSlots.Add((nearest, slot.WorldPosition.x));
            }

            MagnetSystem.AssignFruitsToTrucksAtSlots(grid, trucksAtSlots, wallLeftX, wallWidthWorldUnits);
        }

        Truck FindTruckNearWaypointWorld(Vector3 slotWorldPos)
        {
            float bestDist = 1.5f; // proximity threshold
            Truck best = null;
            foreach (var truck in trucks)
            {
                if (truck.State == TruckState.InGarage || truck.State == TruckState.ReturningToGarage) continue;
                Vector3 truckPos = track.GetWorldPositionAtTrackParam(truck.TrackPosition);
                float d = Vector3.Distance(truckPos, slotWorldPos);
                if (d < bestDist)
                {
                    bestDist = d;
                    best = truck;
                }
            }
            return best;
        }

        void HandleFullTrucks()
        {
            foreach (var truck in trucks)
            {
                if (truck.State != TruckState.StoppedAtSlot && truck.State != TruckState.OnConveyor) continue;
                if (!truck.IsFull) continue;
                // Placeholder for Plan #4: instead of "go to bottle", just teleport back to garage.
                truck.State = TruckState.ReturningToGarage;
                garage.ReturnToGarage(truck.Id);
            }
        }

        float ApproximateTrackParamForWorldPos(Vector3 worldPos)
        {
            const int samples = 200;
            float bestDist = float.PositiveInfinity;
            float bestParam = 0f;
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                Vector3 p = track.GetWorldPositionAtTrackParam(t);
                float d = (p - worldPos).sqrMagnitude;
                if (d < bestDist) { bestDist = d; bestParam = t; }
            }
            return bestParam;
        }
    }
}
```

> **Wymagana zmiana w `Zone1Manager.cs`**: dodać publiczną właściwość `public FruitGrid Grid => grid;` aby `Zone1TrucksManager` miał dostęp do gridu z poziomu drugiej strefy. Edytuj plik z Plan #2.

- [ ] **Step 2: Update `Zone1Manager.cs`** — dodaj public Grid getter

W `Assets/_Project/Scripts/Zone1_FruitWall/Zone1Manager.cs` znajdź pole `FruitGrid grid;` i tuż pod nim dodaj:

```csharp
public FruitGrid Grid => grid;
```

- [ ] **Step 3: Run tests in Unity** — wszystkie EditMode zielone (TruckStateMachineTests N/A bo nie zrobiliśmy w tym planie — zamiast tego ConveyorTrackTests, MagnetSystemTests, GarageTests).

- [ ] **Step 4: Commit**

```bash
git add Assets/_Project/Scripts/Zone1_FruitWall/Zone1Manager.cs Assets/_Project/Scripts/Zone1_Trucks/Zone1TrucksManager.cs
git commit -m "feat(zone1): add Zone1TrucksManager orchestrator with refill-pause integration"
```

---

## Task 9: Truck prefab w Unity Editor

- [ ] **Step 1: Stwórz `TruckPrefab` z ProBuilder**

W scenie tymczasowo: `Tools → ProBuilder → New Shape → Cube`. Skala (1.5, 0.8, 1.0) — to paka. Nazwij `Box`. Dodaj BoxCollider (dla raycast tap detection).

Pod nim dorzuć drugi cube (1, 0.5, 0.5) jako `Cab`, pozycja (0, 0.4, 0.7) (nad pakiem od przodu).

Pod nim dorzuć 4 walce spłaszczone (0.3, 0.1, 0.3) jako koła w 4 rogach paki.

Zaznacz root → drag do `Assets/_Project/Prefabs/` jako `TruckPrefab.prefab`.

Dodaj komponent `TruckView` na root prefaba. W inspectorze: `Box Renderer` = drag&drop `Box` z hierarchii prefaba.

Usuń tymczasowy obiekt ze sceny.

- [ ] **Step 2: Commit prefab**

```bash
git add Assets/_Project/Prefabs/TruckPrefab.prefab Assets/_Project/Prefabs/TruckPrefab.prefab.meta
git commit -m "feat(zone1): add TruckPrefab with ProBuilder cab+box+wheels and TruckView component"
```

---

## Task 10: Scene wiring (Unity Editor)

- [ ] **Step 1: Dodaj GameObject `[Zone1Trucks]` do `Zone1`**

Pod `[World] → Zone1`, prawym → Create Empty → `[Zone1Trucks]`.
Dodaj komponent `Zone1TrucksManager`.

- [ ] **Step 2: Stwórz waypointy conveyora**

Conveyor pętla powinna iść:
- Pod ścianą (od lewej do prawej): start (-15, -19, -2) → end (15, -19, -2). Po drodze 3 active slots: (-10, -19, -2), (0, -19, -2), (10, -19, -2) [last = stop slot]
- Po prawej: zakręt do (15, -22, -2)
- Z prawej do lewej (back): (15, -22, -2) → (-22, -22, -2)
- Po lewej: (-22, -22, -2) → (-22, -19, -2) (wraca do garażu / wjazd)
- Wjazd na conveyor: (-22, -19, -2) → start (-15, -19, -2)

W inspectorze `Zone1TrucksManager`:
- `Conveyor Waypoints` → element 0..N (np. 8 punktów):
  1. Position (-22, -19, -2), IsActiveSlot=false, SlotIndex=-1
  2. Position (-15, -19, -2), false, -1
  3. Position (-10, -19, -2), true, 0
  4. Position (0, -19, -2), true, 1
  5. Position (10, -19, -2), true, 2 (stop slot — patrz Wall Slots)
  6. Position (15, -19, -2), false, -1
  7. Position (15, -22, -2), false, -1
  8. Position (-22, -22, -2), false, -1

- [ ] **Step 3: Stwórz `WallSlots`**

W inspectorze `Wall Slots`:
1. WorldPosition (-10, -19, -2), SlotIndex=0, IsStopSlot=false
2. WorldPosition (0, -19, -2), SlotIndex=1, IsStopSlot=false
3. WorldPosition (10, -19, -2), SlotIndex=2, IsStopSlot=true (ostatni, tu trucks stop)

`Wall Left X` = -18, `Wall Width World Units` = 36 (zgodnie z GameBalanceSO).

- [ ] **Step 4: Garage GameObject + GarageView**

Pod `[Zone1Trucks]` stwórz pusty `Garage`. Pozycja (-22, -19, -2). Dodaj komponent `GarageView`.

W `Parking Slots` dodaj 3 elementy (lokalne pozycje w garażu):
1. (0, 0, -2)
2. (1.5, 0, -2)
3. (3, 0, -2)

W `Zone1TrucksManager.Garage View` przeciągnij `Garage` z hierarchii.

- [ ] **Step 5: ConveyorView GameObject**

Pod `[Zone1Trucks]` stwórz pusty `Conveyor`. Dodaj `LineRenderer` + komponent `ConveyorView`. Material = nowy stworzony `ConveyorMaterial` (dowolny szary unlit material).

Drag `Conveyor` do pola `Zone1Trucks Manager → Conveyor View`.

- [ ] **Step 6: Inne wiring**

W `Zone1TrucksManager`:
- `Balance` = `Assets/_Project/Settings/GameBalance.asset`
- `Zone1 Manager` = drag `[Zone1Manager]` z hierarchii (z Plan #2)
- `On Refilling Changed` = `Assets/_Project/Settings/Events/OnRefillingChanged.asset`
- `Truck View Prefab` = drag `Assets/_Project/Prefabs/TruckPrefab.prefab`
- `Main Camera` = drag Main Camera z hierarchii

`Truck Speed Units Per Sec` = 3 (parametr balansowy — tunable).

- [ ] **Step 7: Commit scene**

```bash
git add Assets/_Project/Scenes/Main.unity
git commit -m "feat(scene): wire Zone1Trucks (conveyor waypoints, slots, garage, truck prefab)"
```

---

## Task 11: Manual playtest + tag

- [ ] **Step 1: Playtest checklist**

- Uruchom Play. Scrolluj kamerą do strefy 1.
- W garażu (lewy dolny róg ekranu, w przodzie ściany) widzisz 3 ciężarówki: czerwona, pomarańczowa, żółta.
- Conveyor jest widoczny jako szary tor pętlący pod ścianą.
- Tappuj na czerwoną ciężarówkę → wjeżdża na conveyor i jeździ pętlą.
- Tappuj `Refill Wall` w HUD → ściana się zapełnia, conveyor PAUSE'uje (ciężarówka stoi).
- Po refilu conveyor wznawia jazdę.
- Ciężarówka dojeżdża do slot 3 (najbardziej prawego) → STOPS jeśli ma jabłka w bottom row → zaczyna magnetować jabłka. Load rośnie.
- Po napełnieniu (Load = 100): ciężarówka znika z conveyora, wraca do garażu (placeholder).
- Tap można powtórzyć.

- [ ] **Step 2: Test EditMode + PlayMode**

EditMode: ConveyorTrackTests + MagnetSystemTests + GarageTests + wszystkie poprzednie. Wszystkie zielone.
PlayMode SceneSmokeTest też.

- [ ] **Step 3: Tag**

```bash
git tag -a mvp-step3 -m "MVP Step 3: Trucks + Conveyor + Magnet + Garage"
```

---

## Definicja Done dla Plan #3

- ✅ EditMode tests: wszystkie zielone (ConveyorTrack ~5, MagnetSystem ~5, Garage ~5).
- ✅ Tap na ciężarówkę w garażu → wjazd na conveyor.
- ✅ Conveyor jeździ pętlą, widoczny tor.
- ✅ W slot 3 ciężarówka stop'uje gdy może zbierać; magnet działa.
- ✅ Ściana się opróżnia z bottom row gdy ciężarówki zbierają.
- ✅ Refill button pauzuje conveyor.
- ✅ Pełna ciężarówka znika z conveyora, wraca do garażu.

## Out of Plan #3 (na później)

- Animacja "fly to truck" magnetowanego owoca (Bezier curve) — Plan #3.5 / polishing.
- Magnet w slotach 1 i 2 (collect "in motion") — w MVP jest tylko stop slot 3. Eksperymentalnie dodać.
- Big bottle: ciężarówka full → drives to bottle, dumps → returns. Plan #4.
- Garage z wieloma ciężarówkami per kolor (upgrade). Currently 1 per kolor.
- Wizualne efekty (kurz pod kołami, dymek z paki). Polish.
