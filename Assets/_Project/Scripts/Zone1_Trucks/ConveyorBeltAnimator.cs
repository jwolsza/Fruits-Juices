using System.Collections.Generic;
using UnityEngine;

namespace Project.Zone1.Trucks
{
    /// <summary>
    /// Animuje texture offset (Y) materiału pasa conveyora — daje iluzję ruchu pasa.
    /// Aktywne tylko gdy któraś ciężarówka zbiera owoce (IsStopped na slocie).
    /// </summary>
    public class ConveyorBeltAnimator : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] Zone1TrucksManager trucksManager;
        [Tooltip("Renderery pasa. Każdy dostaje instanced material.")]
        [SerializeField] List<Renderer> beltRenderers = new();

        [Header("Texture")]
        [Tooltip("Nazwa tekstury w shaderze. URP Lit/SimpleLit: _BaseMap, Built-in: _MainTex.")]
        [SerializeField] string texturePropertyName = "_BaseMap";

        [Header("Scroll")]
        [Tooltip("Tempo przewijania Y (jednostek UV na sekundę).")]
        [SerializeField] float scrollSpeed = 0.5f;
        [Tooltip("Smooth fade-in/out gdy ciężarówka wjeżdża/wyjeżdża (sek). 0 = natychmiast.")]
        [SerializeField] float speedRampSeconds = 0.15f;

        readonly List<Material> instancedMaterials = new();
        Vector2 currentOffset;
        float currentSpeedFactor;

        void Awake()
        {
            foreach (var r in beltRenderers)
            {
                if (r == null) { instancedMaterials.Add(null); continue; }
                instancedMaterials.Add(r.material);
            }
            if (instancedMaterials.Count > 0 && instancedMaterials[0] != null)
                currentOffset = instancedMaterials[0].GetTextureOffset(texturePropertyName);
        }

        void Update()
        {
            if (trucksManager == null || instancedMaterials.Count == 0) return;

            float targetFactor = trucksManager.IsAnyTruckCollecting() ? 1f : 0f;
            if (speedRampSeconds <= 0f)
                currentSpeedFactor = targetFactor;
            else
                currentSpeedFactor = Mathf.MoveTowards(currentSpeedFactor, targetFactor, Time.deltaTime / speedRampSeconds);

            if (currentSpeedFactor <= 0f) return;

            currentOffset.y = Mathf.Repeat(currentOffset.y + scrollSpeed * currentSpeedFactor * Time.deltaTime, 1f);

            foreach (var mat in instancedMaterials)
                if (mat != null) mat.SetTextureOffset(texturePropertyName, currentOffset);
        }
    }
}
