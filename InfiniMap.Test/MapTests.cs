using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace InfiniMap.Test
{
    [TestFixture]
    public class MapTests3D
    {
        [Test]
        public void CanInsertItems()
        {
            var mapStruct = new Map3D<StructItem>();
            mapStruct[4, 4, 4] = new StructItem() {ItemId = 4};
            Assert.That(mapStruct[4, 4, 4].ItemId == 4);

            var mapPrim = new Map3D<float>();
            mapPrim[4, 4, 4] = 4.0f;
            Assert.That(Math.Abs(mapPrim[4, 4, 4] - 4.0f) < 0.001);

            var mapClass = new Map3D<ClassItem>();
            mapClass[4, 4, 4] = new ClassItem() {ItemId = 4};
            Assert.That(mapClass[4, 4, 4].ItemId == 4);
        }

        [Test]
        public void CanUseNegativePositions()
        {
            var map = new Map3D<float>();
            map[-4, -4, -4] = 4.0f;
            Assert.That(Math.Abs(map[-4, -4, -4] - 4.0f) < 0.001);
        }

        [Test]
        public void CanEnumerateRanges()
        {
            var map = new Map3D<float>();
            map[1, 1, 1] = 4.0f;

            Assert.That(map.Within(0, 0, 0, 2, 2, 2).Any());
            Assert.That(map.Within(0, 0, 0, 2, 2, 2).Any(i => Math.Abs(i - 4.0f) < 0.001));
        }

        [Test]
        public void CanAccessRandomly()
        {
            var map = new Map3D<float>();

            map[-8, -8, -8] = 4.0f;
            map[-240, -778, -255] = 8.0f;
            map[8, 8, 8] = 16.0f;
            map[240, 778, 255] = 32.0f;

            Assert.That(Math.Abs(map[-240, -778, -255] - 8.0f) < 0.001);
            Assert.That(Math.Abs(map[240, 778, 255] - 32.0f) < 0.001);
        }

        [Test]
        public void SparseCreation()
        {
            var map = new Map3D<float>();

            map[-8, -8, -8] = 4.0f;
            map[-1024, -887, -900] = 8.0f;

            // With only two areas in memory, we only have (16*16*16)*2 blocks
            Assert.AreEqual(8192, map.Count);
        }

        [Test]
        public void CanUseContains()
        {
            var map = new Map3D<float>();
            map[1, 2, 1] = 4.0f;

            Assert.That(map.Contains(4.0f));
            Assert.That(map.Contains(4.0f, new EqualityLambda<float>((a, b) => Math.Abs(a - b) < 0.001)));
        }

        [Test]
        public void IsCubic()
        {
            var map = new Map3D<float>();

            // 'Ground Level'
            map[1, 1, 1] = 1.0f;

            // 'Cloud Level'
            map[1, 1, 64] = 64.0f;

            // 'Atmosphere'
            map[1, 1, 128] = 128.0f;

            // Spaaaace
            map[1, 1, 256] = 256.0f;

            // Dad, are we space now?
            map[1, 1, 2048] = 2048.0f;

            // No son, we are Aldebaran.
            map[1, 1, Int64.MaxValue] = 8192.0f;

            Assert.AreEqual(((16*16*16)*6), map.Count);
        }

        [Test]
        public void SupportsUnloading()
        {
            var map = new Map3D<float>();

            map[0, 0, 0] = 2.0f;
            map[16, 16, 16] = 2.0f;
            map[33, 33, 33] = 4.0f;

            Assert.AreEqual((16*16*16)*3, map.Count);

            map.UnloadArea(0, 0, 0, 33, 33, 33);

            Assert.AreEqual(0, map.Count);
        }

        [Test]
        public void ChunkGathering()
        {
            var map = new Map3D<float>(16, 16, 16);

            map[1, 1, 1] = 2.0f;
            map[1, 1, 17] = 4.0f;
            map[1, 1, 33] = 8.0f;

            // Assert we have 3 chunks in memory.
            Assert.AreEqual((16 * 16 * 16) * 3, map.Count);

            // A single chunk at the bottom of the stack
            {
                var chunksFound = map.ChunksWithin(0, 0, 0, 15, 15, 15, createIfNull: false).ToList();
                Assert.AreEqual(1, chunksFound.Count());
                Assert.AreEqual(0, chunksFound.Select(s => s.Item3).First());

                // Assert that it is the correct chunk
                Assert.That(chunksFound.ElementAt(0).Item4.Contains(2.0f));
            }

            // All three chunks stacked on top of each other
            {
                var chunksFound = map.ChunksWithin(0, 0, 0, 15, 15, 33, createIfNull: false).ToList();
                Assert.AreEqual(3, chunksFound.Count());

                IEnumerable<long> zSequences = new List<long> { 0, 16, 32 };
                var chunks = chunksFound.Select(chunk => chunk.Item3).OrderBy(s => s);
                Assert.AreEqual(3, chunks.Union(zSequences).Count());
                
                // Assert we have the actual chunks
                Assert.That(chunksFound.ElementAt(0).Item4.Contains(2.0f));
                Assert.That(chunksFound.ElementAt(1).Item4.Contains(4.0f));
                Assert.That(chunksFound.ElementAt(2).Item4.Contains(8.0f));
            }

            // The two top most stacks
            {
                var chunksFound = map.ChunksWithin(0, 0, 16, 15, 15, 33, createIfNull: false).ToList();
                Assert.AreEqual(2, chunksFound.Count());

                // Assert that we got back the right chunks in terms of Z level startings
                var zSequences = new List<long> { 16, 32 };
                var chunks = chunksFound.Select(chunk => chunk.Item3).OrderBy(s => s);
                Assert.AreEqual(2, chunks.Union(zSequences).Count());

                // Assert we have the actual chunks.
                Assert.That(chunksFound.ElementAt(0).Item4.Contains(4.0f));
                Assert.That(chunksFound.ElementAt(1).Item4.Contains(8.0f));
            }
        }

        [Test]
        public void SupportsUnloadingOutsideArea()
        {
            Assert.Fail("Test not written");
        }
    }

    [TestFixture]
    public class MapTests2D
    {
        [Test]
        public void CanInsertItems()
        {
            // Insert structs
            var mapStruct = new Map2D<StructItem>();
            mapStruct[4, 4] = new StructItem() {ItemId = 4};
            Assert.That(mapStruct[4, 4].ItemId == 4);

            // Insert primitives
            var mapPrim = new Map2D<float>();
            mapPrim[4, 4] = 4.0f;
            Assert.That(Math.Abs(mapPrim[4, 4] - 4.0f) < 0.001);

            // Insert classes
            var mapClass = new Map2D<ClassItem>();
            mapClass[4, 4] = new ClassItem() {ItemId = 4};
            Assert.That(mapClass[4, 4].ItemId == 4);
        }

        [Test]
        public void CanUseNegativePositions()
        {
            var map = new Map2D<float>();
            map[-4, -4] = 4.0f;
            Assert.That(Math.Abs(map[-4, -4] - 4.0f) < 0.001);
        }

        [Test]
        public void CanEnumerateRanges()
        {
            var map = new Map2D<float>();
            map[1, 2] = 4.0f;

            Assert.That(map.Within(0, 0, 4, 4).Any());
            Assert.That(map.Within(0, 0, 4, 4).Any(i => Math.Abs(i - 4.0f) < 0.001));
        }

        [Test]
        public void CanAccessRandomly()
        {
            var map = new Map2D<float>();

            map[-8, -8] = 4.0f;
            map[-240, -778] = 8.0f;
            map[8, 8] = 16.0f;
            map[240, 778] = 32.0f;

            Assert.That(Math.Abs(map[-240,-778] - 8) < 0.001);
            Assert.That(Math.Abs(map[240, 778] - 32.0f) < 0.001);
        }

        [Test]
        public void SparseCreation()
        {
            var map = new Map2D<float>(16,16);

            map[-8, -8] = 4.0f;
            map[-1024, -887] = 8.0f;

            // With only two areas in memory, we only have (16*16)*2 blocks.
            Assert.AreEqual(512, map.Count);
        }

        [Test]
        public void CanUseContains()
        {
            var map = new Map2D<float>();
            map[1, 2] = 4.0f;

            Assert.That(map.Contains(4.0f));
            Assert.That(map.Contains(4.0f, new EqualityLambda<float>((a, b) => Math.Abs(a - b) < 0.001)));
        }

        [Test]
        public void ChunkGathering()
        {
            Assert.Fail("Test not written");
        }

        [Test]
        public void SupportsUnloading()
        {
            Assert.Fail("Test not written");
        }

        [Test]
        public void SupportsUnloadingOutsideArea()
        {
            var map = new Map2D<float>(16,16);
            map[4, 4] = 2.0f;
            map[63, 63] = 4.0f;

            // Two chunks loaded
            Assert.AreEqual((16*16)*2, map.Count);

            map.UnloadAreaOutside(0, 0, 15, 15);

            // Ony one chunk left
            Assert.AreEqual((16*16), map.Count);

            // Non-zero test

            map[0, 0] = 2.0f;
            map[16, 16] = 2.0f;
            map[32, 32] = 4.0f;
            map[48, 48] = 8.0f;
            map[64, 64] = 16.0f;
            map[80, 80] = 32.0f;
            map[96, 96] = 64.0f;
            map[128, 128] = 128.0f;

            Assert.AreEqual((16*16)*8, map.Count);

            map.UnloadAreaOutside(48, 48, 80, 80);

            Assert.AreEqual((16*16)*3, map.Count);
        }
    }

    #region Test Helpers

    internal class EqualityLambda<T> : EqualityComparer<T>
    {
        private readonly Func<T, T, bool> _comparer;

        public EqualityLambda(Func<T, T, bool> comparer)
        {
            _comparer = comparer;
        }

        public override bool Equals(T x, T y)
        {
            return _comparer(x, y);
        }

        public override int GetHashCode(T obj)
        {
            return obj.GetHashCode();
        }
    }

    internal struct StructItem
    {
        public int ItemId;
    }

    internal class ClassItem
    {
        public int ItemId;
    }

    #endregion
}