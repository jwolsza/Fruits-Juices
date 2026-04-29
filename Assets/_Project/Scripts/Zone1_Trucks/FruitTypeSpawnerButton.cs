using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Project.Core;
using Project.Zone1.FruitWall;

namespace Project.Zone1.Trucks
{
    /// <summary>
    /// UI button który po kliknięciu dodaje nową ciężarówkę określonego typu owocu do garażu
    /// (przez Zone1TrucksManager.AddTruck). Label aktualizuje się co frame: "Apple (3)".
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class FruitTypeSpawnerButton : MonoBehaviour
    {
        [SerializeField] FruitType type;
        [SerializeField] Zone1TrucksManager manager;
        [SerializeField] TMP_Text label;
        [Tooltip("Tło button'a (Image), kolorowane na typ owocu. Auto-fallback do Image na tym GameObject.")]
        [SerializeField] Image backgroundImage;

        Button button;

        public void Configure(FruitType newType, Zone1TrucksManager newManager)
        {
            type = newType;
            manager = newManager;
            if (label == null) label = GetComponentInChildren<TMP_Text>(includeInactive: true);
            if (backgroundImage == null) backgroundImage = GetComponent<Image>();
            ApplyBackgroundColor();
        }

        void ApplyBackgroundColor()
        {
            if (backgroundImage == null) return;
            backgroundImage.color = FruitColorPalette.GetColor(type);
        }

        void Awake()
        {
            button = GetComponent<Button>();
            button.onClick.AddListener(OnClick);
        }

        void OnDestroy()
        {
            if (button != null) button.onClick.RemoveListener(OnClick);
        }

        void OnClick()
        {
            if (manager != null) manager.AddTruck(type);
        }

        void Update()
        {
            if (manager == null) return;
            if (label != null) label.text = $"{type} ({manager.GetTruckCount(type)})";
            if (button != null) button.interactable = manager.CanAddTruck();
        }
    }
}
