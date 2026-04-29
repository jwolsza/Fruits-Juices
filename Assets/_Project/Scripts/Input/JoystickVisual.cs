using UnityEngine;
using UnityEngine.UI;

namespace Project.Input
{
    public class JoystickVisual : MonoBehaviour
    {
        [SerializeField] RectTransform centerHandle;
        [SerializeField] RectTransform thumbHandle;
        [SerializeField] CanvasGroup canvasGroup;

        public void Show(Vector2 screenCenter, Vector2 screenThumb)
        {
            canvasGroup.alpha = 1f;
            centerHandle.position = screenCenter;
            thumbHandle.position = screenThumb;
        }

        public void Hide()
        {
            canvasGroup.alpha = 0f;
        }
    }
}
