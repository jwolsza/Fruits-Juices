using Project.Core;

namespace Project.Zone1.FruitWall
{
    public class FruitGrid
    {
        readonly FruitType?[,] cells;
        public int Columns { get; }
        public int Rows { get; }
        public int OccupiedCount { get; private set; }

        public bool IsEmpty => OccupiedCount == 0;
        public bool IsFull => OccupiedCount == Columns * Rows;

        public FruitGrid(int columns, int rows)
        {
            Columns = columns;
            Rows = rows;
            cells = new FruitType?[columns, rows];
        }

        public FruitType? GetCell(int x, int y)
        {
            if (x < 0 || x >= Columns || y < 0 || y >= Rows) return null;
            return cells[x, y];
        }

        public void SetCell(int x, int y, FruitType type)
        {
            if (x < 0 || x >= Columns || y < 0 || y >= Rows) return;
            if (cells[x, y] == null) OccupiedCount++;
            cells[x, y] = type;
        }

        public void ClearCell(int x, int y)
        {
            if (x < 0 || x >= Columns || y < 0 || y >= Rows) return;
            if (cells[x, y] != null) OccupiedCount--;
            cells[x, y] = null;
        }

        public bool IsCellEmpty(int x, int y)
        {
            if (x < 0 || x >= Columns || y < 0 || y >= Rows) return false;
            return cells[x, y] == null;
        }
    }
}
