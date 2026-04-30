using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Project.Zone1.FruitWall;

namespace Project.Zone1.Trucks
{
    /// <summary>
    /// UI Button — każde kliknięcie:
    /// 1) zwiększa scale Wall transformu o stepPerLevel (clamped do maxScale),
    /// 2) odblokowuje kolejny typ owocu (Zone1Manager.SetExtraUnlockedTypes(level)),
    /// 3) rozszerza conveyor track o trackXStepPerLevel (Zone1TrucksManager.ExpandTrack).
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class WallUpgradeButton : MonoBehaviour
    {
        [SerializeField] Transform wallTransform;
        [SerializeField] Zone1Manager zone1Manager;
        [SerializeField] Zone1TrucksManager trucksManager;

        [Header("Wall scale")]
        [SerializeField] float baseScale = 0.5f;
        [SerializeField] float maxScale = 1.1f;
        [SerializeField] float stepPerLevel = 0.05f;

        [Header("Track expansion")]
        [Tooltip("Ile world units rozszerzyć conveyor (lewy bok -X, prawy bok +X) per upgrade level.")]
        [SerializeField] float trackXStepPerLevel = 0.3f;

        [SerializeField] TMP_Text label;

        Button button;
        int level;

        public int Level => level;

        void Awake()
        {
            button = GetComponent<Button>();
            button.onClick.AddListener(Upgrade);
            ApplyScaleAndTypes();
        }

        void Upgrade()
        {
            float next = baseScale + (level + 1) * stepPerLevel;
            if (next > maxScale + 0.0001f) return;
            level++;
            ApplyScaleAndTypes();
            if (trucksManager != null) trucksManager.ExpandTrack(trackXStepPerLevel);
        }

        void ApplyScaleAndTypes()
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
