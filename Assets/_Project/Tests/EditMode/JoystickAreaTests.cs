using NUnit.Framework;
using UnityEngine;
using Project.Input;

namespace Project.Tests.EditMode
{
    public class JoystickAreaTests
    {
        const float MaxRadius = 100f;

        [Test]
        public void NewJoystick_StartsIdle_OutputIsZero()
        {
            var j = new JoystickArea(MaxRadius);
            Assert.IsFalse(j.IsActive);
            Assert.AreEqual(Vector2.zero, j.Output);
        }

        [Test]
        public void OnPress_BecomesActive_OutputStillZeroBecauseAtCenter()
        {
            var j = new JoystickArea(MaxRadius);
            j.OnPress(new Vector2(500, 300));
            Assert.IsTrue(j.IsActive);
            Assert.AreEqual(Vector2.zero, j.Output);
            Assert.AreEqual(new Vector2(500, 300), j.Center);
        }

        [Test]
        public void OnDrag_ReturnsNormalizedVector_WithinUnitMagnitude()
        {
            var j = new JoystickArea(MaxRadius);
            j.OnPress(new Vector2(500, 300));
            j.OnDrag(new Vector2(550, 300));
            Assert.That(j.Output.x, Is.EqualTo(0.5f).Within(0.001f));
            Assert.That(j.Output.y, Is.EqualTo(0f).Within(0.001f));
        }

        [Test]
        public void OnDrag_BeyondMaxRadius_ClampsToMagnitudeOne()
        {
            var j = new JoystickArea(MaxRadius);
            j.OnPress(new Vector2(500, 300));
            j.OnDrag(new Vector2(900, 300));
            Assert.That(j.Output.magnitude, Is.EqualTo(1f).Within(0.001f));
            Assert.That(j.Output.x, Is.EqualTo(1f).Within(0.001f));
        }

        [Test]
        public void OnRelease_BecomesIdle_OutputZero()
        {
            var j = new JoystickArea(MaxRadius);
            j.OnPress(new Vector2(500, 300));
            j.OnDrag(new Vector2(550, 350));
            j.OnRelease();
            Assert.IsFalse(j.IsActive);
            Assert.AreEqual(Vector2.zero, j.Output);
        }

        [Test]
        public void OnDrag_DiagonalMovement_NormalizedCorrectly()
        {
            var j = new JoystickArea(MaxRadius);
            j.OnPress(new Vector2(500, 300));
            j.OnDrag(new Vector2(560, 380));
            Assert.That(j.Output.magnitude, Is.EqualTo(1f).Within(0.01f));
            Assert.That(j.Output.x, Is.EqualTo(0.6f).Within(0.01f));
            Assert.That(j.Output.y, Is.EqualTo(0.8f).Within(0.01f));
        }
    }
}
