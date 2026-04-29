using System.Collections.Generic;

namespace Project.Zone1.Trucks
{
    /// <summary>
    /// Trzyma listę ciężarówek (per kolor — startowo 1 per typ).
    /// Logika dispatch leży w Zone1TrucksManager (Garage tylko trzyma referencje).
    /// </summary>
    public class Garage
    {
        readonly Dictionary<int, Truck> trucksById = new();

        public int TruckCount => trucksById.Count;
        public IReadOnlyDictionary<int, Truck> TrucksById => trucksById;

        public void AddStarterTruck(Truck truck)
        {
            trucksById[truck.Id] = truck;
            truck.State = TruckState.InGarage;
        }

        public Truck Get(int truckId)
        {
            return trucksById.TryGetValue(truckId, out var truck) ? truck : null;
        }
    }
}
