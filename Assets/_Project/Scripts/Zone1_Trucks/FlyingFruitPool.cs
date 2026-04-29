using System.Collections.Generic;
using UnityEngine;
using Project.Core;
using Project.Zone1.FruitWall;

namespace Project.Zone1.Trucks
{
    /// <summary>
    /// Pool latających owoców. Pre-aluje N obiektów z SpriteRenderer + FlyingFruitView.
    /// Gdy potrzebny — Fly() z pula bierze wolny obiekt i odpala animację. Po zakończeniu
    /// obiekt wraca do puli (deactivate). Bez Instantiate w runtime.
    /// </summary>
    public class FlyingFruitPool : MonoBehaviour
    {
        [SerializeField] int poolSize = 50;
        [Tooltip("Czas trwania animacji od ściany do ciężarówki (sec).")]
        [SerializeField] float flyDurationSec = 0.3f;
        [Tooltip("Wysokość łuku (peak parabolic, world units).")]
        [SerializeField] float arcHeightWorld = 0.4f;
        [Tooltip("Mnożnik rozmiaru lecącego owocu względem rozmiaru komórki gridu.")]
        [SerializeField] float sizeMultiplier = 4f;
        [SerializeField] string sortingLayerName = "Default";
        [SerializeField] int sortingOrder = 100;

        readonly Queue<FlyingFruitView> available = new();
        Sprite cellSprite;

        void Awake()
        {
            cellSprite = CreateSquareSprite();
            for (int i = 0; i < poolSize; i++)
            {
                var go = new GameObject($"FlyingFruit_{i}");
                go.transform.SetParent(transform, worldPositionStays: false);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = cellSprite;
                sr.sortingLayerName = sortingLayerName;
                sr.sortingOrder = sortingOrder;
                var view = go.AddComponent<FlyingFruitView>();
                view.OnFlightDone = ReturnToPool;
                go.SetActive(false);
                available.Enqueue(view);
            }
        }

        public void Fly(Transform target, Vector3 fromWorld, Quaternion worldRot, FruitType type, Vector2 sizeWorld)
        {
            if (available.Count == 0 || target == null) return;
            var view = available.Dequeue();
            view.gameObject.SetActive(true);

            view.Begin(target, fromWorld, worldRot, arcHeightWorld, flyDurationSec);
            Vector3 lossy = target.lossyScale;
            float sx = lossy.x != 0f ? sizeWorld.x * sizeMultiplier / lossy.x : sizeWorld.x * sizeMultiplier;
            float sy = lossy.y != 0f ? sizeWorld.y * sizeMultiplier / lossy.y : sizeWorld.y * sizeMultiplier;
            view.transform.localScale = new Vector3(sx, sy, 1f);
            view.SpriteRenderer.color = FruitColorPalette.GetColor(type);
        }

        void ReturnToPool(FlyingFruitView view)
        {
            view.transform.SetParent(transform, worldPositionStays: false);
            view.gameObject.SetActive(false);
            available.Enqueue(view);
        }

        static Sprite CreateSquareSprite()
        {
            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, mipChain: false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), pixelsPerUnit: 1f);
        }
    }
}
