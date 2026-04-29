using NUnit.Framework;
using UnityEngine;
using Project.Core;
using Project.Zone1.Trucks;
using System.Collections.Generic;

namespace Project.Tests.EditMode
{
    public class ConveyorTrackTests
    {
        ConveyorTrack BuildSquare()
        {
            return new ConveyorTrack(new List<ConveyorWaypoint>
            {
                new() { Position = new Vector3(0, 0, 0) },
                new() { Position = new Vector3(10, 0, 0) },
                new() { Position = new Vector3(10, 0, 10) },
                new() { Position = new Vector3(0, 0, 10) },
            });
        }

        [Test]
        public void GetWorldPositionAtTrackParam_AtZero_ReturnsFirstWaypoint()
        {
            var t = BuildSquare();
            Assert.That(t.GetWorldPositionAtTrackParam(0f), Is.EqualTo(new Vector3(0, 0, 0)));
        }

        [Test]
        public void GetWorldPositionAtTrackParam_AtQuarter_OnFirstSegment()
        {
            var t = BuildSquare();
            var p = t.GetWorldPositionAtTrackParam(0.25f);
            Assert.That(p.x, Is.EqualTo(10f).Within(0.001f));
            Assert.That(p.z, Is.EqualTo(0f).Within(0.001f));
        }

        [Test]
        public void GetWorldPositionAtTrackParam_LoopsAtOne()
        {
            var t = BuildSquare();
            Assert.That(t.GetWorldPositionAtTrackParam(1f), Is.EqualTo(t.GetWorldPositionAtTrackParam(0f)));
        }

        [Test]
        public void Tick_AdvancesAllTrucks_WhenNoneStopped()
        {
            var track = BuildSquare();
            var t1 = new Truck(1, FruitType.Apple, 100) { TrackPosition = 0f, State = TruckState.OnConveyor };
            var t2 = new Truck(2, FruitType.Orange, 100) { TrackPosition = 0.25f, State = TruckState.OnConveyor };
            track.Tick(new[] { t1, t2 }, 1f, 4f);
            Assert.That(t1.TrackPosition, Is.EqualTo(0.1f).Within(0.001f));
            Assert.That(t2.TrackPosition, Is.EqualTo(0.35f).Within(0.001f));
        }

        [Test]
        public void Tick_TruckStoppedAtSlot_TrucksBehindFreeze()
        {
            var track = BuildSquare();
            var stopped = new Truck(1, FruitType.Apple, 100) { TrackPosition = 0.5f, State = TruckState.StoppedAtSlot };
            var behind = new Truck(2, FruitType.Apple, 100) { TrackPosition = 0.45f, State = TruckState.OnConveyor };
            var ahead = new Truck(3, FruitType.Apple, 100) { TrackPosition = 0.6f, State = TruckState.OnConveyor };
            track.Tick(new[] { stopped, behind, ahead }, 1f, 4f);
            Assert.That(stopped.TrackPosition, Is.EqualTo(0.5f).Within(0.001f));
            Assert.That(behind.TrackPosition, Is.LessThanOrEqualTo(0.5f));
            Assert.That(ahead.TrackPosition, Is.EqualTo(0.7f).Within(0.001f));
        }
    }
}
