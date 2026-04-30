using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Project.Zone1.Trucks
{
    /// <summary>
    /// UI Button — każde kliknięcie wywołuje Zone1TrucksManager.ExpandTrack(xStep).
    /// Punkty 1-4 idą w -X (lewy bok), punkty 0,6,7,8 w +X (prawy bok), 5 fixed.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class TrackUpgradeButton : MonoBehaviour
    {
        [SerializeField] Zone1TrucksManager manager;
        [SerializeField] float xStepPerLevel = 0.3f;
        [SerializeField] int maxLevel = 5;
        [SerializeField] TMP_Text label;

        Button button;
        int level;

        void Awake()
        {
            button = GetComponent<Button>();
            button.onClick.AddListener(Upgrade);
        }

        void Upgrade()
        {
            if (manager == null) return;
            if (level >= maxLevel) return;
            manager.ExpandTrack(xStepPerLevel);
            level++;
        }

        void Update()
        {
            if (label != null) label.text = $"Track lvl {level}";
            if (button != null) button.interactable = level < maxLevel;
        }
    }
}
