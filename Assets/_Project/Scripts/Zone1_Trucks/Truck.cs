using Project.Core;
using UnityEngine;

namespace Project.Zone1.Trucks
{
    public class Truck
    {
        public int Id { get; }
        public FruitType FruitColor { get; }
        public int Capacity { get; private set; }
        public int Load { get; private set; }
        public TruckState State { get; set; } = TruckState.InGarage;
        public float TrackPosition { get; set; }
        public Vector3 DumpTargetWorldPos { get; set; }

        public bool IsFull => Load >= Capacity;

        public Truck(int id, FruitType color, int capacity)
        {
            Id = id;
            FruitColor = color;
            Capacity = capacity;
        }

        public void AddFruit() { if (Load < Capacity) Load++; }
        public void EmptyLoad() { Load = 0; }
        public void SetCapacity(int newCapacity) { Capacity = newCapacity; }
    }
}
