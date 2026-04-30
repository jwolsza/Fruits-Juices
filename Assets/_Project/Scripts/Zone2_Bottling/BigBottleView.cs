using UnityEngine;
using TMPro;
using Project.Zone1.FruitWall;

namespace Project.Zone2.Bottling
{
    public class BigBottleView : MonoBehaviour
    {
        [Tooltip("Wewnętrzny pivot (np. cylinder soku) którego Y-scale rośnie wraz z fillAmount/Capacity (0..1).")]
        [SerializeField] Transform juiceFillPivot;
        [Tooltip("Renderer soku — jego color jest tintowany na typ owocu.")]
        [SerializeField] Renderer juiceRenderer;
        [Tooltip("Anchor gdzie ciężarówka stoi przy zsypywaniu (offset od butelki). Jeśli null — używa transform.position.")]
        [SerializeField] Transform dumpAnchor;
        [Tooltip("Opcjonalny TMP label nad butelką — pokazuje procent napełnienia.")]
        [SerializeField] TMP_Text fillPercentText;
        [Tooltip("Szybkość lerp scaling soku (większe = szybsze nadążanie za FillAmount).")]
        [SerializeField] float fillLerpSpeed = 1.5f;

        BigBottle bottle;
        Material juiceMaterial;
        float currentDisplayedFill;

        public BigBottle Bottle => bottle;
        public Vector3 DumpAnchorWorldPosition => dumpAnchor != null ? dumpAnchor.position : transform.position;

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
            float targetFill = bottle.Capacity > 0 ? Mathf.Clamp01((float)bottle.FillAmount / bottle.Capacity) : 0f;
            currentDisplayedFill = Mathf.MoveTowards(currentDisplayedFill, targetFill, fillLerpSpeed * Time.deltaTime);

            if (juiceFillPivot != null)
            {
                var s = juiceFillPivot.localScale;
                s.y = currentDisplayedFill;
                juiceFillPivot.localScale = s;
            }
            if (juiceMaterial != null)
            {
                Color c = (bottle.CurrentType ?? bottle.ReservedType).HasValue
                    ? FruitColorPalette.GetColor((bottle.CurrentType ?? bottle.ReservedType).Value)
                    : new Color(1f, 1f, 1f, 0f);
                juiceMaterial.color = c;
            }
            if (fillPercentText != null)
            {
                int percent = Mathf.RoundToInt(targetFill * 100f);
                fillPercentText.text = $"{percent}%";
            }
        }
    }
}
