using NUnit.Framework;
using Project.Core;
using Project.Zone1.Trucks;

namespace Project.Tests.EditMode
{
    public class GarageTests
    {
        [Test]
        public void NewGarage_StarterTrucks_AllInGarageState()
        {
            var g = new Garage(maxOnConveyor: 4);
            var t = new Truck(1, FruitType.Apple, 100);
            g.AddStarterTruck(t);
            Assert.AreEqual(1, g.TruckCount);
            Assert.AreEqual(0, g.OnConveyorCount);
            Assert.AreEqual(TruckState.InGarage, t.State);
        }

        [Test]
        public void Dispatch_FromGarage_EnteringConveyor()
        {
            var g = new Garage(4);
            var t = new Truck(1, FruitType.Apple, 100);
            g.AddStarterTruck(t);
            Assert.IsTrue(g.Dispatch(1));
            Assert.AreEqual(TruckState.EnteringConveyor, t.State);
            Assert.AreEqual(0f, t.TrackPosition);
        }

        [Test]
        public void Dispatch_ConveyorFull_ReturnsFalse()
        {
            var g = new Garage(1);
            var t1 = new Truck(1, FruitType.Apple, 100);
            var t2 = new Truck(2, FruitType.Apple, 100);
            g.AddStarterTruck(t1);
            g.AddStarterTruck(t2);
            Assert.IsTrue(g.Dispatch(1));
            Assert.IsFalse(g.Dispatch(2));
        }

        [Test]
        public void Dispatch_AlreadyOnConveyor_ReturnsFalse()
        {
            var g = new Garage(4);
            var t = new Truck(1, FruitType.Apple, 100);
            g.AddStarterTruck(t);
            g.Dispatch(1);
            Assert.IsFalse(g.Dispatch(1));
        }

        [Test]
        public void ReturnToGarage_RestoresStateAndEmpties()
        {
            var g = new Garage(4);
            var t = new Truck(1, FruitType.Apple, 100);
            g.AddStarterTruck(t);
            g.Dispatch(1);
            t.AddFruit();
            t.State = TruckState.ReturningToGarage;
            g.ReturnToGarage(1);
            Assert.AreEqual(TruckState.InGarage, t.State);
            Assert.AreEqual(0, t.Load);
            Assert.AreEqual(0, g.OnConveyorCount);
        }
    }
}
