using NUnit.Framework;
using UnityEngine;
using Project.Input;
using System.Collections.Generic;

namespace Project.Tests.EditMode
{
    public class InputRouterTests
    {
        const int W = 1080;
        const int H = 1920;

        class FakeJoystick
        {
            public List<string> Events = new();
            public void OnPress(Vector2 p) => Events.Add($"press({p.x},{p.y})");
            public void OnDrag(Vector2 p) => Events.Add($"drag({p.x},{p.y})");
            public void OnRelease() => Events.Add("release");
        }

        class FakeScroll
        {
            public List<string> Events = new();
            public void OnDragDelta(float d) => Events.Add($"delta({d:F1})");
            public void OnRelease() => Events.Add("release");
        }

        InputRouter NewRouter(FakeJoystick j, FakeScroll s, out List<Vector2> taps)
        {
            // Test classifier mimics the legacy bottom-left 40% screen-area behavior
            // so existing assertions on (100,100) → Joystick and (W/2, H/2) → Scroll keep working.
            var capturedTaps = new List<Vector2>();
            Vector2 screenSize = new Vector2(W, H);
            var router = new InputRouter(
                classify: pos => ScreenAreaUtils.Classify(pos, screenSize),
                tapDistanceThresholdPx: 10f,
                tapTimeThresholdSec: 0.2f,
                onJoystickPress: j.OnPress,
                onJoystickDrag: j.OnDrag,
                onJoystickRelease: j.OnRelease,
                onScrollDragDelta: s.OnDragDelta,
                onScrollRelease: s.OnRelease,
                onTap: capturedTaps.Add);
            taps = capturedTaps;
            return router;
        }

        [Test]
        public void PressInJoystickArea_RoutesToJoystick()
        {
            var j = new FakeJoystick(); var s = new FakeScroll();
            var r = NewRouter(j, s, out var taps);

            r.OnPointerDown(new Vector2(100, 100), 0f);

            CollectionAssert.Contains(j.Events, "press(100,100)");
            Assert.IsEmpty(s.Events);
            Assert.IsEmpty(taps);
        }

        [Test]
        public void TapInScrollArea_NoMovement_FastRelease_EmitsTap()
        {
            var j = new FakeJoystick(); var s = new FakeScroll();
            var r = NewRouter(j, s, out var taps);

            var pos = new Vector2(W / 2f, H / 2f);
            r.OnPointerDown(pos, 0f);
            r.OnPointerUp(pos + new Vector2(2, 2), 0.1f);

            Assert.AreEqual(1, taps.Count);
            Assert.That(taps[0].x, Is.EqualTo(pos.x + 2).Within(0.001f));
            Assert.IsEmpty(s.Events);
        }

        [Test]
        public void DragInScrollArea_BeyondThreshold_RoutesToScroll_NoTap()
        {
            var j = new FakeJoystick(); var s = new FakeScroll();
            var r = NewRouter(j, s, out var taps);

            var start = new Vector2(W / 2f, H / 2f);
            r.OnPointerDown(start, 0f);
            r.OnPointerMove(start + new Vector2(50, 0), 0.1f);
            r.OnPointerMove(start + new Vector2(80, 0), 0.2f);
            r.OnPointerUp(start + new Vector2(80, 0), 0.3f);

            Assert.IsEmpty(taps);
            CollectionAssert.Contains(s.Events, "delta(50.0)");
            CollectionAssert.Contains(s.Events, "delta(30.0)");
            CollectionAssert.Contains(s.Events, "release");
        }

        [Test]
        public void HoldInScrollArea_BeyondTapTimeButStillStill_DoesNotEmitTap()
        {
            var j = new FakeJoystick(); var s = new FakeScroll();
            var r = NewRouter(j, s, out var taps);

            var pos = new Vector2(W / 2f, H / 2f);
            r.OnPointerDown(pos, 0f);
            r.OnPointerUp(pos, 1.0f);

            Assert.IsEmpty(taps);
        }

        [Test]
        public void DragJoystick_ForwardsAllMovesToJoystick()
        {
            var j = new FakeJoystick(); var s = new FakeScroll();
            var r = NewRouter(j, s, out var taps);

            r.OnPointerDown(new Vector2(100, 100), 0f);
            r.OnPointerMove(new Vector2(150, 150), 0.05f);
            r.OnPointerUp(new Vector2(160, 160), 0.1f);

            CollectionAssert.Contains(j.Events, "press(100,100)");
            CollectionAssert.Contains(j.Events, "drag(150,150)");
            CollectionAssert.Contains(j.Events, "drag(160,160)");
            CollectionAssert.Contains(j.Events, "release");
            Assert.IsEmpty(s.Events);
            Assert.IsEmpty(taps);
        }

        [Test]
        public void Classifier_IsConsultedPerPointerDown()
        {
            // Custom classifier that flips based on Y mid-line (not the default screen-area).
            int callCount = 0;
            var router = new InputRouter(
                classify: pos => { callCount++; return pos.y < 500 ? ScreenArea.Joystick : ScreenArea.Scroll; },
                tapDistanceThresholdPx: 10f,
                tapTimeThresholdSec: 0.2f,
                onJoystickPress: _ => { },
                onJoystickDrag: _ => { },
                onJoystickRelease: () => { },
                onScrollDragDelta: _ => { },
                onScrollRelease: () => { },
                onTap: _ => { });

            router.OnPointerDown(new Vector2(900, 100), 0f);
            router.OnPointerUp(new Vector2(900, 100), 0.05f);
            router.OnPointerDown(new Vector2(900, 1000), 0.1f);

            Assert.AreEqual(2, callCount, "classifier consulted on each PointerDown");
        }
    }
}
