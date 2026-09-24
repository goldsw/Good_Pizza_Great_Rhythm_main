using System.Collections.Generic;
using GPGR.Charting;
using NUnit.Framework;
using UnityEngine;

namespace GPGR.Runtime.Tests
{
    public sealed class MotionPathTests
    {
        [Test]
        public void Angle_ClampsOutsideKeys_AndInterpolatesInside()
        {
            var keys = new List<MotionKey>
            {
                new MotionKey(0, 180f),
                new MotionKey(2, 270f),
            };

            AssertAngle(keys, -1, 180f);
            AssertAngle(keys, 0, 180f);
            AssertAngle(keys, 1, 225f);
            AssertAngle(keys, 2, 270f);
            AssertAngle(keys, 5, 270f);
        }

        [Test]
        public void Angle_ReturnTrip_PassesTheSameAngleTwice()
        {
            var keys = new List<MotionKey>
            {
                new MotionKey(0, 180f),
                new MotionKey(2, 270f),
                new MotionKey(3, 210f),
                new MotionKey(4, 270f),
            };

            AssertAngle(keys, 2.5, 240f);
            AssertAngle(keys, 3.5, 240f);
        }

        [Test]
        public void Angle_UsesTheSignedDelta_NotTheShortWay()
        {
            var keys = new List<MotionKey>
            {
                new MotionKey(0, 10f),
                new MotionKey(2, 350f),
            };

            AssertAngle(keys, 1, 180f);
        }

        [Test]
        public void Position_BottomIsNegativeY()
        {
            Vector2 bottom = MotionPath.Position(270f, Vector2.zero, 3f);
            Assert.AreEqual(0f, bottom.x, 1e-4f);
            Assert.AreEqual(-3f, bottom.y, 1e-4f);

            Vector2 right = MotionPath.Position(0f, Vector2.zero, 3f);
            Assert.AreEqual(3f, right.x, 1e-4f);
            Assert.AreEqual(0f, right.y, 1e-4f);
        }

        static void AssertAngle(List<MotionKey> keys, double beat, float expected)
        {
            Assert.AreEqual(expected, MotionPathMath.AngleAt(keys, beat), 0.001f);
            Assert.AreEqual(expected, MotionPath.AngleAt(keys, beat), 0.001f);
        }
    }
}
