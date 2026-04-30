# MVP Step 4: Big Bottles + Pour + Racks — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Strefa 2 — 3 duże butelki w SecondZone. Pełna ciężarówka opuszcza conveyor, jedzie do kompatybilnej dużej butelki (matching type lub empty/unreserved), opróżnia tam load. Po opróżnieniu wraca do garażu. Tap na dużą butelkę z sokiem → rozlewanie do racka małych butelek (5 owoców = 1 mała). 3 racki na granicy strefy 2 i 3.

**Plan numer:** 4/8 w sekwencji MVP. Spec: `docs/superpowers/specs/2026-04-29-fruits-juices-design.md` sekcja 4. Branch: `feat/mvp-step4` (już utworzony po merge'u step3).

---

## Scene Layout Reference (read from Main.unity)

| Obiekt | World position | Uwagi |
|--------|---------------|-------|
| `FirstZone` | (0, 0, 0) | Plan #1-#3 — wall + conveyor + garaż |
| `SecondZone` | (5, 0, 0) | TARGET PLAN #4 — 3 duże butelki + 3 racki |
| `ThirdZone` | (5.3, 0, 1) | Plan #5-#6 — gracz + klienci |
| `MainCamera` | (0, 4.62, -3.75) | 45°X looking forward |

**Strefa 2 zajmuje obszar ~X[3.5, 6.5] × Z[-1, +2]** (lokalnie wokół (5, 0, 0)). Dokładne pozycje 3 dużych butelek + racków do tunowania.

---

## File Structure

```
Assets/_Project/Scripts/Zone2_Bottling/
├── Project.Zone2.Bottling.asmdef        ref: Project.Core, Project.Data, Project.Zone1.FruitWall, Project.Zone1.Trucks, Unity.InputSystem
├── BigBottle.cs                          data: capacity, fillAmount, currentType (FruitType?), state
├── BigBottleState.cs                     enum: Empty, Filling, TapAble (when fillAmount > 0)
├── BigBottleRouter.cs                    static: TryAssignTruckToBottle(truck, bottles) → Bottle | null
├── SmallBottleRack.cs                    data: 30 bottles slots, currentType
├── PourController.cs                     pure logic: BigBottle → Rack pour (5fruits=1small)
├── BigBottleView.cs                      MonoBehaviour: 3D ProBuilder model + sok level scale
├── SmallBottleRackView.cs                grid of 30 small sprite renderers
├── Zone2Manager.cs                       orchestrator: tap pour, truck routing handoff
└── (no FlyingFruitsManager — reuses Zone1's pool)

Assets/_Project/Scripts/Zone1_Trucks/Zone1TrucksManager.cs   [MODIFY]
  - HandleFullTrucks: instead of straight-to-garage, ask Zone2Manager.RouteFullTruck(truck)
  - On truck.State = Dumping → wait for Zone2Manager → state = ReturningToGarage

Assets/_Project/Scripts/Zone1_Trucks/Truck.cs                 [MODIFY]
  - Add field: public BigBottle TargetBottle { get; set; }

Assets/_Project/Scripts/Zone1_Trucks/TruckState.cs            [MODIFY]
  - Add states: DrivingToBottle, Dumping

Assets/_Project/Scripts/Zone1_Trucks/TruckView.cs             [MODIFY]
  - Add cases for DrivingToBottle (animate from conveyor exit to bottle) + Dumping (stationary at bottle)

Assets/_Project/Tests/EditMode/
├── BigBottleTests.cs                     state transitions, fill, reservedType lock
├── BigBottleRouterTests.cs               assignment by priority (matching > empty > none)
├── SmallBottleRackTests.cs               capacity, pour, empty
└── PourControllerTests.cs                conversion math, partial pour when rack full
```

---

## Task 0: Branch + asmdef Zone2.Bottling

`Assets/_Project/Scripts/Zone2_Bottling/Project.Zone2.Bottling.asmdef`:
```json
{
    "name": "Project.Zone2.Bottling",
    "rootNamespace": "Project.Zone2.Bottling",
    "references": [
        "Project.Core",
        "Project.Data",
        "Project.Zone1.FruitWall",
        "Project.Zone1.Trucks",
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

Update `Project.Tests.EditMode.asmdef` references — add `"Project.Zone2.Bottling"`.

Commit: `chore: scaffold Project.Zone2.Bottling asmdef`.

---

## Task 1: BigBottle data + state (TDD)

```csharp
// BigBottleState.cs
namespace Project.Zone2.Bottling
{
    public enum BigBottleState { Empty, Filling, TapAble }
}

// BigBottle.cs
using Project.Core;

namespace Project.Zone2.Bottling
{
    public class BigBottle
    {
        public int Id { get; }
        public int Capacity { get; private set; }
        public int FillAmount { get; private set; }
        public FruitType? CurrentType { get; private set; }

        public bool IsEmpty => FillAmount == 0;
        public bool IsFull => FillAmount >= Capacity;
        public BigBottleState State =>
            FillAmount == 0 ? BigBottleState.Empty :
            FillAmount < Capacity ? BigBottleState.Filling :
            BigBottleState.TapAble;

        public BigBottle(int id, int capacity)
        {
            Id = id;
            Capacity = capacity;
        }

        /// <summary>Tries to add `amount` of `type`. Locks type on first add. Returns actual amount added.</summary>
        public int Receive(FruitType type, int amount)
        {
            if (amount <= 0) return 0;
            if (CurrentType.HasValue && CurrentType.Value != type) return 0;
            if (!CurrentType.HasValue) CurrentType = type;
            int free = Capacity - FillAmount;
            int take = amount < free ? amount : free;
            FillAmount += take;
            return take;
        }

        /// <summary>Removes `amount` and returns how much was actually removed. Resets state to Empty if drained.</summary>
        public int Drain(int amount)
        {
            if (amount <= 0 || FillAmount == 0) return 0;
            int take = amount < FillAmount ? amount : FillAmount;
            FillAmount -= take;
            if (FillAmount == 0) CurrentType = null;
            return take;
        }

        public void SetCapacity(int newCapacity) { Capacity = newCapacity; }
    }
}
```

Tests cover: new bottle is Empty / unreserved; first Receive locks type; subsequent Receive of different type returns 0; Drain to 0 unreserves; partial fill / partial drain math; IsFull/State transitions.

---

## Task 2: SmallBottleRack data (TDD)

```csharp
// SmallBottleRack.cs
using Project.Core;

namespace Project.Zone2.Bottling
{
    public class SmallBottleRack
    {
        public int Id { get; }
        public int Capacity { get; }
        public int Count { get; private set; }
        public FruitType? CurrentType { get; private set; }

        public bool IsEmpty => Count == 0;
        public bool IsFull => Count >= Capacity;
        public int FreeSlots => Capacity - Count;

        public SmallBottleRack(int id, int capacity)
        {
            Id = id;
            Capacity = capacity;
        }

        /// <summary>Adds N small bottles of type. Returns actual added (capped by capacity).</summary>
        public int Add(FruitType type, int n)
        {
            if (n <= 0) return 0;
            if (CurrentType.HasValue && CurrentType.Value != type) return 0;
            if (!CurrentType.HasValue) CurrentType = type;
            int actual = n < FreeSlots ? n : FreeSlots;
            Count += actual;
            return actual;
        }

        public int RemoveOne()
        {
            if (Count == 0) return 0;
            Count--;
            if (Count == 0) CurrentType = null;
            return 1;
        }
    }
}
```

Tests: capacity cap, type locking, RemoveOne empties → unreserves.

---

## Task 3: PourController (TDD)

Pure-logic: BigBottle.Drain(N*5), Rack.Add(N type) — limited by rack free slots.

```csharp
// PourController.cs
namespace Project.Zone2.Bottling
{
    public static class PourController
    {
        /// <summary>
        /// Pour from BigBottle to Rack. fruitsPerSmall = how many fruits per small bottle.
        /// Returns number of small bottles spawned.
        /// </summary>
        public static int Pour(BigBottle bottle, SmallBottleRack rack, int fruitsPerSmall)
        {
            if (bottle == null || rack == null || fruitsPerSmall <= 0) return 0;
            if (bottle.IsEmpty) return 0;
            if (!bottle.CurrentType.HasValue) return 0;
            if (rack.CurrentType.HasValue && rack.CurrentType.Value != bottle.CurrentType.Value) return 0;

            int possibleByFill = bottle.FillAmount / fruitsPerSmall;
            int possibleByRack = rack.FreeSlots;
            int actual = possibleByFill < possibleByRack ? possibleByFill : possibleByRack;
            if (actual <= 0) return 0;

            bottle.Drain(actual * fruitsPerSmall);
            rack.Add(bottle.CurrentType.HasValue ? bottle.CurrentType.Value : default, actual);
            return actual;
        }
    }
}
```

Hmm — bug: after `bottle.Drain` with full amount, `bottle.CurrentType` becomes null. Then `rack.Add(bottle.CurrentType.Value)` would NRE. Fix: capture type before drain.

```csharp
var type = bottle.CurrentType.Value;
bottle.Drain(actual * fruitsPerSmall);
rack.Add(type, actual);
```

Tests: pour normal, pour limited by rack capacity, pour blocked by type mismatch, pour empty bottle = 0, pour full rack = 0.

---

## Task 4: BigBottleRouter (TDD)

```csharp
// BigBottleRouter.cs
using System.Collections.Generic;
using Project.Zone1.Trucks;

namespace Project.Zone2.Bottling
{
    public static class BigBottleRouter
    {
        /// <summary>
        /// For a full truck, find best matching big bottle.
        /// Priority 1: bottle matching truck's type with enough free space (FillAmount + truckLoad ≤ Capacity).
        /// Priority 2: bottle Empty/unreserved.
        /// Otherwise null (truck waits).
        /// </summary>
        public static BigBottle FindBottleFor(Truck truck, IReadOnlyList<BigBottle> bottles)
        {
            if (truck == null || bottles == null) return null;

            BigBottle bestPriorityOne = null;
            BigBottle anyEmpty = null;

            foreach (var b in bottles)
            {
                if (b.CurrentType.HasValue && b.CurrentType.Value == truck.FruitColor
                    && b.FillAmount + truck.Load <= b.Capacity)
                {
                    bestPriorityOne = b;
                    break;
                }
                if (b.IsEmpty && anyEmpty == null) anyEmpty = b;
            }

            return bestPriorityOne != null ? bestPriorityOne : anyEmpty;
        }
    }
}
```

Tests: matching type with space (priority 1), only empty (priority 2), all unreservable (returns null), matching but full → fall to empty.

---

## Task 5: BigBottleView (3D MonoBehaviour)

```csharp
// BigBottleView.cs
using UnityEngine;
using Project.Zone1.FruitWall;

namespace Project.Zone2.Bottling
{
    public class BigBottleView : MonoBehaviour
    {
        [Tooltip("Inner cylinder/quad whose Y-scale tracks fill amount (0 empty .. 1 full).")]
        [SerializeField] Transform juiceFillPivot;
        [Tooltip("Renderer of juice fill — color tinted to fruit type.")]
        [SerializeField] Renderer juiceRenderer;

        BigBottle bottle;
        Material juiceMaterial;

        public void Bind(BigBottle bottle)
        {
            this.bottle = bottle;
            if (juiceRenderer != null && juiceMaterial == null)
            {
                juiceMaterial = new Material(juiceRenderer.sharedMaterial);
                juiceRenderer.material = juiceMaterial;
            }
        }

        public Vector3 DumpAnchorWorldPosition => transform.position;

        void LateUpdate()
        {
            if (bottle == null) return;
            float fill = bottle.Capacity > 0 ? Mathf.Clamp01((float)bottle.FillAmount / bottle.Capacity) : 0f;
            if (juiceFillPivot != null)
            {
                var s = juiceFillPivot.localScale;
                s.y = fill;
                juiceFillPivot.localScale = s;
            }
            if (juiceMaterial != null && bottle.CurrentType.HasValue)
                juiceMaterial.color = FruitColorPalette.GetColor(bottle.CurrentType.Value);
        }
    }
}
```

3D model w prefabie: `BigBottlePrefab` z ProBuilder Cylinder (szyjka węższa przez extrude), wewnątrz `JuicePivot` z innym cylindrem (juice fill, animowane scale.y).

---

## Task 6: SmallBottleRackView

Grid 5×6=30 mini-cubeów (lub sprite quads). Każdy włączany/wyłączany na podstawie `rack.Count`.

```csharp
// SmallBottleRackView.cs
using UnityEngine;
using Project.Zone1.FruitWall;

namespace Project.Zone2.Bottling
{
    public class SmallBottleRackView : MonoBehaviour
    {
        [SerializeField] GameObject smallBottleTemplate;
        [SerializeField] Vector3 firstSlotOffset = Vector3.zero;
        [SerializeField] Vector3 stepX = new(0.1f, 0, 0);
        [SerializeField] Vector3 stepY = new(0, 0.1f, 0);
        [SerializeField] int columns = 5;

        SmallBottleRack rack;
        GameObject[] slotInstances;
        Renderer[] slotRenderers;

        public void Bind(SmallBottleRack rack)
        {
            this.rack = rack;
            if (smallBottleTemplate != null)
                smallBottleTemplate.SetActive(false);

            slotInstances = new GameObject[rack.Capacity];
            slotRenderers = new Renderer[rack.Capacity];
            for (int i = 0; i < rack.Capacity; i++)
            {
                int col = i % columns;
                int row = i / columns;
                Vector3 local = firstSlotOffset + col * stepX + row * stepY;
                var go = Instantiate(smallBottleTemplate, transform);
                go.transform.localPosition = local;
                go.SetActive(false);
                slotInstances[i] = go;
                slotRenderers[i] = go.GetComponentInChildren<Renderer>();
            }
        }

        void LateUpdate()
        {
            if (rack == null || slotInstances == null) return;
            Color color = rack.CurrentType.HasValue
                ? FruitColorPalette.GetColor(rack.CurrentType.Value)
                : Color.gray;

            for (int i = 0; i < rack.Capacity; i++)
            {
                bool active = i < rack.Count;
                if (slotInstances[i].activeSelf != active)
                    slotInstances[i].SetActive(active);
                if (active && slotRenderers[i] != null)
                    slotRenderers[i].material.color = color;
            }
        }
    }
}
```

---

## Task 7: Zone2Manager (orchestrator)

```csharp
// Zone2Manager.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Project.Core;
using Project.Data;
using Project.Zone1.Trucks;

namespace Project.Zone2.Bottling
{
    public class Zone2Manager : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] GameBalanceSO balance;

        [Header("Bottles + racks (parallel arrays — index i = bottle i + rack i)")]
        [SerializeField] BigBottleView[] bottleViews;
        [SerializeField] SmallBottleRackView[] rackViews;

        [Header("Camera (tap raycast)")]
        [SerializeField] Camera mainCamera;

        readonly List<BigBottle> bottles = new();
        readonly List<SmallBottleRack> racks = new();

        public IReadOnlyList<BigBottle> Bottles => bottles;

        void Start()
        {
            if (balance == null || bottleViews == null || rackViews == null) return;
            int n = Mathf.Min(bottleViews.Length, rackViews.Length);
            for (int i = 0; i < n; i++)
            {
                var b = new BigBottle(i, balance.BigBottleCapacity);
                bottles.Add(b);
                bottleViews[i].Bind(b);

                var r = new SmallBottleRack(i, balance.RackCapacity);
                racks.Add(r);
                rackViews[i].Bind(r);
            }
        }

        public BigBottle TryRouteTruckToBottle(Truck truck)
        {
            var bottle = BigBottleRouter.FindBottleFor(truck, bottles);
            return bottle;
        }

        /// <summary>Called by Zone1TrucksManager when truck reaches its target bottle.</summary>
        public void DepositTruckLoad(Truck truck, BigBottle bottle)
        {
            int added = bottle.Receive(truck.FruitColor, truck.Load);
            // For simplicity in MVP, assume bottle has space (router checked).
            truck.EmptyLoad();
        }

        void Update()
        {
            HandleTapToPour();
        }

        void HandleTapToPour()
        {
            Vector2? tapPos = null;
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
                tapPos = Touchscreen.current.primaryTouch.position.ReadValue();
            else if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                tapPos = Mouse.current.position.ReadValue();
            if (!tapPos.HasValue || mainCamera == null) return;

            Ray ray = mainCamera.ScreenPointToRay(tapPos.Value);
            if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
            {
                var view = hit.collider.GetComponentInParent<BigBottleView>();
                if (view == null) return;
                int idx = System.Array.IndexOf(bottleViews, view);
                if (idx < 0 || idx >= bottles.Count) return;
                PourController.Pour(bottles[idx], racks[idx], balance.FruitsPerSmallBottle);
            }
        }
    }
}
```

---

## Task 8: Truck routing integration (Zone1 ↔ Zone2)

`TruckState.cs`: add `DrivingToBottle` and `Dumping`.
`Truck.cs`: add `BigBottle TargetBottle` field.
`Zone1TrucksManager.cs`:
- Reference `Zone2Manager`
- `HandleFullTrucks`: instead of return-to-garage, call `zone2.TryRouteTruckToBottle(truck)`. If found → `truck.TargetBottle = bottle`, `truck.State = DrivingToBottle`, `track.RemoveTruckFromSlot(truck)` (truck leaves conveyor).
- New per-Update method: process trucks in `DrivingToBottle` (animate position toward bottle.DumpAnchorWorldPosition). When close enough → `truck.State = Dumping`, call `zone2.DepositTruckLoad(truck, bottle)`, after small delay → `truck.State = ReturningToGarage`. Garage pickup remains.

`TruckView.cs`: add cases for `DrivingToBottle` (lerp from current pos to `bottle.transform.position`) and `Dumping` (stationary at bottle).

---

## Task 9: Scene wiring

In SecondZone (world (5, 0, 0)) create:
- 3 `BigBottleX` (X = 0, 1, 2) — children of SecondZone, each with BigBottleView component + ProBuilder cylinder
- 3 `RackX` — children with SmallBottleRackView, positioned on border between SecondZone and ThirdZone
- `[Zone2Manager]` GameObject, drag bottles + racks references

`Zone1TrucksManager` inspector:
- Add `Zone2 Manager` field, drag from scene

Material `JuiceMaterial.mat` (URP/Unlit, white — runtime tinted by fruit type).

`SmallBottleTemplate.prefab` — small ProBuilder cylinder with Renderer.

---

## Task 10: Manual playtest + tag

- Tap truck spawner → trucks fill up at wall via Plan #3 mechanics
- Truck full → drives to bottle → dumps (bottle level rises)
- Tap big bottle → small bottles spawn in rack (5 fruits = 1 small bottle)
- Bottle drains, rack fills
- After bottle empty → Type unlocks for next truck
- Tag: `mvp-step4`

---

## Definicja Done

- ✅ EditMode tests: BigBottle ~6, BigBottleRouter ~4, SmallBottleRack ~4, PourController ~5
- ✅ Truck full routes to compatible bottle, dumps, returns to garage
- ✅ Tap big bottle → rack fills with 5fruits=1bottle conversion
- ✅ Multiple bottles in parallel (different types)
- ✅ Rack full → tap blocked or partial pour

## Out of Plan #4

- Buffer queue dla trucków gdy nie ma kompatybilnej butelki (na razie idą nadpis bezpośrednio do garażu — placeholder)
- Animacja zsypywania ciężarówki do butelki (fruit fly anim z cell→bottle)
- Animacja rozlewania (juice splash, "wzrastanie" małych butelek 1 po 1)
- Player + customers — Plan #5
