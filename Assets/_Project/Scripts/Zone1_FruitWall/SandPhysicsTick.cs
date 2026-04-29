namespace Project.Zone1.FruitWall
{
    public static class SandPhysicsTick
    {
        public static void Step(FruitGrid grid, int tickIndex)
        {
            bool preferLeft = (tickIndex % 2) == 0;

            for (int y = 1; y < grid.Rows; y++)
            {
                for (int x = 0; x < grid.Columns; x++)
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
