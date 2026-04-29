using System;
using UnityEngine;

namespace Project.Zone1.Trucks
{
    /// <summary>
    /// Pojedynczy "flying fruit" sprite. Animuje się od pozycji startowej do końcowej
    /// po krzywej (Bezier z peak'iem ponad linią), potem wywołuje OnFlightDone i jest
    /// returned to pool przez managera.
    /// </summary>
    public class FlyingFruitView : MonoBehaviour
    {
        public SpriteRenderer SpriteRenderer { get; private set; }
        public Action<FlyingFruitView> OnFlightDone;

        Vector3 from;
        Vector3 to;
        float arcHeight;
        float duration;
        float elapsed;
        bool flying;

        void Awake()
        {
            SpriteRenderer = GetComponent<SpriteRenderer>();
        }

        public void Begin(Vector3 fromWorld, Vector3 toWorld, float arcHeightWorld, float durationSec)
        {
            from = fromWorld;
            to = toWorld;
            arcHeight = arcHeightWorld;
            duration = Mathf.Max(0.01f, durationSec);
            elapsed = 0f;
            flying = true;
            transform.position = from;
        }

        void Update()
        {
            if (!flying) return;
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            Vector3 linear = Vector3.Lerp(from, to, t);
            // Parabolic arc on Y: peak at t=0.5.
            float arc = Mathf.Sin(t * Mathf.PI) * arcHeight;
            transform.position = new Vector3(linear.x, linear.y + arc, linear.z);

            if (t >= 1f)
            {
                flying = false;
                OnFlightDone?.Invoke(this);
            }
        }
    }
}
