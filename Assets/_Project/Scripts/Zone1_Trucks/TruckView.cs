using UnityEngine;
using Project.Zone1.FruitWall;

namespace Project.Zone1.Trucks
{
    public class TruckView : MonoBehaviour
    {
        [SerializeField] Renderer boxRenderer;

        Truck truck;
        ConveyorTrack track;
        Vector3 garageParkPosition;
        Material boxMaterial;

        public void Bind(Truck truck, ConveyorTrack track, Vector3 garageParkPosition)
        {
            this.truck = truck;
            this.track = track;
            this.garageParkPosition = garageParkPosition;
            ApplyColor();
        }

        public void SetGaragePosition(Vector3 pos) => garageParkPosition = pos;

        void ApplyColor()
        {
            if (boxRenderer == null) return;
            if (boxMaterial == null)
            {
                boxMaterial = new Material(boxRenderer.sharedMaterial);
                boxRenderer.material = boxMaterial;
            }
            boxMaterial.color = FruitColorPalette.GetColor(truck.FruitColor);
        }

        void LateUpdate()
        {
            if (truck == null) return;
            switch (truck.State)
            {
                case TruckState.InGarage:
                case TruckState.ReturningToGarage:
                    transform.position = garageParkPosition;
                    transform.rotation = Quaternion.identity;
                    break;
                default:
                    if (track != null)
                    {
                        transform.position = track.GetWorldPositionAtTrackParam(truck.TrackPosition);
                        Vector3 ahead = track.GetWorldPositionAtTrackParam((truck.TrackPosition + 0.001f) % 1f);
                        Vector3 fwd = (ahead - transform.position).normalized;
                        if (fwd.sqrMagnitude > 0.0001f)
                            transform.rotation = Quaternion.LookRotation(fwd, Vector3.up);
                    }
                    break;
            }
        }
    }
}
