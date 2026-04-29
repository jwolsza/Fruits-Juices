using UnityEngine;
using System.Collections.Generic;

namespace Project.Zone1.Trucks
{
    [RequireComponent(typeof(LineRenderer))]
    public class ConveyorView : MonoBehaviour
    {
        [SerializeField] float lineWidth = 0.1f;
        [SerializeField] Color lineColor = new(0.4f, 0.4f, 0.45f);

        public void Build(IReadOnlyList<ConveyorWaypoint> waypoints)
        {
            var lr = GetComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.loop = true;
            lr.startWidth = lineWidth;
            lr.endWidth = lineWidth;
            lr.startColor = lineColor;
            lr.endColor = lineColor;
            var pts = new Vector3[waypoints.Count];
            for (int i = 0; i < waypoints.Count; i++) pts[i] = waypoints[i].Position;
            lr.positionCount = pts.Length;
            lr.SetPositions(pts);
        }
    }
}
