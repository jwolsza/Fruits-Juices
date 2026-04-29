# MVP Step 3: Trucks + Conveyor + Magnet + Garage — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Strefa 1 wzbogacona o ciężarówki. Gracz tappuje ciężarówkę w garażu (przed ścianą, w przodzie pod kamerą) → wjeżdża na conveyor (widoczny tor pętlący przed dolną krawędzią ściany) → zbiera owoce magnetem z bottom row gridu w 3 aktywnych slotach → pełna jedzie z toru → na razie wraca do garażu (Plan #4 zastąpi to wizytą u dużej butelki). Conveyor pauzuje gdy `OnRefillingChanged.LastValue == true`.

**Architecture:** Pure-logic warstwa (`Truck`, `TruckStateMachine`, `ConveyorTrack`, `WallSlot`, `MagnetSystem`, `Garage`) testowalna unitowo. Unity warstwa: `TruckView` (ProBuilder cab+paka+koła), `ConveyorView` (LineRenderer pętla), `GarageView` (parking + tap pickup), `Zone1TrucksManager`. Subscribe `OnRefillingChanged` → conveyor pause.

**Plan numer:** 3/8 w sekwencji MVP. Spec: `docs/superpowers/specs/2026-04-29-fruits-juices-design.md` sekcje 3.2–3.7. Branch: `feat/mvp-step3`.

---

## Scene Layout Reference (read from `Main.unity` przy pisaniu planu)

Aktualna scena (jako baseline dla wszystkich koordynatów w tym planie):

| Obiekt | World position | Rotation | Scale | Uwagi |
|--------|---------------|----------|-------|-------|
| `FirstZone` | (0, 0, 0) | identity | 1 | Root strefy 1 |
| `Wall` (child of FirstZone) | local (-0.02, 2.68, -0.87) | -90° X | 0.45 | Lying horizontally, view from above-front |
| `MainCamera` | (0, 4.62, -3.75) | ~45° X (looking down/forward) | 1 | Isometric-ish view |
| `SecondZone` | (5, 0, 0) | identity | 1 | 5 units right of FirstZone |
| `ThirdZone` | (5.3, 0, 1) | identity | 1 | |

**Computed wall bounds in world space** (Wall.localScale 0.45 × balance.WallWidthWorldUnits 36 = 16.2 jednostek; rotacja -90° X przekłada local Y na world -Z):

- Cell `(0, 0)` (bottom-left of grid logically) → world `(0, 2.68, -0.87)`
- Cell `(299, 0)` (bottom-right) → world `(16.15, 2.68, -0.9)` — **bottom row najbliżej kamery**
- Cell `(0, 299)` (top-left) → world `(0, 2.68, -17.04)` — **top row najdalej od kamery**
- Cell `(299, 299)` → world `(16.15, 2.68, -17.04)`
- Cell width in world = `36 * 0.45 / 300 = 0.054`

**Wniosek:** ściana to płaski stół Y=2.68, fruity refilla "spadają" z `Z=-17` w kierunku `Z=-0.87` (czyli idą do kamery). Bottom row (gdzie ciężarówki zbierają) jest przy krawędzi `Z ≈ -0.87`.

**Ground (Y=0)** jest 2.68 jednostki pod ścianą — tam jeżdżą ciężarówki.

---

## File Structure

```
Assets/_Project/
├── Scripts/
│   └── Zone1_Trucks/
│       ├── Project.Zone1.Trucks.asmdef           [NEW]
│       ├── TruckState.cs                          [NEW]
│       ├── Truck.cs                               [NEW]
│       ├── ConveyorWaypoint.cs                    [NEW]
│       ├── ConveyorTrack.cs                       [NEW]
│       ├── WallSlot.cs                            [NEW]
│       ├── MagnetSystem.cs                        [NEW]
│       ├── Garage.cs                              [NEW]
│       ├── TruckView.cs                           [NEW]
│       ├── ConveyorView.cs                        [NEW]
│       ├── GarageView.cs                          [NEW]
│       └── Zone1TrucksManager.cs                  [NEW]
└── Tests/EditMode/
    ├── ConveyorTrackTests.cs                      [NEW]
    ├── MagnetSystemTests.cs                       [NEW]
    └── GarageTests.cs                             [NEW]

Assets/_Project/Scripts/Zone1_FruitWall/Zone1Manager.cs   [MODIFY] add public Grid getter
Assets/_Project/Tests/EditMode/Project.Tests.EditMode.asmdef  [MODIFY] add Project.Zone1.Trucks ref

Assets/_Project/Scenes/Main.unity                  [MODIFY] add Zone1.Trucks GameObjects
Assets/_Project/Prefabs/TruckPrefab.prefab         [NEW]
```

---

## Task 0: Branch + asmdef setup

- [ ] **Step 1: Create feature branch from main**

