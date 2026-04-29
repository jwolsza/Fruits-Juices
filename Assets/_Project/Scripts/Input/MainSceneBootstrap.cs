using UnityEngine;
using UnityEngine.InputSystem;

namespace Project.Input
{
    public class MainSceneBootstrap : MonoBehaviour
    {
        [Header("Camera")]
        [SerializeField] Camera mainCamera;
        [SerializeField] float minX = 0f;
        [SerializeField] float maxX = 24f;
        [SerializeField] float pixelsToWorld = 0.01f;
        [SerializeField] float rubberStrength = 0.5f;
        [SerializeField] float snapBackSpeed = 10f;

        [Header("Joystick")]
        [SerializeField] float joystickMaxRadiusPx = 100f;
        [SerializeField] JoystickVisual joystickVisual;

        [Header("Input thresholds")]
        [SerializeField] float tapDistancePx = 10f;
        [SerializeField] float tapTimeSec = 0.2f;

        JoystickArea joystick;
        CameraScrollController scroll;
        InputRouter router;

        bool pointerDown;

        void Awake()
        {
            joystick = new JoystickArea(joystickMaxRadiusPx);
            float startX = mainCamera != null ? mainCamera.transform.position.x : 12f;
            scroll = new CameraScrollController(
                minX, maxX, pixelsToWorld, rubberStrength, snapBackSpeed, startX);

            router = new InputRouter(
                screenSize: new Vector2(Screen.width, Screen.height),
                tapDistanceThresholdPx: tapDistancePx,
                tapTimeThresholdSec: tapTimeSec,
                onJoystickPress: p => joystick.OnPress(p),
                onJoystickDrag: p => joystick.OnDrag(p),
                onJoystickRelease: () => joystick.OnRelease(),
                onScrollDragDelta: d => scroll.OnDragDelta(d),
                onScrollRelease: () => scroll.OnRelease(),
                onTap: HandleTap);
        }

        void HandleTap(Vector2 screenPos)
        {
            Debug.Log($"[Tap] {screenPos}");
        }

        void Update()
        {
            HandlePointerInput();
            scroll.Update(Time.deltaTime);
            ApplyCameraPosition();
            UpdateJoystickVisual();
        }

        void HandlePointerInput()
        {
            Vector2? pointerPos = null;
            bool pressedThisFrame = false;
            bool releasedThisFrame = false;

            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
            {
                pointerPos = Touchscreen.current.primaryTouch.position.ReadValue();
                pressedThisFrame = Touchscreen.current.primaryTouch.press.wasPressedThisFrame;
            }
            else if (Mouse.current != null && Mouse.current.leftButton.isPressed)
            {
                pointerPos = Mouse.current.position.ReadValue();
                pressedThisFrame = Mouse.current.leftButton.wasPressedThisFrame;
            }

            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasReleasedThisFrame)
                releasedThisFrame = true;
            else if (Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame)
                releasedThisFrame = true;

            float now = Time.time;

            if (pressedThisFrame && pointerPos.HasValue)
            {
                pointerDown = true;
                router.OnPointerDown(pointerPos.Value, now);
            }
            else if (pointerDown && releasedThisFrame)
            {
                Vector2 lastPos = pointerPos ?? GetLastKnownPointerPos();
                router.OnPointerUp(lastPos, now);
                pointerDown = false;
            }
            else if (pointerDown && pointerPos.HasValue)
            {
                router.OnPointerMove(pointerPos.Value, now);
            }
        }

        Vector2 GetLastKnownPointerPos()
        {
            if (Touchscreen.current != null)
                return Touchscreen.current.primaryTouch.position.ReadValue();
            if (Mouse.current != null)
                return Mouse.current.position.ReadValue();
            return Vector2.zero;
        }

        void ApplyCameraPosition()
        {
            if (mainCamera == null) return;
            var pos = mainCamera.transform.position;
            pos.x = scroll.TargetX;
            mainCamera.transform.position = pos;
        }

        void UpdateJoystickVisual()
        {
            if (joystickVisual == null) return;

            if (joystick.IsActive)
            {
                Vector2 center = joystick.Center;
                Vector2 thumb = center + joystick.Output * joystickMaxRadiusPx;
                joystickVisual.Show(center, thumb);
            }
            else
            {
                joystickVisual.Hide();
            }
        }
    }
}
