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

            var list = new List<ChunkSpace>();

            map.RegisterWriter((xyz, items) =>
            {
                Console.WriteLine("Writing: ({0},{1},{2})", xyz.X, xyz.Y, xyz.Z);
                list.Add(xyz);
            });

            map.UnloadArea(new WorldSpace2D(0, 0), new WorldSpace2D(32, 32));

            Assert.AreEqual(3, list.Count);

            Assert.That(list.Contains(new ChunkSpace(0L, 0L, 0L)));
            Assert.That(list.Contains(new ChunkSpace(1L, 1L, 0L)));
            Assert.That(list.Contains(new ChunkSpace(2L, 2L, 0L)));

            map.UnregisterWriter();
            map[48, 48] = 4.0f;

            map.UnloadArea((0, 0), (48, 48));
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

            var begin = new WorldSpace2D(0, 0);
            var end = new WorldSpace2D(4, 4);

            Assert.That(map.Within(begin,end).Any());
            Assert.That(map.Within(begin, end).Any(i => Math.Abs(i - 4.0f) < 0.001));
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
            map[31, 31] = 4.0f;

            // Assert we have 2 chunks in memory.
            Assert.AreEqual((16 * 16) * 2, map.Count);

            // A single chunk
            {
                var chunksFound = map.ChunksWithin(0, 0, 15, 15, createIfNull: false).ToList();
                Assert.AreEqual(1, chunksFound.Count());
                Assert.AreEqual(0, chunksFound.Select(s => s.Item1.X).First());

                // Assert that it is the correct chunk
                Assert.That(chunksFound.ElementAt(0).Item2.Contains(2.0f));
            }

            // Two chunks
            {
                var chunksFound = map.ChunksWithin(0, 0, 31, 31, createIfNull: false).ToList();
                Assert.AreEqual(2, chunksFound.Count);
                Assert.AreEqual(1, chunksFound.Select(s => s.Item1.X).ElementAt(1));
                
                // Assert that these are the correct chunks.
                Assert.That(chunksFound.ElementAt(0).Item2.Contains(2.0f));
                Assert.That(chunksFound.ElementAt(1).Item2.Contains(4.0f));
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

            var begin = new WorldSpace2D(0, 0);
            var end = new WorldSpace2D(33, 33);

            map.UnloadArea(begin, end);

            Assert.AreEqual(0, map.Count);
        }

        [Test]
        public void SupportsUnloadingWithPersistance()
        {
            var map = new Map2D<float>();

            map[0, 0] = 2.0f;
            map[16, 16] = 2.0f;
            map[33, 33] = 4.0f;

            Assert.AreEqual((16 * 16) * 3, map.Count);

            map.MakePersistant((1, 1));

            var begin = new WorldSpace2D(0, 0);
            var end = new WorldSpace2D(33, 33);

            map.UnloadArea(begin, end);

            // One chunk left
            Assert.AreEqual((16*16), map.Count);
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

    [TestFixture]
    public class MapTestEntities2D
    {
        [Test]
        public void SupportsAddingEntities()
        {
            var map = new Map2D<float>(16, 16);

            var oldBoot = new Entity { Name = "Old Boot" };

            map.PutEntity(1, 1, oldBoot);

            Assert.That(map.GetEntitiesAt(1, 1).Any());
            Assert.That(oldBoot.X == 1 && oldBoot.Y == 1);
        }

        [Test]
        public void SupportsQuantumEntanglementPrevention()
        {
            var map = new Map2D<float>(16, 16);

            var oldBoot = new Entity { Name = "Old Boot" };

            map.PutEntity(1, 1, oldBoot);

            Assert.That(map.GetEntitiesAt(1, 1).Any());
            Assert.That(oldBoot.X == 1 && oldBoot.Y == 1);

            // Put the boot somewhere else, moving it.
            map.PutEntity(17, 17, oldBoot);
            oldBoot.Name = "Spooky Old Boot";

            Assert.That(map.GetEntitiesAt(1, 1).Any() == false);
            Assert.That(map.GetEntitiesAt(17, 17).Any());
        }

        [Test]
        public void SupportsAddingAndRemovingEntities()
        {
            var map = new Map2D<float>(16, 16);

            var oldBoot = new Entity { Name = "Old Boot" };

            map.PutEntity(1, 1, oldBoot);

            Assert.That(map.GetEntitiesAt(1, 1).Any());
            Assert.That(oldBoot.X == 1 && oldBoot.Y == 1);

            map.RemoveEntity(oldBoot);
            Assert.That(map.GetEntitiesAt(1, 1).Any() == false);
            
            // After removing, items have null coordinates.
            Assert.That(oldBoot.X == null);
            Assert.That(oldBoot.Y == null);
            Assert.That(oldBoot.Z == null);
        }

        [Test]
        public void SupportsGettingEntitiesInLocalArea()
        {
            var map = new Map2D<float>(16, 16);

            var oldBoot = new Entity { Name = "Old Boot" };
            var oldCan = new Entity { Name = "Old Boot" };
            var oldBucket = new Entity { Name = "Old Boot" };

            map.PutEntity(1, 1, oldBoot);
            map.PutEntity(1, 1, oldBucket);
            map.PutEntity(1, 1, oldCan);

            Assert.That(map.GetEntitiesAt(1, 1).Any());
            Assert.That(oldBoot.X == 1 && oldBoot.Y == 1);

            Assert.That(map.GetEntitiesInChunk(1, 1).Count() == 3);
        }

        [Test]
        public void SupportsAddingAndMovingEntities()
        {
            var map = new Map2D<float>(16, 16);

            var oldBoot = new Entity { Name = "Old Boot" };

            map.PutEntity(1, 1, oldBoot);

            Assert.That(map.GetEntitiesAt(1, 1).Any());
            Assert.That(oldBoot.X == 1 && oldBoot.Y == 1);

            map.PutEntity(17, 17, oldBoot);
            Assert.That(map.GetEntitiesAt(1, 1).Any() == false);
            Assert.That(map.GetEntitiesAt(17, 17).Any());
            Assert.That(oldBoot.X == 17 && oldBoot.Y == 17);
        }

        public class Entity : IEntityLocationData
        {
            public long? X { get; set; }
            public long? Y { get; set; }
            public long? Z { get; set; }

            public string Name { get; set; }
        }
    }
}