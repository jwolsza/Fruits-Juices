using UnityEngine;
using Project.Core;

namespace Project.Data
{
    [CreateAssetMenu(fileName = "GameBalance", menuName = "Project/GameBalance", order = 0)]
    public class GameBalanceSO : ScriptableObject
    {
        [Header("Wall (sand-physics grid)")]
        public int WallColumns = 300;
        public int WallRows = 300;
        [Tooltip("Total wall width in world units. Cell width = WallWidthWorldUnits / WallColumns.")]
        public float WallWidthWorldUnits = 5f;
        [Tooltip("Total wall height in world units. Cell height = WallHeightWorldUnits / WallRows.")]
        public float WallHeightWorldUnits = 5f;
        public float GravityRateHz = 10f;

        [Header("Wall refill")]
        public float RefillTickRateHz = 30f;
        public int RefillSpawnsPerTick = 100;

        [Header("Trucks / Conveyor")]
        public float MagnetRateHz = 5f;
        public int ConveyorSlotCount = 4;
        public int TruckCapacity = 100;

        [Header("Big bottles & racks")]
        public int BigBottleCapacity = 200;
        public int FruitsPerSmallBottle = 5;
        public int RackCapacity = 30;
        public float PourSpeed = 6f;

        [Header("Player")]
        public float PlayerSpeed = 5f;
        public int PlayerCapacity = 10;
        public float PickupRadius = 1.5f;
        public float DeliverRadius = 1.5f;
        public float PickupRateHz = 10f;
        public float DeliverRateHz = 10f;

        [Header("Customers")]
        public int CustomerQueueLength = 5;
        public float CustomerSpawnRateHz = 0.25f;
        public int CoinsPerCustomerBase = 10;

        [Header("Fruits")]
        public FruitType[] StartingFruitTypes = new[]
        {
            FruitType.Apple, FruitType.Orange, FruitType.Lemon,
        };

        public FruitType[] LockedFruitTypes = new[]
        {
            FruitType.Strawberry, FruitType.Grape, FruitType.Banana,
            FruitType.Kiwi, FruitType.Pineapple, FruitType.Watermelon, FruitType.Mango,
        };

        public void ResetToDefaults()
        {
            WallColumns = 300;
            WallRows = 300;
            WallWidthWorldUnits = 5f;
            WallHeightWorldUnits = 5f;
            GravityRateHz = 10f;

            RefillTickRateHz = 30f;
            RefillSpawnsPerTick = 100;

            MagnetRateHz = 5f;
            ConveyorSlotCount = 4;
            TruckCapacity = 100;

            BigBottleCapacity = 200;
            FruitsPerSmallBottle = 5;
            RackCapacity = 30;
            PourSpeed = 6f;

            PlayerSpeed = 5f;
            PlayerCapacity = 10;
            PickupRadius = 1.5f;
            DeliverRadius = 1.5f;
            PickupRateHz = 10f;
            DeliverRateHz = 10f;

            CustomerQueueLength = 5;
            CustomerSpawnRateHz = 0.25f;
            CoinsPerCustomerBase = 10;

            StartingFruitTypes = new[] { FruitType.Apple, FruitType.Orange, FruitType.Lemon };
            LockedFruitTypes = new[]
            {
                FruitType.Strawberry, FruitType.Grape, FruitType.Banana,
                FruitType.Kiwi, FruitType.Pineapple, FruitType.Watermelon, FruitType.Mango,
            };
        }
    }
}
