using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Project.Zone2.Bottling
{
    /// <summary>
    /// UI Button — każde kliknięcie dodaje kolejną dużą butelkę (i jej rack) w SecondZone,
    /// pozycjonowaną w jednej linii według firstBottleOffset + N * bottleStepOffset z Zone2Manager.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class BottleUpgradeButton : MonoBehaviour
    {
        [SerializeField] Zone2Manager zone2Manager;
        [SerializeField] TMP_Text label;

        Button button;

        void Awake()
        {
            button = GetComponent<Button>();
            button.onClick.AddListener(Upgrade);
        }

        void Upgrade()
        {
            if (zone2Manager != null) zone2Manager.AddBottle();
        }

        void Update()
        {
            if (zone2Manager == null) return;
            if (label != null)
                label.text = $"Buy Bottle ({zone2Manager.BottleCount}/{zone2Manager.MaxBottles})";
            if (button != null) button.interactable = zone2Manager.CanAddBottle();
        }
    }
}
