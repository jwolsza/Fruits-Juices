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

        [Header("Player Zone Detection (world raycast)")]
        [Tooltip("Tap z rayem trafiającym w world X >= ta wartość = joystick. Inaczej = scroll kamerą.")]
        [SerializeField] float playerZoneMinWorldX = 18f;
        [Tooltip("Y płaszczyzny ground dla raycastu klasyfikującego.")]
        [SerializeField] float groundPlaneY = 0f;

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
                classify: ClassifyByGroundRaycast,
                tapDistanceThresholdPx: tapDistancePx,
                tapTimeThresholdSec: tapTimeSec,
                onJoystickPress: p => joystick.OnPress(p),
                onJoystickDrag: p => joystick.OnDrag(p),
                onJoystickRelease: () => joystick.OnRelease(),
                onScrollDragDelta: d => scroll.OnDragDelta(d, Time.deltaTime),
                onScrollRelease: () => scroll.OnRelease(),
                onTap: HandleTap);
        }

        ScreenArea ClassifyByGroundRaycast(Vector2 screenPos)
        {
            if (mainCamera == null) return ScreenArea.Scroll;
            Plane ground = new Plane(Vector3.up, new Vector3(0f, groundPlaneY, 0f));
            Ray ray = mainCamera.ScreenPointToRay(screenPos);
            if (!ground.Raycast(ray, out float distance)) return ScreenArea.Scroll;
            Vector3 hit = ray.GetPoint(distance);
            return hit.x >= playerZoneMinWorldX ? ScreenArea.Joystick : ScreenArea.Scroll;
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

        enum PointerSource { None, Touch, Mouse }
        PointerSource activePointer = PointerSource.None;

        void HandlePointerInput()
        {
            float now = Time.time;

            // Wykryj rozpoczęcie nowego dotknięcia/kliknięcia. Lock device dla całego dragu — żeby
            // browser-side pseudo-touch z myszy nie flickerował z prawdziwą myszą i nie podawał
            // pozycji z dwóch różnych urządzeń per klatka.
            if (!pointerDown)
            {
                if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
                {
                    activePointer = PointerSource.Touch;
                    pointerDown = true;
                    router.OnPointerDown(Touchscreen.current.primaryTouch.position.ReadValue(), now);
                    return;
                }
                if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                {
                    activePointer = PointerSource.Mouse;
                    pointerDown = true;
                    router.OnPointerDown(Mouse.current.position.ReadValue(), now);
                    return;
                }
                return;
            }

            // pointerDown == true: czytaj pozycję TYLKO z urządzenia które rozpoczęło drag.
            switch (activePointer)
            {
                case PointerSource.Touch:
                    if (Touchscreen.current == null) { ForceRelease(now, Vector2.zero); return; }
                    if (Touchscreen.current.primaryTouch.press.wasReleasedThisFrame)
                    {
                        ForceRelease(now, Touchscreen.current.primaryTouch.position.ReadValue());
                        return;
                    }
                    if (Touchscreen.current.primaryTouch.press.isPressed)
                        router.OnPointerMove(Touchscreen.current.primaryTouch.position.ReadValue(), now);
                    break;

                case PointerSource.Mouse:
                    if (Mouse.current == null) { ForceRelease(now, Vector2.zero); return; }
                    if (Mouse.current.leftButton.wasReleasedThisFrame)
                    {
                        ForceRelease(now, Mouse.current.position.ReadValue());
                        return;
                    }
                    if (Mouse.current.leftButton.isPressed)
                        router.OnPointerMove(Mouse.current.position.ReadValue(), now);
                    break;
            }
        }

        void ForceRelease(float now, Vector2 pos)
        {
            router.OnPointerUp(pos, now);
            pointerDown = false;
            activePointer = PointerSource.None;
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