```bash
cd "/Users/jakubwolsza/Documents/Fruits&Juices"
git checkout main
git merge --no-ff feat/mvp-step2 -m "merge: feat/mvp-step2 (MVP Step 2)"
git checkout -b feat/mvp-step3
```

- [ ] **Step 2: Create folder + asmdef**

```bash
mkdir -p "/Users/jakubwolsza/Documents/Fruits&Juices/Assets/_Project/Scripts/Zone1_Trucks"
```

`Project.Zone1.Trucks.asmdef`:
```json
{
    "name": "Project.Zone1.Trucks",
    "rootNamespace": "Project.Zone1.Trucks",
    "references": ["Project.Core", "Project.Data", "Project.Zone1.FruitWall"],
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

- [ ] **Step 3: Update test asmdef** — dodaj `"Project.Zone1.Trucks"` do `Project.Tests.EditMode.asmdef.references`.

- [ ] **Step 4: Commit**

```bash
git add Assets/_Project/Scripts/Zone1_Trucks/Project.Zone1.Trucks.asmdef Assets/_Project/Tests/EditMode/Project.Tests.EditMode.asmdef
git commit -m "chore: scaffold Project.Zone1.Trucks asmdef"
```

---

## Task 1: TruckState + Truck

**Files:** `TruckState.cs`, `Truck.cs`

```csharp
// TruckState.cs
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

```csharp
// Truck.cs
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
        public float TrackPosition { get; set; }

        public bool IsFull => Load >= Capacity;

        public Truck(int id, FruitType color, int capacity)
        {
            Id = id;
            FruitColor = color;
            Capacity = capacity;
        }

        public void AddFruit() { if (Load < Capacity) Load++; }
        public void EmptyLoad() { Load = 0; }
    }
}
```

- [ ] **Commit:** `feat(zone1): add TruckState enum and Truck data class`

---

## Task 2: ConveyorWaypoint + ConveyorTrack (TDD)

**Files:** `ConveyorWaypoint.cs`, `ConveyorTrack.cs`, `ConveyorTrackTests.cs`

```csharp
// ConveyorWaypoint.cs
using UnityEngine;

namespace Project.Zone1.Trucks
{
    [System.Serializable]
    public struct ConveyorWaypoint
    {
        public Vector3 Position;
        public bool IsActiveSlot;
        public int SlotIndex;
    }
}
```

Tests (skrócona wersja — pełne testy w pliku):

```csharp
// ConveyorTrackTests.cs
using NUnit.Framework;
using UnityEngine;
using Project.Core;
using Project.Zone1.Trucks;
using System.Collections.Generic;

namespace Project.Tests.EditMode
{
    public class ConveyorTrackTests
    {
        ConveyorTrack BuildSquare()
        {
            return new ConveyorTrack(new List<ConveyorWaypoint>
            {
                new() { Position = new Vector3(0, 0, 0) },
                new() { Position = new Vector3(10, 0, 0) },
                new() { Position = new Vector3(10, 0, 10) },
                new() { Position = new Vector3(0, 0, 10) },
            });
        }

        [Test] public void GetWorldPositionAtTrackParam_AtZero_ReturnsFirstWaypoint()
        { var t = BuildSquare(); Assert.That(t.GetWorldPositionAtTrackParam(0f), Is.EqualTo(new Vector3(0,0,0))); }

        [Test] public void GetWorldPositionAtTrackParam_AtQuarter_OnFirstSegment()
        { var t = BuildSquare(); var p = t.GetWorldPositionAtTrackParam(0.25f);
          Assert.That(p.x, Is.EqualTo(10f).Within(0.001f)); Assert.That(p.z, Is.EqualTo(0f).Within(0.001f)); }

        [Test] public void GetWorldPositionAtTrackParam_LoopsAtOne()
        { var t = BuildSquare(); Assert.That(t.GetWorldPositionAtTrackParam(1f), Is.EqualTo(t.GetWorldPositionAtTrackParam(0f))); }

        [Test] public void Tick_AdvancesAllTrucks_WhenNoneStopped()
        {
            var track = BuildSquare();
            var t1 = new Truck(1, FruitType.Apple, 100) { TrackPosition = 0f, State = TruckState.OnConveyor };
            var t2 = new Truck(2, FruitType.Orange, 100) { TrackPosition = 0.25f, State = TruckState.OnConveyor };
            track.Tick(new[] { t1, t2 }, 1f, 4f); // total length 40, advance 4 → +0.1
            Assert.That(t1.TrackPosition, Is.EqualTo(0.1f).Within(0.001f));
            Assert.That(t2.TrackPosition, Is.EqualTo(0.35f).Within(0.001f));
        }

        [Test] public void Tick_TruckStoppedAtSlot_TrucksBehindFreeze()
        {
            var track = BuildSquare();
            var stopped = new Truck(1, FruitType.Apple, 100) { TrackPosition = 0.5f, State = TruckState.StoppedAtSlot };
            var behind = new Truck(2, FruitType.Apple, 100) { TrackPosition = 0.45f, State = TruckState.OnConveyor };
            var ahead = new Truck(3, FruitType.Apple, 100) { TrackPosition = 0.6f, State = TruckState.OnConveyor };
            track.Tick(new[] { stopped, behind, ahead }, 1f, 4f);
            Assert.That(stopped.TrackPosition, Is.EqualTo(0.5f).Within(0.001f));
            Assert.That(behind.TrackPosition, Is.LessThanOrEqualTo(0.5f));
            Assert.That(ahead.TrackPosition, Is.EqualTo(0.7f).Within(0.001f));
        }
    }
}
```

