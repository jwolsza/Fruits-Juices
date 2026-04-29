using UnityEngine;
using TMPro;
using Project.Zone1.FruitWall;

namespace Project.Zone1.Trucks
{
    public class TruckView : MonoBehaviour
    {
        [SerializeField] Renderer boxRenderer;
        [Tooltip("Pivot, którego Y-scale rośnie z Load/Capacity (0..1). Box renderer powinien być jego dzieckiem.")]
        [SerializeField] Transform boxPivot;
        [Tooltip("Opcjonalny TextMeshPro 3D — wyświetla procent napełnienia trucka (np. \"75%\").")]
        [SerializeField] TextMeshPro fillPercentText;

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

        void UpdateBoxFillScale()
        {
            if (truck.Capacity <= 0) return;
            float fill = Mathf.Clamp01((float)truck.Load / truck.Capacity);

            if (boxPivot != null)
            {
                var s = boxPivot.localScale;
                s.y = fill;
                boxPivot.localScale = s;
            }

            if (fillPercentText != null)
            {
                int percent = Mathf.RoundToInt(fill * 100f);
                fillPercentText.text = $"{percent}%";
            }
        }

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

            UpdateBoxFillScale();

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
