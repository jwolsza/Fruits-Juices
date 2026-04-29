using NUnit.Framework;
using Project.Input;

namespace Project.Tests.EditMode
{
    public class CameraScrollControllerTests
    {
        CameraScrollController NewCtrl(float min = 0f, float max = 10f, float startX = 5f)
        {
            return new CameraScrollController(
                minX: min,
                maxX: max,
                pixelsToWorld: 0.01f,
                rubberStrength: 0.5f,
                snapBackSpeed: 10f,
                startX: startX);
        }

        [Test]
        public void NewController_HasStartXAsTargetX()
        {
            var c = NewCtrl(startX: 5f);
            Assert.AreEqual(5f, c.TargetX);
        }

        [Test]
        public void OnDragDelta_WithinBounds_TranslatesPixelsToWorld()
        {
            var c = NewCtrl(startX: 5f);
            c.OnDragDelta(100f);
            Assert.That(c.TargetX, Is.EqualTo(6f).Within(0.001f));
        }

        [Test]
        public void OnDragDelta_NegativeDelta_GoesLeft()
        {
            var c = NewCtrl(startX: 5f);
            c.OnDragDelta(-200f);
            Assert.That(c.TargetX, Is.EqualTo(3f).Within(0.001f));
        }

        [Test]
        public void OnDragDelta_PastMaxBound_RubberBandSlowsMovement()
        {
            var c = NewCtrl(min: 0f, max: 10f, startX: 9.5f);
            c.OnDragDelta(100f);
            Assert.That(c.TargetX, Is.GreaterThan(10f), "should pass bound");
            Assert.That(c.TargetX, Is.LessThan(10.5f), "rubber should attenuate");
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
        public void OnRelease_PositionWithinBounds_DoesNotMove()
        {
            var c = NewCtrl(min: 0f, max: 10f, startX: 5f);
            c.OnRelease();
            c.Update(1.0f);
            Assert.AreEqual(5f, c.TargetX);
        }
    }
}
