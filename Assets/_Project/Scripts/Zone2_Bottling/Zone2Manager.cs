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

        [Header("Bottles + racks (parallel arrays — index i: bottle i + rack i)")]
        [SerializeField] BigBottleView[] bottleViews;
        [SerializeField] SmallBottleRackView[] rackViews;

        [Header("Camera (tap raycast)")]
        [SerializeField] Camera mainCamera;

        readonly List<BigBottle> bottles = new();
        readonly List<SmallBottleRack> racks = new();

        public IReadOnlyList<BigBottle> Bottles => bottles;
        public IReadOnlyList<SmallBottleRack> Racks => racks;

        void Start()
        {
            if (balance == null || bottleViews == null || rackViews == null) return;
            int n = Mathf.Min(bottleViews.Length, rackViews.Length);
            for (int i = 0; i < n; i++)
            {
                var b = new BigBottle(i, balance.BigBottleCapacity);
                bottles.Add(b);
                if (bottleViews[i] != null) bottleViews[i].Bind(b);

                var r = new SmallBottleRack(i, balance.RackCapacity);
                racks.Add(r);
                if (rackViews[i] != null) rackViews[i].Bind(r);
            }
        }

        public BigBottle TryRouteTruckToBottle(Truck truck) => BigBottleRouter.FindBottleFor(truck, bottles);

        public Vector3 GetBottleWorldPosition(BigBottle bottle)
        {
            if (bottle == null) return Vector3.zero;
            int idx = bottles.IndexOf(bottle);
            if (idx < 0 || idx >= bottleViews.Length || bottleViews[idx] == null) return Vector3.zero;
            return bottleViews[idx].DumpAnchorWorldPosition;
        }

        public void DepositTruckLoad(Truck truck, BigBottle bottle)
        {
            if (truck == null || bottle == null) return;
            bottle.Receive(truck.FruitColor, truck.Load);
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
