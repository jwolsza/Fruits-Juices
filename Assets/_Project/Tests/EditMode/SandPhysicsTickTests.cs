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
        public void DownPriority_GlobalAcrossRow_NotPerCellIteration()
        {
            // Setup: row y=1 has fruit at (0, 1) and (1, 1). Row y=0 has fruit at (1, 0) only;
            // (0, 0) is empty.
            //
            // BAD per-cell logic (LTR + preferLeft) might make (1, 1) skip its blocked-down
            // and grab diagonal (0, 0) before (0, 1) gets to fall straight down.
            //
            // GOOD two-pass logic: pass 1 lets (0, 1) fall to (0, 0). Pass 2 sees (1, 1) still
            // blocked (because (0, 0) is now occupied AND (1, 0) is occupied) — stays.
            var grid = new FruitGrid(3, 2);
            grid.SetCell(1, 0, FruitType.Lemon);
            grid.SetCell(0, 1, FruitType.Apple);
            grid.SetCell(1, 1, FruitType.Orange);

            SandPhysicsTick.Step(grid, tickIndex: 0); // even, preferLeft

            Assert.AreEqual(FruitType.Apple, grid.GetCell(0, 0), "(0,1) fell straight down to (0,0)");
            Assert.AreEqual(FruitType.Lemon, grid.GetCell(1, 0));
            Assert.AreEqual(FruitType.Orange, grid.GetCell(1, 1), "(1,1) stays — diagonal blocked after pass 1");
        }

        [Test]
        public void Fruit_OnLeftEdge_OnlyHasRightDiagonalAvailable()
        {
            var grid = new FruitGrid(3, 3);
            grid.SetCell(0, 0, FruitType.Lemon);
            grid.SetCell(0, 1, FruitType.Apple);

            SandPhysicsTick.Step(grid, tickIndex: 0); // preferLeft, but left out-of-bounds

            Assert.IsNull(grid.GetCell(0, 1));
            Assert.AreEqual(FruitType.Apple, grid.GetCell(1, 0));
        }
    }
}
