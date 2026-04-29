using NUnit.Framework;
using UnityEngine;
using Project.Core;
using Project.Data;

namespace Project.Tests.EditMode
{
    public class GameBalanceSOTests
    {
        [Test]
        public void DefaultInstance_HasExpectedStartingValues()
        {
            var balance = ScriptableObject.CreateInstance<GameBalanceSO>();
            balance.ResetToDefaults();

            Assert.AreEqual(300, balance.WallColumns);
            Assert.AreEqual(300, balance.WallRows);
            Assert.AreEqual(36f, balance.WallWidthWorldUnits);
            Assert.AreEqual(36f, balance.WallHeightWorldUnits);
            Assert.AreEqual(10f, balance.GravityRateHz);
            Assert.AreEqual(30f, balance.RefillTickRateHz);
            Assert.AreEqual(100, balance.RefillSpawnsPerTick);
            Assert.AreEqual(5f, balance.MagnetRateHz);
            Assert.AreEqual(100, balance.TruckCapacity);
            Assert.AreEqual(200, balance.BigBottleCapacity);
            Assert.AreEqual(5, balance.FruitsPerSmallBottle);
            Assert.AreEqual(30, balance.RackCapacity);
            Assert.AreEqual(6f, balance.PourSpeed);
            Assert.AreEqual(5f, balance.PlayerSpeed);
            Assert.AreEqual(10, balance.PlayerCapacity);
            Assert.AreEqual(1.5f, balance.PickupRadius);
            Assert.AreEqual(1.5f, balance.DeliverRadius);
            Assert.AreEqual(10f, balance.PickupRateHz);
            Assert.AreEqual(10f, balance.DeliverRateHz);
            Assert.AreEqual(5, balance.CustomerQueueLength);
            Assert.AreEqual(0.25f, balance.CustomerSpawnRateHz);
            Assert.AreEqual(10, balance.CoinsPerCustomerBase);

            CollectionAssert.AreEqual(
                new[] { FruitType.Apple, FruitType.Orange, FruitType.Lemon },
                balance.StartingFruitTypes);
        }
    }
}
