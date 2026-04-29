using NUnit.Framework;
using UnityEngine;
using Project.Core;
using Project.Zone1.Trucks;
using System.Collections.Generic;

namespace Project.Tests.EditMode
{
    public class ConveyorTrackTests
    {
        ConveyorTrack BuildSquare(int slotCount = 4)
        {
            return new ConveyorTrack(new List<ConveyorWaypoint>
            {
                new() { Position = new Vector3(0, 0, 0) },
                new() { Position = new Vector3(10, 0, 0) },
                new() { Position = new Vector3(10, 0, 10) },
                new() { Position = new Vector3(0, 0, 10) },
            }, slotCount);
        }

        [Test]
        public void NewTrack_HasCorrectSlotCount_AndEvenlySpacedInitial()
        {
            var t = BuildSquare(slotCount: 4);
            Assert.AreEqual(4, t.Slots.Count);
            Assert.That(t.Slots[0].TrackPosition, Is.EqualTo(0f).Within(0.001f));
            Assert.That(t.Slots[1].TrackPosition, Is.EqualTo(0.25f).Within(0.001f));
            Assert.That(t.Slots[2].TrackPosition, Is.EqualTo(0.5f).Within(0.001f));
            Assert.That(t.Slots[3].TrackPosition, Is.EqualTo(0.75f).Within(0.001f));
        }

