using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Project.Core;
using Project.Data;
using Project.Zone1.FruitWall;
using Project.Zone2.Bottling;

namespace Project.Zone1.Trucks
{
    public class Zone1TrucksManager : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] GameBalanceSO balance;
        [SerializeField] Zone1Manager zone1Manager;

        [Header("Conveyor")]
        [SerializeField] List<ConveyorWaypoint> conveyorWaypoints;
        [SerializeField] ConveyorView conveyorView;
        [Tooltip("World units per second.")]
        [SerializeField] float truckSpeedUnitsPerSec = 1.5f;
        public float TruckSpeedUnitsPerSec
        {
            get => truckSpeedUnitsPerSec;
            set => truckSpeedUnitsPerSec = Mathf.Max(0f, value);
        }
        [Tooltip("Wizualna długość pojedynczej ciężarówki na torze (world units). Używana do dynamic slot count.")]
        [SerializeField] float truckLengthWorldUnits = 1f;
        [Tooltip("Wymagany odstęp między ciężarówkami na torze (world units).")]
        [SerializeField] float gapBetweenTrucksWorldUnits = 0.3f;

        [Header("Conveyor entry point")]
        [Tooltip("Waypoint index gdzie nowe ciężarówki wjeżdżają na conveyor.")]
        [SerializeField] int entryWaypointIndex = 5;
        [Tooltip("Tolerancja (track-param 0..1) — slot uznany za 'pod entry' jeśli w tym oknie.")]
        [SerializeField] float entryWindowTrackParam = 0.05f;
        [Tooltip("Offset pierwszej czekającej ciężarówki względem jej pozycji w garażu (world units).")]
        [SerializeField] Vector3 waitingFirstOffset = new(0f, 0f, 0.3f);
        [Tooltip("Dodatkowy offset per kolejny indeks w queue (kolejne czekające stoją dalej od pierwszego).")]
        [SerializeField] Vector3 waitingStepOffset = new(0f, 0f, 0.2f);

        [Header("Collecting zone (track param range between two waypoints)")]
        [Tooltip("Waypoint indeks startu strefy zbierania. Truck zbiera magnetem gdy jego TrackPosition ∈ [start, end].")]
        [SerializeField] int collectStartWaypointIndex = 4;
        [Tooltip("Waypoint indeks końca strefy zbierania. Tu też zatrzymuje truck który jeszcze może zbierać.")]
        [SerializeField] int collectEndWaypointIndex = 6;
        [Tooltip("Tolerancja (track-param 0..1) dla zatrzymania przy waypoincie końca strefy.")]
        [SerializeField] float collectStopWindow = 0.02f;

        [Header("Garage")]
        [SerializeField] GarageView garageView;
        [SerializeField] GameObject truckViewPrefab;

        [Header("Camera (for tap raycast)")]
        [SerializeField] Camera mainCamera;

        [Header("Flying fruit animation")]
        [SerializeField] FlyingFruitPool flyingFruitPool;
        [SerializeField] FruitWall.WallView wallView;

        [Header("Zone 2 (bottling)")]
        [SerializeField] Zone2Manager zone2Manager;
        [Tooltip("Pierwszy waypoint między conveyorem a butelkami. Truck dojeżdża tu zanim zjedzie do rzędu butelek (i wraca przez ten punkt do garażu).")]
        [SerializeField] Transform pathStartPoint;
        [Tooltip("Distance threshold (world) below which truck is considered arrived at dump target.")]
        [SerializeField] float dumpArriveDistance = 0.3f;
        [Tooltip("Tempo dumpingu — ile owoców na sekundę leci z ciężarówki do butelki.")]
        [SerializeField] float dumpRateFruitsPerSec = 80f;
        [Tooltip("Rozmiar (world) lecącego owocu — małe, dużo na raz.")]
        [SerializeField] Vector2 dumpFruitSize = new(0.05f, 0.05f);

        ConveyorTrack track;
        Garage garage;
        readonly List<Truck> trucks = new();
        readonly Dictionary<int, TruckView> truckViews = new();
        readonly List<int> waitingDispatchQueue = new();
        readonly Dictionary<int, BigBottle> truckTargetBottles = new();
        readonly Dictionary<int, int> truckCurrentReservedAmount = new();
        readonly Dictionary<int, float> truckDumpEmitAccumulator = new();
        int nextTruckId = 1;

        float magnetAccumulator;
        int magnetTickIndex;

        void Start()
        {
            if (balance == null || zone1Manager == null || conveyorView == null
                || garageView == null || truckViewPrefab == null)
            {
                Debug.LogError("[Zone1TrucksManager] missing references");
                enabled = false;
                return;
            }

            track = ConveyorTrack.CreateWithDynamicSlots(
                conveyorWaypoints,
                truckLengthWorldUnits,
                gapBetweenTrucksWorldUnits);
            conveyorView.Build(track.Waypoints);
            garage = new Garage();

            var initialTypes = balance.InitialTruckTypes ?? balance.StartingFruitTypes;
            if (initialTypes != null)
                foreach (var fruit in initialTypes) AddTruck(fruit);

            // Even if no trucks spawned, pool needs starter types right away.
            RefreshRefillPool();
        }

        public bool AddTruck(FruitType type)
        {
            if (garageView == null || track == null) return false;
            if (garage.TruckCount >= garageView.MaxParkingSlots) return false;
            if (GetTruckCount(type) >= 1) return false; // limit: 1 truck per type

            var truck = new Truck(nextTruckId++, type, balance.TruckCapacity);
            garage.AddStarterTruck(truck);
            trucks.Add(truck);

            var go = Instantiate(truckViewPrefab, transform);
            go.name = $"TruckView_{type}_{truck.Id}";
            var view = go.GetComponent<TruckView>();

            // Register FIRST so GetParkPositionFor can resolve the truck's index
            // (IndexOf in orderedTruckIds). Without this all trucks fall back to garage origin.
            garageView.RegisterTruckView(truck.Id, view);
            truckViews[truck.Id] = view;

            Vector3 parkPos = garageView.GetParkPositionFor(truck.Id);
            view.Bind(truck, track, parkPos);

            RefreshRefillPool();
            return true;
        }

        void RefreshRefillPool()
        {
            if (zone1Manager == null) return;
            var unique = new HashSet<FruitType>();
            // Starter types (Apple, Orange, Lemon) zawsze w poolu — nawet bez trucka.
            if (balance != null && balance.StartingFruitTypes != null)
                foreach (var t in balance.StartingFruitTypes) unique.Add(t);
            // Plus typy obecnie posiadanych trucków.
            foreach (var t in trucks) unique.Add(t.FruitColor);
            var arr = new FruitType[unique.Count];
            int i = 0;
            foreach (var ft in unique) arr[i++] = ft;
            zone1Manager.SetRefillFruitPool(arr);
        }

        public int GetTruckCount(FruitType type)
        {
            int count = 0;
            foreach (var t in trucks) if (t.FruitColor == type) count++;
            return count;
        }

        public bool CanAddTruck() => garageView != null && garage != null && garage.TruckCount < garageView.MaxParkingSlots;

        public bool CanAddTruckOfType(FruitType type) => CanAddTruck() && GetTruckCount(type) < 1;

        /// <summary>True jeśli choć jedna ciężarówka aktywnie zbiera — w strefie, nie pełna,
        /// I jej kolor jest dostępny na dolnym rzędzie ściany (czyli magnet faktycznie coś jej da).</summary>
        public bool IsAnyTruckCollecting()
        {
            if (track == null) return false;
            foreach (var slot in track.Slots)
            {
                if (slot.IsEmpty || slot.Truck == null) continue;
                if (!IsTrackParamInCollectingRange(slot.TrackPosition)) continue;
                if (!CanTruckStillCollect(slot.Truck)) continue;
                return true;
            }
            return false;
        }

        bool IsTrackParamInCollectingRange(float p)
        {
            if (track == null) return false;
            float a = track.GetWaypointTrackParam(collectStartWaypointIndex);
            float b = track.GetWaypointTrackParam(collectEndWaypointIndex);
            p = Mathf.Repeat(p, 1f);
            a = Mathf.Repeat(a, 1f);
            b = Mathf.Repeat(b, 1f);
            return a <= b ? (p >= a && p <= b) : (p >= a || p <= b);
        }

        bool CanTruckStillCollect(Truck truck)
        {
            if (truck == null || truck.IsFull) return false;
            var grid = zone1Manager != null ? zone1Manager.Grid : null;
            if (grid == null) return false;
            for (int x = 0; x < grid.Columns; x++)
                if (grid.GetCell(x, 0) == truck.FruitColor) return true;
            return false;
        }

        /// <summary>
        /// Stop the front-most truck (closest to collectEndWaypointIndex) that still has work
        /// to do — i.e. nie pełna i grid ma jeszcze pasujące owoce. Pojedyncza ciężarówka stoi,
        /// ogonek za nią dalej zbiera magnetem przejazdem (też w [start,end]).
        /// </summary>
        void UpdateCollectStopStates()
        {
            if (track == null) return;
            foreach (var slot in track.Slots) slot.IsStopped = false;

            float stopParam = track.GetWaypointTrackParam(collectEndWaypointIndex);

            ConveyorSlot best = null;
            float bestDist = collectStopWindow;
            foreach (var slot in track.Slots)
            {
                if (slot.IsEmpty || slot.Truck == null) continue;
                if (!CanTruckStillCollect(slot.Truck)) continue;
                // Must STILL be inside collecting zone — don't freeze trucks that already drifted past waypoint end
                // (they'd be stopped but outside magnet range → stuck forever).
                if (!IsTrackParamInCollectingRange(slot.TrackPosition)) continue;
                float dist = Mathf.Abs(((slot.TrackPosition - stopParam) + 1f) % 1f);
                dist = Mathf.Min(dist, 1f - dist);
                if (dist <= bestDist)
                {
                    bestDist = dist;
                    best = slot;
                }
            }
            if (best != null) best.IsStopped = true;
        }

        /// <summary>
        /// Expands (or contracts) the conveyor by moving waypoints by `step` on X.
        /// Indexes 1,2,3,4 → -X (left side). Indexes 0,6,7,8 → +X (right side). Index 5 stays fixed.
        /// Slots keep their normalized track positions so they auto-adjust.
        /// </summary>
        public void ExpandTrack(float xStep)
        {
            if (track == null || conveyorWaypoints == null || conveyorWaypoints.Count == 0) return;

            for (int i = 0; i < conveyorWaypoints.Count; i++)
            {
                if (i == 5) continue;
                var wp = conveyorWaypoints[i];
                bool leftSide = (i >= 1 && i <= 4);
                wp.Position += new Vector3(leftSide ? -xStep : xStep, 0f, 0f);
                conveyorWaypoints[i] = wp;
            }

            track.RebuildFromWaypoints(conveyorWaypoints);

            if (track.TotalLength > 0f)
            {
                track.MinSlotSpacing = truckLengthWorldUnits / track.TotalLength;

                // Recompute desired slot count from new track length and add slots if needed.
                float perSlot = Mathf.Max(0.0001f, truckLengthWorldUnits + gapBetweenTrucksWorldUnits);
                int newCount = Mathf.Max(1, Mathf.FloorToInt(track.TotalLength / perSlot));
                if (newCount > track.Slots.Count) track.EnsureSlotCount(newCount);
            }

            conveyorView?.Build(track.Waypoints);
        }

        void Update()
        {
            float dt = Time.deltaTime;

            HandleTapDispatch();

            UpdateCollectStopStates();
            track.Tick(dt, truckSpeedUnitsPerSec);

            ProcessWaitingDispatchQueue();
            UpdateWaitingTruckPositions();

            float magnetInterval = 1f / Mathf.Max(0.01f, balance.MagnetRateHz);
            magnetAccumulator += dt;
            while (magnetAccumulator >= magnetInterval)
            {
                magnetAccumulator -= magnetInterval;
                RunMagnetTick();
            }

            HandleFullTrucks();
            ProcessBottleRouting(dt);
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
            if (tappedId < 0) return;

            var truck = garage.Get(tappedId);
            if (truck == null || truck.State != TruckState.InGarage) return;

            // Capacity guard: occupied conveyor slots + already queued must leave room for one more.
            int occupied = track.Slots.Count - track.EmptySlotCount;
            if (occupied + waitingDispatchQueue.Count >= track.Slots.Count) return; // path full, reject

            float entryParam = track.GetWaypointTrackParam(entryWaypointIndex);

            // Conveyor is fully empty AND no one queued → snap empty slot to entry, dispatch immediately.
            if (occupied == 0 && waitingDispatchQueue.Count == 0)
            {
                if (track.ForceAssignTruckAt(truck, entryParam)) return;
            }

            // Slot already drifted into entry window → enter immediately.
            bool placed = waitingDispatchQueue.Count == 0
                && track.TryAssignTruckAtTrackParam(truck, entryParam, entryWindowTrackParam);
            if (placed) return;

            // Otherwise queue truck for waiting dispatch.
            truck.State = TruckState.WaitingDispatch;
            waitingDispatchQueue.Add(truck.Id);
        }

        void ProcessWaitingDispatchQueue()
        {
            if (waitingDispatchQueue.Count == 0) return;
            float entryParam = track.GetWaypointTrackParam(entryWaypointIndex);

            int firstId = waitingDispatchQueue[0];
            var truck = garage.Get(firstId);
            if (truck == null || truck.State != TruckState.WaitingDispatch)
            {
                waitingDispatchQueue.RemoveAt(0);
                return;
            }
            if (track.TryAssignTruckAtTrackParam(truck, entryParam, entryWindowTrackParam))
                waitingDispatchQueue.RemoveAt(0);
        }

        void UpdateWaitingTruckPositions()
        {
            if (waitingDispatchQueue.Count == 0) return;

            for (int i = 0; i < waitingDispatchQueue.Count; i++)
            {
                int id = waitingDispatchQueue[i];
                var truck = garage.Get(id);
                if (truck == null) continue;
                if (!truckViews.TryGetValue(id, out var view) || view == null) continue;

                Vector3 garagePos = garageView.GetParkPositionFor(id);
                Vector3 waitingPos = garagePos + waitingFirstOffset + i * waitingStepOffset;
                view.SetWaitingPosition(waitingPos);
            }
        }

        readonly List<Truck> activeTrucksBuffer = new();

        void RunMagnetTick()
        {
            var grid = zone1Manager.Grid;
            if (grid == null) return;

            activeTrucksBuffer.Clear();
            foreach (var slot in track.Slots)
            {
                if (slot.IsEmpty || slot.Truck == null) continue;
                if (!IsTrackParamInCollectingRange(slot.TrackPosition)) continue;
                activeTrucksBuffer.Add(slot.Truck);
            }

            var assignments = MagnetSystem.AssignFruitsToTrucksAtSlots(grid, activeTrucksBuffer, magnetTickIndex);
            magnetTickIndex++;

            if (flyingFruitPool != null && wallView != null)
            {
                Vector2 cellSize = wallView.GetCellWorldSize();
                Quaternion wallRot = wallView.transform.rotation;
                foreach (var a in assignments)
                {
                    Vector3 from = wallView.GetCellWorldPosition(a.GridCellRemoved.x, a.GridCellRemoved.y);
                    if (truckViews.TryGetValue(a.Truck.Id, out var view) && view != null)
                    {
                        flyingFruitPool.Fly(view.transform, from, wallRot, a.FruitType, cellSize);
                    }
                }
            }
        }

        void HandleFullTrucks()
        {
            foreach (var truck in trucks)
            {
                if (truck.State != TruckState.OnConveyor) continue;
                if (!truck.IsFull) continue;

                // Try to route to a compatible bottle. If no Zone2 or no compatible bottle —
                // fall back to direct return to garage (placeholder behavior).
                int reserved = 0;
                BigBottle bottle = zone2Manager != null
                    ? zone2Manager.TryReserveTruckBottle(truck.FruitColor, truck.Load, out reserved)
                    : null;

                track.RemoveTruckFromSlot(truck);
                waitingDispatchQueue.Remove(truck.Id);

                if (bottle != null)
                {
                    BeginDriveToBottle(truck, bottle, reserved, fromConveyor: true, previousBottle: null);
                }
                else
                {
                    truck.EmptyLoad();
                    BeginReturnToGarage(truck, fromBottle: null);
                }
            }
        }

        Vector3 WithPathY(Vector3 p)
        {
            if (pathStartPoint == null) return p;
            p.y = pathStartPoint.position.y;
            return p;
        }

        void BeginDriveToBottle(Truck truck, BigBottle bottle, int reserved, bool fromConveyor, BigBottle previousBottle)
        {
            truck.State = TruckState.DrivingToBottle;
            truckTargetBottles[truck.Id] = bottle;
            truckCurrentReservedAmount[truck.Id] = reserved;

            truck.WaypointQueue.Clear();
            Vector3 finalTarget = WithPathY(zone2Manager.GetBottleWorldPosition(bottle));
            Vector3 rowEntry = WithPathY(zone2Manager.GetRowEntryWorldPosition(bottle));
            Vector3? userWp = pathStartPoint != null ? pathStartPoint.position : (Vector3?)null;

            // Coming straight from a previous bottle in the SAME row → just drive across the row.
            if (previousBottle != null
                && zone2Manager.GetBottleRow(previousBottle) == zone2Manager.GetBottleRow(bottle))
            {
                truck.DumpTargetWorldPos = finalTarget;
                return;
            }

            // From a previous bottle in a DIFFERENT row → exit old row first, then user wp, then enter new row.
            if (previousBottle != null)
            {
                Vector3 oldRowEntry = WithPathY(zone2Manager.GetRowEntryWorldPosition(previousBottle));
                truck.DumpTargetWorldPos = oldRowEntry;
                if (userWp.HasValue) truck.WaypointQueue.Enqueue(userWp.Value);
                truck.WaypointQueue.Enqueue(rowEntry);
                truck.WaypointQueue.Enqueue(finalTarget);
                return;
            }

            // From conveyor: user wp → row entry → final.
            if (userWp.HasValue)
            {
                truck.DumpTargetWorldPos = userWp.Value;
                truck.WaypointQueue.Enqueue(rowEntry);
                truck.WaypointQueue.Enqueue(finalTarget);
            }
            else
            {
                truck.DumpTargetWorldPos = rowEntry;
                truck.WaypointQueue.Enqueue(finalTarget);
            }
        }

        void BeginReturnToGarage(Truck truck, BigBottle fromBottle)
        {
            truck.State = TruckState.ReturningToGarage;
            truck.WaypointQueue.Clear();
            Vector3 garagePos = garageView.GetParkPositionFor(truck.Id);
            Vector3? userWp = pathStartPoint != null ? pathStartPoint.position : (Vector3?)null;

            if (fromBottle != null && zone2Manager != null)
            {
                Vector3 rowEntry = WithPathY(zone2Manager.GetRowEntryWorldPosition(fromBottle));
                truck.DumpTargetWorldPos = rowEntry;
                if (userWp.HasValue) truck.WaypointQueue.Enqueue(userWp.Value);
                truck.WaypointQueue.Enqueue(garagePos);
            }
            else if (userWp.HasValue)
            {
                truck.DumpTargetWorldPos = userWp.Value;
                truck.WaypointQueue.Enqueue(garagePos);
            }
            else
            {
                truck.DumpTargetWorldPos = garagePos;
            }

            if (truckViews.TryGetValue(truck.Id, out var view))
                view.SetGaragePosition(garagePos);
        }

        bool TryRouteRemainingLoad(Truck truck, BigBottle previousBottle)
        {
            if (zone2Manager == null || truck.Load <= 0) return false;
            int reserved = 0;
            var bottle = zone2Manager.TryReserveTruckBottle(truck.FruitColor, truck.Load, out reserved);
            if (bottle == null) return false;
            BeginDriveToBottle(truck, bottle, reserved, fromConveyor: false, previousBottle: previousBottle);
            truckDumpEmitAccumulator.Remove(truck.Id);
            return true;
        }

        void ProcessBottleRouting(float dt)
        {
            foreach (var truck in trucks)
            {
                if (truck.State == TruckState.DrivingToBottle)
                {
                    if (!truckViews.TryGetValue(truck.Id, out var view) || view == null) continue;
                    float dist = Vector3.Distance(view.transform.position, truck.DumpTargetWorldPos);
                    if (dist <= dumpArriveDistance)
                    {
                        if (truck.WaypointQueue.Count > 0)
                        {
                            truck.DumpTargetWorldPos = truck.WaypointQueue.Dequeue();
                        }
                        else
                        {
                            truck.State = TruckState.Dumping;
                            truckDumpEmitAccumulator[truck.Id] = 0f;
                        }
                    }
                }
                else if (truck.State == TruckState.Dumping)
                {
                    if (!truckTargetBottles.TryGetValue(truck.Id, out var bottle))
                    {
                        truck.State = TruckState.ReturningToGarage;
                        continue;
                    }

                    int currentReserved = truckCurrentReservedAmount.GetValueOrDefault(truck.Id, 0);
                    if (truck.Load > 0 && currentReserved > 0)
                    {
                        float accum = truckDumpEmitAccumulator.GetValueOrDefault(truck.Id, 0f) + dumpRateFruitsPerSec * dt;
                        int emit = Mathf.Min((int)accum, Mathf.Min(truck.Load, currentReserved));
                        accum -= emit;
                        truckDumpEmitAccumulator[truck.Id] = accum;

                        if (emit > 0)
                        {
                            if (flyingFruitPool != null && truckViews.TryGetValue(truck.Id, out var view) && view != null && zone2Manager != null)
                            {
                                var bottleTransform = zone2Manager.GetBottleTransform(bottle);
                                if (bottleTransform != null)
                                    for (int k = 0; k < emit; k++)
                                        flyingFruitPool.Fly(
                                            bottleTransform,
                                            view.transform.position,
                                            Quaternion.identity,
                                            truck.FruitColor,
                                            dumpFruitSize);
                            }

                            if (zone2Manager != null) zone2Manager.Deposit(bottle, truck.FruitColor, emit);
                            truck.RemoveFruits(emit);
                            currentReserved -= emit;
                            truckCurrentReservedAmount[truck.Id] = currentReserved;
                        }
                    }

                    // Reservation depleted? Reroute remaining load OR return.
                    if (currentReserved <= 0)
                    {
                        var previousBottle = bottle;
                        truckTargetBottles.Remove(truck.Id);
                        truckCurrentReservedAmount.Remove(truck.Id);

                        if (truck.Load > 0 && TryRouteRemainingLoad(truck, previousBottle))
                        {
                            // truck.State = DrivingToBottle, waypoint path rebuilt; loop continues next frame.
                        }
                        else
                        {
                            truck.EmptyLoad();
                            truckDumpEmitAccumulator.Remove(truck.Id);
                            BeginReturnToGarage(truck, previousBottle);
                        }
                    }
                }
                else if (truck.State == TruckState.ReturningToGarage)
                {
                    if (!truckViews.TryGetValue(truck.Id, out var view) || view == null) continue;
                    float dist = Vector3.Distance(view.transform.position, truck.DumpTargetWorldPos);
                    if (dist <= dumpArriveDistance)
                    {
                        if (truck.WaypointQueue.Count > 0)
                        {
                            truck.DumpTargetWorldPos = truck.WaypointQueue.Dequeue();
                        }
                        else
                        {
                            truck.State = TruckState.InGarage;
                        }
                    }
                }
            }
        }

    }
}
