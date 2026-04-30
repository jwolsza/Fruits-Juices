using UnityEngine;

namespace Project.Zone3.Service
{
    /// <summary>POCO — klient w kolejce. Zamówienie to po prostu N butelek (typ nieistotny).</summary>
    public class Customer
    {
        public int Id { get; }
        public int OrderAmount { get; private set; }
        public int Delivered { get; private set; }
        public int Remaining => Mathf.Max(0, OrderAmount - Delivered);
        public bool IsSatisfied => Delivered >= OrderAmount;

        public Customer(int id, int amount)
        {
            Id = id;
            OrderAmount = Mathf.Max(1, amount);
        }

        public int Receive(int amount)
        {
            if (amount <= 0) return 0;
            int taken = Mathf.Min(amount, Remaining);
            Delivered += taken;
            return taken;
        }
    }
}
