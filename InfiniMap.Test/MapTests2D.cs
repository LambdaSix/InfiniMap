using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace InfiniMap.Test
{
    [TestFixture]
    public class MapTestsSerialization2D
    {
        [Test]
        public void MapSerializer()
        {
            var map = new Map2D<float>();

            map[0, 0] = 1.0f;        // Chunk: (0,0,0)
            map[16, 16] = 2.0f;     // Chunk: (1,1,1)
            map[32, 32] = 4.0f;     // Chunk: (2,2,2)

            var list = new List<Tuple<long, long, long>>();

            map.RegisterWriter((xyz, tuple) =>
            {
                Console.WriteLine("Writing: ({0},{1},{2})", tuple.Item1, tuple.Item2, tuple.Item3);
                list.Add(tuple);
            });

            map.UnloadArea(0, 0, 32, 32);

            Assert.AreEqual(3, list.Count);

            Assert.That(list.Contains(Tuple.Create(0L, 0L, 0L)));
            Assert.That(list.Contains(Tuple.Create(1L, 1L, 0L)));
            Assert.That(list.Contains(Tuple.Create(2L, 2L, 0L)));

            map.UnregisterWriter();
            map[48, 48] = 4.0f;

            map.UnloadArea(0, 0, 48, 48);
            Assert.AreEqual(3, list.Count);
        }

        [Test]
        public void MapDeserializer()
        {
            var map = new Map2D<float>();
            int i = 0;

            // Assert that the Reader function is called for each new chunk loaded into memory
            map.RegisterReader(tuple =>
            {
                i++;
                return Enumerable.Empty<float>();
            });

            map[0, 0] = 1.0f;        // Chunk: (0,0,0)
            map[16, 16] = 2.0f;      // Chunk: (1,1,0)
            map[32, 32] = 4.0f;      // Chunk: (2,2,0)

            Assert.AreEqual(3, i);

            map.UnregisterReader();
            map[48, 48] = 8.0f;

            // Assert that after unregistering, the callback is not invoked.
            Assert.AreEqual(3, i);
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
            var map = new Map2D<float>(16, 16);

            map[1, 1] = 2.0f;
            map[63, 63] = 4.0f;
            map[1, 127] = 8.0f;

            // Assert we have 3 chunks in memory.
            Assert.AreEqual((16 * 16) * 3, map.Count);

            // A single chunk
            {
                var chunksFound = map.ChunksWithin(0, 0, 15, 15, createIfNull: false).ToList();
                Assert.AreEqual(1, chunksFound.Count());
                Assert.AreEqual(0, chunksFound.Select(s => s.Item1).First());

                // Assert that it is the correct chunk
                Assert.That(chunksFound.ElementAt(0).Item3.Contains(2.0f));
            }

            // Two chunks
            {
                var chunksFound = map.ChunksWithin(0, 0, 63, 63, createIfNull: false).ToList();
                Assert.AreEqual(2, chunksFound.Count);
                Assert.AreEqual(3, chunksFound.Select(s => s.Item1).ElementAt(1));
                
                // Assert that these are the correct chunks.
                Assert.That(chunksFound.ElementAt(0).Item3.Contains(2.0f));
                Assert.That(chunksFound.ElementAt(1).Item3.Contains(4.0f));
            }
        }

        [Test]
        public void SupportsUnloading()
        {
            var map = new Map2D<float>();

            map[0, 0] = 2.0f;
            map[16, 16] = 2.0f;
            map[33, 33] = 4.0f;

            Assert.AreEqual((16 * 16) * 3, map.Count);

            map.UnloadArea(0, 0, 33, 33);

            Assert.AreEqual(0, map.Count);
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
}