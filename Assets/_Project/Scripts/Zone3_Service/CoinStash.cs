using System.Collections.Generic;
using UnityEngine;

namespace Project.Zone3.Service
{
    /// <summary>
    /// Stos coinów. Klient rzuca coiny (animacja od klienta), gracz potem podchodzi i pobiera.
    /// Wizualnie te same zasady co BottleStorage (cuboid layout).
    /// </summary>
    public class CoinStash : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] GameObject coinPrefab;
        [SerializeField] Transform stackOrigin;

        [Header("Stack layout")]
        [SerializeField] Vector3 stepX = new(0.1f, 0f, 0f);
        [SerializeField] Vector3 stepY = new(0f, 0.05f, 0f);
        [SerializeField] Vector3 stepZ = new(0f, 0f, 0.1f);
        [SerializeField] int columns = 3;
        [SerializeField] int rows = 4;

        [Header("Fly animation (incoming)")]
        [SerializeField] float flyDuration = 0.4f;
        [SerializeField] float flyArcHeight = 0.5f;

        readonly List<GameObject> coins = new();

        public Vector3 WorldPosition => transform.position;
        public int Count => coins.Count;
        public bool IsEmpty => coins.Count == 0;

        public void AddCoinAnimated(Vector3 sourceWorld)
        {
            if (coinPrefab == null || stackOrigin == null) return;
            int slot = coins.Count;
            var go = Instantiate(coinPrefab, stackOrigin);
            go.SetActive(true);
            go.name = $"Coin_{slot}";
            Vector3 endLocal = SlotLocalPosition(slot);
            var fly = go.AddComponent<FlyingBottle>();
            fly.Begin(stackOrigin, sourceWorld, endLocal, flyDuration, flyArcHeight);
            coins.Add(go);
        }

        /// <summary>Wyjmij topowego coina — zwraca jego world position (do animacji do gracza).</summary>
        public bool TryTakeOne(out Vector3 fromWorld)
        {
            if (coins.Count == 0) { fromWorld = transform.position; return false; }
            int last = coins.Count - 1;
            var go = coins[last];
            fromWorld = go != null ? go.transform.position : transform.position;
            if (go != null) Destroy(go);
            coins.RemoveAt(last);
            return true;
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
    }
}
