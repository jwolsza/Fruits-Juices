using System.Collections.Generic;
using Project.Core;

namespace Project.Zone2.Bottling
{
    public static class BigBottleRouter
    {
        /// <summary>
        /// Priorytet 1: butelka pasująca typu trucka z miejscem.
        /// Priorytet 2: butelka pusta (unreserved).
        /// Inaczej null — truck musi czekać.
        /// </summary>
        public static BigBottle FindBottleFor(FruitType truckFruitColor, int truckLoad, IReadOnlyList<BigBottle> bottles)
        {
            if (bottles == null) return null;

            BigBottle anyEmpty = null;

            foreach (var b in bottles)
            {
                if (b.CurrentType.HasValue
                    && b.CurrentType.Value == truckFruitColor
                    && b.FillAmount + truckLoad <= b.Capacity)
                {
                    return b;
                }
                if (b.IsEmpty && anyEmpty == null) anyEmpty = b;
            }

            return anyEmpty;
        }
    }
}
