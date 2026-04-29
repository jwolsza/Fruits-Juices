using NUnit.Framework;
using Project.Core;
using Project.Zone2.Bottling;

namespace Project.Tests.EditMode
{
    public class PourControllerTests
    {
        [Test]
        public void Pour_NormalCase_ConvertsFruitsToSmallBottles()
        {
            var b = new BigBottle(0, 200);
            b.Receive(FruitType.Apple, 50);
            var r = new SmallBottleRack(0, 30);

            int spawned = PourController.Pour(b, r, fruitsPerSmall: 5);

            Assert.AreEqual(10, spawned); // 50 / 5 = 10
            Assert.AreEqual(0, b.FillAmount);
            Assert.AreEqual(10, r.Count);
            Assert.AreEqual(FruitType.Apple, r.CurrentType);
        }

        [Test]
        public void Pour_LimitedByRackCapacity_PartialAndBottleRetainsRest()
        {
            var b = new BigBottle(0, 200);
            b.Receive(FruitType.Orange, 50); // 50 fruits → 10 potential
            var r = new SmallBottleRack(0, 4); // only 4 slots free

            int spawned = PourController.Pour(b, r, fruitsPerSmall: 5);

            Assert.AreEqual(4, spawned);
            Assert.AreEqual(50 - 4 * 5, b.FillAmount); // 30 left
            Assert.AreEqual(4, r.Count);
            Assert.IsTrue(r.IsFull);
        }

        [Test]
        public void Pour_TypeMismatchBetweenBottleAndRack_ReturnsZero()
        {
            var b = new BigBottle(0, 200);
            b.Receive(FruitType.Apple, 50);
            var r = new SmallBottleRack(0, 30);
            r.Add(FruitType.Orange, 5);

            int spawned = PourController.Pour(b, r, fruitsPerSmall: 5);
            Assert.AreEqual(0, spawned);
        }

        [Test]
        public void Pour_EmptyBottle_ReturnsZero()
        {
            var b = new BigBottle(0, 200);
            var r = new SmallBottleRack(0, 30);
            int spawned = PourController.Pour(b, r, fruitsPerSmall: 5);
            Assert.AreEqual(0, spawned);
        }

        [Test]
        public void Pour_FractionalLeftover_StaysInBottle()
        {
            var b = new BigBottle(0, 200);
            b.Receive(FruitType.Apple, 13); // 13 / 5 = 2 spawn, 3 leftover
            var r = new SmallBottleRack(0, 30);

            int spawned = PourController.Pour(b, r, fruitsPerSmall: 5);
            Assert.AreEqual(2, spawned);
            Assert.AreEqual(3, b.FillAmount);
            Assert.AreEqual(FruitType.Apple, b.CurrentType, "type still locked while remainder > 0");
        }
    }
}