```csharp
// ConveyorTrack.cs
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
            t = ((t % 1f) + 1f) % 1f;
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

        public void Tick(IReadOnlyList<Truck> trucks, float deltaTime, float speedUnitsPerSec)
        {
            if (trucks.Count == 0 || totalLength <= 0f) return;
            float deltaParam = (speedUnitsPerSec * deltaTime) / totalLength;

            var stoppedPositions = new List<float>();
            foreach (var t in trucks)
                if (t.State == TruckState.StoppedAtSlot) stoppedPositions.Add(t.TrackPosition);

            foreach (var truck in trucks)
            {
                if (truck.State == TruckState.StoppedAtSlot) continue;
                if (truck.State == TruckState.InGarage) continue;
                if (truck.State == TruckState.ReturningToGarage) continue;

                float current = truck.TrackPosition;
                float desired = current + deltaParam;
                float minBlocker = float.PositiveInfinity;
                foreach (float sp in stoppedPositions)
                {
                    float distAhead = (sp - current + 1f) % 1f;
                    if (distAhead > 0f && distAhead < minBlocker) minBlocker = distAhead;
                }
                if (minBlocker < float.PositiveInfinity && deltaParam >= minBlocker)
                    desired = current + minBlocker - 0.001f;

                truck.TrackPosition = ((desired % 1f) + 1f) % 1f;
            }
        }
    }
}
```

- [ ] **Run tests → all pass; Commit:** `feat(zone1): add ConveyorTrack with formation logic + tests`

---

## Task 3: WallSlot + MagnetSystem (TDD)

`MagnetSystem` operuje na cellach gridu (X-only proximity dla osi-aligned wall). Caller dostarcza wallLeftX i wallWidth **w world units** (już po skali).

**Files:** `WallSlot.cs`, `MagnetSystem.cs`, `MagnetSystemTests.cs`

```csharp
// WallSlot.cs
using UnityEngine;

namespace Project.Zone1.Trucks
{
    [System.Serializable]
    public struct WallSlot
    {
        public Vector3 WorldPosition;     // gdzie truck stoi w slocie (Y=0 ground level)
        public int SlotIndex;             // 0..2
        public bool IsStopSlot;           // true tylko dla ostatniego (slot 2 by default)
    }
}
```

```csharp
// MagnetSystemTests.cs
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
            var truck = new Truck(1, FruitType.Apple, 100) { State = TruckState.StoppedAtSlot };

            var assignments = MagnetSystem.AssignFruitsToTrucksAtSlots(
                grid, new[] { (truck, 0f) }, wallLeftXWorld: 0f, wallWidthWorld: 10f);

            Assert.AreEqual(1, assignments.Count);
            Assert.AreEqual(1, truck.Load);
            Assert.IsNull(grid.GetCell(5, 0));
        }

        [Test]
        public void Magnet_NoMatchingFruit_NoAssignment()
        {
            var grid = new FruitGrid(10, 10);
            grid.SetCell(5, 0, FruitType.Orange);
            var truck = new Truck(1, FruitType.Apple, 100) { State = TruckState.StoppedAtSlot };
            var assignments = MagnetSystem.AssignFruitsToTrucksAtSlots(
                grid, new[] { (truck, 0f) }, 0f, 10f);
            Assert.AreEqual(0, assignments.Count);
            Assert.AreEqual(0, truck.Load);
        }

        [Test]
        public void Magnet_FullTruck_NoAssignment()
        {
            var grid = new FruitGrid(10, 10);
            grid.SetCell(5, 0, FruitType.Apple);
            var truck = new Truck(1, FruitType.Apple, 1) { State = TruckState.StoppedAtSlot };
            truck.AddFruit();
            var assignments = MagnetSystem.AssignFruitsToTrucksAtSlots(
                grid, new[] { (truck, 0f) }, 0f, 10f);
            Assert.AreEqual(0, assignments.Count);
        }

        [Test]
        public void Magnet_MultipleTrucks_AssignsClosestFruitPerTruckByX()
        {
            var grid = new FruitGrid(10, 10);
            grid.SetCell(2, 0, FruitType.Apple);
            grid.SetCell(8, 0, FruitType.Apple);
            var truckLeft = new Truck(1, FruitType.Apple, 100) { State = TruckState.StoppedAtSlot };
            var truckRight = new Truck(2, FruitType.Apple, 100) { State = TruckState.StoppedAtSlot };
            var assignments = MagnetSystem.AssignFruitsToTrucksAtSlots(
                grid, new[] { (truckLeft, 2f), (truckRight, 8f) }, 0f, 10f);
            Assert.AreEqual(2, assignments.Count);
            Assert.AreEqual(1, truckLeft.Load);
            Assert.AreEqual(1, truckRight.Load);
        }

        [Test]
        public void Magnet_TwoTrucksSameColor_SecondGetsNextNearest()
        {
            var grid = new FruitGrid(10, 10);
            grid.SetCell(4, 0, FruitType.Apple);
            grid.SetCell(6, 0, FruitType.Apple);
            var a = new Truck(1, FruitType.Apple, 100) { State = TruckState.StoppedAtSlot };
            var b = new Truck(2, FruitType.Apple, 100) { State = TruckState.StoppedAtSlot };
            var assignments = MagnetSystem.AssignFruitsToTrucksAtSlots(
                grid, new[] { (a, 5f), (b, 5f) }, 0f, 10f);
            Assert.AreEqual(2, assignments.Count);
            Assert.AreEqual(1, a.Load);
            Assert.AreEqual(1, b.Load);
        }
    }
}
```

