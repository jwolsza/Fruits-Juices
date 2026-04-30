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
        [SerializeField] Zone1Manager zone1Manager;
        [SerializeField] Zone1TrucksManager trucksManager;

        [Header("Wall growth (rows + columns added per level)")]
        [Tooltip("Ile rzędów dodać u góry ściany per upgrade level (puste komórki, fruity istniejące zostają na dole).")]
        [SerializeField] int addRowsPerLevel = 20;
        [Tooltip("Ile kolumn dodać po prawej stronie ściany per upgrade level.")]
        [SerializeField] int addColsPerLevel = 0;
        [SerializeField] int maxLevel = 12;

        [Header("Track expansion")]
        [Tooltip("Ile world units rozszerzyć conveyor (lewy bok -X, prawy bok +X) per upgrade level.")]
        [SerializeField] float trackXStepPerLevel = 0.3f;

        [Header("Truck speed")]
        [Tooltip("Bonus prędkości ciężarówek (world units/s) dodawany per level upgrade'u.")]
        [SerializeField] float truckSpeedStepPerLevel = 0.2f;

        [SerializeField] TMP_Text label;

        Button button;
        int level;
        float baseTruckSpeed;

        public int Level => level;

        void Awake()
        {
            button = GetComponent<Button>();
            button.onClick.AddListener(Upgrade);
            if (trucksManager != null) baseTruckSpeed = trucksManager.TruckSpeedUnitsPerSec;
            // Initial unlock state for level 0 (no extras).
            if (zone1Manager != null) zone1Manager.SetExtraUnlockedTypes(0);
        }

        void Upgrade()
        {
            if (level >= maxLevel) return;
            level++;
            if (zone1Manager != null)
            {
                zone1Manager.SetExtraUnlockedTypes(level);
                zone1Manager.GrowWall(addColsPerLevel, addRowsPerLevel);
            }
            if (trucksManager != null)
            {
                trucksManager.ExpandTrack(trackXStepPerLevel);
                trucksManager.TruckSpeedUnitsPerSec = baseTruckSpeed + level * truckSpeedStepPerLevel;
            }
        }

        void Update()
        {
            if (label != null)
                label.text = $"Upgrade Wall (lvl. {level})";
            if (button != null) button.interactable = level < maxLevel;
        }
    }
}
