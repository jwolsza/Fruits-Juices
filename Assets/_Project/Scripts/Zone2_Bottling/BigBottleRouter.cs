using System.Collections.Generic;
using Project.Zone1.Trucks;

namespace Project.Zone2.Bottling
{
    public static class BigBottleRouter
    {
        /// <summary>
        /// Priorytet 1: butelka pasująca typu trucka z miejscem.
        /// Priorytet 2: butelka pusta (unreserved).
        /// Inaczej null — truck musi czekać.
        /// </summary>
        public static BigBottle FindBottleFor(Truck truck, IReadOnlyList<BigBottle> bottles)
        {
            if (truck == null || bottles == null) return null;

            BigBottle anyEmpty = null;

            foreach (var b in bottles)
            {
                if (b.CurrentType.HasValue
                    && b.CurrentType.Value == truck.FruitColor
                    && b.FillAmount + truck.Load <= b.Capacity)
                {
                    return b;
                }
                if (b.IsEmpty && anyEmpty == null) anyEmpty = b;
            }

            return anyEmpty;
        }
    }
}
