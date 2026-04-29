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
        public void NewTrack_HasCorrectSlotCount_AndEvenlySpacedSlots()
        {
            var t = BuildSquare(slotCount: 4);
            Assert.AreEqual(4, t.Slots.Count);
            Assert.That(t.Slots[0].SlotOffsetFromZero, Is.EqualTo(0f).Within(0.001f));
            Assert.That(t.Slots[1].SlotOffsetFromZero, Is.EqualTo(0.25f).Within(0.001f));
            Assert.That(t.Slots[2].SlotOffsetFromZero, Is.EqualTo(0.5f).Within(0.001f));
            Assert.That(t.Slots[3].SlotOffsetFromZero, Is.EqualTo(0.75f).Within(0.001f));
        }

        [Test]
        public void NewTrack_AllSlotsEmpty()
        {
            var t = BuildSquare();
            foreach (var s in t.Slots) Assert.IsTrue(s.IsEmpty);
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
        public void Tick_AdvancesAllSlotsTogether()
        {
            var t = BuildSquare(slotCount: 2);
            float prev0 = t.Slots[0].TrackPosition;
            float prev1 = t.Slots[1].TrackPosition;

            // Total length = 40, advance 4 units → +0.1 base progress
            t.Tick(deltaTime: 1f, speedUnitsPerSec: 4f);

            Assert.That(t.Slots[0].TrackPosition, Is.EqualTo((prev0 + 0.1f) % 1f).Within(0.001f));
            Assert.That(t.Slots[1].TrackPosition, Is.EqualTo((prev1 + 0.1f) % 1f).Within(0.001f));
        }

        [Test]
        public void Paused_Tick_DoesNotAdvance()
        {
            var t = BuildSquare(slotCount: 2);
            float prev0 = t.Slots[0].TrackPosition;
            t.Paused = true;
            t.Tick(1f, 4f);
            Assert.That(t.Slots[0].TrackPosition, Is.EqualTo(prev0).Within(0.001f));
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
        public void TryAssignTruckToFirstEmptySlot_FillsFromIndexZero()
        {
            var t = BuildSquare(slotCount: 3);
            var t1 = new Truck(1, FruitType.Apple, 100);
            var t2 = new Truck(2, FruitType.Orange, 100);
            t.TryAssignTruckToFirstEmptySlot(t1);
            t.TryAssignTruckToFirstEmptySlot(t2);
            Assert.AreSame(t1, t.Slots[0].Truck);
            Assert.AreSame(t2, t.Slots[1].Truck);
            Assert.IsTrue(t.Slots[2].IsEmpty);
        }

        [Test]
        public void RemoveTruckFromSlot_FreesSlot()
        {
            var t = BuildSquare(slotCount: 2);
            var truck = new Truck(1, FruitType.Apple, 100);
            t.TryAssignTruckToFirstEmptySlot(truck);
            t.RemoveTruckFromSlot(truck);
            Assert.IsTrue(t.Slots[0].IsEmpty);
        }
    }
}
