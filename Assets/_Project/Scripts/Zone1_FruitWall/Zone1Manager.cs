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
            refill = new RefillController(
                grid,
                balance.StartingFruitTypes,
                rng,
                balance.RefillSpawnsPerTick);

            wallView.Initialize(grid, balance.WallWidthWorldUnits, balance.WallHeightWorldUnits);
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
