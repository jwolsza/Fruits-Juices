using System.Collections.Generic;
using UnityEngine;

namespace Project.Zone1.Trucks
{
    public class ConveyorTrack
    {
        readonly List<ConveyorWaypoint> waypoints;
        readonly float[] segmentLengths;
        readonly float totalLength;

        public IReadOnlyList<ConveyorWaypoint> Waypoints => waypoints;
        public float TotalLength => totalLength;

        public ConveyorTrack(IList<ConveyorWaypoint> waypoints)
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

        public void Tick(IReadOnlyList<Truck> trucks, float deltaTime, float speedUnitsPerSec)
        {
            if (trucks.Count == 0 || totalLength <= 0f) return;
            float deltaParam = (speedUnitsPerSec * deltaTime) / totalLength;

            var stoppedPositions = new List<float>();
            foreach (var t in trucks)
                if (t.State == TruckState.StoppedAtSlot) stoppedPositions.Add(t.TrackPosition);

            foreach (var truck in trucks)
            {
                if (truck.State == TruckState.StoppedAtSlot) continue;
                if (truck.State == TruckState.InGarage) continue;
                if (truck.State == TruckState.ReturningToGarage) continue;

                float current = truck.TrackPosition;
                float desired = current + deltaParam;
                float minBlocker = float.PositiveInfinity;
                foreach (float sp in stoppedPositions)
                {
                    float distAhead = (sp - current + 1f) % 1f;
                    if (distAhead > 0f && distAhead < minBlocker) minBlocker = distAhead;
                }
                if (minBlocker < float.PositiveInfinity && deltaParam >= minBlocker)
                    desired = current + minBlocker - 0.001f;

                truck.TrackPosition = ((desired % 1f) + 1f) % 1f;
            }
        }
    }
}
