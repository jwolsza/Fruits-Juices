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

            BigBottle anyAvailable = null;

            foreach (var b in bottles)
            {
                var effective = b.EffectiveType;
                // Matching type with ANY free space — truck deposits as much as fits, then reroutes for rest.
                if (effective.HasValue
                    && effective.Value == truckFruitColor
                    && b.EffectiveLoad < b.Capacity)
                {
                    return b;
                }
                if (!effective.HasValue && anyAvailable == null) anyAvailable = b;
            }

            return anyAvailable;
        }
    }
}
