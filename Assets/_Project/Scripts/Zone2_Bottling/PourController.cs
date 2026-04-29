namespace Project.Zone2.Bottling
{
    public static class PourController
    {
        /// <summary>
        /// Pour from BigBottle to Rack. fruitsPerSmall = how many fruits per small bottle.
        /// Returns number of small bottles spawned.
        /// </summary>
        public static int Pour(BigBottle bottle, SmallBottleRack rack, int fruitsPerSmall)
        {
            if (bottle == null || rack == null || fruitsPerSmall <= 0) return 0;
            if (bottle.IsEmpty || !bottle.CurrentType.HasValue) return 0;
            if (rack.CurrentType.HasValue && rack.CurrentType.Value != bottle.CurrentType.Value) return 0;

            int possibleByFill = bottle.FillAmount / fruitsPerSmall;
            int possibleByRack = rack.FreeSlots;
            int actual = possibleByFill < possibleByRack ? possibleByFill : possibleByRack;
            if (actual <= 0) return 0;

            var type = bottle.CurrentType.Value;
            bottle.Drain(actual * fruitsPerSmall);
            rack.Add(type, actual);
            return actual;
        }
    }
}
