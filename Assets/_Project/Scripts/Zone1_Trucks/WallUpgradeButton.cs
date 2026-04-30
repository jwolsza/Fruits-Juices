using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Project.Zone1.FruitWall;

namespace Project.Zone1.Trucks
{
    /// <summary>
    /// UI Button — każde kliknięcie:
    /// 1) zwiększa scale Wall transformu o stepPerLevel (clamped do maxScale)
    /// 2) odblokowuje kolejny typ owocu w Zone1Manager (extra unlocked = level, capped)
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class WallUpgradeButton : MonoBehaviour
    {
        [SerializeField] Transform wallTransform;
        [SerializeField] Zone1Manager zone1Manager;
        [SerializeField] float baseScale = 0.5f;
        [SerializeField] float maxScale = 1.1f;
        [SerializeField] float stepPerLevel = 0.05f;
        [SerializeField] TMP_Text label;

        Button button;
        int level;

        public int Level => level;

        void Awake()
        {
            button = GetComponent<Button>();
            button.onClick.AddListener(Upgrade);
            ApplyAll();
        }

        void Upgrade()
        {
            if (wallTransform == null) return;
            float next = baseScale + (level + 1) * stepPerLevel;
            if (next > maxScale + 0.0001f) return;
            level++;
            ApplyAll();
        }

        void ApplyAll()
        {
            if (wallTransform != null)
            {
                float scale = Mathf.Clamp(baseScale + level * stepPerLevel, baseScale, maxScale);
                wallTransform.localScale = new Vector3(scale, scale, scale);
            }
            if (zone1Manager != null)
                zone1Manager.SetExtraUnlockedTypes(level);
        }

        void Update()
        {
            if (label != null)
            {
                float scale = Mathf.Clamp(baseScale + level * stepPerLevel, baseScale, maxScale);
                int activeTypes = zone1Manager != null ? zone1Manager.ActiveFruitTypes.Count : 0;
                label.text = $"Wall lvl {level}\n({scale:F2}x, {activeTypes} types)";
            }
            if (button != null)
            {
                bool atMax = baseScale + (level + 1) * stepPerLevel > maxScale + 0.0001f;
                button.interactable = !atMax;
            }
        }
    }
}
