using NUnit.Framework;
using Project.Input;

namespace Project.Tests.EditMode
{
    public class CameraScrollControllerTests
    {
        const float Dt = 0.01f;

        CameraScrollController NewCtrl(
            float min = 0f, float max = 10f, float startX = 5f, float drag = 5f)
        {
            return new CameraScrollController(
                minX: min,
                maxX: max,
                pixelsToWorld: 0.01f,
                rubberStrength: 0.5f,
                snapBackSpeed: 10f,
                drag: drag,
                startX: startX);
        }

        [Test]
        public void NewController_HasStartXAsTargetX_AndZeroVelocity()
        {
            var c = NewCtrl(startX: 5f);
            Assert.AreEqual(5f, c.TargetX);
            Assert.AreEqual(0f, c.Velocity);
        }

        [Test]
        public void OnDragDelta_PositivePixelDelta_MovesTargetXLeft_BecauseInverted()
        {
            var c = NewCtrl(startX: 5f);
            c.OnDragDelta(100f, Dt); // +100 px → world -1
            Assert.That(c.TargetX, Is.EqualTo(4f).Within(0.001f));
        }

        [Test]
        public void OnDragDelta_NegativePixelDelta_MovesTargetXRight_BecauseInverted()
        {
            var c = NewCtrl(startX: 5f);
            c.OnDragDelta(-200f, Dt); // -200 px → world +2
            Assert.That(c.TargetX, Is.EqualTo(7f).Within(0.001f));
        }

        [Test]
        public void OnDragDelta_PastMaxBound_RubberBandSlowsMovement()
        {
            var c = NewCtrl(min: 0f, max: 10f, startX: 9.5f);
            c.OnDragDelta(-100f, Dt); // -100 px → world +1, pushes past 10
            Assert.That(c.TargetX, Is.GreaterThan(10f), "should pass bound");
            Assert.That(c.TargetX, Is.LessThan(10.5f), "rubber should attenuate");
        }

        [Test]
        public void OnDragDelta_PastMinBound_RubberBandSlowsMovement()
        {
            var c = NewCtrl(min: 0f, max: 10f, startX: 0.5f);
            c.OnDragDelta(100f, Dt); // +100 px → world -1, pushes past 0
            Assert.That(c.TargetX, Is.LessThan(0f));
            Assert.That(c.TargetX, Is.GreaterThan(-0.5f));
        }

        [Test]
        public void OnRelease_PositionOutsideBounds_SnapsBackOverTime()
        {
            var c = NewCtrl(min: 0f, max: 10f, startX: 11f);
            c.OnRelease();

            c.Update(0.05f);
            Assert.That(c.TargetX, Is.LessThan(11f));

            for (int i = 0; i < 100; i++) c.Update(0.05f);
            Assert.That(c.TargetX, Is.EqualTo(10f).Within(0.01f));
        }

        [Test]
        public void OnRelease_PositionWithinBounds_NoVelocity_DoesNotMove()
        {
            var c = NewCtrl(min: 0f, max: 10f, startX: 5f);
            c.OnRelease();
            c.Update(1.0f);
            Assert.AreEqual(5f, c.TargetX);
        }

        [Test]
        public void Inertia_AfterRelease_VelocityCarriesPosition()
        {
            var c = NewCtrl(min: 0f, max: 100f, startX: 50f, drag: 2f);
            c.OnDragDelta(-100f, Dt); // velocity = +1 / 0.01 = +100 world/s
            c.OnRelease();

            float beforeUpdate = c.TargetX;
            c.Update(0.1f);
            Assert.That(c.TargetX, Is.GreaterThan(beforeUpdate),
                "inertia should carry TargetX in velocity direction");
        }

        [Test]
        public void Inertia_DragBringsVelocityToZero()
        {
            var c = NewCtrl(min: 0f, max: 100f, startX: 50f, drag: 5f);
            c.OnDragDelta(-50f, Dt); // velocity = 50 world/s
            c.OnRelease();

            // Drag = 5, velocity 50 → ~10s to reach zero. Sample more often.
            for (int i = 0; i < 1500; i++) c.Update(0.01f);
            Assert.That(c.Velocity, Is.EqualTo(0f).Within(0.01f));
        }

        [Test]
        public void Inertia_HittingBoundary_VelocityDropsToZero()
        {
            var c = NewCtrl(min: 0f, max: 10f, startX: 9.5f, drag: 1f);
            c.OnDragDelta(-1000f, Dt); // big velocity outward (worldDelta +10, but rubber attenuates immediate)
            c.OnRelease();

            for (int i = 0; i < 100; i++) c.Update(0.05f);
            Assert.That(c.Velocity, Is.EqualTo(0f).Within(0.001f));
            Assert.That(c.TargetX, Is.EqualTo(10f).Within(0.01f));
        }
    }
}
