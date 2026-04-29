using Project.Core;

namespace Project.Zone2.Bottling
{
    public class SmallBottleRack
    {
        public int Id { get; }
        public int Capacity { get; }
        public int Count { get; private set; }
        public FruitType? CurrentType { get; private set; }

        public bool IsEmpty => Count == 0;
        public bool IsFull => Count >= Capacity;
        public int FreeSlots => Capacity - Count;

        public SmallBottleRack(int id, int capacity)
        {
            Id = id;
            Capacity = capacity;
        }

        public int Add(FruitType type, int n)
        {
            if (n <= 0) return 0;
            if (CurrentType.HasValue && CurrentType.Value != type) return 0;
            if (!CurrentType.HasValue) CurrentType = type;
            int actual = n < FreeSlots ? n : FreeSlots;
            Count += actual;
            return actual;
        }

        public int RemoveOne()
        {
            if (Count == 0) return 0;
            Count--;
            if (Count == 0) CurrentType = null;
            return 1;
        }
    }
}
