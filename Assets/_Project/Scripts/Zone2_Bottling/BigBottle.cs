using Project.Core;

namespace Project.Zone2.Bottling
{
    public class BigBottle
    {
        public int Id { get; }
        public int Capacity { get; private set; }
        public int FillAmount { get; private set; }
        public FruitType? CurrentType { get; private set; }

        public bool IsEmpty => FillAmount == 0;
        public bool IsFull => FillAmount >= Capacity;

        public BigBottleState State =>
            FillAmount == 0 ? BigBottleState.Empty :
            FillAmount < Capacity ? BigBottleState.Filling :
            BigBottleState.TapAble;

        public BigBottle(int id, int capacity)
        {
            Id = id;
            Capacity = capacity;
        }

        public int Receive(FruitType type, int amount)
        {
            if (amount <= 0) return 0;
            if (CurrentType.HasValue && CurrentType.Value != type) return 0;
            if (!CurrentType.HasValue) CurrentType = type;
            int free = Capacity - FillAmount;
            int take = amount < free ? amount : free;
            FillAmount += take;
            return take;
        }

        public int Drain(int amount)
        {
            if (amount <= 0 || FillAmount == 0) return 0;
            int take = amount < FillAmount ? amount : FillAmount;
            FillAmount -= take;
            if (FillAmount == 0) CurrentType = null;
            return take;
        }

        public void SetCapacity(int newCapacity) { Capacity = newCapacity; }
    }
}
