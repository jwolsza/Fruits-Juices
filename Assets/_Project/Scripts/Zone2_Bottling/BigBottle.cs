using Project.Core;

namespace Project.Zone2.Bottling
{
    public class BigBottle
    {
        public int Id { get; }
        public int Capacity { get; private set; }
        public int FillAmount { get; private set; }
        public FruitType? CurrentType { get; private set; }

        // Reservation: load incoming from trucks driving toward this bottle.
        public int ReservedAmount { get; private set; }
        public FruitType? ReservedType { get; private set; }

        public FruitType? EffectiveType => CurrentType ?? ReservedType;
        public int EffectiveLoad => FillAmount + ReservedAmount;

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
            if (FillAmount == 0 && ReservedAmount == 0) CurrentType = null;
            return take;
        }

        /// <summary>
        /// Reserve future load from a truck en route. Locks ReservedType if not already locked.
        /// Returns true on success, false if type mismatch or capacity exceeded (fill+reserved+amount>cap).
        /// </summary>
        public bool TryReserve(FruitType type, int amount)
        {
            if (amount <= 0) return false;
            var effective = EffectiveType;
            if (effective.HasValue && effective.Value != type) return false;
            if (EffectiveLoad + amount > Capacity) return false;
            if (!ReservedType.HasValue) ReservedType = type;
            ReservedAmount += amount;
            return true;
        }

        /// <summary>
        /// Cancel a previously-made reservation (truck doesn't arrive).
        /// </summary>
        public void CancelReservation(int amount)
        {
            if (amount <= 0) return;
            ReservedAmount -= amount;
            if (ReservedAmount < 0) ReservedAmount = 0;
            if (FillAmount == 0 && ReservedAmount == 0)
            {
                CurrentType = null;
                ReservedType = null;
            }
        }

        /// <summary>
        /// Truck arrived: transfer reserved amount → fill. Returns actually added.
        /// </summary>
        public int CommitReservation(FruitType type, int amount)
        {
            int added = Receive(type, amount);
            ReservedAmount -= added;
            if (ReservedAmount < 0) ReservedAmount = 0;
            if (FillAmount == 0 && ReservedAmount == 0) ReservedType = null;
            return added;
        }

        public void SetCapacity(int newCapacity) { Capacity = newCapacity; }
    }
}
