using UnityEngine;

namespace Project.Zone1.Trucks
{
    [System.Serializable]
    public struct WallSlot
    {
        public Vector3 WorldPosition;
        public int SlotIndex;
        public bool IsStopSlot;
    }
}
