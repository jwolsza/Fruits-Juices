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
        FruitType[] fruitPool;
        readonly IRandomSource random;
        readonly int spawnsPerTick;

        readonly List<int> emptyTopColumnsBuffer = new();

        public bool IsRefilling { get; private set; }
        public int TargetOccupied { get; private set; }

        public RefillController(FruitGrid grid, FruitType[] fruitPool, IRandomSource random, int spawnsPerTick)
        {
            this.grid = grid;
            this.fruitPool = fruitPool;
            this.random = random;
            this.spawnsPerTick = spawnsPerTick;
        }

        public void SetFruitPool(FruitType[] pool)
        {
            fruitPool = pool ?? new FruitType[0];
        }

        /// <summary>
        /// Start refill targeting an absolute occupied-cell count (caller computes from level/percent).
        /// No-op if grid is already at/above target.
        /// </summary>
        public void Start(int targetOccupied)
        {
            int cap = Math.Min(targetOccupied, grid.Columns * grid.Rows);
            if (grid.OccupiedCount >= cap) return;
            TargetOccupied = cap;
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
                if (grid.OccupiedCount >= TargetOccupied) break;

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

            if (grid.IsFull || grid.OccupiedCount >= TargetOccupied)
                IsRefilling = false;
        }
    }
}
