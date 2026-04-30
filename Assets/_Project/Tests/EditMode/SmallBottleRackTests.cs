using NUnit.Framework;
using Project.Core;
using Project.Zone2.Bottling;

namespace Project.Tests.EditMode
{
    public class SmallBottleRackTests
    {
        [Test]
        public void NewRack_IsEmpty()
        {
            var r = new SmallBottleRack(0, 30);
            Assert.IsTrue(r.IsEmpty);
            Assert.AreEqual(0, r.Count);
            Assert.IsNull(r.CurrentType);
        }

        [Test]
        public void Add_LocksType_ReturnsAddedCount()
        {
            var r = new SmallBottleRack(0, 30);
            int added = r.Add(FruitType.Apple, 5);
            Assert.AreEqual(5, added);
            Assert.AreEqual(FruitType.Apple, r.CurrentType);
            Assert.AreEqual(5, r.Count);
        }

        [Test]
        public void Add_DifferentType_Rejected()
        {
            var r = new SmallBottleRack(0, 30);
            r.Add(FruitType.Apple, 5);
            int added = r.Add(FruitType.Orange, 5);
            Assert.AreEqual(0, added);
            Assert.AreEqual(5, r.Count);
        }

        [Test]
        public void Add_BeyondCapacity_Caps()
        {
            var r = new SmallBottleRack(0, 10);
            int added = r.Add(FruitType.Apple, 20);
            Assert.AreEqual(10, added);
            Assert.IsTrue(r.IsFull);
        }

        [Test]
        public void RemoveOne_ToEmpty_UnreservesType()
        {
            var r = new SmallBottleRack(0, 30);
            r.Add(FruitType.Lemon, 2);
            r.RemoveOne();
            Assert.AreEqual(1, r.Count);
            Assert.AreEqual(FruitType.Lemon, r.CurrentType);
            r.RemoveOne();
            Assert.AreEqual(0, r.Count);
            Assert.IsNull(r.CurrentType);
        }
    }
}
