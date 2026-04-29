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
        public static List<MagnetAssignment> AssignFruitsToTrucksAtSlots(
            FruitGrid grid,
            IReadOnlyList<(Truck truck, float slotWorldX)> trucksAtSlots,
            float wallLeftXWorld,
            float wallWidthWorld)
        {
            var result = new List<MagnetAssignment>();
            if (grid == null || trucksAtSlots == null || trucksAtSlots.Count == 0) return result;
            if (grid.Columns <= 0) return result;

            float cellWidthWorld = wallWidthWorld / grid.Columns;

            var available = new List<(int cellX, FruitType type)>();
            for (int x = 0; x < grid.Columns; x++)
            {
                var c = grid.GetCell(x, 0);
                if (c.HasValue) available.Add((x, c.Value));
            }

            foreach (var (truck, slotX) in trucksAtSlots)
            {
                if (truck.IsFull) continue;

                int bestIdx = -1;
                float bestDist = float.PositiveInfinity;
                for (int i = 0; i < available.Count; i++)
                {
                    var (cellX, type) = available[i];
                    if (type != truck.FruitColor) continue;
                    float worldX = wallLeftXWorld + cellX * cellWidthWorld + cellWidthWorld * 0.5f;
                    float d = Mathf.Abs(worldX - slotX);
                    if (d < bestDist) { bestDist = d; bestIdx = i; }
                }

                if (bestIdx >= 0)
                {
                    var (cellX, type) = available[bestIdx];
                    grid.ClearCell(cellX, 0);
                    truck.AddFruit();
                    available.RemoveAt(bestIdx);
                    result.Add(new MagnetAssignment
                    {
                        Truck = truck,
                        GridCellRemoved = new Vector2Int(cellX, 0),
                        FruitType = type,
                    });
                }
            }
            return result;
        }
    }
}
