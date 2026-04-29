using NUnit.Framework;
using UnityEngine;
using Project.Input;

namespace Project.Tests.EditMode
{
    public class ScreenAreaUtilsTests
    {
        const int W = 1080;
        const int H = 1920;

        [Test]
        public void Classify_PointInBottomLeft40Percent_ReturnsJoystickArea()
        {
            var area = ScreenAreaUtils.Classify(new Vector2(100, 200), new Vector2(W, H));
            Assert.AreEqual(ScreenArea.Joystick, area);
        }

        [Test]
        public void Classify_PointInTopHalf_ReturnsScrollArea()
        {
            var area = ScreenAreaUtils.Classify(new Vector2(W / 2f, H * 0.8f), new Vector2(W, H));
            Assert.AreEqual(ScreenArea.Scroll, area);
        }

        [Test]
        public void Classify_PointInBottomRight_ReturnsScrollArea()
        {
            var area = ScreenAreaUtils.Classify(new Vector2(W * 0.8f, 200), new Vector2(W, H));
            Assert.AreEqual(ScreenArea.Scroll, area);
        }

        [Test]
        public void Classify_PointAtJoystickBoundary_TopRightCorner_StillJoystick()
        {
            var area = ScreenAreaUtils.Classify(new Vector2(W * 0.4f - 1, H * 0.4f - 1), new Vector2(W, H));
            Assert.AreEqual(ScreenArea.Joystick, area);
        }

        [Test]
        public void Classify_PointJustOutsideJoystick_IsScroll()
        {
            var area = ScreenAreaUtils.Classify(new Vector2(W * 0.4f + 1, H * 0.4f - 1), new Vector2(W, H));
            Assert.AreEqual(ScreenArea.Scroll, area);
        }

        [Test]
        public void Classify_PointOutsideScreen_ReturnsOutsideAll()
        {
            var area = ScreenAreaUtils.Classify(new Vector2(-10, -10), new Vector2(W, H));
            Assert.AreEqual(ScreenArea.OutsideAll, area);

            var area2 = ScreenAreaUtils.Classify(new Vector2(W + 10, H / 2f), new Vector2(W, H));
            Assert.AreEqual(ScreenArea.OutsideAll, area2);
        }
    }
}
