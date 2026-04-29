using NUnit.Framework;
using Project.Core;
using Project.Zone1.Trucks;
using Project.Zone2.Bottling;

namespace Project.Tests.EditMode
{
    public class BigBottleRouterTests
    {
        [Test]
        public void Find_MatchingTypeWithSpace_PriorityOne()
        {
            var b0 = new BigBottle(0, 200);
            var b1 = new BigBottle(1, 200);
            b1.Receive(FruitType.Apple, 50);

            var truck = new Truck(1, FruitType.Apple, 100);
            for (int i = 0; i < 50; i++) truck.AddFruit();

            var found = BigBottleRouter.FindBottleFor(truck, new[] { b0, b1 });
            Assert.AreSame(b1, found, "should pick matching type bottle, not the empty one");
        }

        [Test]
        public void Find_NoMatching_FallsBackToEmpty()
        {
            var b0 = new BigBottle(0, 200);
            var b1 = new BigBottle(1, 200);
            b1.Receive(FruitType.Orange, 50);

            var truck = new Truck(1, FruitType.Apple, 100);
            for (int i = 0; i < 50; i++) truck.AddFruit();

            var found = BigBottleRouter.FindBottleFor(truck, new[] { b0, b1 });
            Assert.AreSame(b0, found);
        }

        [Test]
        public void Find_AllOccupiedDifferentType_ReturnsNull()
        {
            var b0 = new BigBottle(0, 200);
            var b1 = new BigBottle(1, 200);
            b0.Receive(FruitType.Orange, 50);
            b1.Receive(FruitType.Lemon, 50);

            var truck = new Truck(1, FruitType.Apple, 100);
            for (int i = 0; i < 50; i++) truck.AddFruit();

            var found = BigBottleRouter.FindBottleFor(truck, new[] { b0, b1 });
            Assert.IsNull(found);
        }

        [Test]
        public void Find_MatchingButNotEnoughSpace_FallsBackToEmpty()
        {
            var b0 = new BigBottle(0, 200);
            var b1 = new BigBottle(1, 100);
            b1.Receive(FruitType.Apple, 80); // 20 free

            var truck = new Truck(1, FruitType.Apple, 100);
            for (int i = 0; i < 50; i++) truck.AddFruit(); // truck has 50 — won't fit in 20

            var found = BigBottleRouter.FindBottleFor(truck, new[] { b0, b1 });
            Assert.AreSame(b0, found);
        }
    }
}
