using UnityEngine;
using TMPro;
using Project.Data;
using Project.Input;

namespace Project.Zone3.Service
{
    /// <summary>
    /// Sterowanie graczem joystickiem. Ruch w płaszczyźnie XZ (X = bok, Z = przód).
    /// PlayerInventory ekspresjonowane publicznie do podglądu (pickup/deliver dochodzi później).
    /// </summary>
    public class PlayerController : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] GameBalanceSO balance;
        [SerializeField] MainSceneBootstrap inputBootstrap;

        [Header("Movement")]
        [Tooltip("Tempo zwrotu ku wektorowi ruchu (im więcej, tym ostrzej).")]
        [SerializeField] float rotationSlerpSpeed = 12f;
        [Tooltip("Strefa martwa joysticka (output o magnitude poniżej tego = brak ruchu).")]
        [SerializeField] float deadZone = 0.05f;

        [Header("Bounds (optional)")]
        [Tooltip("Min X dla pozycji gracza (0 = brak limitu).")]
        [SerializeField] float boundsMinX = 0f;
        [SerializeField] float boundsMaxX = 0f;
        [SerializeField] float boundsMinZ = 0f;
        [SerializeField] float boundsMaxZ = 0f;

        [Header("Animation")]
        [Tooltip("Animator gracza. Steruje paramami runtime gdy się porusza.")]
        [SerializeField] Animator animator;
        [Tooltip("Bool param w Animator Controller — true gdy gracz się porusza. Pusty = nie ustawiaj.")]
        [SerializeField] string isMovingParamName = "IsMoving";
        [Tooltip("Float param — magnitude wektora ruchu (0..1). Do blendowania idle↔run. Pusty = nie ustawiaj.")]
        [SerializeField] string speedParamName = "Speed";

        [Header("UI")]
        [Tooltip("Opcjonalny TMP label nad graczem — wyświetla ilość niesionych butelek (np. \"3/10\").")]
        [SerializeField] TMP_Text countLabel;
        [Tooltip("Opcjonalny TMP label — wyświetla ilość coinów gracza.")]
        [SerializeField] TMP_Text coinsLabel;
        [Tooltip("Punch scale na liczniku gdy coiny rosną. 1 = bez efektu.")]
        [SerializeField] float coinsPunchScale = 1.25f;
        [SerializeField] float coinsPunchDuration = 0.18f;

        public PlayerInventory Inventory { get; private set; }
        public int Coins { get; private set; }
        public Vector3 WorldPosition => transform.position;

        int lastDisplayedCoins;
        float coinsPunchT;
        Vector3 coinsLabelBaseScale = Vector3.one;

        int isMovingHash;
        int speedHash;
        bool hasIsMovingParam;
        bool hasSpeedParam;

        public void AddCoins(int amount) { if (amount > 0) Coins += amount; }
        public bool TrySpendCoins(int amount)
        {
            if (amount <= 0 || Coins < amount) return false;
            Coins -= amount;
            return true;
        }

        void Awake()
        {
            int cap = balance != null ? balance.PlayerCapacity : 10;
            Inventory = new PlayerInventory(cap);
            if (coinsLabel != null) coinsLabelBaseScale = coinsLabel.transform.localScale;
            if (!string.IsNullOrEmpty(isMovingParamName)) { isMovingHash = Animator.StringToHash(isMovingParamName); hasIsMovingParam = true; }
            if (!string.IsNullOrEmpty(speedParamName)) { speedHash = Animator.StringToHash(speedParamName); hasSpeedParam = true; }
        }

        void Update()
        {
            UpdateCountLabel();

            float speedMag = 0f;
            bool moving = false;

            if (inputBootstrap != null && balance != null && inputBootstrap.JoystickActive)
            {
                Vector2 j = inputBootstrap.JoystickOutput;
                speedMag = Mathf.Min(1f, j.magnitude);
                if (speedMag >= deadZone)
                {
                    moving = true;
                    Vector3 dir = new(j.x, 0f, j.y);
                    float mag = dir.magnitude;
                    if (mag > 1f) dir /= mag;

                    Vector3 delta = dir * (balance.PlayerSpeed * Time.deltaTime);
                    Vector3 next = transform.position + delta;
                    next = ApplyBounds(next);
                    transform.position = next;

                    if (delta.sqrMagnitude > 0.0001f)
                    {
                        Quaternion target = Quaternion.LookRotation(delta.normalized, Vector3.up);
                        transform.rotation = Quaternion.Slerp(transform.rotation, target, rotationSlerpSpeed * Time.deltaTime);
                    }
                }
            }

            UpdateAnimator(moving, speedMag);
        }

        void UpdateAnimator(bool moving, float speedMag)
        {
            if (animator == null) return;
            if (hasIsMovingParam) animator.SetBool(isMovingHash, moving);
            if (hasSpeedParam) animator.SetFloat(speedHash, moving ? speedMag : 0f);
        }

        Vector3 ApplyBounds(Vector3 p)
        {
            if (boundsMaxX > boundsMinX) p.x = Mathf.Clamp(p.x, boundsMinX, boundsMaxX);
            if (boundsMaxZ > boundsMinZ) p.z = Mathf.Clamp(p.z, boundsMinZ, boundsMaxZ);
            return p;
        }

        void UpdateCountLabel()
        {
            if (countLabel != null && Inventory != null)
                countLabel.text = $"{Inventory.TotalCount}/{Inventory.Capacity}";

            if (coinsLabel == null) return;
            coinsLabel.text = Coins.ToString();

            if (Coins != lastDisplayedCoins)
            {
                lastDisplayedCoins = Coins;
                coinsPunchT = 1f;
            }
            if (coinsPunchT > 0f)
            {
                coinsPunchT = Mathf.Max(0f, coinsPunchT - Time.deltaTime / Mathf.Max(0.0001f, coinsPunchDuration));
                float bump = Mathf.Sin((1f - coinsPunchT) * Mathf.PI); // 0 → 1 → 0
                float k = 1f + (coinsPunchScale - 1f) * bump;
                coinsLabel.transform.localScale = coinsLabelBaseScale * k;
            }
        }
    }
}
