using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace InfiniMap.Test
{
    [TestFixture]
    public class MapTests
    {
        [Test]
        public void CanInsertItems()
        {
            // Insert structs
            var mapStruct = new ChunkMap<StructItem>();
            mapStruct[4, 4] = new StructItem() {ItemId = 4};
            Assert.That(mapStruct[4, 4].ItemId == 4);

            // Insert primitives
            var mapPrim = new ChunkMap<float>();
            mapPrim[4, 4] = 4.0f;
            Assert.That(Math.Abs(mapPrim[4, 4] - 4.0f) < 0.001);

            // Insert classes
            var mapClass = new ChunkMap<ClassItem>();
            mapClass[4, 4] = new ClassItem() {ItemId = 4};
            Assert.That(mapClass[4, 4].ItemId == 4);
        }

        [Test]
        public void CanUseNegativePositions()
        {
            var map = new ChunkMap<float>();
            map[-4, -4] = 4.0f;
            Assert.That(Math.Abs(map[-4, -4] - 4.0f) < 0.001);
        }

        [Test]
        public void CanEnumerateRanges()
        {
            var map = new ChunkMap<float>();
            map[1, 2] = 4.0f;

            Assert.That(map.Within(0, 0, 4, 4).Any());
            Assert.That(map.Within(0, 0, 4, 4).Any(i => Math.Abs(i - 4.0f) < 0.001));
        }

        [Test]
        public void CanAccessRandomly()
        {
            var map = new ChunkMap<float>();

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
            var map = new ChunkMap<float>();

            map[-8, -8] = 4.0f;
            map[-1024, -887] = 8.0f;

            // With only two areas in memory, we only have (16*16)*2 blocks.
            Assert.That(map.Count == 512);
        }

        [Test]
        public void CanUseContains()
        {
            var map = new ChunkMap<float>();
            map[1, 2] = 4.0f;

            Assert.That(map.Contains(4.0f));
            Assert.That(map.Contains(4.0f, new EqualityLambda<float>((a, b) => Math.Abs(a - b) < 0.001)));
        }

        #region Test Helpers

        internal class EqualityLambda<T> : EqualityComparer<T>
        {
            private readonly Func<T, T, bool> _comparer;

            public EqualityLambda(Func<T,T,bool> comparer)
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

        private struct StructItem
        {
            public int ItemId;
        }

        private class ClassItem
        {
            public int ItemId;
        }

        #endregion
    }
}