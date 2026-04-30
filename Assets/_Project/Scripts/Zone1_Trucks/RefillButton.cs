using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Project.Zone1.FruitWall;

namespace Project.Zone1.Trucks
{
    /// <summary>
    /// UI Button — refill na żądanie z poziomowym storage (% gridu) + cooldown.
    /// Każdy click napełnia grid do CurrentFillPercent (cap 50%) i odpala cooldown
    /// (skraca się z poziomem). Storage % i cooldown definiowane per-poziom w arrayach.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class RefillButton : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] Zone1Manager zone1Manager;
        [SerializeField] TMP_Text label;
        [Tooltip("Opcjonalny Image typu Filled — fillAmount animowany w trakcie cooldownu.")]
        [SerializeField] Image cooldownFillImage;
        [Tooltip("Opcjonalny RectTransform — szerokość (sizeDelta.x) animowana w trakcie cooldownu. Użyj przy sprite'ach Sliced (filled+sliced źle się renderuje).")]
        [SerializeField] RectTransform cooldownFillRect;
        [Tooltip("Pełna szerokość cooldownFillRect (referencyjna) gdy cooldown jest pełny. 0 = czytaj z aktualnego sizeDelta.x na Awake.")]
        [SerializeField] float cooldownFillRectFullWidth = 0f;
        [Tooltip("True: 0 → 1 (ładowanie się napełnia). False: 1 → 0 (drenuje).")]
        [SerializeField] bool fillGrowsDuringCooldown = true;

        [Header("Levels (index = level, 0-based)")]
        [Tooltip("Jeśli ustawione, level brany z WallUpgradeButton.Level. Inaczej z pola 'level' poniżej.")]
        [SerializeField] WallUpgradeButton wallUpgradeButton;
        [SerializeField] int level = 0;
        [Tooltip("% gridu DOSYPYWANY przy każdym kliknięciu wg poziomu. Wartości > 0.5 będą capnięte do 0.5.")]
        [SerializeField] float[] fillPercentPerLevel = new[] { 0.25f, 0.30f, 0.35f, 0.40f, 0.45f, 0.50f };
        [Tooltip("Cooldown (sek) wg poziomu.")]
        [SerializeField] float[] cooldownPerLevel = new[] { 12f, 10f, 8f, 6f, 5f, 4f };

        [Header("Caps")]
        [Tooltip("Twardy cap pojedynczego dosypania (zgodnie ze specem: 50% gridu).")]
        [SerializeField] float maxFillPercent = 0.5f;

        Button button;
        float cooldownRemaining;
        float cooldownStarted;

        public int Level
        {
            get => wallUpgradeButton != null ? wallUpgradeButton.Level : level;
            set { if (wallUpgradeButton == null) level = Mathf.Max(0, value); }
        }
        public int MaxLevel => Mathf.Max(fillPercentPerLevel?.Length ?? 0, cooldownPerLevel?.Length ?? 0) - 1;
        public float CurrentFillPercent => Mathf.Min(maxFillPercent, SampleArray(fillPercentPerLevel, Level, 0.25f));
        public float CurrentCooldown => SampleArray(cooldownPerLevel, Level, 5f);
        public float CooldownRemaining => cooldownRemaining;

        static float SampleArray(float[] arr, int idx, float fallback)
        {
            if (arr == null || arr.Length == 0) return fallback;
            return arr[Mathf.Clamp(idx, 0, arr.Length - 1)];
        }

        void Awake()
        {
            button = GetComponent<Button>();
            button.onClick.AddListener(OnClick);
            if (cooldownFillRect != null && cooldownFillRectFullWidth <= 0f)
                cooldownFillRectFullWidth = cooldownFillRect.sizeDelta.x;
        }

        void Update()
        {
            if (cooldownRemaining > 0f)
                cooldownRemaining = Mathf.Max(0f, cooldownRemaining - Time.deltaTime);
            UpdateUI();
        }

        int ComputeIncrementCells()
        {
            if (zone1Manager == null) return 0;
            return Mathf.RoundToInt(zone1Manager.GridCellCount * CurrentFillPercent);
        }

        int ComputeTargetOccupied()
        {
            if (zone1Manager == null) return 0;
            int target = zone1Manager.GridOccupiedCount + ComputeIncrementCells();
            return Mathf.Min(target, zone1Manager.GridCellCount);
        }

        void OnClick()
        {
            if (cooldownRemaining > 0f) return;
            if (zone1Manager == null) return;
            if (zone1Manager.IsRefillInProgress) return;
            if (zone1Manager.GridOccupiedCount >= zone1Manager.GridCellCount) return; // grid pełny

            int target = ComputeTargetOccupied();
            zone1Manager.StartRefill(target);
            cooldownStarted = CurrentCooldown;
            cooldownRemaining = cooldownStarted;
        }

        void UpdateUI()
        {
            if (zone1Manager == null) return;

            bool onCooldown = cooldownRemaining > 0f;
            bool gridFull = zone1Manager.GridOccupiedCount >= zone1Manager.GridCellCount;
            bool busy = zone1Manager.IsRefillInProgress;

            if (button != null) button.interactable = !onCooldown && !gridFull && !busy;

            if (label != null)
                label.text = onCooldown ? $"{cooldownRemaining:0.0}s" : "Refill";

            float drainNorm = cooldownStarted > 0f ? cooldownRemaining / cooldownStarted : 0f;
            float displayNorm = fillGrowsDuringCooldown ? 1f - drainNorm : drainNorm;

            if (cooldownFillImage != null)
                cooldownFillImage.fillAmount = displayNorm;

            if (cooldownFillRect != null)
            {
                var sd = cooldownFillRect.sizeDelta;
                sd.x = cooldownFillRectFullWidth * displayNorm;
                cooldownFillRect.sizeDelta = sd;
            }
        }
    }
}
