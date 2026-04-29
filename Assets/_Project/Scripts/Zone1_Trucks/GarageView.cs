using UnityEngine;
using System.Collections.Generic;

namespace Project.Zone1.Trucks
{
    /// <summary>
    /// Parking sloty generowane proceduralnie: pozycja[i] = firstParkingOffset + i * parkingStepOffset
    /// (w local space GarageView). Cap na maxParkingSlots — trucki poza cap'em stackują na ostatnim.
    /// Dzięki temu upgrade dodający więcej ciężarówek "po prostu" wydłuża rząd.
    /// </summary>
    public class GarageView : MonoBehaviour
    {
        [Header("Parking layout (local to this transform)")]
        [Tooltip("Lokalna pozycja pierwszego parking slotu.")]
        [SerializeField] Vector3 firstParkingOffset = Vector3.zero;
        [Tooltip("Lokalny offset między kolejnymi parking slotami.")]
        [SerializeField] Vector3 parkingStepOffset = new(1f, 0f, 0f);
        [Tooltip("Maksymalna liczba unikalnych parking pozycji. Powyżej — trucki stackują na ostatniej.")]
        [SerializeField] int maxParkingSlots = 10;

        public int MaxParkingSlots => maxParkingSlots;

        readonly Dictionary<int, TruckView> truckViewsById = new();
        readonly List<int> orderedTruckIds = new();

        public Vector3 GetParkPositionFor(int truckId)
        {
            int idx = orderedTruckIds.IndexOf(truckId);
            if (idx < 0) return transform.position;
            idx = Mathf.Min(idx, Mathf.Max(0, maxParkingSlots - 1));
            Vector3 local = firstParkingOffset + idx * parkingStepOffset;
            return transform.TransformPoint(local);
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
