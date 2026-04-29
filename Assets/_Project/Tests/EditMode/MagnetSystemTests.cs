using NUnit.Framework;
using Project.Core;
using Project.Zone1.FruitWall;
using Project.Zone1.Trucks;

namespace Project.Tests.EditMode
{
    public class MagnetSystemTests
    {
        [Test]
        public void Magnet_TruckPicksAnyMatchingFruit_RegardlessOfPosition()
        {
            var grid = new FruitGrid(10, 10);
            // Apple far on the right — should still be collected by truck.
            grid.SetCell(8, 0, FruitType.Apple);
            var truck = new Truck(1, FruitType.Apple, 100);

            var assignments = MagnetSystem.AssignFruitsToTrucksAtSlots(grid, new[] { truck });

            Assert.AreEqual(1, assignments.Count);
            Assert.AreEqual(1, truck.Load);
            Assert.IsNull(grid.GetCell(8, 0));
        }

        [Test]
        public void Magnet_NoMatchingFruit_NoAssignment()
        {
            var grid = new FruitGrid(10, 10);
            grid.SetCell(5, 0, FruitType.Orange);
            var truck = new Truck(1, FruitType.Apple, 100);

            var assignments = MagnetSystem.AssignFruitsToTrucksAtSlots(grid, new[] { truck });

            Assert.AreEqual(0, assignments.Count);
            Assert.AreEqual(0, truck.Load);
        }

        [Test]
        public void Magnet_FullTruck_NoAssignment()
        {
            var grid = new FruitGrid(10, 10);
            grid.SetCell(5, 0, FruitType.Apple);
            var truck = new Truck(1, FruitType.Apple, 1);
            truck.AddFruit();

            var assignments = MagnetSystem.AssignFruitsToTrucksAtSlots(grid, new[] { truck });

            Assert.AreEqual(0, assignments.Count);
        }

        [Test]
        public void Magnet_MultipleTrucksDifferentColors_EachGetsOwnColor()
        {
            var grid = new FruitGrid(10, 10);
            grid.SetCell(2, 0, FruitType.Apple);
            grid.SetCell(7, 0, FruitType.Orange);

            var truckApple = new Truck(1, FruitType.Apple, 100);
            var truckOrange = new Truck(2, FruitType.Orange, 100);

            var assignments = MagnetSystem.AssignFruitsToTrucksAtSlots(
                grid, new[] { truckApple, truckOrange });

            Assert.AreEqual(2, assignments.Count);
            Assert.AreEqual(1, truckApple.Load);
            Assert.AreEqual(1, truckOrange.Load);
            Assert.IsTrue(grid.IsCellEmpty(2, 0));
            Assert.IsTrue(grid.IsCellEmpty(7, 0));
        }

        [Test]
        public void Magnet_TwoTrucksSameColor_ShareFruits_OnePerTruckPerTick()
        {
            var grid = new FruitGrid(10, 10);
            grid.SetCell(2, 0, FruitType.Apple);
            grid.SetCell(8, 0, FruitType.Apple);

            var t1 = new Truck(1, FruitType.Apple, 100);
            var t2 = new Truck(2, FruitType.Apple, 100);

            var assignments = MagnetSystem.AssignFruitsToTrucksAtSlots(grid, new[] { t1, t2 });

            // First truck takes leftmost (x=2), second takes next leftmost (x=8).
            Assert.AreEqual(2, assignments.Count);
            Assert.AreEqual(1, t1.Load);
            Assert.AreEqual(1, t2.Load);
            Assert.IsTrue(grid.IsCellEmpty(2, 0));
            Assert.IsTrue(grid.IsCellEmpty(8, 0));
        }

        [Test]
        public void Magnet_TruckTakesLeftmostMatchingFruit()
        {
            var grid = new FruitGrid(10, 10);
            grid.SetCell(3, 0, FruitType.Apple);
            grid.SetCell(7, 0, FruitType.Apple);

            var truck = new Truck(1, FruitType.Apple, 100);

            var assignments = MagnetSystem.AssignFruitsToTrucksAtSlots(grid, new[] { truck });

            Assert.AreEqual(1, assignments.Count);
            Assert.AreEqual(new UnityEngine.Vector2Int(3, 0), assignments[0].GridCellRemoved);
            Assert.IsTrue(grid.IsCellEmpty(3, 0));
            Assert.IsFalse(grid.IsCellEmpty(7, 0), "rightmost stays for next tick");
        }
    }
}
