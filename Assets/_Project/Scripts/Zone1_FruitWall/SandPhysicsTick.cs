namespace Project.Zone1.FruitWall
{
    public static class SandPhysicsTick
    {
        public static void Step(FruitGrid grid, int tickIndex)
        {
            // Strict vertical gravity: each fruit only falls straight down.
            // No diagonal moves — eliminates horizontal drift.
            for (int y = 1; y < grid.Rows; y++)
            {
                for (int x = 0; x < grid.Columns; x++)
                {
                    var fruit = grid.GetCell(x, y);
                    if (fruit == null) continue;
                    if (!grid.IsCellEmpty(x, y - 1)) continue;

                    grid.ClearCell(x, y);
                    grid.SetCell(x, y - 1, fruit.Value);
                }
            }
        }
    }
}
