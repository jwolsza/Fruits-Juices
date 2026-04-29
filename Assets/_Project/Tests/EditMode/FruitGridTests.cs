using NUnit.Framework;
using Project.Core;
using Project.Zone1.FruitWall;

namespace Project.Tests.EditMode
{
    public class FruitGridTests
    {
        [Test]
        public void NewGrid_AllCellsEmpty()
        {
            var grid = new FruitGrid(columns: 5, rows: 4);

            Assert.AreEqual(5, grid.Columns);
            Assert.AreEqual(4, grid.Rows);
            for (int x = 0; x < 5; x++)
                for (int y = 0; y < 4; y++)
                    Assert.IsNull(grid.GetCell(x, y), $"cell ({x},{y}) should be empty");

            Assert.IsTrue(grid.IsEmpty);
            Assert.IsFalse(grid.IsFull);
            Assert.AreEqual(0, grid.OccupiedCount);
        }

        [Test]
        public void SetCell_StoresValue_AndUpdatesCounters()
        {
            var grid = new FruitGrid(5, 4);
            grid.SetCell(2, 1, FruitType.Apple);

            Assert.AreEqual(FruitType.Apple, grid.GetCell(2, 1));
            Assert.AreEqual(1, grid.OccupiedCount);
            Assert.IsFalse(grid.IsEmpty);
            Assert.IsFalse(grid.IsFull);
        }

        [Test]
        public void ClearCell_RemovesValue()
        {
            var grid = new FruitGrid(3, 3);
            grid.SetCell(0, 0, FruitType.Lemon);
            grid.ClearCell(0, 0);

            Assert.IsNull(grid.GetCell(0, 0));
            Assert.AreEqual(0, grid.OccupiedCount);
        }

        [Test]
        public void IsFull_TrueWhenAllCellsOccupied()
        {
            var grid = new FruitGrid(2, 2);
            grid.SetCell(0, 0, FruitType.Apple);
            grid.SetCell(1, 0, FruitType.Apple);
            grid.SetCell(0, 1, FruitType.Orange);
            grid.SetCell(1, 1, FruitType.Lemon);

            Assert.IsTrue(grid.IsFull);
            Assert.AreEqual(4, grid.OccupiedCount);
        }

        [Test]
        public void OutOfBounds_GetCell_ReturnsNull()
        {
            var grid = new FruitGrid(3, 3);
            Assert.IsNull(grid.GetCell(-1, 0));
            Assert.IsNull(grid.GetCell(3, 0));
            Assert.IsNull(grid.GetCell(0, -1));
            Assert.IsNull(grid.GetCell(0, 3));
        }

        [Test]
        public void IsCellEmpty_HelperMethod()
        {
            var grid = new FruitGrid(3, 3);
            Assert.IsTrue(grid.IsCellEmpty(0, 0));
            grid.SetCell(0, 0, FruitType.Apple);
            Assert.IsFalse(grid.IsCellEmpty(0, 0));
            Assert.IsTrue(grid.IsCellEmpty(1, 1));
            Assert.IsFalse(grid.IsCellEmpty(-1, 0));
            Assert.IsFalse(grid.IsCellEmpty(3, 0));
        }
    }
}
