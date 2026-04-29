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

        bool paused;
        float minSlotSpacing = 0.05f;

        public IReadOnlyList<ConveyorWaypoint> Waypoints => waypoints;
        public IReadOnlyList<ConveyorSlot> Slots => slots;
        public float TotalLength => totalLength;

        /// <summary>Global pause (np. refill na ścianie). Per-slot stop oparty na ConveyorSlot.IsStopped.</summary>
        public bool Paused
        {
            get => paused;
            set => paused = value;
        }

        /// <summary>Minimalny dystans w track-param (0..1) jaki sloty muszą zachować między sobą.</summary>
        public float MinSlotSpacing
        {
            get => minSlotSpacing;
            set => minSlotSpacing = Mathf.Max(0f, value);
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

        /// <summary>
        /// Advance slots independently. A slot with IsStopped=true stays put; non-stopped slots
        /// advance by deltaParam but cannot pass within a stopped slot in front (formation pile-up).
        /// </summary>
        public void Tick(float deltaTime, float speedUnitsPerSec)
        {
            if (paused || totalLength <= 0f) return;
            float deltaParam = (speedUnitsPerSec * deltaTime) / totalLength;

            var stoppedPositions = new List<float>();
            foreach (var s in slots)
                if (s.IsStopped) stoppedPositions.Add(s.TrackPosition);

            foreach (var slot in slots)
            {
                if (slot.IsStopped) continue;

                float current = slot.TrackPosition;
                float desired = current + deltaParam;

                float minBlocker = float.PositiveInfinity;
                foreach (float sp in stoppedPositions)
                {
                    float distAhead = (sp - current + 1f) % 1f;
                    if (distAhead > 0f && distAhead < minBlocker) minBlocker = distAhead;
                }
                if (minBlocker < float.PositiveInfinity)
                {
                    float maxAdvance = Mathf.Max(0f, minBlocker - minSlotSpacing);
                    float allowed = current + maxAdvance;
                    if (desired > allowed) desired = allowed;
                }

                slot.TrackPosition = ((desired % 1f) + 1f) % 1f;
                if (slot.Truck != null) slot.Truck.TrackPosition = slot.TrackPosition;
            }
        }

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
                if (slot.Truck == truck)
                {
                    slot.Truck = null;
                    slot.IsStopped = false;
                    return;
                }
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
