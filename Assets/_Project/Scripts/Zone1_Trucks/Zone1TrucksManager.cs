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
        [Tooltip("Minimum gap (track-param 0..1) between slots — prevents stacking when piling up behind a stopped slot.")]
        [SerializeField] float minSlotSpacing = 0.05f;

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
                enabled = false;
                return;
            }

            track = new ConveyorTrack(conveyorWaypoints, balance.ConveyorSlotCount);
            track.MinSlotSpacing = minSlotSpacing;
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

            // Try to put truck into first empty conveyor slot. If none, do nothing.
            track.TryAssignTruckToFirstEmptySlot(truck);
        }

        void UpdateSlotStopStates()
        {
            int stopSlotIdx = -1;
            for (int i = 0; i < wallSlots.Count; i++)
                if (wallSlots[i].IsStopSlot) { stopSlotIdx = i; break; }
            if (stopSlotIdx < 0)
            {
                foreach (var slot in track.Slots) slot.IsStopped = false;
                return;
            }

            float stopParam = ApproximateTrackParamForWorldPos(wallSlots[stopSlotIdx].WorldPosition);
            const float stopWindow = 0.02f;

            foreach (var slot in track.Slots)
            {
                if (slot.IsEmpty)
                {
                    slot.IsStopped = false;
                    continue;
                }
                float dist = Mathf.Abs(((slot.TrackPosition - stopParam) + 1f) % 1f);
                dist = Mathf.Min(dist, 1f - dist);
                bool atStopPos = dist <= stopWindow;
                slot.IsStopped = atStopPos && CanTruckStillCollect(slot.Truck);
            }
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
            foreach (var wallSlot in wallSlots)
            {
                Truck nearest = FindTruckAtConveyorSlotNear(wallSlot.WorldPosition);
                if (nearest != null) trucksAtSlots.Add((nearest, wallSlot.WorldPosition.x));
            }

            MagnetSystem.AssignFruitsToTrucksAtSlots(
                grid, trucksAtSlots,
                wallLeftXWorld: zone1Manager.WallLeftXWorld,
                wallWidthWorld: zone1Manager.WallWidthWorldUnits);
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
