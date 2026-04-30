using NUnit.Framework;
using Project.Core;
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

            var found = BigBottleRouter.FindBottleFor(FruitType.Apple, 50, new[] { b0, b1 });
            Assert.AreSame(b1, found);
        }

        [Test]
        public void Find_NoMatching_FallsBackToEmpty()
        {
            var b0 = new BigBottle(0, 200);
            var b1 = new BigBottle(1, 200);
            b1.Receive(FruitType.Orange, 50);

            var found = BigBottleRouter.FindBottleFor(FruitType.Apple, 50, new[] { b0, b1 });
            Assert.AreSame(b0, found);
        }

        [Test]
        public void Find_AllOccupiedDifferentType_ReturnsNull()
        {
            var b0 = new BigBottle(0, 200);
            var b1 = new BigBottle(1, 200);
            b0.Receive(FruitType.Orange, 50);
            b1.Receive(FruitType.Lemon, 50);

            var found = BigBottleRouter.FindBottleFor(FruitType.Apple, 50, new[] { b0, b1 });
            Assert.IsNull(found);
        }

        [Test]
        public void Find_MatchingButNotEnoughSpace_FallsBackToEmpty()
        {
            var b0 = new BigBottle(0, 200);
            var b1 = new BigBottle(1, 100);
            b1.Receive(FruitType.Apple, 80); // 20 free

            var found = BigBottleRouter.FindBottleFor(FruitType.Apple, 50, new[] { b0, b1 });
            Assert.AreSame(b0, found);
        }
    }
}
