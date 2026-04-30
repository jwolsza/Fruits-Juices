using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Project.Zone1.FruitWall;

namespace Project.Zone1.Trucks
{
    /// <summary>
    /// UI Button — każde kliknięcie:
    /// 1) rozszerza grid: addLeft kolumn po lewej, addRight kolumn po prawej, addTop rzędów u góry
    ///    (istniejące fruity zostają przy oryginalnych world positions),
    /// 2) (opcjonalnie) skaluje wallVisualTransform — JEŚLI Wall jest osobnym GameObject od
    ///    WallView/Grid w hierarchii, scale nie wpłynie na fruity,
    /// 3) odblokowuje kolejny typ owocu,
    /// 4) rozszerza conveyor track,
    /// 5) zwiększa prędkość ciężarówek.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class WallUpgradeButton : MonoBehaviour
    {
        [SerializeField] Zone1Manager zone1Manager;
        [SerializeField] Zone1TrucksManager trucksManager;

        [Header("Wall grid growth")]
        [SerializeField] int addLeftColsPerLevel = 10;
        [SerializeField] int addRightColsPerLevel = 10;
        [SerializeField] int addRowsPerLevel = 20;

        [Header("Wall visual scale (optional — Wall musi być OSOBNYM GameObject od WallView/Grid)")]
        [Tooltip("Visual mesh ściany (osobny GameObject od WallView). Skala AUTO-liczona ze stosunku currentGrid/initialGrid, dzięki czemu rośnie razem z gridem.")]
        [SerializeField] Transform wallVisualTransform;

        [Header("Track expansion")]
        [SerializeField] float trackXStepPerLevel = 0.3f;

        [Header("Truck speed")]
        [SerializeField] float truckSpeedStepPerLevel = 0.2f;

        [Header("Limit")]
        [SerializeField] int maxLevel = 12;

        [SerializeField] TMP_Text label;

        Button button;
        int level;
        float baseTruckSpeed;
        Vector3 initialVisualScale;
        int initialGridCols;
        int initialGridRows;

        public int Level => level;

        void Awake()
        {
            button = GetComponent<Button>();
            button.onClick.AddListener(Upgrade);
            if (trucksManager != null) baseTruckSpeed = trucksManager.TruckSpeedUnitsPerSec;
            if (wallVisualTransform != null) initialVisualScale = wallVisualTransform.localScale;
            if (zone1Manager != null) zone1Manager.SetExtraUnlockedTypes(0);
        }

        void Start()
        {
            // Capture initial grid dimensions AFTER Zone1Manager.Awake created the grid.
            if (zone1Manager != null && zone1Manager.Grid != null)
            {
                initialGridCols = zone1Manager.Grid.Columns;
                initialGridRows = zone1Manager.Grid.Rows;
            }
            ApplyVisualScale();
        }

        void Upgrade()
        {
            if (level >= maxLevel) return;
            level++;

            if (zone1Manager != null)
            {
                zone1Manager.SetExtraUnlockedTypes(level);
                zone1Manager.GrowWall(addLeftColsPerLevel, addRightColsPerLevel, addRowsPerLevel);
            }
            if (trucksManager != null)
            {
                trucksManager.ExpandTrack(trackXStepPerLevel);
                trucksManager.TruckSpeedUnitsPerSec = baseTruckSpeed + level * truckSpeedStepPerLevel;
            }
            ApplyVisualScale();
        }

        void ApplyVisualScale()
        {
            if (wallVisualTransform == null) return;
            if (initialGridCols <= 0 || initialGridRows <= 0) return;

            int currentCols = initialGridCols + level * (addLeftColsPerLevel + addRightColsPerLevel);
            int currentRows = initialGridRows + level * addRowsPerLevel;
            float widthRatio = (float)currentCols / initialGridCols;
            float heightRatio = (float)currentRows / initialGridRows;

            // Wall jest rotowane -90X — local X = world X (cols), local Y = world Z (rows after rotation).
            wallVisualTransform.localScale = new Vector3(
                initialVisualScale.x * widthRatio,
                initialVisualScale.y * heightRatio,
                initialVisualScale.z);
        }

        void Update()
        {
            if (label != null) label.text = $"Upgrade Wall (lvl. {level})";
            if (button != null) button.interactable = level < maxLevel;
        }
    }
}
