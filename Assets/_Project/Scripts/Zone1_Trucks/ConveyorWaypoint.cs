using UnityEngine;

namespace Project.Zone1.Trucks
{
    [System.Serializable]
    public struct ConveyorWaypoint
    {
        public Vector3 Position;
        public bool IsActiveSlot;
        public int SlotIndex;
    }
}
