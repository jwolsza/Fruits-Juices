using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Project.Core;
using Project.Data;

namespace Project.Zone2.Bottling
{
    public class Zone2Manager : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] GameBalanceSO balance;

        [Header("Procedural spawn")]
        [SerializeField] BigBottleView bottlePrefab;
        [SerializeField] SmallBottleRackView rackPrefab;
        [SerializeField] int bottleCount = 3;
        [Tooltip("Pozycja pierwszej butelki (local Zone2Manager).")]
        [SerializeField] Vector3 firstBottleOffset = Vector3.zero;
        [Tooltip("Krok offsetu w obrębie rzędu (zwykle X).")]
        [SerializeField] Vector3 bottleStepOffset = new(1f, 0f, 0f);
        [Tooltip("Krok offsetu między rzędami butelek (zwykle Z).")]
        [SerializeField] Vector3 bottleRowStepOffset = new(0f, 0f, 1f);
        [Tooltip("Ile butelek mieści się w jednym rzędzie zanim zacznie się nowy.")]
        [SerializeField] int bottlesPerRow = 4;

        [Tooltip("Pozycja pierwszego racka (local Zone2Manager).")]
        [SerializeField] Vector3 firstRackOffset = new(0f, 0f, 0.7f);
        [Tooltip("Krok offsetu racków w rzędzie.")]
        [SerializeField] Vector3 rackStepOffset = new(1f, 0f, 0f);
        [Tooltip("Krok offsetu między rzędami racków.")]
        [SerializeField] Vector3 rackRowStepOffset = new(0f, 0f, 1f);
        [Tooltip("Ile racków mieści się w jednym rzędzie zanim zacznie się nowy.")]
        [SerializeField] int racksPerRow = 4;

        [Header("Camera (tap raycast)")]
        [SerializeField] Camera mainCamera;

        readonly List<BigBottle> bottles = new();
        readonly List<SmallBottleRack> racks = new();
        readonly List<BigBottleView> bottleViews = new();
        readonly List<SmallBottleRackView> rackViews = new();

        public IReadOnlyList<BigBottle> Bottles => bottles;
        public IReadOnlyList<SmallBottleRack> Racks => racks;
        public int BottleCount => bottles.Count;
        public int MaxBottles
        {
            get
            {
                if (balance == null) return 0;
                int starting = balance.StartingFruitTypes?.Length ?? 0;
                int locked = balance.LockedFruitTypes?.Length ?? 0;
                return starting + locked;
            }
        }
        public bool CanAddBottle() => bottles.Count < MaxBottles && bottlePrefab != null && rackPrefab != null;

        void Start()
        {
            if (balance == null || bottlePrefab == null || rackPrefab == null)
            {
                Debug.LogError("[Zone2Manager] missing references (balance/bottlePrefab/rackPrefab)");
                return;
            }

            for (int i = 0; i < bottleCount; i++) SpawnBottleAndRack();
        }

        public bool AddBottle()
        {
            if (!CanAddBottle()) return false;
            SpawnBottleAndRack();
            return true;
        }

        void SpawnBottleAndRack()
        {
            int i = bottles.Count;

            int bottlePerRow = Mathf.Max(1, bottlesPerRow);
            int bCol = i % bottlePerRow;
            int bRow = i / bottlePerRow;

            int rackPerRow = Mathf.Max(1, racksPerRow);
            int rCol = i % rackPerRow;
            int rRow = i / rackPerRow;

            var bottleView = Instantiate(bottlePrefab, transform);
            bottleView.name = $"BigBottle{i}";
            bottleView.transform.localPosition = firstBottleOffset
                                                 + bCol * bottleStepOffset
                                                 + bRow * bottleRowStepOffset;
            var bottle = new BigBottle(i, balance.BigBottleCapacity);
            bottles.Add(bottle);
            bottleView.Bind(bottle);
            bottleViews.Add(bottleView);

            var rackView = Instantiate(rackPrefab, transform);
            rackView.name = $"Rack{i}";
            rackView.transform.localPosition = firstRackOffset
                                               + rCol * rackStepOffset
                                               + rRow * rackRowStepOffset;
            var rack = new SmallBottleRack(i, balance.RackCapacity);
            racks.Add(rack);
            rackView.Bind(rack);
            rackViews.Add(rackView);
        }

        /// <summary>
        /// Route + reserve in one shot. Reserves up to min(truckLoad, freeSpace).
        /// Returns null if no bottle accepts even partial reservation.
        /// </summary>
        public BigBottle TryReserveTruckBottle(FruitType truckFruitColor, int truckLoad, out int reservedAmount)
        {
            reservedAmount = 0;
            var bottle = BigBottleRouter.FindBottleFor(truckFruitColor, truckLoad, bottles);
            if (bottle == null) return null;
            int free = bottle.Capacity - bottle.EffectiveLoad;
            int amount = Mathf.Min(truckLoad, free);
            if (amount <= 0) return null;
            if (!bottle.TryReserve(truckFruitColor, amount)) return null;
            reservedAmount = amount;
            return bottle;
        }

        public Vector3 GetBottleWorldPosition(BigBottle bottle)
        {
            if (bottle == null) return Vector3.zero;
            int idx = bottles.IndexOf(bottle);
            if (idx < 0 || idx >= bottleViews.Count || bottleViews[idx] == null) return Vector3.zero;
            return bottleViews[idx].DumpAnchorWorldPosition;
        }

        public Transform GetBottleTransform(BigBottle bottle)
        {
            if (bottle == null) return null;
            int idx = bottles.IndexOf(bottle);
            if (idx < 0 || idx >= bottleViews.Count || bottleViews[idx] == null) return null;
            return bottleViews[idx].transform;
        }

        public void Deposit(BigBottle bottle, FruitType type, int amount)
        {
            if (bottle == null) return;
            bottle.CommitReservation(type, amount);
        }

        public void CancelReservation(BigBottle bottle, int amount)
        {
            if (bottle == null) return;
            bottle.CancelReservation(amount);
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
                int idx = bottleViews.IndexOf(view);
                if (idx < 0 || idx >= bottles.Count) return;
                PourBottleToBestRacks(bottles[idx]);
            }
        }

        /// <summary>
        /// Pours a bottle's content across racks. Priority:
        /// 1) racks already containing matching type (fill them first),
        /// 2) empty/unreserved racks.
        /// Stops when bottle is empty or all racks are full.
        /// </summary>
        public int PourBottleToBestRacks(BigBottle bottle)
        {
            if (bottle == null || bottle.IsEmpty || !bottle.CurrentType.HasValue) return 0;
            int total = 0;
            var type = bottle.CurrentType.Value;

            foreach (var r in racks)
            {
                if (bottle.IsEmpty) break;
                if (r.CurrentType.HasValue && r.CurrentType.Value == type && !r.IsFull)
                    total += PourController.Pour(bottle, r, balance.FruitsPerSmallBottle);
            }

            foreach (var r in racks)
            {
                if (bottle.IsEmpty) break;
                if (r.IsEmpty)
                    total += PourController.Pour(bottle, r, balance.FruitsPerSmallBottle);
            }

            return total;
        }
    }
}
