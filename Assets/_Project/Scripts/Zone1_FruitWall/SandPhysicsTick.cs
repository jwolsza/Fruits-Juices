namespace Project.Zone1.FruitWall
{
    public static class SandPhysicsTick
    {
        public static void Step(FruitGrid grid, int tickIndex)
        {
            // Both X iteration direction and diagonal preference flip together — gives mirror
            // symmetry between consecutive ticks, eliminating accumulated horizontal bias.
            bool ltr = (tickIndex % 2) == 0;
            bool preferLeft = ltr;
            int xStart = ltr ? 0 : grid.Columns - 1;
            int xEnd = ltr ? grid.Columns : -1;
            int xStep = ltr ? 1 : -1;

            for (int y = 1; y < grid.Rows; y++)
            {
                for (int x = xStart; x != xEnd; x += xStep)
                {
                    var fruit = grid.GetCell(x, y);
                    if (fruit == null) continue;

                    if (grid.IsCellEmpty(x, y - 1))
                    {
                        grid.ClearCell(x, y);
                        grid.SetCell(x, y - 1, fruit.Value);
                        continue;
                    }

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
                        continue;
                    }
                }
            }
        }
    }
}
