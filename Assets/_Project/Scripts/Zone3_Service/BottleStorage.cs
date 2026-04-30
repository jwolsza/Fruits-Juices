using System.Collections.Generic;
using UnityEngine;
using Project.Core;
using Project.Zone1.FruitWall;

namespace Project.Zone3.Service
{
    /// <summary>
    /// Storage z wizualnym stosem butelek (cuboid layout: X×Z na poziom, potem Y w górę).
    /// Trzyma referencje do GameObject'ów per zapisanej butelce (do animacji wyjścia).
    /// </summary>
    public class BottleStorage : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] GameObject bottlePrefab;
        [SerializeField] Transform stackOrigin;

        [Header("Stack layout (cuboid: X×Z na poziom, potem Y w górę)")]
        [SerializeField] Vector3 stepX = new(0.1f, 0f, 0f);
        [SerializeField] Vector3 stepY = new(0f, 0.1f, 0f);
        [SerializeField] Vector3 stepZ = new(0f, 0f, 0.1f);
        [SerializeField] int columns = 3;
        [SerializeField] int rows = 4;
        [Tooltip("Maksymalna liczba butelek w storage. 0 = brak limitu.")]
        [SerializeField] int capacity = 0;

        [Header("Fly animation (incoming)")]
        [SerializeField] float flyDuration = 0.4f;
        [SerializeField] float flyArcHeight = 0.5f;

        class Entry
        {
            public FruitType Type;
            public GameObject Go;
            public int SlotIndex;
        }

        readonly List<Entry> entries = new();
        readonly HashSet<int> usedSlots = new();

        public Vector3 WorldPosition => transform.position;
        public int Count => entries.Count;
        public int Capacity => capacity;
        public bool IsFull => capacity > 0 && entries.Count >= capacity;
        public bool IsEmpty => entries.Count == 0;

        public bool HasType(FruitType type)
        {
            for (int i = 0; i < entries.Count; i++)
                if (entries[i].Type == type) return true;
            return false;
        }

        /// <summary>Wyjmij topową (ostatnio dodaną) butelkę — bez dziur w stosie.</summary>
        public bool TryTakeAny(out Vector3 fromWorld)
        {
            if (entries.Count == 0) { fromWorld = transform.position; return false; }
            int last = entries.Count - 1;
            var e = entries[last];
            fromWorld = e.Go != null ? e.Go.transform.position : transform.position;
            if (e.Go != null) Destroy(e.Go);
            usedSlots.Remove(e.SlotIndex);
            entries.RemoveAt(last);
            return true;
        }

        /// <summary>
        /// Wyjmij topową butelkę i odpal animację do targetParent (np. klient).
        /// Po dotarciu visual sam się niszczy. Zwraca false gdy storage pusty.
        /// </summary>
        public bool TryTakeAnyFlyTo(Transform targetParent, Vector3 targetLocalOffset, float duration, float arcHeight)
        {
            if (entries.Count == 0 || targetParent == null) return false;
            int last = entries.Count - 1;
            var e = entries[last];
            usedSlots.Remove(e.SlotIndex);
            entries.RemoveAt(last);

            if (e.Go == null) return true;
            Vector3 fromWorld = e.Go.transform.position;
            var fly = e.Go.GetComponent<FlyingBottle>();
            if (fly == null) fly = e.Go.AddComponent<FlyingBottle>();
            fly.Begin(targetParent, fromWorld, targetLocalOffset, duration, arcHeight);
            Destroy(e.Go, duration + 0.05f);
            return true;
        }

        /// <summary>Dodaj butelkę do storage z animacją z punktu źródłowego (np. gracz). Zwraca false gdy full.</summary>
        public bool AddBottleAnimated(FruitType type, Vector3 sourceWorld)
        {
            if (bottlePrefab == null || stackOrigin == null) return false;
            if (IsFull) return false;

            int slot = FindFreeSlot();
            if (capacity > 0 && slot >= capacity) return false;

            usedSlots.Add(slot);

            var go = Instantiate(bottlePrefab, stackOrigin);
            go.SetActive(true);
            go.name = $"StoredBottle_{slot}_{type}";
            TintRenderer(go, type);

            Vector3 endLocal = SlotLocalPosition(slot);
            var fly = go.AddComponent<FlyingBottle>();
            fly.Begin(stackOrigin, sourceWorld, endLocal, flyDuration, flyArcHeight);

            entries.Add(new Entry { Type = type, Go = go, SlotIndex = slot });
            return true;
        }

        /// <summary>
        /// Wyjmij pierwszą butelkę pasującego typu — usuń ją ze storage.
        /// Zwraca world position skąd "leciała" (do animacji do klienta) lub false gdy brak.
        /// Visual GameObject jest niszczony — caller spawnuje nową animację (FlyingBottle) sam.
        /// </summary>
        public bool TryTakeBottleOfType(FruitType type, out Vector3 fromWorld)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].Type != type) continue;
                var e = entries[i];
                fromWorld = e.Go != null ? e.Go.transform.position : transform.position;
                if (e.Go != null) Destroy(e.Go);
                usedSlots.Remove(e.SlotIndex);
                entries.RemoveAt(i);
                return true;
            }
            fromWorld = transform.position;
            return false;
        }

        int FindFreeSlot()
        {
            int i = 0;
            while (usedSlots.Contains(i)) i++;
            return i;
        }

        Vector3 SlotLocalPosition(int idx)
        {
            int perRow = Mathf.Max(1, columns);
            int perLayer = perRow * Mathf.Max(1, rows);
            int y = idx / perLayer;
            int rem = idx % perLayer;
            int z = rem / perRow;
            int x = rem % perRow;
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
