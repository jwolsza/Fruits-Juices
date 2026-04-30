using Project.Core;

namespace Project.Zone1.FruitWall
{
    public class FruitGrid
    {
        FruitType?[,] cells;
        public int Columns { get; private set; }
        public int Rows { get; private set; }
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

        /// <summary>
        /// Grow grid by addLeft (cols at low x), addRight (cols at high x), addTop (rows at high y).
        /// Existing data shifted by (addLeft, 0). World positions of existing cells preserved by
        /// caller adjusting anchor offset.
        /// </summary>
        public void Grow(int addLeft, int addRight, int addTop)
        {
            if (addLeft <= 0 && addRight <= 0 && addTop <= 0) return;
            int newCols = Columns + Mathf.Max(0, addLeft) + Mathf.Max(0, addRight);
            int newRows = Rows + Mathf.Max(0, addTop);
            var newCells = new FruitType?[newCols, newRows];
            int xShift = Mathf.Max(0, addLeft);
            for (int x = 0; x < Columns; x++)
                for (int y = 0; y < Rows; y++)
                    newCells[x + xShift, y] = cells[x, y];
            cells = newCells;
            Columns = newCols;
            Rows = newRows;
        }
    }
}
