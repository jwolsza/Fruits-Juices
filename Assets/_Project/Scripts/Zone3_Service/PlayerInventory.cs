using System.Collections.Generic;
using Project.Core;

namespace Project.Zone3.Service
{
    /// <summary>POCO inventory butelek niesionych przez gracza, per typ owocu.</summary>
    public class PlayerInventory
    {
        readonly Dictionary<FruitType, int> counts = new Dictionary<FruitType, int>();
        public int Capacity { get; private set; }
        public int TotalCount { get; private set; }

        public bool IsFull => TotalCount >= Capacity;
        public int FreeSpace => System.Math.Max(0, Capacity - TotalCount);

        public PlayerInventory(int capacity)
        {
            Capacity = capacity;
        }

        public void SetCapacity(int newCapacity)
        {
            Capacity = System.Math.Max(0, newCapacity);
        }

        public int GetCount(FruitType type) => counts.TryGetValue(type, out var n) ? n : 0;

        public IReadOnlyDictionary<FruitType, int> Counts => counts;

        /// <summary>Returns how many were actually added (clipped by free capacity).</summary>
        public int Add(FruitType type, int amount)
        {
            if (amount <= 0) return 0;
            int free = FreeSpace;
            if (free <= 0) return 0;
            int added = System.Math.Min(amount, free);
            counts[type] = GetCount(type) + added;
            TotalCount += added;
            return added;
        }

        /// <summary>Returns how many were actually removed.</summary>
        public int Remove(FruitType type, int amount)
        {
            if (amount <= 0) return 0;
            int have = GetCount(type);
            if (have <= 0) return 0;
            int removed = System.Math.Min(amount, have);
            int newCount = have - removed;
            if (newCount > 0) counts[type] = newCount;
            else counts.Remove(type);
            TotalCount -= removed;
            return removed;
        }
    }
}