```csharp
// MagnetSystem.cs
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
    }

    public static class MagnetSystem
    {
        /// <summary>
        /// X-only proximity matching. Caller dostarcza wallLeftXWorld (left edge in world)
        /// and wallWidthWorld (total width in world AFTER any parent scale).
        /// For each truck at a slot, find nearest matching-color fruit in bottom row by world X distance.
        /// </summary>
        public static List<MagnetAssignment> AssignFruitsToTrucksAtSlots(
            FruitGrid grid,
            IReadOnlyList<(Truck truck, float slotWorldX)> trucksAtSlots,
            float wallLeftXWorld,
            float wallWidthWorld)
        {
            var result = new List<MagnetAssignment>();
            if (grid == null || trucksAtSlots == null || trucksAtSlots.Count == 0) return result;
            if (grid.Columns <= 0) return result;

            float cellWidthWorld = wallWidthWorld / grid.Columns;

            var available = new List<(int cellX, FruitType type)>();
            for (int x = 0; x < grid.Columns; x++)
            {
                var c = grid.GetCell(x, 0);
                if (c.HasValue) available.Add((x, c.Value));
            }

            foreach (var (truck, slotX) in trucksAtSlots)
            {
                if (truck.IsFull) continue;

                int bestIdx = -1;
                float bestDist = float.PositiveInfinity;
                for (int i = 0; i < available.Count; i++)
                {
                    var (cellX, type) = available[i];
                    if (type != truck.FruitColor) continue;
                    float worldX = wallLeftXWorld + cellX * cellWidthWorld + cellWidthWorld * 0.5f;
                    float d = Mathf.Abs(worldX - slotX);
                    if (d < bestDist) { bestDist = d; bestIdx = i; }
                }

                if (bestIdx >= 0)
                {
                    var (cellX, type) = available[bestIdx];
                    grid.ClearCell(cellX, 0);
                    truck.AddFruit();
                    available.RemoveAt(bestIdx);
                    result.Add(new MagnetAssignment { Truck = truck, GridCellRemoved = new Vector2Int(cellX, 0), FruitType = type });
                }
            }
            return result;
        }
    }
}
```

- [ ] **Run tests → pass; Commit:** `feat(zone1): add WallSlot and MagnetSystem with X-proximity assignment + tests`

---

## Task 4: Garage (TDD)

**Files:** `Garage.cs`, `GarageTests.cs`

