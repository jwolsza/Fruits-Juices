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
        [SerializeField] BoolEventChannelSO onRefillingChanged;

        [Header("Conveyor")]
        [SerializeField] List<ConveyorWaypoint> conveyorWaypoints;
        [SerializeField] ConveyorView conveyorView;
        [Tooltip("World units per second.")]
        [SerializeField] float truckSpeedUnitsPerSec = 1.5f;
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

        [Header("Wall slots (active spots)")]
        [SerializeField] List<WallSlot> wallSlots;

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
        [Tooltip("Distance threshold (world) below which truck is considered arrived at dump target.")]
        [SerializeField] float dumpArriveDistance = 0.3f;
        [Tooltip("Czas trwania stanu Dumping (sec) zanim truck wraca do garażu.")]
        [SerializeField] float dumpDurationSec = 0.6f;

        ConveyorTrack track;
        Garage garage;
        readonly List<Truck> trucks = new();
        readonly Dictionary<int, TruckView> truckViews = new();
        readonly List<int> waitingDispatchQueue = new();
        readonly Dictionary<int, BigBottle> truckTargetBottles = new();
        readonly Dictionary<int, float> truckDumpTimer = new();
        int nextTruckId = 1;

        float magnetAccumulator;
        int magnetTickIndex;
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
                enabled = false;
                return;
            }

            track = ConveyorTrack.CreateWithDynamicSlots(
                conveyorWaypoints,
                truckLengthWorldUnits,
                gapBetweenTrucksWorldUnits);
            conveyorView.Build(track.Waypoints);
            garage = new Garage();

            foreach (var fruit in balance.StartingFruitTypes)
                AddTruck(fruit);
        }

        public bool AddTruck(FruitType type)
        {
            if (garageView == null || track == null) return false;
            if (garage.TruckCount >= garageView.MaxParkingSlots) return false;

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
            return true;
        }

        public int GetTruckCount(FruitType type)
        {
            int count = 0;
            foreach (var t in trucks) if (t.FruitColor == type) count++;
            return count;
        }

        public bool CanAddTruck() => garageView != null && garage != null && garage.TruckCount < garageView.MaxParkingSlots;

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

            // Recompute MinSlotSpacing relative to new totalLength.
            if (track.TotalLength > 0f)
                track.MinSlotSpacing = truckLengthWorldUnits / track.TotalLength;

            conveyorView?.Build(track.Waypoints);
        }

        void Update()
        {
            float dt = Time.deltaTime;

            HandleTapDispatch();

            // Global pause = refill only. Per-slot stops handled below.
            track.Paused = isRefilling;

            UpdateSlotStopStates();
            track.Tick(dt, truckSpeedUnitsPerSec);

            ProcessWaitingDispatchQueue();
            UpdateWaitingTruckPositions();

            float magnetInterval = 1f / Mathf.Max(0.01f, balance.MagnetRateHz);
            magnetAccumulator += dt;
            while (magnetAccumulator >= magnetInterval)
            {
                magnetAccumulator -= magnetInterval;
                if (!isRefilling) RunMagnetTick();
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

        void UpdateSlotStopStates()
        {
            // Reset all flags then mark at most ONE slot as stopped — the slot whose truck
            // is closest to the stop wall slot AND can still collect. Otherwise trailing
            // trucks clamped right behind would also fall in the stopWindow and freeze the
            // whole queue.
            foreach (var slot in track.Slots) slot.IsStopped = false;

            int stopSlotIdx = -1;
            for (int i = 0; i < wallSlots.Count; i++)
                if (wallSlots[i].IsStopSlot) { stopSlotIdx = i; break; }
            if (stopSlotIdx < 0) return;

            float stopParam = ApproximateTrackParamForWorldPos(wallSlots[stopSlotIdx].WorldPosition);
            const float stopWindow = 0.02f;

            ConveyorSlot best = null;
            float bestDist = stopWindow;
            foreach (var slot in track.Slots)
            {
                if (slot.IsEmpty) continue;
                if (!CanTruckStillCollect(slot.Truck)) continue;
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

            var activeTrucks = new List<Truck>();
            foreach (var wallSlot in wallSlots)
            {
                Truck nearest = FindTruckAtConveyorSlotNear(wallSlot.WorldPosition);
                if (nearest != null) activeTrucks.Add(nearest);
            }

            var assignments = MagnetSystem.AssignFruitsToTrucksAtSlots(grid, activeTrucks, magnetTickIndex);
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

        Truck FindTruckAtConveyorSlotNear(Vector3 wallSlotWorldPos)
        {
            float bestDist = 1.0f;
            Truck best = null;
            foreach (var slot in track.Slots)
            {
                if (slot.IsEmpty) continue;
                Vector3 slotWorldPos = track.GetWorldPositionAtTrackParam(slot.TrackPosition);
                float d = Vector3.Distance(slotWorldPos, wallSlotWorldPos);
                if (d < bestDist) { bestDist = d; best = slot.Truck; }
            }
            return best;
        }

        void HandleFullTrucks()
        {
            foreach (var truck in trucks)
            {
                if (truck.State != TruckState.OnConveyor) continue;
                if (!truck.IsFull) continue;

                // Try to route to a compatible bottle. If no Zone2 or no compatible bottle —
                // fall back to direct return to garage (placeholder behavior).
                BigBottle bottle = zone2Manager != null
                    ? zone2Manager.TryRouteTruck(truck.FruitColor, truck.Load)
                    : null;

                track.RemoveTruckFromSlot(truck);
                waitingDispatchQueue.Remove(truck.Id);

                if (bottle != null)
                {
                    truck.State = TruckState.DrivingToBottle;
                    truck.DumpTargetWorldPos = zone2Manager.GetBottleWorldPosition(bottle);
                    truckTargetBottles[truck.Id] = bottle;
                }
                else
                {
                    truck.State = TruckState.ReturningToGarage;
                    truck.EmptyLoad();
                    if (truckViews.TryGetValue(truck.Id, out var view))
                        view.SetGaragePosition(garageView.GetParkPositionFor(truck.Id));
                }
            }
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
                        if (truckTargetBottles.TryGetValue(truck.Id, out var bottle) && zone2Manager != null)
                            zone2Manager.Deposit(bottle, truck.FruitColor, truck.Load);
                        truck.EmptyLoad();
                        truck.State = TruckState.Dumping;
                        truckDumpTimer[truck.Id] = dumpDurationSec;
                    }
                }
                else if (truck.State == TruckState.Dumping)
                {
                    float t = truckDumpTimer.TryGetValue(truck.Id, out var v) ? v - dt : 0f;
                    truckDumpTimer[truck.Id] = t;
                    if (t <= 0f)
                    {
                        truck.State = TruckState.ReturningToGarage;
                        truckTargetBottles.Remove(truck.Id);
                        truckDumpTimer.Remove(truck.Id);
                        if (truckViews.TryGetValue(truck.Id, out var view))
                            view.SetGaragePosition(garageView.GetParkPositionFor(truck.Id));
                    }
                }
                else if (truck.State == TruckState.ReturningToGarage)
                {
                    if (!truckViews.TryGetValue(truck.Id, out var view) || view == null) continue;
                    float dist = Vector3.Distance(view.transform.position, garageView.GetParkPositionFor(truck.Id));
                    if (dist <= dumpArriveDistance)
                        truck.State = TruckState.InGarage;
                }
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
