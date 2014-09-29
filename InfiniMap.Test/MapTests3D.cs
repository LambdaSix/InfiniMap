using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace InfiniMap.Test
{

    [TestFixture]
    public class MapTestsSerialization3D
    {
        [Test]
        public void MapSerializer()
        {
            var map = new Map3D<float>();

            map[0, 0, 0] = 1.0f;        // Chunk: (0,0,0)
            map[16, 16, 16] = 2.0f;     // Chunk: (1,1,1)
            map[32, 32, 32] = 4.0f;     // Chunk: (2,2,2)

            var list = new List<Tuple<long, long, long>>();

            map.RegisterWriter((xyz, tuple) => {
                                   Console.WriteLine("Writing: ({0},{1},{2})", tuple.Item1, tuple.Item2, tuple.Item3);
                                   list.Add(tuple);
                               });

            map.UnloadArea(0, 0, 0, 32, 32, 32);

            Assert.AreEqual(3, list.Count);

            Assert.That(list.Contains(Tuple.Create(0L, 0L, 0L)));
            Assert.That(list.Contains(Tuple.Create(1L, 1L, 1L)));
            Assert.That(list.Contains(Tuple.Create(2L, 2L, 2L)));

            map.UnregisterWriter();
            map[48, 48, 48] = 4.0f;
            
            map.UnloadArea(0, 0, 0, 48, 48, 48);
            Assert.AreEqual(3, list.Count);
        }

        [Test]
        public void MapDeserializer()
        {
            var map = new Map3D<float>();
            int i = 0;

            // Assert that the Reader function is called for each new chunk loaded into memory
            map.RegisterReader(tuple => {
                                   i++;
                                   return Enumerable.Empty<float>();
                               });

            map[0, 0, 0] = 1.0f;        // Chunk: (0,0,0)
            map[16, 16, 16] = 2.0f;     // Chunk: (1,1,1)
            map[32, 32, 32] = 4.0f;     // Chunk: (2,2,2)

            Assert.AreEqual(3, i);

            map.UnregisterReader();
            map[48, 48, 48] = 8.0f;

            // Assert that after unregistering, the callback is not invoked.
            Assert.AreEqual(3, i);
        }
    }

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

                IEnumerable<long> zSequences = new List<long> { 0, 1, 2 };
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
                var zSequences = new List<long> { 1, 2 };
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
            var map = new Map3D<float>(16, 16, 16);
            map[4, 4, 0] = 2.0f;
            map[63, 63, 0] = 4.0f;

            // Two chunks loaded
            Assert.AreEqual((16 * 16 * 16) * 2, map.Count);

            map.UnloadAreaOutside(0, 0, 0, 15, 15, 0);

            // Ony one chunk left
            Assert.AreEqual((16 * 16 * 16), map.Count);

            // Non-zero test

            map[0, 0, 0] = 2.0f;
            map[16, 16, 0] = 2.0f;
            map[32, 32, 0] = 4.0f;
            map[48, 48, 7] = 8.0f;
            map[64, 64, 15] = 16.0f;
            map[80, 80, 15] = 32.0f;
            map[96, 96, 0] = 64.0f;
            map[128, 128, 0] = 128.0f;

            Assert.AreEqual((16 * 16 * 16) * 8, map.Count);

            map.UnloadAreaOutside(48, 48, 0, 80, 80, 15);

            Assert.AreEqual((16 * 16 * 16) * 3, map.Count);
        }
    }

    [TestFixture]
    public class MapTestEntities3D
    {
        [Test]
        public void SupportsAddingEntities()
        {
            var map = new Map3D<float>(16, 16, 16);

            var oldBoot = new Entity { Name = "Old Boot" };

            map.PutEntity(1, 1, 1, oldBoot);

            Assert.That(map.GetEntitiesAt(1, 1, 1).Any());
            Assert.That(oldBoot.X == 1 && oldBoot.Y == 1);
        }

        [Test]
        public void SupportsQuantumEntanglementPrevention()
        {
            var map = new Map3D<float>(16, 16, 16);

            var oldBoot = new Entity { Name = "Old Boot" };

            map.PutEntity(1, 1, 1, oldBoot);

            Assert.That(map.GetEntitiesAt(1, 1, 1).Any());
            Assert.That(oldBoot.X == 1 && oldBoot.Y == 1);

            // Put the boot somewhere else, moving it.
            map.PutEntity(17, 17, 17, oldBoot);
            oldBoot.Name = "Spooky Old Boot";

            Assert.That(map.GetEntitiesAt(1, 1, 1).Any() == false);
            Assert.That(map.GetEntitiesAt(17, 17, 17).Any());
        }

        [Test]
        public void SupportsAddingAndRemovingEntities()
        {
            var map = new Map3D<float>(16, 16, 16);

            var oldBoot = new Entity { Name = "Old Boot" };

            map.PutEntity(1, 1, 1, oldBoot);

            Assert.That(map.GetEntitiesAt(1, 1, 1).Any());
            Assert.That(oldBoot.X == 1 && oldBoot.Y == 1);

            map.RemoveEntity(oldBoot);
            Assert.That(map.GetEntitiesAt(1, 1, 1).Any() == false);

            // After removing, items have null co-ordinates.
            Assert.That(oldBoot.X == null);
            Assert.That(oldBoot.Y == null);
            Assert.That(oldBoot.Z == null);
        }

        [Test]
        public void SupportsGettingEntitiesInLocalArea()
        {
            var map = new Map3D<float>(16, 16, 16);

            var oldBoot = new Entity { Name = "Old Boot" };
            var oldCan = new Entity { Name = "Old Boot" };
            var oldBucket = new Entity { Name = "Old Boot" };

            map.PutEntity(1, 1, 1, oldBoot);
            map.PutEntity(1, 1, 1, oldBucket);
            map.PutEntity(1, 1, 1, oldCan);

            Assert.That(map.GetEntitiesAt(1, 1, 1).Any());
            Assert.That(oldBoot.X == 1 && oldBoot.Y == 1);

            Assert.That(map.GetEntitiesInChunk(1, 1, 1).Count() == 3);
        }

        [Test]
        public void SupportsAddingAndMovingEntities()
        {
            var map = new Map3D<float>(16, 16, 16);

            var oldBoot = new Entity { Name = "Old Boot" };

            map.PutEntity(1, 1, 1, oldBoot);

            Assert.That(map.GetEntitiesAt(1, 1, 1).Any());
            Assert.That(oldBoot.X == 1 && oldBoot.Y == 1);

            map.PutEntity(17, 17, 17, oldBoot);
            Assert.That(map.GetEntitiesAt(1, 1, 1).Any() == false);
            Assert.That(map.GetEntitiesAt(17, 17, 17).Any());
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