using UnityEngine;
using TMPro;

namespace Project.Zone3.Service
{
    /// <summary>Wizualizacja klienta: ciało + label z ilością pozostałych butelek do dostarczenia.</summary>
    public class CustomerView : MonoBehaviour
    {
        [Tooltip("Opcjonalny TMP_Text — pokazuje ile butelek pozostało do zamówienia.")]
        [SerializeField] TMP_Text remainingLabel;

        Customer customer;

        public Customer Customer => customer;

        public void Bind(Customer c)
        {
            customer = c;
        }

        void LateUpdate()
        {
            if (customer == null || remainingLabel == null) return;
            remainingLabel.text = customer.Remaining.ToString();
        }
    }
}
