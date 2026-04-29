using NUnit.Framework;
using Project.Core;
using Project.Zone1.FruitWall;

namespace Project.Tests.EditMode
{
    public class SandPhysicsTickTests
    {
        [Test]
        public void SingleFruit_AboveEmpty_FallsStraightDown()
        {
            var grid = new FruitGrid(3, 3);
            grid.SetCell(1, 2, FruitType.Apple);

            SandPhysicsTick.Step(grid, tickIndex: 0);

            Assert.IsNull(grid.GetCell(1, 2));
            Assert.AreEqual(FruitType.Apple, grid.GetCell(1, 1));
        }

        [Test]
        public void Fruit_OnTopOfPile_StaysWhenNoEmptyBelow()
        {
            var grid = new FruitGrid(1, 3);
            grid.SetCell(0, 0, FruitType.Lemon);
            grid.SetCell(0, 1, FruitType.Apple);

            SandPhysicsTick.Step(grid, tickIndex: 0);

            Assert.AreEqual(FruitType.Lemon, grid.GetCell(0, 0));
            Assert.AreEqual(FruitType.Apple, grid.GetCell(0, 1));
        }

        [Test]
        public void Fruit_OnEdgeOfPile_FallsDiagonallyLeft_TickEven()
        {
            var grid = new FruitGrid(3, 3);
            grid.SetCell(1, 0, FruitType.Lemon);
            grid.SetCell(1, 1, FruitType.Apple);

            SandPhysicsTick.Step(grid, tickIndex: 0);

            Assert.IsNull(grid.GetCell(1, 1));
            Assert.AreEqual(FruitType.Apple, grid.GetCell(0, 0));
            Assert.AreEqual(FruitType.Lemon, grid.GetCell(1, 0));
        }

        [Test]
        public void Fruit_OnEdgeOfPile_FallsDiagonallyRight_TickOdd()
        {
            var grid = new FruitGrid(3, 3);
            grid.SetCell(1, 0, FruitType.Lemon);
            grid.SetCell(1, 1, FruitType.Apple);

            SandPhysicsTick.Step(grid, tickIndex: 1);

            Assert.IsNull(grid.GetCell(1, 1));
            Assert.AreEqual(FruitType.Apple, grid.GetCell(2, 0));
        }

        [Test]
        public void Fruit_BlockedAllSides_DoesNotMove()
        {
            var grid = new FruitGrid(3, 3);
            grid.SetCell(0, 0, FruitType.Lemon);
            grid.SetCell(1, 0, FruitType.Lemon);
            grid.SetCell(2, 0, FruitType.Lemon);
            grid.SetCell(1, 1, FruitType.Apple);

            SandPhysicsTick.Step(grid, tickIndex: 0);

            Assert.AreEqual(FruitType.Apple, grid.GetCell(1, 1));
        }

        [Test]
        public void Fruit_AtBottomRow_DoesNotMove()
        {
            var grid = new FruitGrid(3, 3);
            grid.SetCell(1, 0, FruitType.Apple);

            SandPhysicsTick.Step(grid, tickIndex: 0);

            Assert.AreEqual(FruitType.Apple, grid.GetCell(1, 0));
        }

        [Test]
        public void Fruit_OnLeftEdge_OnlyHasRightDiagonalAvailable()
        {
            var grid = new FruitGrid(3, 3);
            grid.SetCell(0, 0, FruitType.Lemon);
            grid.SetCell(0, 1, FruitType.Apple);

            SandPhysicsTick.Step(grid, tickIndex: 0);

            Assert.IsNull(grid.GetCell(0, 1));
            Assert.AreEqual(FruitType.Apple, grid.GetCell(1, 0));
        }

        [Test]
        public void MultipleFruitsInColumn_BottomUpIteration()
        {
            var grid = new FruitGrid(3, 3);
            grid.SetCell(0, 0, FruitType.Apple);
            grid.SetCell(0, 1, FruitType.Apple);
            grid.SetCell(0, 2, FruitType.Apple);

            SandPhysicsTick.Step(grid, tickIndex: 0);

            Assert.AreEqual(FruitType.Apple, grid.GetCell(0, 0));
            Assert.AreEqual(FruitType.Apple, grid.GetCell(0, 1));
            Assert.AreEqual(FruitType.Apple, grid.GetCell(1, 0));
        }
    }
}
