using UnityEngine;

namespace Project.Input
{
    public class JoystickArea
    {
        public float MaxRadius { get; }
        public bool IsActive { get; private set; }
        public Vector2 Center { get; private set; }
        public Vector2 Output { get; private set; }

        public JoystickArea(float maxRadius)
        {
            MaxRadius = maxRadius;
            Reset();
        }

        public void OnPress(Vector2 screenPosition)
        {
            IsActive = true;
            Center = screenPosition;
            Output = Vector2.zero;
        }

        public void OnDrag(Vector2 screenPosition)
        {
            if (!IsActive) return;

            Vector2 delta = screenPosition - Center;
            if (delta.magnitude > MaxRadius)
                Output = delta.normalized;
            else
                Output = delta / MaxRadius;
        }

        public void OnRelease()
        {
            Reset();
        }

        void Reset()
        {
            IsActive = false;
            Center = Vector2.zero;
            Output = Vector2.zero;
        }
    }
}
