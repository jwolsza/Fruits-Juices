using System;
using UnityEngine;

namespace Project.Zone1.Trucks
{
    /// <summary>
    /// Pojedynczy "flying fruit" sprite. Animuje się od pozycji startowej (world) do
    /// localPosition (0,0,0) w przestrzeni TARGETU (zwykle ciężarówki) — dzięki temu
    /// gdy target się porusza, owoc nadal go trafia. Parabolic arc na local Y.
    /// </summary>
    public class FlyingFruitView : MonoBehaviour
    {
        public SpriteRenderer SpriteRenderer { get; private set; }
        public Action<FlyingFruitView> OnFlightDone;

        Vector3 startLocal;
        Quaternion worldRotation;
        float arcHeight;
        float duration;
        float elapsed;
        bool flying;

        void Awake()
        {
            SpriteRenderer = GetComponent<SpriteRenderer>();
        }

        public void Begin(Transform target, Vector3 fromWorld, Quaternion worldRot, float arcHeightWorld, float durationSec)
        {
            transform.SetParent(target, worldPositionStays: false);
            startLocal = target.InverseTransformPoint(fromWorld);
            transform.localPosition = startLocal;
            worldRotation = worldRot;
            transform.rotation = worldRotation;
            arcHeight = arcHeightWorld;
            duration = Mathf.Max(0.01f, durationSec);
            elapsed = 0f;
            flying = true;
        }

        void Update()
        {
            if (!flying) return;
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            Vector3 linear = Vector3.Lerp(startLocal, Vector3.zero, t);
            float arc = Mathf.Sin(t * Mathf.PI) * arcHeight;
            transform.localPosition = new Vector3(linear.x, linear.y + arc, linear.z);
            transform.rotation = worldRotation;

            if (t >= 1f)
            {
                flying = false;
                OnFlightDone?.Invoke(this);
            }
        }
    }
}
