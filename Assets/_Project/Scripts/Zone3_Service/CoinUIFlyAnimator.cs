using UnityEngine;
using UnityEngine.UI;

namespace Project.Zone3.Service
{
    /// <summary>
    /// Spawnuje UI sprite'y monet, które startują na pozycji ekranowej odpowiadającej world pos
    /// rzeczywistej (3D) monety i lecą do (0,0,0) w spawnParent. Po dotarciu woła onArrived callback —
    /// to tam gracz dostaje +1 do walleta (counter rośnie wtedy gdy monetka "wpada do worka").
    /// </summary>
    public class CoinUIFlyAnimator : MonoBehaviour
    {
        [Header("Refs")]
        [Tooltip("Parent dla spawnowanych UI monet (RectTransform). 0,0 w jego local space = miejsce docelowe (counter).")]
        [SerializeField] RectTransform spawnParent;
        [Tooltip("Template Image (z spritem monety) — będzie klonowany. Sam template dezaktywowany w Awake.")]
        [SerializeField] Image coinTemplate;
        [Tooltip("Camera z której world pos jest projektowana na ekran. Domyślnie Camera.main.")]
        [SerializeField] Camera worldCamera;
        [Tooltip("Canvas (rodzic spawnParent). Domyślnie GetComponentInParent.")]
        [SerializeField] Canvas canvas;

        [Header("Fly")]
        [SerializeField] float flyDurationMin = 0.5f;
        [SerializeField] float flyDurationMax = 0.8f;
        [Tooltip("Wysokość łuku (w pikselach UI). Jeśli arcProportional włączone — to bazowa wartość (mnożnik).")]
        [SerializeField] float arcAmplitudeMin = 80f;
        [SerializeField] float arcAmplitudeMax = 160f;
        [Tooltip("Gdy true, arcAmplitude × (dystans/200) — daleki lot ma proporcjonalnie wyższy łuk.")]
        [SerializeField] bool arcProportionalToDistance = true;
        [Tooltip("Boczne odchylenie startu (jitter pikselowy) — dodaje randomowości gdy paczka monet leci jednocześnie.")]
        [SerializeField] float lateralJitterPx = 30f;
        [SerializeField] AnimationCurve easing = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Scale (world → UI illusion)")]
        [Tooltip("Skala UI sprite'a na starcie (większa = sugeruje że jest 'bliżej kamery'/większa).")]
        [SerializeField] float startScale = 1.6f;
        [SerializeField] float endScale = 1f;

        void Awake()
        {
            if (worldCamera == null) worldCamera = Camera.main;
            if (canvas == null) canvas = GetComponentInParent<Canvas>();
            if (coinTemplate != null) coinTemplate.gameObject.SetActive(false);
        }

        public void PlayCoinFly(Vector3 fromWorld, System.Action onArrived)
        {
            if (spawnParent == null || coinTemplate == null || worldCamera == null || canvas == null)
            {
                onArrived?.Invoke();
                return;
            }

            Vector2 screenPos = worldCamera.WorldToScreenPoint(fromWorld);
            // Random lateral jitter + small upward bias.
            screenPos += new Vector2(Random.Range(-lateralJitterPx, lateralJitterPx), Random.Range(-lateralJitterPx * 0.3f, lateralJitterPx * 0.3f));

            Camera uiCam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(spawnParent, screenPos, uiCam, out Vector2 startLocal))
            {
                onArrived?.Invoke();
                return;
            }

            var coinGo = Instantiate(coinTemplate.gameObject, spawnParent);
            coinGo.SetActive(true);
            coinGo.name = "FlyingUICoin";
            var rt = coinGo.transform as RectTransform;
            if (rt == null)
            {
                Destroy(coinGo);
                onArrived?.Invoke();
                return;
            }
            rt.anchoredPosition = startLocal;

            float distance = startLocal.magnitude;
            float baseArc = Random.Range(arcAmplitudeMin, arcAmplitudeMax);
            float effectiveArc = arcProportionalToDistance ? baseArc * (distance / 200f) : baseArc;

            var fly = coinGo.AddComponent<FlyingUICoin>();
            fly.Configure(
                startLocal,
                Vector2.zero,
                Random.Range(flyDurationMin, flyDurationMax),
                effectiveArc,
                easing,
                startScale,
                endScale,
                onArrived);
        }
    }

    class FlyingUICoin : MonoBehaviour
    {
        RectTransform rt;
        Vector2 startLocal, endLocal;
        float duration, arcAmplitude, t;
        float startScale, endScale;
        AnimationCurve curve;
        System.Action onArrived;

        public void Configure(Vector2 startLocal, Vector2 endLocal, float duration, float arcAmplitude, AnimationCurve curve, float startScale, float endScale, System.Action onArrived)
        {
            rt = transform as RectTransform;
            this.startLocal = startLocal;
            this.endLocal = endLocal;
            this.duration = Mathf.Max(0.001f, duration);
            this.arcAmplitude = arcAmplitude;
            this.curve = curve;
            this.startScale = startScale;
            this.endScale = endScale;
            this.onArrived = onArrived;
            t = 0f;
            if (rt != null)
            {
                rt.anchoredPosition = startLocal;
                rt.localScale = Vector3.one * startScale;
            }
        }

        void Update()
        {
            t += Time.deltaTime / duration;
            if (t >= 1f)
            {
                if (rt != null)
                {
                    rt.anchoredPosition = endLocal;
                    rt.localScale = Vector3.one * endScale;
                }
                onArrived?.Invoke();
                Destroy(gameObject);
                return;
            }
            float k = curve != null ? curve.Evaluate(t) : t;
            Vector2 p = Vector2.Lerp(startLocal, endLocal, k);
            p.y += Mathf.Sin(t * Mathf.PI) * arcAmplitude;
            if (rt != null)
            {
                rt.anchoredPosition = p;
                float s = Mathf.Lerp(startScale, endScale, k);
                rt.localScale = Vector3.one * s;
            }
        }
    }
}
