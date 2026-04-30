using System.Collections.Generic;
using NUnit.Framework;
using Project.Core;
using Project.Zone1.FruitWall;

namespace Project.Tests.EditMode
{
    public class RefillControllerTests
    {
        class FakeRandom : IRandomSource
        {
            readonly Queue<int> intQueue;
            public FakeRandom(params int[] values)
            {
                intQueue = new Queue<int>(values);
            }
            public int NextInt(int minInclusive, int maxExclusive)
            {
                if (intQueue.Count == 0)
                    return minInclusive;
                int v = intQueue.Dequeue();
                int range = maxExclusive - minInclusive;
                if (range <= 0) return minInclusive;
                return minInclusive + (((v % range) + range) % range);
            }
        }

        FruitType[] DefaultPool() => new[] { FruitType.Apple, FruitType.Orange, FruitType.Lemon };

        [Test]
        public void NewController_IsIdle()
        {
            var grid = new FruitGrid(5, 5);
            var ctrl = new RefillController(grid, DefaultPool(), new FakeRandom(), spawnsPerTick: 10);
            Assert.IsFalse(ctrl.IsRefilling);
        }

        [Test]
        public void Start_PutsControllerInRefillingState()
        {
            var grid = new FruitGrid(5, 5);
            var ctrl = new RefillController(grid, DefaultPool(), new FakeRandom(), spawnsPerTick: 10);
            ctrl.Start(grid.Columns * grid.Rows);
            Assert.IsTrue(ctrl.IsRefilling);
        }

        [Test]
        public void Tick_NotStarted_Noop()
        {
            var grid = new FruitGrid(3, 3);
            var ctrl = new RefillController(grid, DefaultPool(), new FakeRandom(), spawnsPerTick: 5);
            ctrl.Tick();
            Assert.AreEqual(0, grid.OccupiedCount);
        }

        [Test]
        public void Tick_SpawnsInTopRowEmptyCells_UpToSpawnsPerTick()
        {
            var grid = new FruitGrid(5, 5);
            grid.SetCell(0, 4, FruitType.Apple);
            grid.SetCell(2, 4, FruitType.Apple);

            var random = new FakeRandom(0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
            var ctrl = new RefillController(grid, DefaultPool(), random, spawnsPerTick: 3);
            ctrl.Start(grid.Columns * grid.Rows);

            ctrl.Tick();

            Assert.AreEqual(2 + 3, grid.OccupiedCount);
            for (int x = 0; x < 5; x++)
                Assert.IsNotNull(grid.GetCell(x, 4));
        }

        [Test]
        public void Tick_TopRowFull_NothingSpawned()
        {
            var grid = new FruitGrid(3, 3);
            for (int x = 0; x < 3; x++) grid.SetCell(x, 2, FruitType.Apple);

            var ctrl = new RefillController(grid, DefaultPool(), new FakeRandom(0), spawnsPerTick: 5);
            ctrl.Start(grid.Columns * grid.Rows);

            int before = grid.OccupiedCount;
            ctrl.Tick();
            Assert.AreEqual(before, grid.OccupiedCount, "no spawn possible if top row full");
        }

        [Test]
        public void Tick_GridFull_StopsRefilling()
        {
            var grid = new FruitGrid(2, 2);
            grid.SetCell(0, 0, FruitType.Apple);
            grid.SetCell(1, 0, FruitType.Apple);
            grid.SetCell(1, 1, FruitType.Apple);

            var ctrl = new RefillController(grid, DefaultPool(), new FakeRandom(0, 0), spawnsPerTick: 5);
            ctrl.Start(grid.Columns * grid.Rows);
            ctrl.Tick();

            Assert.IsTrue(grid.IsFull);
            Assert.IsFalse(ctrl.IsRefilling, "should auto-stop when grid full");
        }

        [Test]
        public void Tick_UsesPoolForFruitType()
        {
            var grid = new FruitGrid(3, 1);
            var random = new FakeRandom(0, 0, 0, 1, 0, 2);
            var ctrl = new RefillController(grid, DefaultPool(), random, spawnsPerTick: 3);
            ctrl.Start(grid.Columns * grid.Rows);
            ctrl.Tick();

            Assert.AreEqual(3, grid.OccupiedCount);
            for (int x = 0; x < 3; x++)
            {
                var cell = grid.GetCell(x, 0);
                Assert.IsNotNull(cell);
                CollectionAssert.Contains(DefaultPool(), cell);
            }
        }
    }
}
