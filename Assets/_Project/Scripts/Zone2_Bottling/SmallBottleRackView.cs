using UnityEngine;
using Project.Zone1.FruitWall;

namespace Project.Zone2.Bottling
{
    public class SmallBottleRackView : MonoBehaviour
    {
        [Tooltip("Prefab małej butelki — będzie klonowany dla każdego slotu (capacity razy).")]
        [SerializeField] GameObject smallBottleTemplate;
        [SerializeField] Vector3 firstSlotOffset = Vector3.zero;
        [SerializeField] Vector3 stepX = new(0.1f, 0f, 0f);
        [SerializeField] Vector3 stepY = new(0f, 0.1f, 0f);
        [SerializeField] int columns = 5;

        SmallBottleRack rack;
        GameObject[] slotInstances;
        Renderer[] slotRenderers;

        public SmallBottleRack Rack => rack;

        public void Bind(SmallBottleRack rack)
        {
            this.rack = rack;
            if (smallBottleTemplate != null) smallBottleTemplate.SetActive(false);

            slotInstances = new GameObject[rack.Capacity];
            slotRenderers = new Renderer[rack.Capacity];
            for (int i = 0; i < rack.Capacity; i++)
            {
                int col = i % columns;
                int row = i / columns;
                Vector3 local = firstSlotOffset + col * stepX + row * stepY;
                var go = smallBottleTemplate != null
                    ? Instantiate(smallBottleTemplate, transform)
                    : new GameObject($"Slot_{i}");
                if (smallBottleTemplate == null) go.transform.SetParent(transform, false);
                go.transform.localPosition = local;
                go.SetActive(false);
                slotInstances[i] = go;
                slotRenderers[i] = go.GetComponentInChildren<Renderer>();
            }
        }

        void LateUpdate()
        {
            if (rack == null || slotInstances == null) return;
            Color color = rack.CurrentType.HasValue
                ? FruitColorPalette.GetColor(rack.CurrentType.Value)
                : Color.gray;

            for (int i = 0; i < rack.Capacity; i++)
            {
                bool active = i < rack.Count;
                if (slotInstances[i] != null && slotInstances[i].activeSelf != active)
                    slotInstances[i].SetActive(active);
                if (active && slotRenderers[i] != null)
                    slotRenderers[i].material.color = color;
            }
        }
    }
}
