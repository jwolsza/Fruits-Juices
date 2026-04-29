using NUnit.Framework;
using Project.Core;
using Project.Zone1.Trucks;

namespace Project.Tests.EditMode
{
    public class GarageTests
    {
        [Test]
        public void AddStarterTruck_StoresAndSetsInGarageState()
        {
            var g = new Garage();
            var t = new Truck(1, FruitType.Apple, 100);
            g.AddStarterTruck(t);
            Assert.AreEqual(1, g.TruckCount);
            Assert.AreEqual(TruckState.InGarage, t.State);
            Assert.AreSame(t, g.Get(1));
        }

        [Test]
        public void Get_UnknownId_ReturnsNull()
        {
            var g = new Garage();
            Assert.IsNull(g.Get(42));
        }

        [Test]
        public void TruckCount_ReflectsAddedTrucks()
        {
            var g = new Garage();
            g.AddStarterTruck(new Truck(1, FruitType.Apple, 100));
            g.AddStarterTruck(new Truck(2, FruitType.Orange, 100));
            Assert.AreEqual(2, g.TruckCount);
        }
    }
}
