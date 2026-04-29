using UnityEngine;
using Project.Zone1.FruitWall;

namespace Project.Zone2.Bottling
{
    public class BigBottleView : MonoBehaviour
    {
        [Tooltip("Wewnętrzny pivot (np. cylinder soku) którego Y-scale rośnie wraz z fillAmount/Capacity (0..1).")]
        [SerializeField] Transform juiceFillPivot;
        [Tooltip("Renderer soku — jego color jest tintowany na typ owocu.")]
        [SerializeField] Renderer juiceRenderer;

        BigBottle bottle;
        Material juiceMaterial;

        public BigBottle Bottle => bottle;
        public Vector3 DumpAnchorWorldPosition => transform.position;

        public void Bind(BigBottle bottle)
        {
            this.bottle = bottle;
            if (juiceRenderer != null && juiceMaterial == null)
            {
                juiceMaterial = new Material(juiceRenderer.sharedMaterial);
                juiceRenderer.material = juiceMaterial;
            }
        }

        void LateUpdate()
        {
            if (bottle == null) return;
            float fill = bottle.Capacity > 0 ? Mathf.Clamp01((float)bottle.FillAmount / bottle.Capacity) : 0f;
            if (juiceFillPivot != null)
            {
                var s = juiceFillPivot.localScale;
                s.y = fill;
                juiceFillPivot.localScale = s;
            }
            if (juiceMaterial != null)
            {
                Color c = bottle.CurrentType.HasValue
                    ? FruitColorPalette.GetColor(bottle.CurrentType.Value)
                    : new Color(1f, 1f, 1f, 0f);
                juiceMaterial.color = c;
            }
        }
    }
}
