using UnityEngine;

namespace Project.Zone3.Service
{
    /// <summary>
    /// Animuje localPosition od start (world → local przez parent) do endLocal po paraboli.
    /// Po dotarciu — sam się usuwa (komponent), GameObject zostaje jako stałe miejsce w stosie.
    /// Parented do stackOrigin gracza, więc podąża za graczem podczas lotu.
    /// </summary>
    public class FlyingBottle : MonoBehaviour
    {
        Vector3 startLocal;
        Vector3 endLocal;
        float duration;
        float arcHeight;
        float t;

        public void Begin(Transform parent, Vector3 startWorld, Vector3 endLocal, float duration, float arcHeight)
        {
            if (parent == null) { Destroy(this); return; }
            transform.SetParent(parent, worldPositionStays: false);
            startLocal = parent.InverseTransformPoint(startWorld);
            this.endLocal = endLocal;
            this.duration = Mathf.Max(0.001f, duration);
            this.arcHeight = arcHeight;
            t = 0f;
            transform.localPosition = startLocal;
        }

        void Update()
        {
            t += Time.deltaTime / duration;
            if (t >= 1f)
            {
                transform.localPosition = endLocal;
                Destroy(this);
                return;
            }
            Vector3 pos = Vector3.Lerp(startLocal, endLocal, t);
            pos.y += arcHeight * 4f * t * (1f - t);
            transform.localPosition = pos;
        }
    }
}
