using System.Collections.Generic;
using UnityEngine;

namespace Project.Zone1.Trucks
{
    public class ConveyorTrack
    {
        readonly List<ConveyorWaypoint> waypoints;
        readonly float[] segmentLengths;
        readonly float totalLength;
        readonly List<ConveyorSlot> slots;

        float baseProgress;
        bool paused;

        public IReadOnlyList<ConveyorWaypoint> Waypoints => waypoints;
        public IReadOnlyList<ConveyorSlot> Slots => slots;
        public float TotalLength => totalLength;
        public bool Paused
        {
            get => paused;
            set => paused = value;
        }

        public ConveyorTrack(IList<ConveyorWaypoint> waypoints, int slotCount)
        {
            this.waypoints = new List<ConveyorWaypoint>(waypoints);
            int n = this.waypoints.Count;
            segmentLengths = new float[n];
            float total = 0f;
            for (int i = 0; i < n; i++)
            {
                int next = (i + 1) % n;
                float len = Vector3.Distance(this.waypoints[i].Position, this.waypoints[next].Position);
                segmentLengths[i] = len;
                total += len;
            }
            totalLength = total;

            slots = new List<ConveyorSlot>(slotCount);
            for (int i = 0; i < slotCount; i++)
            {
                float offset = slotCount > 0 ? (float)i / slotCount : 0f;
                slots.Add(new ConveyorSlot(i, offset));
            }
        }

        public Vector3 GetWorldPositionAtTrackParam(float t)
        {
            if (totalLength <= 0f) return waypoints.Count > 0 ? waypoints[0].Position : Vector3.zero;
            t = ((t % 1f) + 1f) % 1f;
            float distance = t * totalLength;
            for (int i = 0; i < segmentLengths.Length; i++)
            {
                if (distance <= segmentLengths[i])
                {
                    int next = (i + 1) % waypoints.Count;
                    float frac = segmentLengths[i] > 0f ? distance / segmentLengths[i] : 0f;
                    return Vector3.Lerp(waypoints[i].Position, waypoints[next].Position, frac);
                }
                distance -= segmentLengths[i];
            }
            return waypoints[0].Position;
        }

        public void Tick(float deltaTime, float speedUnitsPerSec)
        {
            if (paused || totalLength <= 0f) return;
            float deltaParam = (speedUnitsPerSec * deltaTime) / totalLength;
            baseProgress = ((baseProgress + deltaParam) % 1f + 1f) % 1f;
            foreach (var slot in slots)
            {
                slot.TrackPosition = (baseProgress + slot.SlotOffsetFromZero) % 1f;
                if (slot.Truck != null) slot.Truck.TrackPosition = slot.TrackPosition;
            }
        }

        /// <summary>
        /// Find first empty slot and place truck inside it. Returns true on success.
        /// </summary>
        public bool TryAssignTruckToFirstEmptySlot(Truck truck)
        {
            foreach (var slot in slots)
            {
                if (slot.IsEmpty)
                {
                    slot.Truck = truck;
                    truck.TrackPosition = slot.TrackPosition;
                    truck.State = TruckState.OnConveyor;
                    return true;
                }
            }
            return false;
        }

        public void RemoveTruckFromSlot(Truck truck)
        {
            foreach (var slot in slots)
            {
                if (slot.Truck == truck) { slot.Truck = null; return; }
            }
        }

        public ConveyorSlot FindSlotForTruck(Truck truck)
        {
            foreach (var slot in slots)
                if (slot.Truck == truck) return slot;
            return null;
        }
    }
}
