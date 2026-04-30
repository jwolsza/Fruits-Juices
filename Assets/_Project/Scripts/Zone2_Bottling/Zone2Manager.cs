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
        [SerializeField] int maxBottles = 8;
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

        [Header("Camera (tap raycast)")]
        [SerializeField] Camera mainCamera;

        readonly List<BigBottle> bottles = new();
        readonly List<SmallBottleRack> racks = new();
        readonly List<BigBottleView> bottleViews = new();
        readonly List<SmallBottleRackView> rackViews = new();

        public IReadOnlyList<BigBottle> Bottles => bottles;
        public IReadOnlyList<SmallBottleRack> Racks => racks;
        public int BottleCount => bottles.Count;
        public int MaxBottles => maxBottles;
        public bool CanAddBottle() => bottles.Count < maxBottles && bottlePrefab != null && rackPrefab != null;

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
            int perRow = Mathf.Max(1, bottlesPerRow);
            int col = i % perRow;
            int row = i / perRow;

            var bottleView = Instantiate(bottlePrefab, transform);
            bottleView.name = $"BigBottle{i}";
            bottleView.transform.localPosition = firstBottleOffset
                                                 + col * bottleStepOffset
                                                 + row * bottleRowStepOffset;
            var bottle = new BigBottle(i, balance.BigBottleCapacity);
            bottles.Add(bottle);
            bottleView.Bind(bottle);
            bottleViews.Add(bottleView);

            var rackView = Instantiate(rackPrefab, transform);
            rackView.name = $"Rack{i}";
            rackView.transform.localPosition = firstRackOffset
                                               + col * rackStepOffset
                                               + row * rackRowStepOffset;
            var rack = new SmallBottleRack(i, balance.RackCapacity);
            racks.Add(rack);
            rackView.Bind(rack);
            rackViews.Add(rackView);
        }

        public BigBottle TryReserveTruckBottle(FruitType truckFruitColor, int truckLoad)
        {
            var bottle = BigBottleRouter.FindBottleFor(truckFruitColor, truckLoad, bottles);
            if (bottle == null) return null;
            return bottle.TryReserve(truckFruitColor, truckLoad) ? bottle : null;
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
                PourController.Pour(bottles[idx], racks[idx], balance.FruitsPerSmallBottle);
            }
        }
    }
}
