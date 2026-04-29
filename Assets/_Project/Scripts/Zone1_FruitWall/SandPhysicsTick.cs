namespace Project.Zone1.FruitWall
{
    public static class SandPhysicsTick
    {
        public static void Step(FruitGrid grid, int tickIndex)
        {
            // Two-pass per row: down has GLOBAL priority across the row, then diagonals.
            // Iteration direction + preferLeft flip together each tick (mirror symmetry).
            bool ltr = (tickIndex % 2) == 0;
            bool preferLeft = ltr;
            int xStart = ltr ? 0 : grid.Columns - 1;
            int xEnd = ltr ? grid.Columns : -1;
            int xStep = ltr ? 1 : -1;

            for (int y = 1; y < grid.Rows; y++)
            {
                // Pass 1: every fruit that can fall straight down does so.
                // Done across whole row before any diagonal attempt — ensures down priority
                // even if a left-iteration order would have let a fruit slip diagonally first.
                for (int x = xStart; x != xEnd; x += xStep)
                {
                    var fruit = grid.GetCell(x, y);
                    if (fruit == null) continue;
                    if (!grid.IsCellEmpty(x, y - 1)) continue;
                    grid.ClearCell(x, y);
                    grid.SetCell(x, y - 1, fruit.Value);
                }

                // Pass 2: fruits that couldn't go down try diagonal — preferred side first.
                for (int x = xStart; x != xEnd; x += xStep)
                {
                    var fruit = grid.GetCell(x, y);
                    if (fruit == null) continue;

                    int firstDx = preferLeft ? -1 : +1;
                    int secondDx = -firstDx;

                    if (grid.IsCellEmpty(x + firstDx, y - 1))
                    {
                        grid.ClearCell(x, y);
                        grid.SetCell(x + firstDx, y - 1, fruit.Value);
                        continue;
                    }

                    if (grid.IsCellEmpty(x + secondDx, y - 1))
                    {
                        grid.ClearCell(x, y);
                        grid.SetCell(x + secondDx, y - 1, fruit.Value);
                    }
                }
            }
        }
    }
}
