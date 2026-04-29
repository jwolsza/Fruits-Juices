using UnityEngine;

namespace Project.Input
{
    public class CameraScrollController
    {
        public float MinX { get; }
        public float MaxX { get; }
        public float PixelsToWorld { get; }
        public float RubberStrength { get; }
        public float SnapBackSpeed { get; }

        public float TargetX { get; private set; }

        bool isDragging;

        public CameraScrollController(
            float minX, float maxX, float pixelsToWorld,
            float rubberStrength, float snapBackSpeed, float startX)
        {
            MinX = minX;
            MaxX = maxX;
            PixelsToWorld = pixelsToWorld;
            RubberStrength = rubberStrength;
            SnapBackSpeed = snapBackSpeed;
            TargetX = Mathf.Clamp(startX, minX, maxX);
        }

        public void OnDragStart()
        {
            isDragging = true;
        }

        public void OnDragDelta(float pixelDeltaX)
        {
            isDragging = true;
            float worldDelta = pixelDeltaX * PixelsToWorld;
            float newX = TargetX + worldDelta;

            if (newX > MaxX)
            {
                float overshoot = newX - MaxX;
                newX = MaxX + overshoot * (1f - RubberStrength);
            }
            else if (newX < MinX)
            {
                float undershoot = MinX - newX;
                newX = MinX - undershoot * (1f - RubberStrength);
            }

            TargetX = newX;
        }

        public void OnRelease()
        {
            isDragging = false;
        }

        public void Update(float deltaTime)
        {
            if (isDragging) return;

            if (TargetX > MaxX)
            {
                TargetX = Mathf.MoveTowards(TargetX, MaxX, SnapBackSpeed * deltaTime);
            }
            else if (TargetX < MinX)
            {
                TargetX = Mathf.MoveTowards(TargetX, MinX, SnapBackSpeed * deltaTime);
            }
        }
    }
}
