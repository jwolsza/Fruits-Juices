using System.Collections.Generic;
using UnityEngine;
using Project.Core;
using Project.Data;

namespace Project.Zone1.Trucks
{
    /// <summary>
    /// Generuje FruitTypeSpawnerButton'y dynamicznie — po jednym dla każdego typu owocu z
    /// balance.StartingFruitTypes. Jeśli lista typów się rozszerzy w runtime (np. odblokowanie
    /// nowego owoca), brakujące button'y dodawane są automatycznie.
    /// </summary>
    public class FruitTypeSpawnerPanel : MonoBehaviour
    {
        [Tooltip("Source listy aktualnie dostępnych typów owoców (StartingFruitTypes).")]
        [SerializeField] GameBalanceSO balance;
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
            if (balance == null || manager == null || buttonTemplate == null || buttonContainer == null) return;
            if (balance.StartingFruitTypes == null) return;

            foreach (var type in balance.StartingFruitTypes)
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
