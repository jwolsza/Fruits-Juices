using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Project.Zone1.Trucks
{
    /// <summary>
    /// UI Button — każde kliknięcie zwiększa scale Wall transformu o stepPerLevel,
    /// clampując do maxScale. Label pokazuje aktualny level/scale.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class WallUpgradeButton : MonoBehaviour
    {
        [SerializeField] Transform wallTransform;
        [SerializeField] float baseScale = 0.5f;
        [SerializeField] float maxScale = 1.1f;
        [SerializeField] float stepPerLevel = 0.05f;
        [SerializeField] TMP_Text label;

        Button button;
        int level;

        void Awake()
        {
            button = GetComponent<Button>();
            button.onClick.AddListener(Upgrade);
            ApplyScale();
        }

        void Upgrade()
        {
            if (wallTransform == null) return;
            float next = baseScale + (level + 1) * stepPerLevel;
            if (next > maxScale + 0.0001f) return;
            level++;
            ApplyScale();
        }

        void ApplyScale()
        {
            if (wallTransform == null) return;
            float scale = Mathf.Clamp(baseScale + level * stepPerLevel, baseScale, maxScale);
            wallTransform.localScale = new Vector3(scale, scale, scale);
        }

        void Update()
        {
            if (label != null)
            {
                float scale = Mathf.Clamp(baseScale + level * stepPerLevel, baseScale, maxScale);
                label.text = $"Wall lvl {level}\n({scale:F2}x)";
            }
            if (button != null)
            {
                bool atMax = baseScale + (level + 1) * stepPerLevel > maxScale + 0.0001f;
                button.interactable = !atMax;
            }
        }
    }
}