```csharp
// GarageTests.cs
using NUnit.Framework;
using Project.Core;
using Project.Zone1.Trucks;

namespace Project.Tests.EditMode
{
    public class GarageTests
    {
        [Test] public void NewGarage_StarterTrucks_AllInGarageState()
        {
            var g = new Garage(maxOnConveyor: 4);
            var t = new Truck(1, FruitType.Apple, 100);
            g.AddStarterTruck(t);
            Assert.AreEqual(1, g.TruckCount);
            Assert.AreEqual(0, g.OnConveyorCount);
            Assert.AreEqual(TruckState.InGarage, t.State);
        }

        [Test] public void Dispatch_FromGarage_EnteringConveyor()
        {
            var g = new Garage(4);
            var t = new Truck(1, FruitType.Apple, 100);
            g.AddStarterTruck(t);
            Assert.IsTrue(g.Dispatch(1));
            Assert.AreEqual(TruckState.EnteringConveyor, t.State);
            Assert.AreEqual(0f, t.TrackPosition);
        }

        [Test] public void Dispatch_ConveyorFull_ReturnsFalse()
        {
            var g = new Garage(1);
            var t1 = new Truck(1, FruitType.Apple, 100);
            var t2 = new Truck(2, FruitType.Apple, 100);
            g.AddStarterTruck(t1); g.AddStarterTruck(t2);
            Assert.IsTrue(g.Dispatch(1));
            Assert.IsFalse(g.Dispatch(2));
        }

        [Test] public void Dispatch_AlreadyOnConveyor_ReturnsFalse()
        {
            var g = new Garage(4);
            var t = new Truck(1, FruitType.Apple, 100);
            g.AddStarterTruck(t);
            g.Dispatch(1);
            Assert.IsFalse(g.Dispatch(1));
        }

        [Test] public void ReturnToGarage_RestoresStateAndEmpties()
        {
            var g = new Garage(4);
            var t = new Truck(1, FruitType.Apple, 100);
            g.AddStarterTruck(t);
            g.Dispatch(1);
            t.AddFruit();
            t.State = TruckState.ReturningToGarage;
            g.ReturnToGarage(1);
            Assert.AreEqual(TruckState.InGarage, t.State);
            Assert.AreEqual(0, t.Load);
            Assert.AreEqual(0, g.OnConveyorCount);
        }
    }
}
```

```csharp
// Garage.cs
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

        public Garage(int maxOnConveyor) { MaxOnConveyor = maxOnConveyor; }

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

- [ ] **Run tests → pass; Commit:** `feat(zone1): add Garage with dispatch and return-to-garage logic + tests`

---

## Task 5: TruckView

```csharp
// TruckView.cs
using UnityEngine;
using Project.Zone1.FruitWall;

namespace Project.Zone1.Trucks
{
    public class TruckView : MonoBehaviour
    {
        [SerializeField] Renderer boxRenderer;

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
        }

        public void SetGaragePosition(Vector3 pos) => garageParkPosition = pos;

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

        void LateUpdate()
        {
            if (truck == null) return;
            switch (truck.State)
            {
                case TruckState.InGarage:
                case TruckState.ReturningToGarage:
                    transform.position = garageParkPosition;
                    transform.rotation = Quaternion.identity;
                    break;
                default:
                    if (track != null)
                    {
                        transform.position = track.GetWorldPositionAtTrackParam(truck.TrackPosition);
                        Vector3 ahead = track.GetWorldPositionAtTrackParam((truck.TrackPosition + 0.001f) % 1f);
                        Vector3 fwd = (ahead - transform.position).normalized;
                        if (fwd.sqrMagnitude > 0.0001f)
                            transform.rotation = Quaternion.LookRotation(fwd, Vector3.up);
                    }
                    break;
            }
        }
    }
}
```

- [ ] **Commit:** `feat(zone1): add TruckView with track-position sync and color tinting`

---

## Task 6: ConveyorView

```csharp
// ConveyorView.cs
using UnityEngine;
using System.Collections.Generic;

namespace Project.Zone1.Trucks
{
    [RequireComponent(typeof(LineRenderer))]
    public class ConveyorView : MonoBehaviour
    {
        [SerializeField] float lineWidth = 0.1f;
        [SerializeField] Color lineColor = new(0.4f, 0.4f, 0.45f);

        public void Build(IReadOnlyList<ConveyorWaypoint> waypoints)
        {
            var lr = GetComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.loop = true;
            lr.startWidth = lineWidth; lr.endWidth = lineWidth;
            lr.startColor = lineColor; lr.endColor = lineColor;
            var pts = new Vector3[waypoints.Count];
            for (int i = 0; i < waypoints.Count; i++) pts[i] = waypoints[i].Position;
            lr.positionCount = pts.Length;
            lr.SetPositions(pts);
        }
    }
}
```

- [ ] **Commit:** `feat(zone1): add ConveyorView (LineRenderer pętla)`

---

## Task 7: GarageView

```csharp
// GarageView.cs
using UnityEngine;
using System.Collections.Generic;

namespace Project.Zone1.Trucks
{
    public class GarageView : MonoBehaviour
    {
        [SerializeField] Vector3[] parkingSlots; // local positions

        readonly Dictionary<int, TruckView> truckViewsById = new();
        readonly List<int> orderedTruckIds = new();

        public Vector3 GetParkPositionFor(int truckId)
        {
            int idx = orderedTruckIds.IndexOf(truckId);
            if (idx < 0 || parkingSlots == null || parkingSlots.Length == 0) return transform.position;
            return transform.TransformPoint(parkingSlots[idx % parkingSlots.Length]);
        }

        public void RegisterTruckView(int truckId, TruckView view)
        {
            truckViewsById[truckId] = view;
            if (!orderedTruckIds.Contains(truckId)) orderedTruckIds.Add(truckId);
        }

