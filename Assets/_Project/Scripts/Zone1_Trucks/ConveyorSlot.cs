namespace Project.Zone1.Trucks
{
    /// <summary>
    /// Pojedynczy "carrier" na conveyorze. Każdy slot ma własny TrackPosition i porusza się
    /// niezależnie — gdy jego truck ładuje (IsStopped=true) tylko ten slot stoi, a sloty za
    /// nim w formacji zatrzymują się dopiero gdy w niego "wpadną".
    /// </summary>
    public class ConveyorSlot
    {
        public int Index { get; }
        public float TrackPosition { get; set; }
        public Truck Truck { get; set; }
        public bool IsStopped { get; set; }

        public bool IsEmpty => Truck == null;

        public ConveyorSlot(int index, float initialTrackPosition)
        {
            Index = index;
            TrackPosition = initialTrackPosition;
        }
    }
}
