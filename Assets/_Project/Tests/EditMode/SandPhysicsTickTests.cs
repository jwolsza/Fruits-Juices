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
        public void Fruit_BlockedDirectlyBelow_StaysPut_NoDiagonal()
        {
            // Strict vertical gravity: even if (0, 0) is empty, fruit at (1, 1) does NOT slide
            // diagonally — it stays because (1, 0) is blocked.
            var grid = new FruitGrid(3, 3);
            grid.SetCell(1, 0, FruitType.Lemon);
            grid.SetCell(1, 1, FruitType.Apple);

            SandPhysicsTick.Step(grid, tickIndex: 0);

            Assert.AreEqual(FruitType.Apple, grid.GetCell(1, 1));
            Assert.AreEqual(FruitType.Lemon, grid.GetCell(1, 0));
            Assert.IsNull(grid.GetCell(0, 0));
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
        public void MultipleFruitsInColumn_FallStraightOnePerTick()
        {
            var grid = new FruitGrid(3, 3);
            grid.SetCell(0, 0, FruitType.Apple);
            grid.SetCell(0, 1, FruitType.Apple);
            grid.SetCell(0, 2, FruitType.Apple);

            SandPhysicsTick.Step(grid, tickIndex: 0);

            // (0,0) stays, (0,1) stays (blocked by (0,0)), (0,2) stays (blocked by (0,1)).
            // No motion this tick — pile is already settled.
            Assert.AreEqual(FruitType.Apple, grid.GetCell(0, 0));
            Assert.AreEqual(FruitType.Apple, grid.GetCell(0, 1));
            Assert.AreEqual(FruitType.Apple, grid.GetCell(0, 2));
        }

        [Test]
        public void ColumnDrains_FruitsFallStraightDown_OneTickPerCell()
        {
            var grid = new FruitGrid(3, 4);
            grid.SetCell(1, 0, FruitType.Apple); // bottom
            grid.SetCell(1, 1, FruitType.Apple);
            grid.SetCell(1, 2, FruitType.Apple);
            grid.SetCell(1, 3, FruitType.Apple); // top

            // Remove bottom — column 1 has gap at y=0.
            grid.ClearCell(1, 0);

            SandPhysicsTick.Step(grid, tickIndex: 0);

            // Each fruit falls one row down (because we iterate bottom-up, can chain).
            Assert.AreEqual(FruitType.Apple, grid.GetCell(1, 0));
            Assert.AreEqual(FruitType.Apple, grid.GetCell(1, 1));
            Assert.AreEqual(FruitType.Apple, grid.GetCell(1, 2));
            Assert.IsNull(grid.GetCell(1, 3));
        }
    }
}
