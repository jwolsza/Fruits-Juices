using System.Collections.Generic;

namespace Project.Zone1.Trucks
{
    public class Garage
    {
        readonly Dictionary<int, Truck> trucksById = new();
        readonly HashSet<int> onConveyorIds = new();
        public int MaxOnConveyor { get; }
        public int TruckCount => trucksById.Count;
        public int OnConveyorCount => onConveyorIds.Count;
        public IReadOnlyDictionary<int, Truck> TrucksById => trucksById;

        public Garage(int maxOnConveyor) { MaxOnConveyor = maxOnConveyor; }

        public void AddStarterTruck(Truck truck)
        {
            trucksById[truck.Id] = truck;
            truck.State = TruckState.InGarage;
        }

        public bool Dispatch(int truckId)
        {
            if (!trucksById.TryGetValue(truckId, out var truck)) return false;
            if (truck.State != TruckState.InGarage) return false;
            if (onConveyorIds.Count >= MaxOnConveyor) return false;
            truck.State = TruckState.EnteringConveyor;
            truck.TrackPosition = 0f;
            onConveyorIds.Add(truckId);
            return true;
        }

        public void ReturnToGarage(int truckId)
        {
            if (!trucksById.TryGetValue(truckId, out var truck)) return;
            truck.State = TruckState.InGarage;
            truck.EmptyLoad();
            onConveyorIds.Remove(truckId);
        }
    }
}
