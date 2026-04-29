namespace Project.Zone1.Trucks
{
    /// <summary>
    /// Pojedynczy "carrier" na conveyorze — fixed offset na torze, optional Truck wewnątrz.
    /// Wszystkie sloty conveyora poruszają się w formacji ze wspólnym base progress.
    /// </summary>
    public class ConveyorSlot
    {
        public int Index { get; }
        public float SlotOffsetFromZero { get; }
        public float TrackPosition { get; set; }
        public Truck Truck { get; set; }

        public bool IsEmpty => Truck == null;

        public ConveyorSlot(int index, float offsetFromZero)
        {
            Index = index;
            SlotOffsetFromZero = offsetFromZero;
            TrackPosition = offsetFromZero;
        }
    }
}
