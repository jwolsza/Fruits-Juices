using System.Collections.Generic;
using UnityEngine;
using Project.Core;
using Project.Zone1.FruitWall;

namespace Project.Zone1.Trucks
{
    public struct MagnetAssignment
    {
        public Truck Truck;
        public Vector2Int GridCellRemoved;
        public FruitType FruitType;
    }

    public static class MagnetSystem
    {
        /// <summary>
        /// Każdy truck w activeTrucks (czyli truck zaparkowany przy aktywnym wall slocie) ma
        /// "magnet" na CAŁĄ bottom row ściany — pozycja jego slotu nie ogranicza zasięgu.
        /// Per call (1 magnet tick) każdy truck zabiera 1 najbliższy lewostronnie pasujący owoc
        /// (z indexu 0 wzwyż). Trucki tego samego koloru dzielą się owocami (każdy bierze po 1
        /// na tick, w kolejności activeTrucks).
        /// </summary>
        public static List<MagnetAssignment> AssignFruitsToTrucksAtSlots(
            FruitGrid grid,
            IReadOnlyList<Truck> activeTrucks,
            int tickIndex)
        {
            var result = new List<MagnetAssignment>();
            if (grid == null || activeTrucks == null || activeTrucks.Count == 0) return result;

            bool ltr = (tickIndex % 2) == 0;
            int start = ltr ? 0 : grid.Columns - 1;
            int end = ltr ? grid.Columns : -1;
            int step = ltr ? 1 : -1;

            foreach (var truck in activeTrucks)
            {
                if (truck.IsFull) continue;

                for (int x = start; x != end; x += step)
                {
                    var cell = grid.GetCell(x, 0);
                    if (!cell.HasValue) continue;
                    if (cell.Value != truck.FruitColor) continue;

                    grid.ClearCell(x, 0);
                    truck.AddFruit();
                    result.Add(new MagnetAssignment
                    {
                        Truck = truck,
                        GridCellRemoved = new Vector2Int(x, 0),
                        FruitType = cell.Value,
                    });
                    break;
                }
            }

            return result;
        }
    }
}
