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
        /// Grows the grid by adding `count` new empty rows at the top (high y).
        /// Existing data preserved at original (x, y) positions (anchored at bottom).
        /// </summary>
        public void AddRowsAtTop(int count)
        {
            if (count <= 0) return;
            int newRows = Rows + count;
            var newCells = new FruitType?[Columns, newRows];
            for (int x = 0; x < Columns; x++)
                for (int y = 0; y < Rows; y++)
                    newCells[x, y] = cells[x, y];
            cells = newCells;
            Rows = newRows;
        }

        /// <summary>
        /// Grows the grid by adding `count` new empty columns to the right (high x).
        /// </summary>
        public void AddColumnsAtRight(int count)
        {
            if (count <= 0) return;
            int newCols = Columns + count;
            var newCells = new FruitType?[newCols, Rows];
            for (int x = 0; x < Columns; x++)
                for (int y = 0; y < Rows; y++)
                    newCells[x, y] = cells[x, y];
            cells = newCells;
            Columns = newCols;
        }
    }
}