        public int TryGetTappedTruckId(Camera cam, Vector2 screenPos)
        {
            if (cam == null) return -1;
            Ray ray = cam.ScreenPointToRay(screenPos);
            if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
            {
                var view = hit.collider.GetComponentInParent<TruckView>();
                if (view != null)
                    foreach (var kv in truckViewsById)
                        if (kv.Value == view) return kv.Key;
            }
            return -1;
        }
    }
}
```

- [ ] **Commit:** `feat(zone1): add GarageView with parking layout and tap raycast`

---

## Task 8: Update `Zone1Manager.cs` + `Zone1TrucksManager.cs`

- [ ] **Step 1:** Open `Assets/_Project/Scripts/Zone1_FruitWall/Zone1Manager.cs`. Find `FruitGrid grid;` and add right after it:

```csharp
public FruitGrid Grid => grid;
public Transform WallTransform => wallView != null ? wallView.transform : null;
public float WallWidthWorldUnits => balance != null && wallView != null
    ? balance.WallWidthWorldUnits * wallView.transform.lossyScale.x
    : 0f;
public float WallLeftXWorld => wallView != null ? wallView.transform.position.x : 0f;
```

(Useful exposures dla Zone1TrucksManager żeby mieć world-space bounds wall'a po zaaplikowaniu Wall.transform.scale.)

- [ ] **Step 2:** Create `Zone1TrucksManager.cs`:

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
        [SerializeField] Zone1Manager zone1Manager;
        [SerializeField] BoolEventChannelSO onRefillingChanged;

        [Header("Conveyor")]
        [SerializeField] List<ConveyorWaypoint> conveyorWaypoints;
        [SerializeField] ConveyorView conveyorView;
        [Tooltip("World units per second.")]
        [SerializeField] float truckSpeedUnitsPerSec = 1.5f;

        [Header("Wall slots (active spots)")]
        [SerializeField] List<WallSlot> wallSlots;

        [Header("Garage")]
        [SerializeField] GarageView garageView;
        [SerializeField] GameObject truckViewPrefab;

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
            if (onRefillingChanged != null) onRefillingChanged.Raised += OnRefillingChangedHandler;
        }
        void OnDisable()
        {
            if (onRefillingChanged != null) onRefillingChanged.Raised -= OnRefillingChangedHandler;
        }
        void OnRefillingChangedHandler(bool v) => isRefilling = v;

        void Start()
        {
            if (balance == null || zone1Manager == null || conveyorView == null
                || garageView == null || truckViewPrefab == null)
            {
                Debug.LogError("[Zone1TrucksManager] missing references");
                enabled = false; return;
            }

            track = new ConveyorTrack(conveyorWaypoints);
            conveyorView.Build(track.Waypoints);
            garage = new Garage(balance.ConveyorSlotCount);

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

            HandleTapDispatch();

            if (!isRefilling)
                track.Tick(trucks, dt, truckSpeedUnitsPerSec);

            ApplyStopAtSlotIfShouldCollect();

            float magnetInterval = 1f / Mathf.Max(0.01f, balance.MagnetRateHz);
            magnetAccumulator += dt;
            while (magnetAccumulator >= magnetInterval)
            {
                magnetAccumulator -= magnetInterval;
                if (!isRefilling) RunMagnetTick();
            }

            HandleFullTrucks();
        }

        void HandleTapDispatch()
        {
            Vector2? tapPos = null;
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
                tapPos = Touchscreen.current.primaryTouch.position.ReadValue();
            else if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                tapPos = Mouse.current.position.ReadValue();
            if (!tapPos.HasValue) return;

            int tappedId = garageView.TryGetTappedTruckId(mainCamera, tapPos.Value);
            if (tappedId >= 0) garage.Dispatch(tappedId);
        }

        void ApplyStopAtSlotIfShouldCollect()
        {
            int stopSlotIdx = -1;
            for (int i = 0; i < wallSlots.Count; i++) if (wallSlots[i].IsStopSlot) { stopSlotIdx = i; break; }
            if (stopSlotIdx < 0) return;

            float stopParam = ApproximateTrackParamForWorldPos(wallSlots[stopSlotIdx].WorldPosition);
            const float stopWindow = 0.02f;

            foreach (var truck in trucks)
            {
                if (truck.State != TruckState.OnConveyor && truck.State != TruckState.EnteringConveyor) continue;
                float dist = Mathf.Abs(((truck.TrackPosition - stopParam) + 1f) % 1f);
                dist = Mathf.Min(dist, 1f - dist);
                if (dist <= stopWindow && CanTruckStillCollect(truck))
                    truck.State = TruckState.StoppedAtSlot;
            }

            foreach (var truck in trucks)
                if (truck.State == TruckState.StoppedAtSlot && !CanTruckStillCollect(truck))
                    truck.State = TruckState.OnConveyor;
        }

        bool CanTruckStillCollect(Truck truck)
        {
            if (truck.IsFull) return false;
            var grid = zone1Manager.Grid;
            if (grid == null) return false;
            for (int x = 0; x < grid.Columns; x++)
                if (grid.GetCell(x, 0) == truck.FruitColor) return true;
            return false;
        }

        void RunMagnetTick()
        {
            var grid = zone1Manager.Grid;
            if (grid == null) return;

            var trucksAtSlots = new List<(Truck, float)>();
            foreach (var slot in wallSlots)
            {
                Truck nearest = FindTruckNearWaypointWorld(slot.WorldPosition);
                if (nearest != null) trucksAtSlots.Add((nearest, slot.WorldPosition.x));
            }

            MagnetSystem.AssignFruitsToTrucksAtSlots(
                grid, trucksAtSlots,
                wallLeftXWorld: zone1Manager.WallLeftXWorld,
                wallWidthWorld: zone1Manager.WallWidthWorldUnits);
        }

        Truck FindTruckNearWaypointWorld(Vector3 slotWorldPos)
        {
            float bestDist = 1.0f; // proximity threshold (small scale wall)
            Truck best = null;
            foreach (var truck in trucks)
            {
                if (truck.State == TruckState.InGarage || truck.State == TruckState.ReturningToGarage) continue;
                Vector3 truckPos = track.GetWorldPositionAtTrackParam(truck.TrackPosition);
                float d = Vector3.Distance(truckPos, slotWorldPos);
                if (d < bestDist) { bestDist = d; best = truck; }
            }
            return best;
        }

        void HandleFullTrucks()
        {
            foreach (var truck in trucks)
            {
                if (truck.State != TruckState.StoppedAtSlot && truck.State != TruckState.OnConveyor) continue;
                if (!truck.IsFull) continue;
                truck.State = TruckState.ReturningToGarage;
                garage.ReturnToGarage(truck.Id);
                if (truckViews.TryGetValue(truck.Id, out var view))
                    view.SetGaragePosition(garageView.GetParkPositionFor(truck.Id));
            }
        }

        float ApproximateTrackParamForWorldPos(Vector3 worldPos)
        {
            const int samples = 200;
            float bestDist = float.PositiveInfinity, bestParam = 0f;
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

- [ ] **Commit:** `feat(zone1): add Zone1TrucksManager + expose Grid/Wall world bounds in Zone1Manager`

---

## Task 9: Truck Prefab w Unity Editor

Skala dopasowana do small-scale wall (~16 jednostek szerokości):

- **Box** (paka): ProBuilder Cube, scale `(1.0, 0.4, 0.6)`, BoxCollider auto. Kolor zostanie nadpisany w runtime przez `TruckView` na podstawie typu owocu.
- **Cab** (kabina): ProBuilder Cube, scale `(0.5, 0.3, 0.4)`, position relative do paki: `(0, 0.05, 0.5)` (przed paką, lekko wyżej).
- **Wheels** (4 koła): Cylinders splaszczone, scale `(0.18, 0.05, 0.18)`, pozycje: `(±0.4, -0.2, ±0.25)`.

Prefab root: pusty GameObject `TruckPrefab` z 3 dziećmi (Box, Cab, 4 Wheels). `TruckView` komponent na root, w inspectorze `Box Renderer` = drag `Box`.

Zapisz jako `Assets/_Project/Prefabs/TruckPrefab.prefab`.

- [ ] **Commit:** `feat(zone1): add TruckPrefab (ProBuilder cab+box+wheels)`

---

## Task 10: Scene wiring (Unity Editor) — REALISTIC COORDS

> Wszystkie koordynaty w worldspace, oparte na rzeczywistym layoucie sceny: FirstZone=(0,0,0), wall-bottom-row najbliżej kamery przy Z≈-0.87, wall rozciąga się X∈[0,16].

- [ ] **Step 1:** Pod `[World] → FirstZone`, prawym → Create Empty → `[Zone1Trucks]`. Position `(0, 0, 0)`. Dodaj komponent `Zone1TrucksManager`.

- [ ] **Step 2: Conveyor waypoints** w inspectorze `Zone1TrucksManager.Conveyor Waypoints` (8 punktów):

| Idx | Position | IsActiveSlot | SlotIndex | Note |
|-----|----------|-------------|-----------|------|
| 0 | (-1.5, 0, -0.5) | false | -1 | Wjazd na conveyor (start) |
| 1 | (4, 0, -0.5) | true | 0 | Active slot 1 |
| 2 | (8, 0, -0.5) | true | 1 | Active slot 2 |
| 3 | (12, 0, -0.5) | true | 2 | Active slot 3 (stop slot) |
| 4 | (17, 0, -0.5) | false | -1 | Wyjazd ze slotów (zakręt) |
| 5 | (17, 0, 0.8) | false | -1 | Tył pętli (prawy) |
| 6 | (-1.5, 0, 0.8) | false | -1 | Tył pętli (lewy) |
| 7 | (-1.5, 0, -0.5) | false | -1 | (już wpisany jako idx 0; alternatywnie ostatni waypoint nawiązuje do pierwszego — lista zamknięta przez track) |

> Uwaga: track.cs traktuje listę jako pętlę zamkniętą — ostatni waypoint łączy się z pierwszym automatycznie. Idx 7 zostaw jak idx 6 albo usuń. Najprostsze: 7 elementów (0..6), pętla automatyczna.

- [ ] **Step 3: Wall slots** (3 sloty) w `Wall Slots`:

| Idx | WorldPosition | SlotIndex | IsStopSlot |
|-----|---------------|-----------|------------|
| 0 | (4, 0, -0.5) | 0 | false |
| 1 | (8, 0, -0.5) | 1 | false |
| 2 | (12, 0, -0.5) | 2 | true |

- [ ] **Step 4: Garage GameObject**

Pod `[Zone1Trucks]` → Create Empty → `Garage`. Position `(-2.5, 0, 0.2)`. Dodaj `GarageView`. W inspectorze `Parking Slots` (lokalne pozycje):

| Idx | Local Position |
|-----|----------------|
| 0 | (0, 0, 0) |
| 1 | (-0.7, 0, 0.6) |
| 2 | (0, 0, 0.6) |

- [ ] **Step 5: Conveyor GameObject**

Pod `[Zone1Trucks]` → Create Empty → `Conveyor`. Position `(0, 0, 0)`. Dodaj `LineRenderer` + komponent `ConveyorView`. Material = stwórz `Assets/_Project/Materials/ConveyorMaterial.mat` (Unlit/Color, szary `#666670`).

