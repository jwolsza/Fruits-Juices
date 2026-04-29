using NUnit.Framework;
using Project.Core;
using Project.Zone1.FruitWall;
using Project.Zone1.Trucks;

namespace Project.Tests.EditMode
{
    public class MagnetSystemTests
    {
        [Test]
        public void Magnet_WithMatchingFruitInBottomRow_RemovesFromGridAndIncrementsLoad()
        {
            var grid = new FruitGrid(10, 10);
            grid.SetCell(5, 0, FruitType.Apple);
            var truck = new Truck(1, FruitType.Apple, 100) { State = TruckState.StoppedAtSlot };

            var assignments = MagnetSystem.AssignFruitsToTrucksAtSlots(
                grid, new[] { (truck, 0f) }, wallLeftXWorld: 0f, wallWidthWorld: 10f);

            Assert.AreEqual(1, assignments.Count);
            Assert.AreEqual(1, truck.Load);
            Assert.IsNull(grid.GetCell(5, 0));
        }

        [Test]
        public void Magnet_NoMatchingFruit_NoAssignment()
        {
            var grid = new FruitGrid(10, 10);
            grid.SetCell(5, 0, FruitType.Orange);
            var truck = new Truck(1, FruitType.Apple, 100) { State = TruckState.StoppedAtSlot };
            var assignments = MagnetSystem.AssignFruitsToTrucksAtSlots(
                grid, new[] { (truck, 0f) }, 0f, 10f);
            Assert.AreEqual(0, assignments.Count);
            Assert.AreEqual(0, truck.Load);
        }

        [Test]
        public void Magnet_FullTruck_NoAssignment()
        {
            var grid = new FruitGrid(10, 10);
            grid.SetCell(5, 0, FruitType.Apple);
            var truck = new Truck(1, FruitType.Apple, 1) { State = TruckState.StoppedAtSlot };
            truck.AddFruit();
            var assignments = MagnetSystem.AssignFruitsToTrucksAtSlots(
                grid, new[] { (truck, 0f) }, 0f, 10f);
            Assert.AreEqual(0, assignments.Count);
        }

        [Test]
        public void Magnet_MultipleTrucks_AssignsClosestFruitPerTruckByX()
        {
            var grid = new FruitGrid(10, 10);
            grid.SetCell(2, 0, FruitType.Apple);
            grid.SetCell(8, 0, FruitType.Apple);
            var truckLeft = new Truck(1, FruitType.Apple, 100) { State = TruckState.StoppedAtSlot };
            var truckRight = new Truck(2, FruitType.Apple, 100) { State = TruckState.StoppedAtSlot };
            var assignments = MagnetSystem.AssignFruitsToTrucksAtSlots(
                grid, new[] { (truckLeft, 2f), (truckRight, 8f) }, 0f, 10f);
            Assert.AreEqual(2, assignments.Count);
            Assert.AreEqual(1, truckLeft.Load);
            Assert.AreEqual(1, truckRight.Load);
        }

        [Test]
        public void Magnet_TwoTrucksSameColor_SecondGetsNextNearest()
        {
            var grid = new FruitGrid(10, 10);
            grid.SetCell(4, 0, FruitType.Apple);
            grid.SetCell(6, 0, FruitType.Apple);
            var a = new Truck(1, FruitType.Apple, 100) { State = TruckState.StoppedAtSlot };
            var b = new Truck(2, FruitType.Apple, 100) { State = TruckState.StoppedAtSlot };
            var assignments = MagnetSystem.AssignFruitsToTrucksAtSlots(
                grid, new[] { (a, 5f), (b, 5f) }, 0f, 10f);
            Assert.AreEqual(2, assignments.Count);
            Assert.AreEqual(1, a.Load);
            Assert.AreEqual(1, b.Load);
        }
    }
}
