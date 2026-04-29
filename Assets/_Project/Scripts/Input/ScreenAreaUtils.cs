using UnityEngine;

namespace Project.Input
{
    public enum ScreenArea
    {
        OutsideAll,
        Joystick,
        Scroll,
    }

    public static class ScreenAreaUtils
    {
        public const float JoystickWidthFraction = 0.4f;
        public const float JoystickHeightFraction = 0.4f;

        public static ScreenArea Classify(Vector2 point, Vector2 screenSize)
        {
            if (point.x < 0f || point.y < 0f || point.x > screenSize.x || point.y > screenSize.y)
                return ScreenArea.OutsideAll;

            bool inJoystick =
                point.x < screenSize.x * JoystickWidthFraction &&
                point.y < screenSize.y * JoystickHeightFraction;

            return inJoystick ? ScreenArea.Joystick : ScreenArea.Scroll;
        }
    }
}