- [ ] **Step 6: Wiring `Zone1TrucksManager` w inspectorze**

| Pole | Wartość |
|------|---------|
| Balance | `Assets/_Project/Settings/GameBalance.asset` |
| Zone1 Manager | drag `[Zone1Manager]` z hierarchii |
| On Refilling Changed | `Assets/_Project/Settings/Events/OnRefillingChanged.asset` |
| Conveyor View | drag `Conveyor` z hierarchii |
| Truck Speed Units Per Sec | 1.5 |
| Garage View | drag `Garage` z hierarchii |
| Truck View Prefab | `Assets/_Project/Prefabs/TruckPrefab.prefab` |
| Main Camera | drag `Main Camera` |

- [ ] **Step 7: Commit scene**

```bash
git add Assets/_Project/Scenes/Main.unity Assets/_Project/Materials/ConveyorMaterial.mat Assets/_Project/Materials/ConveyorMaterial.mat.meta
git commit -m "feat(scene): wire Zone1Trucks (conveyor pętla, slots, garage) in actual scene coords"
```

---

## Task 11: Manual playtest + tag

- [ ] **Playtest:**
  - Play, kamera widzi ścianę leżącą i ground area pod nią.
  - Trzy ciężarówki (czerwona/pomarańczowa/żółta) stoją w garażu (X≈-2.5, Z≈0.2).
  - Conveyor LineRenderer widoczny jako szara pętla pod ścianą.
  - Tap na ciężarówkę → wjeżdża na conveyor, jedzie pętlą.
  - Tap Refill Wall → ściana fillsię, **conveyor pauzuje** (truck stoi).
  - Po refilu conveyor wznawia.
  - Truck dochodzi do slot 3 (X=12), STOP'uje jeśli ma jabłka w bottom row.
  - Magnet co `1/MagnetRateHz` (5Hz) usuwa jabłko z gridu, increments Load.
  - Po Load=100 (TruckCapacity): truck znika z conveyora, wraca do garażu.

- [ ] **Tests EditMode + PlayMode** wszystkie zielone.

- [ ] **Tag:** `git tag -a mvp-step3 -m "MVP Step 3: Trucks + Conveyor + Magnet + Garage"`

---

## Definicja Done

- ✅ Tap na ciężarówkę → wjazd na conveyor.
- ✅ Conveyor pętla widoczna, ciężarówki jeżdżą.
- ✅ STOP w slot 3 gdy może zbierać.
- ✅ Magnet usuwa owoce z bottom row.
- ✅ Refill button pauzuje conveyor (przez `OnRefillingChanged`).
- ✅ Pełna ciężarówka → wraca do garażu (placeholder do Plan #4).
- ✅ EditMode tests zielone (~15 nowych: ConveyorTrack 5, MagnetSystem 5, Garage 5).

## Out of Plan #3 (na później)

- Animacja "fly to truck" magnetowanego owoca.
- Magnet w slotach 1 i 2 (collect "in motion") — w MVP tylko stop slot.
- Big bottle Plan #4.
- Multiple trucks per kolor (upgrade Plan #5+).
