using System.Collections.Generic;
using UnityEngine;

namespace Project.Zone1.Trucks
{
    public class ConveyorTrack
    {
        readonly List<ConveyorWaypoint> waypoints;
        float[] segmentLengths;
        float totalLength;
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

        public static float ComputeTotalLength(IList<ConveyorWaypoint> waypoints)
        {
            float total = 0f;
            int n = waypoints.Count;
            for (int i = 0; i < n; i++)
            {
                int next = (i + 1) % n;
                total += Vector3.Distance(waypoints[i].Position, waypoints[next].Position);
            }
            return total;
        }

        /// <summary>
        /// Creates a track where slot count is computed from physical dimensions:
        /// slotCount = floor(trackLength / (truckLength + gap)), MinSlotSpacing = truckLength / trackLength.
        /// </summary>
        public static ConveyorTrack CreateWithDynamicSlots(
            IList<ConveyorWaypoint> waypoints,
            float truckLengthWorld,
            float gapBetweenTrucksWorld)
        {
            float total = ComputeTotalLength(waypoints);
            float perSlot = Mathf.Max(0.0001f, truckLengthWorld + gapBetweenTrucksWorld);
            int count = Mathf.Max(1, Mathf.FloorToInt(total / perSlot));
            var track = new ConveyorTrack(waypoints, count);
            if (total > 0f) track.MinSlotSpacing = truckLengthWorld / total;
            return track;
        }

        public ConveyorTrack(IList<ConveyorWaypoint> waypoints, int slotCount)
        {
            this.waypoints = new List<ConveyorWaypoint>(waypoints);
            RecomputeLengths();

            slots = new List<ConveyorSlot>(slotCount);
            for (int i = 0; i < slotCount; i++)
            {
                float offset = slotCount > 0 ? (float)i / slotCount : 0f;
                slots.Add(new ConveyorSlot(i, offset));
            }
        }

        /// <summary>
        /// Replace waypoints in-place and recompute segment lengths + total length.
        /// Slots keep their TrackPosition (normalized 0..1) so they stay at the same
        /// fraction of the new geometry. Use to support runtime expansion / contraction.
        /// </summary>
        public void RebuildFromWaypoints(IList<ConveyorWaypoint> newWaypoints)
        {
            if (newWaypoints == null) return;
            this.waypoints.Clear();
            foreach (var wp in newWaypoints) this.waypoints.Add(wp);
            RecomputeLengths();
        }

        void RecomputeLengths()
        {
            int n = waypoints.Count;
            if (segmentLengths == null || segmentLengths.Length != n)
                segmentLengths = new float[n];
            float total = 0f;
            for (int i = 0; i < n; i++)
            {
                int next = (i + 1) % n;
                float len = Vector3.Distance(waypoints[i].Position, waypoints[next].Position);
                segmentLengths[i] = len;
                total += len;
            }
            totalLength = total;
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

        readonly List<int> sortOrderBuffer = new();

        /// <summary>
        /// Advance slots independently. A slot with IsStopped=true stays put. Each non-stopped
        /// slot advances by deltaParam but cannot come closer than MinSlotSpacing to ANY other
        /// slot ahead (stopped or not). Slots are processed in order of descending TrackPosition
        /// (front-most first) so trailing slots see the up-to-date positions of leaders.
        /// </summary>
        public void Tick(float deltaTime, float speedUnitsPerSec)
        {
            if (paused || totalLength <= 0f) return;
            float deltaParam = (speedUnitsPerSec * deltaTime) / totalLength;

            // Build processing order: indices of slots sorted by descending TrackPosition.
            sortOrderBuffer.Clear();
            for (int i = 0; i < slots.Count; i++) sortOrderBuffer.Add(i);
            sortOrderBuffer.Sort((a, b) => slots[b].TrackPosition.CompareTo(slots[a].TrackPosition));

            foreach (int idx in sortOrderBuffer)
            {
                var slot = slots[idx];
                if (slot.IsStopped) continue;

                float current = slot.TrackPosition;
                float desired = current + deltaParam;

                // Find nearest OCCUPIED slot ahead. Empty slots don't physically collide
                // (no actual truck visualized) — they're abstract carriers used for capacity
                // accounting only. Without this, empty slot in formation could block trailing
                // trucks even though there's no truck in front of them.
                float minBlocker = float.PositiveInfinity;
                for (int j = 0; j < slots.Count; j++)
                {
                    if (j == idx) continue;
                    if (slots[j].IsEmpty) continue;
                    float other = slots[j].TrackPosition;
                    float distAhead = (other - current + 1f) % 1f;
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

        /// <summary>
        /// Assigns truck only if there's an empty slot within `windowTrackParam` distance from
        /// `entryTrackParam`. Used dla "wjazd na conveyor w konkretnym waypoincie" — truck
        /// trafia na slot który aktualnie jest pod punktem wjazdu.
        /// </summary>
        public bool TryAssignTruckAtTrackParam(Truck truck, float entryTrackParam, float windowTrackParam)
        {
            foreach (var slot in slots)
            {
                if (!slot.IsEmpty) continue;
                float dist = Mathf.Abs(((slot.TrackPosition - entryTrackParam) + 1f) % 1f);
                dist = Mathf.Min(dist, 1f - dist);
                if (dist <= windowTrackParam)
                {
                    slot.Truck = truck;
                    truck.TrackPosition = slot.TrackPosition;
                    truck.State = TruckState.OnConveyor;
                    return true;
                }
            }
            return false;
        }

        public float GetWaypointTrackParam(int waypointIndex)
        {
            if (waypointIndex < 0 || waypointIndex >= waypoints.Count || totalLength <= 0f) return 0f;
            float dist = 0f;
            for (int i = 0; i < waypointIndex; i++) dist += segmentLengths[i];
            return dist / totalLength;
        }

        public int EmptySlotCount
        {
            get
            {
                int n = 0;
                foreach (var s in slots) if (s.IsEmpty) n++;
                return n;
            }
        }

        /// <summary>
        /// Snaps the first empty slot to the given track position and assigns the truck.
        /// Useful when conveyor is empty — bypasses the entry-window check so first truck
        /// doesn't have to wait for a slot to drift to entry.
        /// </summary>
        public bool ForceAssignTruckAt(Truck truck, float trackPosition)
        {
            foreach (var slot in slots)
            {
                if (slot.IsEmpty)
                {
                    slot.TrackPosition = ((trackPosition % 1f) + 1f) % 1f;
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

        /// <summary>Ensures track has at least `desiredCount` slots. New slots are appended with
        /// evenly-distributed initial TrackPosition. Existing slots keep their positions.</summary>
        public void EnsureSlotCount(int desiredCount)
        {
            while (slots.Count < desiredCount)
            {
                int newIdx = slots.Count;
                float pos = desiredCount > 0 ? (float)newIdx / desiredCount : 0f;
                slots.Add(new ConveyorSlot(newIdx, pos));
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
