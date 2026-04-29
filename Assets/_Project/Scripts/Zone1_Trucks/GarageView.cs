using UnityEngine;
using System.Collections.Generic;

namespace Project.Zone1.Trucks
{
    public class GarageView : MonoBehaviour
    {
        [SerializeField] Vector3[] parkingSlots;

        readonly Dictionary<int, TruckView> truckViewsById = new();
        readonly List<int> orderedTruckIds = new();

        public Vector3 GetParkPositionFor(int truckId)
        {
            int idx = orderedTruckIds.IndexOf(truckId);
            if (idx < 0 || parkingSlots == null || parkingSlots.Length == 0) return transform.position;
            return transform.TransformPoint(parkingSlots[idx % parkingSlots.Length]);
        }

        public void RegisterTruckView(int truckId, TruckView view)
        {
            truckViewsById[truckId] = view;
            if (!orderedTruckIds.Contains(truckId)) orderedTruckIds.Add(truckId);
        }

        public int TryGetTappedTruckId(Camera cam, Vector2 screenPos)
        {
            if (cam == null) return -1;
            Ray ray = cam.ScreenPointToRay(screenPos);
            if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
            {
                var view = hit.collider.GetComponentInParent<TruckView>();
                if (view != null)
                    foreach (var kv in truckViewsById)
                        if (kv.Value == view) return kv.Key;
            }
            return -1;
        }
    }
}
