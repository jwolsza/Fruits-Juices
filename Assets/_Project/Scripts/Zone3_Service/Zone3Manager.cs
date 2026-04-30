using System.Collections.Generic;
using UnityEngine;
using Project.Core;
using Project.Data;
using Project.Zone2.Bottling;

namespace Project.Zone3.Service
{
    /// <summary>
    /// Strefa 3: ruch + pickup z racków + drop rack przy kolejce + customer service + coiny.
    /// </summary>
    public class Zone3Manager : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] GameBalanceSO balance;
        [SerializeField] PlayerController playerController;
        [SerializeField] PlayerCarryView playerCarryView;
        [SerializeField] Zone2Manager zone2Manager;

        [Header("Customers")]
        [SerializeField] CustomerView customerPrefab;
        [SerializeField] Transform queueAnchor;
        [SerializeField] Vector3 queueStepOffset = new(0f, 0f, -1f);
        [SerializeField] int orderMinAmount = 1;
        [SerializeField] int orderMaxAmount = 5;

        [Header("Drop rack (storage przy kolejce)")]
        [SerializeField] BottleStorage dropRack;

        [Header("Coin rack")]
        [SerializeField] CoinStash coinStash;
        [Tooltip("Opcjonalnie — animator UI lecącej monety. Gdy ustawiony, każdy pickup spawnuje sprite na canvas i lerpuje do counter UI.")]
        [SerializeField] CoinUIFlyAnimator coinUIFlyAnimator;

        [Header("Deliver-to-customer animation")]
        [Tooltip("Lokalny offset względem CustomerView gdzie ląduje lecąca butelka (zwykle nad/przy ich rękach).")]
        [SerializeField] Vector3 deliverTargetLocalOffset = new(0f, 1.2f, 0f);
        [SerializeField] float deliverFlyDuration = 0.4f;
        [SerializeField] float deliverFlyArcHeight = 0.6f;
        [Tooltip("Min odstęp między kolejnymi butelkami lecącymi do klienta (sek). Dzięki temu zamówienia >1 są wyraźnie widoczne.")]
        [SerializeField] float deliverIntervalSeconds = 0.25f;
        [Tooltip("Po spełnieniu zamówienia klient czeka tyle sekund (animacja ostatniej butelki + buffer) przed odejściem.")]
        [SerializeField] float customerLeaveDelay = 0.7f;

        float pickupAccumulator;
        float dropAccumulator;
        float coinPickupAccumulator;
        float customerSpawnAccumulator;
        float lastCustomerDeliverTime;
        int nextCustomerId = 1;

        readonly List<Customer> queue = new();
        readonly Dictionary<int, CustomerView> customerViews = new();
        readonly Dictionary<int, float> satisfiedTimers = new();
        readonly List<PendingCoinPayout> pendingCoinPayouts = new();

        class PendingCoinPayout { public int CustomerId; public float SpawnAtTime; public int CoinsRemaining; }

        public IReadOnlyList<Customer> Queue => queue;

        void Update()
        {
            if (balance == null || playerController == null || zone2Manager == null) return;
            float dt = Time.deltaTime;

            TickRate(ref pickupAccumulator, balance.PickupRateHz, dt, TryPickupOne);
            TickRate(ref dropAccumulator, balance.DeliverRateHz, dt, TryDropOneToRack);
            TryDeliverOneToFirstCustomer();
            TickRate(ref coinPickupAccumulator, balance.PickupRateHz, dt, TryPlayerPickupCoin);

            float spawnInterval = 1f / Mathf.Max(0.01f, balance.CustomerSpawnRateHz);
            customerSpawnAccumulator += dt;
            while (customerSpawnAccumulator >= spawnInterval)
            {
                customerSpawnAccumulator -= spawnInterval;
                TrySpawnCustomer();
            }

            TickCustomerLeaveTimer(dt);
            UpdateCustomerPositions();
        }

        static void TickRate(ref float accumulator, float rateHz, float dt, System.Action action)
        {
            float interval = 1f / Mathf.Max(0.01f, rateHz);
            accumulator += dt;
            while (accumulator >= interval)
            {
                accumulator -= interval;
                action();
            }
        }

        // -------- Pickup z Zone2 racków --------

        void TryPickupOne()
        {
            if (playerController.Inventory.IsFull) return;
            float radius = balance.PickupRadius;
            Vector3 playerPos = playerController.WorldPosition;

            SmallBottleRack best = null;
            float bestDistSqr = radius * radius;
            foreach (var rack in zone2Manager.Racks)
            {
                if (rack.IsEmpty || !rack.CurrentType.HasValue) continue;
                Vector3 rackPos = zone2Manager.GetRackWorldPosition(rack);
                float dSqr = (rackPos - playerPos).sqrMagnitude;
                if (dSqr <= bestDistSqr) { bestDistSqr = dSqr; best = rack; }
            }
            if (best == null) return;

            var type = best.CurrentType.Value;
            Vector3 from = zone2Manager.GetRackWorldPosition(best);
            int taken = best.RemoveOne();
            if (taken <= 0) return;
            playerController.Inventory.Add(type, taken);
            if (playerCarryView != null) playerCarryView.BeginPickupAnimation(type, from);
        }

        // -------- Drop do dropRack --------

        void TryDropOneToRack()
        {
            if (dropRack == null) return;
            if (playerController.Inventory.TotalCount <= 0) return;
            if (dropRack.IsFull) return;
            if (Vector3.Distance(playerController.WorldPosition, dropRack.WorldPosition) > balance.DeliverRadius) return;

            // Pick any type that gracz ma — bierzemy pierwszy z Counts.
            FruitType? typeToDrop = null;
            foreach (var kv in playerController.Inventory.Counts)
            {
                if (kv.Value > 0) { typeToDrop = kv.Key; break; }
            }
            if (!typeToDrop.HasValue) return;

            Vector3 from = playerCarryView != null
                ? playerCarryView.transform.position
                : playerController.WorldPosition;

            if (!dropRack.AddBottleAnimated(typeToDrop.Value, from)) return;
            playerController.Inventory.Remove(typeToDrop.Value, 1);
            if (playerCarryView != null) playerCarryView.RemoveTop();
        }

        // -------- Customer service --------

        void TryDeliverOneToFirstCustomer()
        {
            if (dropRack == null || queue.Count == 0) return;
            var first = queue[0];
            if (first.IsSatisfied) return;
            if (dropRack.IsEmpty) return;
            if (!customerViews.TryGetValue(first.Id, out var view) || view == null) return;
            if (Time.time - lastCustomerDeliverTime < deliverIntervalSeconds) return;

            if (!dropRack.TryTakeAnyFlyTo(view.transform, deliverTargetLocalOffset, deliverFlyDuration, deliverFlyArcHeight)) return;
            first.Receive(1);
            lastCustomerDeliverTime = Time.time;

            // Coin payout per butelkę — odpalany po dotarciu butelki do klienta.
            int coinsForThisBottle = Mathf.Max(0, balance.CoinsPerCustomerBase);
            if (coinsForThisBottle > 0)
            {
                pendingCoinPayouts.Add(new PendingCoinPayout
                {
                    CustomerId = first.Id,
                    SpawnAtTime = Time.time + deliverFlyDuration,
                    CoinsRemaining = coinsForThisBottle,
                });
            }

            if (first.IsSatisfied && !satisfiedTimers.ContainsKey(first.Id))
                satisfiedTimers[first.Id] = 0f;
        }

        void TickCustomerLeaveTimer(float dt)
        {
            ProcessPendingCoinPayouts();

            if (queue.Count == 0) return;
            var first = queue[0];
            if (!satisfiedTimers.TryGetValue(first.Id, out float elapsed)) return;
            elapsed += dt;

            // Despawn dopiero gdy wszystkie pending coiny dla tego klienta wypłacone + leaveDelay buffer.
            if (elapsed >= deliverFlyDuration + customerLeaveDelay && !HasPendingCoinsFor(first.Id))
            {
                satisfiedTimers.Remove(first.Id);
                DespawnFirstCustomer();
            }
            else
            {
                satisfiedTimers[first.Id] = elapsed;
            }
        }

        void ProcessPendingCoinPayouts()
        {
            if (pendingCoinPayouts.Count == 0) return;
            float now = Time.time;
            for (int i = pendingCoinPayouts.Count - 1; i >= 0; i--)
            {
                var p = pendingCoinPayouts[i];
                if (now < p.SpawnAtTime) continue;
                if (coinStash != null)
                {
                    Vector3 from = customerViews.TryGetValue(p.CustomerId, out var v) && v != null
                        ? v.transform.position
                        : transform.position;
                    for (int k = 0; k < p.CoinsRemaining; k++)
                        coinStash.AddCoinAnimated(from);
                }
                pendingCoinPayouts.RemoveAt(i);
            }
        }

        bool HasPendingCoinsFor(int customerId)
        {
            for (int i = 0; i < pendingCoinPayouts.Count; i++)
                if (pendingCoinPayouts[i].CustomerId == customerId) return true;
            return false;
        }

        void DespawnFirstCustomer()
        {
            if (queue.Count == 0) return;
            var first = queue[0];
            if (customerViews.TryGetValue(first.Id, out var view) && view != null)
                Destroy(view.gameObject);
            customerViews.Remove(first.Id);
            queue.RemoveAt(0);
        }


        // -------- Player podejmowanie coinów --------

        void TryPlayerPickupCoin()
        {
            if (coinStash == null || coinStash.IsEmpty) return;
            if (Vector3.Distance(playerController.WorldPosition, coinStash.WorldPosition) > balance.PickupRadius) return;
            if (!coinStash.TryTakeOne(out Vector3 fromWorld)) return;

            if (coinUIFlyAnimator != null)
                coinUIFlyAnimator.PlayCoinFly(fromWorld, () => playerController.AddCoins(1));
            else
                playerController.AddCoins(1);
        }

        // -------- Spawn / queue update --------

        void TrySpawnCustomer()
        {
            if (customerPrefab == null || queueAnchor == null) return;
            if (queue.Count >= balance.CustomerQueueLength) return;

            int amount = Random.Range(orderMinAmount, orderMaxAmount + 1);
            var customer = new Customer(nextCustomerId++, amount);
            queue.Add(customer);

            int idx = queue.Count - 1;
            Vector3 spawnPos = queueAnchor.position + idx * queueStepOffset;
            Quaternion spawnRot = ComputeFacingRotation();
            var view = Instantiate(customerPrefab, spawnPos, spawnRot, transform);
            view.gameObject.SetActive(true);
            view.name = $"Customer_{customer.Id}";
            view.Bind(customer);
            customerViews[customer.Id] = view;
        }

        void UpdateCustomerPositions()
        {
            if (queueAnchor == null) return;
            Quaternion facing = ComputeFacingRotation();
            for (int i = 0; i < queue.Count; i++)
            {
                if (!customerViews.TryGetValue(queue[i].Id, out var view) || view == null) continue;
                Vector3 target = queueAnchor.position + i * queueStepOffset;
                view.transform.position = Vector3.MoveTowards(view.transform.position, target, 6f * Time.deltaTime);
                view.transform.rotation = facing;
            }
        }

        Quaternion ComputeFacingRotation()
        {
            Vector3 forward = -queueStepOffset;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f) return queueAnchor != null ? queueAnchor.rotation : Quaternion.identity;
            return Quaternion.LookRotation(forward.normalized, Vector3.up);
        }
    }
}
