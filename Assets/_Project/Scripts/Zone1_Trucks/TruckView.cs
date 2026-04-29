using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Project.Zone1.FruitWall;

namespace Project.Zone1.Trucks
{
    public class TruckView : MonoBehaviour
    {
        [Tooltip("Lista renderów które dostają kolor odpowiadający typowi owocu trucka.")]
        [SerializeField] List<Renderer> coloredRenderers = new();
        [Tooltip("Pivot, którego Y-scale rośnie z Load/Capacity (0..1). Box renderer powinien być jego dzieckiem.")]
        [SerializeField] Transform boxPivot;
        [Tooltip("Opcjonalny TMP label (TextMeshPro 3D albo TextMeshProUGUI) — wyświetla procent napełnienia trucka (np. \"75%\").")]
        [SerializeField] TMP_Text fillPercentText;

        Truck truck;
        ConveyorTrack track;
        Vector3 garageParkPosition;
        Vector3 waitingPosition;
        readonly List<Material> instancedMaterials = new();

        public void Bind(Truck truck, ConveyorTrack track, Vector3 garageParkPosition)
        {
            this.truck = truck;
            this.track = track;
            this.garageParkPosition = garageParkPosition;
            ApplyColor();
        }

        public void SetGaragePosition(Vector3 pos) => garageParkPosition = pos;
        public void SetWaitingPosition(Vector3 pos) => waitingPosition = pos;

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
            if (coloredRenderers == null || coloredRenderers.Count == 0) return;

            Color color = FruitColorPalette.GetColor(truck.FruitColor);

            if (instancedMaterials.Count == 0)
            {
                foreach (var r in coloredRenderers)
                {
                    if (r == null) { instancedMaterials.Add(null); continue; }
                    var mat = new Material(r.sharedMaterial);
                    r.material = mat;
                    instancedMaterials.Add(mat);
                }
            }

            foreach (var mat in instancedMaterials)
                if (mat != null) mat.color = color;
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
                case TruckState.WaitingDispatch:
                    transform.position = waitingPosition;
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
