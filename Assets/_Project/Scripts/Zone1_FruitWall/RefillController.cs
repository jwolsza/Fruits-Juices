using System;
using System.Collections.Generic;
using Project.Core;

namespace Project.Zone1.FruitWall
{
    public interface IRandomSource
    {
        int NextInt(int minInclusive, int maxExclusive);
    }

    public class SystemRandomSource : IRandomSource
    {
        readonly Random rng;
        public SystemRandomSource(int? seed = null)
        {
            rng = seed.HasValue ? new Random(seed.Value) : new Random();
        }
        public int NextInt(int min, int max) => rng.Next(min, max);
    }

    public class RefillController
    {
        readonly FruitGrid grid;
        readonly FruitType[] fruitPool;
        readonly IRandomSource random;
        readonly int spawnsPerTick;

        readonly List<int> emptyTopColumnsBuffer = new();

        public bool IsRefilling { get; private set; }

        public RefillController(FruitGrid grid, FruitType[] fruitPool, IRandomSource random, int spawnsPerTick)
        {
            this.grid = grid;
            this.fruitPool = fruitPool;
            this.random = random;
            this.spawnsPerTick = spawnsPerTick;
        }

        public void Start()
        {
            if (grid.IsFull) return;
            IsRefilling = true;
        }

        public void Stop()
        {
            IsRefilling = false;
        }

        public void Tick()
        {
            if (!IsRefilling) return;

            int topRowY = grid.Rows - 1;

            for (int attempt = 0; attempt < spawnsPerTick; attempt++)
            {
                emptyTopColumnsBuffer.Clear();
                for (int x = 0; x < grid.Columns; x++)
                {
                    if (grid.IsCellEmpty(x, topRowY))
                        emptyTopColumnsBuffer.Add(x);
                }

                if (emptyTopColumnsBuffer.Count == 0) break;
                if (fruitPool.Length == 0) break;

                int colIdx = random.NextInt(0, emptyTopColumnsBuffer.Count);
                int chosenX = emptyTopColumnsBuffer[colIdx];

                int fruitIdx = random.NextInt(0, fruitPool.Length);
                grid.SetCell(chosenX, topRowY, fruitPool[fruitIdx]);
            }

            if (grid.IsFull)
                IsRefilling = false;
        }
    }
}
