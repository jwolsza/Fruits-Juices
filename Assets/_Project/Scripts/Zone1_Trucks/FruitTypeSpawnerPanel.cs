using System.Collections.Generic;
using UnityEngine;
using Project.Core;
using Project.Zone1.FruitWall;

namespace Project.Zone1.Trucks
{
    /// <summary>
    /// Generuje FruitTypeSpawnerButton'y dynamicznie — po jednym dla każdego typu owocu z
    /// Zone1Manager.ActiveFruitTypes (starter + odblokowane). Jeśli lista się rozszerzy w
    /// runtime (np. po wall upgrade), brakujące button'y dodawane są automatycznie.
    /// </summary>
    public class FruitTypeSpawnerPanel : MonoBehaviour
    {
        [SerializeField] Zone1Manager zone1Manager;
        [SerializeField] Zone1TrucksManager manager;
        [Tooltip("Prefab template button'a — będzie klonowany dla każdego typu. Sam template jest dezaktywowany przy starcie.")]
        [SerializeField] FruitTypeSpawnerButton buttonTemplate;
        [Tooltip("Parent w UI gdzie spawnowane button'y będą umieszczane (np. VerticalLayoutGroup).")]
        [SerializeField] Transform buttonContainer;

        readonly Dictionary<FruitType, FruitTypeSpawnerButton> buttons = new();

        void Start()
        {
            if (buttonTemplate != null) buttonTemplate.gameObject.SetActive(false);
            RefreshButtons();
        }

        void Update()
        {
            RefreshButtons();
        }

        public void RefreshButtons()
        {
            if (zone1Manager == null || manager == null || buttonTemplate == null || buttonContainer == null) return;
            var active = zone1Manager.ActiveFruitTypes;
            if (active == null) return;

            foreach (var type in active)
            {
                if (buttons.ContainsKey(type)) continue;
                var btn = Instantiate(buttonTemplate, buttonContainer);
                btn.gameObject.SetActive(true);
                btn.name = $"SpawnerButton_{type}";
                btn.Configure(type, manager);
                buttons[type] = btn;
            }
        }
    }
}
