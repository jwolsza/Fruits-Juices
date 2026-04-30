using NUnit.Framework;
using Project.Core;
using Project.Zone2.Bottling;

namespace Project.Tests.EditMode
{
    public class BigBottleTests
    {
        [Test]
        public void NewBottle_IsEmpty_NoTypeReserved()
        {
            var b = new BigBottle(0, 200);
            Assert.IsTrue(b.IsEmpty);
            Assert.IsFalse(b.IsFull);
            Assert.IsNull(b.CurrentType);
            Assert.AreEqual(BigBottleState.Empty, b.State);
        }

        [Test]
        public void Receive_FirstAdd_LocksType()
        {
            var b = new BigBottle(0, 200);
            int added = b.Receive(FruitType.Apple, 50);
            Assert.AreEqual(50, added);
            Assert.AreEqual(50, b.FillAmount);
            Assert.AreEqual(FruitType.Apple, b.CurrentType);
            Assert.AreEqual(BigBottleState.Filling, b.State);
        }

        [Test]
        public void Receive_DifferentType_RejectedReturnsZero()
        {
            var b = new BigBottle(0, 200);
            b.Receive(FruitType.Apple, 50);
            int added = b.Receive(FruitType.Orange, 30);
            Assert.AreEqual(0, added);
            Assert.AreEqual(50, b.FillAmount);
            Assert.AreEqual(FruitType.Apple, b.CurrentType);
        }

        [Test]
        public void Receive_BeyondCapacity_PartialAdd()
        {
            var b = new BigBottle(0, 100);
            int added = b.Receive(FruitType.Apple, 150);
            Assert.AreEqual(100, added);
            Assert.IsTrue(b.IsFull);
            Assert.AreEqual(BigBottleState.TapAble, b.State);
        }

        [Test]
        public void Drain_ToEmpty_UnreservesType()
        {
            var b = new BigBottle(0, 100);
            b.Receive(FruitType.Lemon, 80);
            int drained = b.Drain(80);
            Assert.AreEqual(80, drained);
            Assert.IsTrue(b.IsEmpty);
            Assert.IsNull(b.CurrentType);
            Assert.AreEqual(BigBottleState.Empty, b.State);
        }

        [Test]
        public void Drain_PartialKeepsType()
        {
            var b = new BigBottle(0, 100);
            b.Receive(FruitType.Lemon, 80);
            int drained = b.Drain(50);
            Assert.AreEqual(50, drained);
            Assert.AreEqual(30, b.FillAmount);
            Assert.AreEqual(FruitType.Lemon, b.CurrentType);
        }
    }
}
