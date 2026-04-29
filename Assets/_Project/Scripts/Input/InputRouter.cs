using System;
using UnityEngine;

namespace Project.Input
{
    public class InputRouter
    {
        readonly Vector2 screenSize;
        readonly float tapDistanceThresholdPx;
        readonly float tapTimeThresholdSec;

        readonly Action<Vector2> onJoystickPress;
        readonly Action<Vector2> onJoystickDrag;
        readonly Action onJoystickRelease;
        readonly Action<float> onScrollDragDelta;
        readonly Action onScrollRelease;
        readonly Action<Vector2> onTap;

        ScreenArea downArea;
        Vector2 downPosition;
        Vector2 lastPosition;
        float downTime;
        bool exceededMovementThreshold;

        public InputRouter(
            Vector2 screenSize,
            float tapDistanceThresholdPx,
            float tapTimeThresholdSec,
            Action<Vector2> onJoystickPress,
            Action<Vector2> onJoystickDrag,
            Action onJoystickRelease,
            Action<float> onScrollDragDelta,
            Action onScrollRelease,
            Action<Vector2> onTap)
        {
            this.screenSize = screenSize;
            this.tapDistanceThresholdPx = tapDistanceThresholdPx;
            this.tapTimeThresholdSec = tapTimeThresholdSec;
            this.onJoystickPress = onJoystickPress;
            this.onJoystickDrag = onJoystickDrag;
            this.onJoystickRelease = onJoystickRelease;
            this.onScrollDragDelta = onScrollDragDelta;
            this.onScrollRelease = onScrollRelease;
            this.onTap = onTap;
            downArea = ScreenArea.OutsideAll;
        }

        public void OnPointerDown(Vector2 position, float timeSec)
        {
            downArea = ScreenAreaUtils.Classify(position, screenSize);
            downPosition = position;
            lastPosition = position;
            downTime = timeSec;
            exceededMovementThreshold = false;

            if (downArea == ScreenArea.Joystick)
                onJoystickPress?.Invoke(position);
        }

        public void OnPointerMove(Vector2 position, float timeSec)
        {
            if (downArea == ScreenArea.OutsideAll) return;

            if (downArea == ScreenArea.Joystick)
            {
                onJoystickDrag?.Invoke(position);
                lastPosition = position;
                return;
            }

            float distFromDown = (position - downPosition).magnitude;
            if (distFromDown >= tapDistanceThresholdPx)
                exceededMovementThreshold = true;

            if (exceededMovementThreshold)
            {
                float deltaX = position.x - lastPosition.x;
                onScrollDragDelta?.Invoke(deltaX);
            }

            lastPosition = position;
        }

        public void OnPointerUp(Vector2 position, float timeSec)
        {
            if (downArea == ScreenArea.OutsideAll) return;

            if (downArea == ScreenArea.Joystick)
            {
                onJoystickRelease?.Invoke();
                downArea = ScreenArea.OutsideAll;
                return;
            }

            float distFromDown = (position - downPosition).magnitude;
            float duration = timeSec - downTime;
            bool isTap = !exceededMovementThreshold
                         && distFromDown < tapDistanceThresholdPx
                         && duration < tapTimeThresholdSec;

            if (isTap)
            {
                onTap?.Invoke(position);
            }
            else
            {
                if (exceededMovementThreshold)
                {
                    float deltaX = position.x - lastPosition.x;
                    if (Mathf.Abs(deltaX) > 0.0001f)
                        onScrollDragDelta?.Invoke(deltaX);
                }
                onScrollRelease?.Invoke();
            }

            downArea = ScreenArea.OutsideAll;
        }
    }
}
