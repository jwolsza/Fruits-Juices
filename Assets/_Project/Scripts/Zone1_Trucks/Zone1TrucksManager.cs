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
        [Tooltip("Wizualna długość pojedynczej ciężarówki na torze (world units). Używana do dynamic slot count.")]
        [SerializeField] float truckLengthWorldUnits = 1f;
        [Tooltip("Wymagany odstęp między ciężarówkami na torze (world units).")]
        [SerializeField] float gapBetweenTrucksWorldUnits = 0.3f;

        [Header("Conveyor entry point")]
        [Tooltip("Waypoint index gdzie nowe ciężarówki wjeżdżają na conveyor.")]
        [SerializeField] int entryWaypointIndex = 5;
        [Tooltip("Tolerancja (track-param 0..1) — slot uznany za 'pod entry' jeśli w tym oknie.")]
        [SerializeField] float entryWindowTrackParam = 0.05f;
        [Tooltip("Pozycje czekających ciężarówek (między garaż a entry waypoint, lerp t).")]
        [SerializeField] float waitingFirstOffset = 0.7f;
        [SerializeField] float waitingStepOffset = -0.15f;

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

        ConveyorTrack track;
        Garage garage;
        readonly List<Truck> trucks = new();
        readonly Dictionary<int, TruckView> truckViews = new();
        readonly List<int> waitingDispatchQueue = new();

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

            // Try immediate assignment AT entry waypoint slot (only if a slot is sitting under entry now).
            float entryParam = track.GetWaypointTrackParam(entryWaypointIndex);
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
            if (entryWaypointIndex < 0 || entryWaypointIndex >= track.Waypoints.Count) return;

            Vector3 entryPos = track.Waypoints[entryWaypointIndex].Position;
            for (int i = 0; i < waitingDispatchQueue.Count; i++)
            {
                int id = waitingDispatchQueue[i];
                var truck = garage.Get(id);
                if (truck == null) continue;
                if (!truckViews.TryGetValue(id, out var view) || view == null) continue;

                Vector3 garagePos = garageView.GetParkPositionFor(id);
                float t = Mathf.Clamp01(waitingFirstOffset + i * waitingStepOffset);
                Vector3 waitingPos = Vector3.Lerp(garagePos, entryPos, t);
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
                track.RemoveTruckFromSlot(truck);
                truck.State = TruckState.InGarage;
                truck.EmptyLoad();
                if (truckViews.TryGetValue(truck.Id, out var view))
                    view.SetGaragePosition(garageView.GetParkPositionFor(truck.Id));
                waitingDispatchQueue.Remove(truck.Id);
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
