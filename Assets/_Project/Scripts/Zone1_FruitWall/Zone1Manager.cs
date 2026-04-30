using System.Collections.Generic;
using UnityEngine;
using Project.Core;
using Project.Data;

namespace Project.Zone1.FruitWall
{
    public class Zone1Manager : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] GameBalanceSO balance;
        [SerializeField] WallView wallView;

        [Header("Events")]
        [SerializeField] BoolEventChannelSO onRefillingChanged;

        FruitGrid grid;
        RefillController refill;
        SystemRandomSource rng;
        readonly List<FruitType> activeFruitTypes = new();
        public IReadOnlyList<FruitType> ActiveFruitTypes => activeFruitTypes;

        public FruitGrid Grid => grid;
        public Transform WallTransform => wallView != null ? wallView.transform : null;
        public float WallWidthWorldUnits => balance != null && wallView != null
            ? balance.WallWidthWorldUnits * wallView.transform.lossyScale.x
            : 0f;
        public float WallLeftXWorld => wallView != null ? wallView.transform.position.x : 0f;

        float gravityAccumulator;
        float refillAccumulator;
        int gravityTickIndex;

        void Awake()
        {
            if (balance == null)
            {
                Debug.LogError("[Zone1Manager] GameBalanceSO not assigned");
                enabled = false;
                return;
            }
            if (wallView == null)
            {
                Debug.LogError("[Zone1Manager] WallView not assigned");
                enabled = false;
                return;
            }

            grid = new FruitGrid(balance.WallColumns, balance.WallRows);
            rng = new SystemRandomSource();

            RebuildActiveFruitTypes(0);
            refill = new RefillController(
                grid,
                activeFruitTypes.ToArray(),
                rng,
                balance.RefillSpawnsPerTick);

            wallView.Initialize(grid, balance.WallWidthWorldUnits, balance.WallHeightWorldUnits);
        }

        /// <summary>
        /// Set how many extra (locked) fruit types are unlocked. extraUnlocked=0 → only StartingFruitTypes.
        /// Updates RefillController's pool live.
        /// </summary>
        public void SetExtraUnlockedTypes(int extraUnlocked)
        {
            RebuildActiveFruitTypes(extraUnlocked);
            if (refill != null) refill.SetFruitPool(activeFruitTypes.ToArray());
        }

        void RebuildActiveFruitTypes(int extraUnlocked)
        {
            activeFruitTypes.Clear();
            if (balance.StartingFruitTypes != null)
                foreach (var t in balance.StartingFruitTypes) activeFruitTypes.Add(t);
            if (balance.LockedFruitTypes != null)
            {
                int n = Mathf.Min(Mathf.Max(0, extraUnlocked), balance.LockedFruitTypes.Length);
                for (int i = 0; i < n; i++) activeFruitTypes.Add(balance.LockedFruitTypes[i]);
            }
        }

        /// <summary>
        /// Grow the wall — adds columns at left, columns at right, rows at top. Existing cells/fruits
        /// stay at their original world positions (achieved by shifting WallView anchor).
        /// </summary>
        public void GrowWall(int addLeft, int addRight, int addTop)
        {
            if (grid == null || wallView == null) return;
            if (addLeft <= 0 && addRight <= 0 && addTop <= 0) return;
            grid.Grow(addLeft, addRight, addTop);
            wallView.OnGridGrew(addLeft, addRight, addTop);
        }

        void Update()
        {
            float dt = Time.deltaTime;

            float gravityInterval = 1f / Mathf.Max(0.01f, balance.GravityRateHz);
            gravityAccumulator += dt;
            while (gravityAccumulator >= gravityInterval)
            {
                gravityAccumulator -= gravityInterval;
                SandPhysicsTick.Step(grid, gravityTickIndex);
                gravityTickIndex++;
            }

            if (refill.IsRefilling)
            {
                float refillInterval = 1f / Mathf.Max(0.01f, balance.RefillTickRateHz);
                refillAccumulator += dt;
                while (refillAccumulator >= refillInterval)
                {
                    refillAccumulator -= refillInterval;
                    bool wasRefilling = refill.IsRefilling;
                    refill.Tick();
                    if (wasRefilling && !refill.IsRefilling)
                        EmitRefillingChanged(false);
                }
            }
            else
            {
                refillAccumulator = 0f;
            }
        }

        public void StartRefill()
        {
            if (refill.IsRefilling || grid.IsFull) return;
            refill.Start();
            EmitRefillingChanged(true);
        }

        void EmitRefillingChanged(bool isRefilling)
        {
            if (onRefillingChanged != null)
                onRefillingChanged.Raise(isRefilling);
        }
    }
}