        [Test]
        public void NewTrack_AllSlotsEmpty_AndNotStopped()
        {
            var t = BuildSquare();
            foreach (var s in t.Slots)
            {
                Assert.IsTrue(s.IsEmpty);
                Assert.IsFalse(s.IsStopped);
            }
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
        public void Tick_NoStoppedSlots_AllAdvanceUniformly()
        {
            var t = BuildSquare(slotCount: 2);
            float p0 = t.Slots[0].TrackPosition;
            float p1 = t.Slots[1].TrackPosition;

            t.Tick(deltaTime: 1f, speedUnitsPerSec: 4f); // total 40 → +0.1

            Assert.That(t.Slots[0].TrackPosition, Is.EqualTo((p0 + 0.1f) % 1f).Within(0.001f));
            Assert.That(t.Slots[1].TrackPosition, Is.EqualTo((p1 + 0.1f) % 1f).Within(0.001f));
        }

        [Test]
        public void Paused_Tick_DoesNotAdvance()
        {
            var t = BuildSquare(slotCount: 2);
            float p0 = t.Slots[0].TrackPosition;
            t.Paused = true;
            t.Tick(1f, 4f);
            Assert.That(t.Slots[0].TrackPosition, Is.EqualTo(p0).Within(0.001f));
        }

        [Test]
        public void StoppedSlot_StaysPut_OtherSlotsContinueUntilFormation()
        {
            // Slot 0 at 0.5 stopped. Slot 1 at 0.4 (behind by 0.1), slot 2 at 0.7 (ahead by 0.2).
            var t = BuildSquare(slotCount: 3);
            t.MinSlotSpacing = 0.05f;
            t.Slots[0].TrackPosition = 0.5f; t.Slots[0].IsStopped = true;
            t.Slots[1].TrackPosition = 0.4f;
            t.Slots[2].TrackPosition = 0.7f;

            t.Tick(1f, 4f); // deltaParam = 0.1

            Assert.That(t.Slots[0].TrackPosition, Is.EqualTo(0.5f).Within(0.001f), "stopped stays");
            // Slot 1 was 0.1 behind stopped (gap 0.1). MinSpacing 0.05 → can advance up to 0.45.
            Assert.That(t.Slots[1].TrackPosition, Is.EqualTo(0.45f).Within(0.001f), "behind clamps to maintain min gap");
            Assert.That(t.Slots[2].TrackPosition, Is.EqualTo(0.8f).Within(0.001f), "ahead advances normally");
        }

        [Test]
        public void TrailingChain_AllSlotsRespectSpacing_EvenWithoutStoppedAhead()
        {
            // Slot 0 stopped at 0.5. Slot 1 at 0.4 (gap 0.1). Slot 2 at 0.3 (gap 0.1 to slot 1).
            // After tick: slot 1 clamps to 0.45 (behind slot 0 with min spacing).
            //             slot 2 must clamp behind UPDATED slot 1 (0.45) → 0.40, not behind slot 0 directly.
            var t = BuildSquare(slotCount: 3);
            t.MinSlotSpacing = 0.05f;
            t.Slots[0].TrackPosition = 0.5f; t.Slots[0].IsStopped = true;
            t.Slots[1].TrackPosition = 0.4f;
            t.Slots[2].TrackPosition = 0.3f;

            t.Tick(1f, 4f); // delta 0.1

            Assert.That(t.Slots[0].TrackPosition, Is.EqualTo(0.5f).Within(0.001f));
            Assert.That(t.Slots[1].TrackPosition, Is.EqualTo(0.45f).Within(0.001f));
            Assert.That(t.Slots[2].TrackPosition, Is.EqualTo(0.40f).Within(0.001f),
                "slot 2 must respect spacing to slot 1's UPDATED position, not just stopped slots");
        }

        [Test]
        public void MinSlotSpacing_PreventsOverlap()
        {
            // Slot 0 stopped at 0.5. Slot 1 at 0.49 (already too close given min spacing 0.05).
            var t = BuildSquare(slotCount: 2);
            t.MinSlotSpacing = 0.05f;
            t.Slots[0].TrackPosition = 0.5f; t.Slots[0].IsStopped = true;
            t.Slots[1].TrackPosition = 0.49f;

            t.Tick(1f, 4f);

            // Slot 1 was already inside MinSpacing zone — should not move forward at all.
            Assert.That(t.Slots[1].TrackPosition, Is.EqualTo(0.49f).Within(0.001f));
        }

        [Test]
        public void StoppedSlot_NonStoppedTraffic_PassesUntilHittingFormation()
        {
            // Slot 0 stopped at 0.5. Slot 1 at 0.0 (far behind). Tick should let slot 1 advance fully.
            var t = BuildSquare(slotCount: 2);
            t.Slots[0].TrackPosition = 0.5f; t.Slots[0].IsStopped = true;
            t.Slots[1].TrackPosition = 0.0f;

            t.Tick(1f, 4f); // delta 0.1, slot 1 → 0.1, no clamp needed (still 0.4 from stopped)

            Assert.That(t.Slots[1].TrackPosition, Is.EqualTo(0.1f).Within(0.001f));
        }

        [Test]
        public void TwoStoppedSlots_OnlyNearestBlocks()
        {
            // Slot 0 stopped 0.5, slot 1 stopped 0.7. Slot 2 at 0.4. Advancing should clamp behind nearest (0.5).
            var t = BuildSquare(slotCount: 3);
            t.MinSlotSpacing = 0.05f;
            t.Slots[0].TrackPosition = 0.5f; t.Slots[0].IsStopped = true;
            t.Slots[1].TrackPosition = 0.7f; t.Slots[1].IsStopped = true;
            t.Slots[2].TrackPosition = 0.4f;

            t.Tick(1f, 4f); // delta 0.1, gap to nearest stopped (0.5) = 0.1, allowed = 0.05 → clamp to 0.45

            Assert.That(t.Slots[2].TrackPosition, Is.EqualTo(0.45f).Within(0.001f));
        }

        [Test]
        public void TryAssignTruckToFirstEmptySlot_PutsTruckIntoSlotZero()
        {
            var t = BuildSquare(slotCount: 4);
            var truck = new Truck(1, FruitType.Apple, 100);
            Assert.IsTrue(t.TryAssignTruckToFirstEmptySlot(truck));
            Assert.AreSame(truck, t.Slots[0].Truck);
            Assert.AreEqual(TruckState.OnConveyor, truck.State);
        }

        [Test]
        public void TryAssignTruckToFirstEmptySlot_AllOccupied_ReturnsFalse()
        {
            var t = BuildSquare(slotCount: 2);
            var t1 = new Truck(1, FruitType.Apple, 100);
            var t2 = new Truck(2, FruitType.Orange, 100);
            var t3 = new Truck(3, FruitType.Lemon, 100);
            Assert.IsTrue(t.TryAssignTruckToFirstEmptySlot(t1));
            Assert.IsTrue(t.TryAssignTruckToFirstEmptySlot(t2));
            Assert.IsFalse(t.TryAssignTruckToFirstEmptySlot(t3));
            Assert.AreEqual(TruckState.InGarage, t3.State);
        }

        [Test]
        public void RemoveTruckFromSlot_FreesSlotAndClearsStopFlag()
        {
            var t = BuildSquare(slotCount: 2);
            var truck = new Truck(1, FruitType.Apple, 100);
            t.TryAssignTruckToFirstEmptySlot(truck);
            t.Slots[0].IsStopped = true;
            t.RemoveTruckFromSlot(truck);
            Assert.IsTrue(t.Slots[0].IsEmpty);
            Assert.IsFalse(t.Slots[0].IsStopped);
        }
    }
}
