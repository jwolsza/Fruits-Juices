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
        public float Drag { get; }

        public float TargetX { get; private set; }
        public float Velocity { get; private set; }

        bool isDragging;

        public CameraScrollController(
            float minX, float maxX, float pixelsToWorld,
            float rubberStrength, float snapBackSpeed, float drag, float startX)
        {
            MinX = minX;
            MaxX = maxX;
            PixelsToWorld = pixelsToWorld;
            RubberStrength = rubberStrength;
            SnapBackSpeed = snapBackSpeed;
            Drag = drag;
            TargetX = Mathf.Clamp(startX, minX, maxX);
            Velocity = 0f;
        }

        public void OnDragStart()
        {
            isDragging = true;
            Velocity = 0f;
        }

        public void OnDragDelta(float pixelDeltaX, float deltaTime)
        {
            isDragging = true;

            // Klamruj jednoklatkowy skok — w Chrome WebGL gdy kursor wyjedzie poza canvas i wróci,
            // Mouse.position skacze do nowej pozycji jednym frame'em → camera by lurchowała.
            if (Mathf.Abs(pixelDeltaX) > 300f) return;

            // Inverted: drag right → world moves right under finger → camera moves LEFT.
            float worldDelta = -pixelDeltaX * PixelsToWorld;
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

            if (deltaTime > 0f)
                Velocity = worldDelta / deltaTime;
        }

        public void OnRelease()
        {
            isDragging = false;
            Velocity = 0f;
        }

        public void Update(float deltaTime)
        {
            if (isDragging) return;

            // Inertia: only apply velocity while inside bounds. Outside = snap-back wins.
            bool insideBounds = TargetX >= MinX && TargetX <= MaxX;
            if (insideBounds && Mathf.Abs(Velocity) > 0.001f)
            {
                TargetX += Velocity * deltaTime;
                Velocity = Mathf.MoveTowards(Velocity, 0f, Drag * deltaTime);

                // If inertia pushed us across a bound, kill velocity at the bound.
                if (TargetX > MaxX) { TargetX = MaxX; Velocity = 0f; }
                else if (TargetX < MinX) { TargetX = MinX; Velocity = 0f; }
            }

            if (TargetX > MaxX)
            {
                TargetX = Mathf.MoveTowards(TargetX, MaxX, SnapBackSpeed * deltaTime);
                Velocity = 0f;
            }
            else if (TargetX < MinX)
            {
                TargetX = Mathf.MoveTowards(TargetX, MinX, SnapBackSpeed * deltaTime);
                Velocity = 0f;
            }
        }
    }
}
