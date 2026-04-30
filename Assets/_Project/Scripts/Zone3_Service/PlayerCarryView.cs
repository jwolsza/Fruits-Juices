using System.Collections.Generic;
using UnityEngine;
using Project.Core;
using Project.Zone1.FruitWall;

namespace Project.Zone3.Service
{
    /// <summary>
    /// Wizualny stos butelek niesionych przez gracza. Każdy pickup spawnuje butelkę
    /// (animowaną z miejsca racka) i dokleja do najbliższego wolnego slotu w stosie.
    /// Stos parented do stackOrigin — porusza/obraca się razem z graczem.
    /// </summary>
    public class PlayerCarryView : MonoBehaviour
    {
        [Header("Refs")]
        [Tooltip("Prefab pojedynczej butelki (3D mesh, z Renderer do tinta).")]
        [SerializeField] GameObject bottlePrefab;
        [Tooltip("Transform pod którym budowany jest stos. Zwykle child gracza, lekko nad/za jego pozycją.")]
        [SerializeField] Transform stackOrigin;

        [Header("Stack layout (cuboid: X×Z na poziom, potem Y w górę)")]
        [SerializeField] Vector3 stepX = new(0.1f, 0f, 0f);
        [SerializeField] Vector3 stepY = new(0f, 0.1f, 0f);
        [SerializeField] Vector3 stepZ = new(0f, 0f, 0.1f);
        [Tooltip("Ilość butelek wzdłuż X w jednym rzędzie.")]
        [SerializeField] int columns = 3;
        [Tooltip("Ilość rzędów wzdłuż Z w jednym poziomie (XZ floor).")]
        [SerializeField] int rows = 4;

        [Header("Fly animation")]
        [SerializeField] float flyDuration = 0.4f;
        [SerializeField] float flyArcHeight = 0.6f;

        readonly List<GameObject> bottles = new();

        public int VisibleCount => bottles.Count;

        public void BeginPickupAnimation(FruitType type, Vector3 fromWorld)
        {
            if (bottlePrefab == null) { Debug.LogWarning("[PlayerCarryView] bottlePrefab not assigned"); return; }
            if (stackOrigin == null) { Debug.LogWarning("[PlayerCarryView] stackOrigin not assigned"); return; }

            int idx = bottles.Count;
            Vector3 endLocal = SlotLocalPosition(idx);

            var go = Instantiate(bottlePrefab, stackOrigin);
            go.SetActive(true);
            go.name = $"CarriedBottle_{idx}_{type}";
            TintRenderer(go, type);
            bottles.Add(go);

            var fly = go.AddComponent<FlyingBottle>();
            fly.Begin(stackOrigin, fromWorld, endLocal, flyDuration, flyArcHeight);
        }

        /// <summary>Removes the most-recently-added bottle (LIFO). Used when delivering to customer.</summary>
        public void RemoveTop()
        {
            if (bottles.Count == 0) return;
            int last = bottles.Count - 1;
            var go = bottles[last];
            bottles.RemoveAt(last);
            if (go != null) Destroy(go);
        }

        Vector3 SlotLocalPosition(int idx)
        {
            int perRow = Mathf.Max(1, columns);
            int perLayer = perRow * Mathf.Max(1, rows);
            int y = idx / perLayer;             // poziom (Y) — wypełnia się jako ostatni
            int rem = idx % perLayer;
            int z = rem / perRow;               // kolejny rząd w głąb (Z) w obrębie poziomu
            int x = rem % perRow;               // kolumna w rzędzie (X)
            return x * stepX + y * stepY + z * stepZ;
        }

        void TintRenderer(GameObject go, FruitType type)
        {
            var r = go.GetComponentInChildren<Renderer>();
            if (r == null) return;
            r.material.color = FruitColorPalette.GetColor(type);
        }
    }
}
