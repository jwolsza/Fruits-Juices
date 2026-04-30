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
        [Tooltip("Konwersja px → world. 0.01 = 100 px to 1 jednostka świata.")]
        [SerializeField] float pixelsToWorld = 0.01f;
        [SerializeField] float rubberStrength = 0.5f;
        [SerializeField] float snapBackSpeed = 10f;
        [Tooltip("Tarcie/drag dla inercji po puszczeniu palca (jednostek/s²).")]
        [SerializeField] float drag = 5f;

        [Header("Player Zone Detection (screen-based)")]
        [Tooltip("Tap na ekranie z X >= screenWidth × ta wartość (0..1) → joystick. Inaczej → scroll kamerą. 0.5 = prawa połowa = joystick.")]
        [SerializeField] float playerZoneMinScreenXPercent = 0.5f;

        [Header("Joystick")]
        [SerializeField] float joystickMaxRadiusPx = 100f;
        [SerializeField] JoystickVisual joystickVisual;

        [Header("Input thresholds")]
        [SerializeField] float tapDistancePx = 10f;
        [SerializeField] float tapTimeSec = 0.2f;

        JoystickArea joystick;
        CameraScrollController scroll;
        InputRouter router;

        /// <summary>Output joysticka (X = right, Y = forward) w zakresie ~[-1,1]. Vector2.zero gdy nieaktywny.</summary>
        public Vector2 JoystickOutput => joystick != null ? joystick.Output : Vector2.zero;
        public bool JoystickActive => joystick != null && joystick.IsActive;

        bool pointerDown;

        void Awake()
        {
            joystick = new JoystickArea(joystickMaxRadiusPx);
            float startX = mainCamera != null ? mainCamera.transform.position.x : 12f;
            scroll = new CameraScrollController(
                minX, maxX, pixelsToWorld, rubberStrength, snapBackSpeed, drag, startX);

            router = new InputRouter(
                classify: ClassifyByScreenSide,
                tapDistanceThresholdPx: tapDistancePx,
                tapTimeThresholdSec: tapTimeSec,
                onJoystickPress: p => joystick.OnPress(p),
                onJoystickDrag: p => joystick.OnDrag(p),
                onJoystickRelease: () => joystick.OnRelease(),
                onScrollDragDelta: d => scroll.OnDragDelta(d, Time.deltaTime),
                onScrollRelease: () => scroll.OnRelease(),
                onTap: HandleTap);
        }

        ScreenArea ClassifyByScreenSide(Vector2 screenPos)
        {
            float threshold = Screen.width * Mathf.Clamp01(playerZoneMinScreenXPercent);
            return screenPos.x >= threshold ? ScreenArea.Joystick : ScreenArea.Scroll;
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
