using System;
using NUnit.Framework;

namespace InfiniMap.Test
{
    [TestFixture]
    public class MapTests
    {
        private Map createMap(int chunkSize, int chunksToFill)
        {
            var map = new Map(chunkSize, chunkSize);

            for (int x = 0; x <= (chunksToFill * chunkSize-1); x++)
            {
                for (int y = 0; y <= (chunksToFill * chunkSize-1); y++)
                {
                    map[x, y].BlockId = (ushort) x;
                    map[x, y].BlockMeta = (ushort) y;
                }
            }

            return map;
        }

        [Test]
        public void FreesChunksProperly()
        {
            var map = createMap(16, 1);

            var dictionaryCount = map.Chunks.Count;

            map.UnloadArea(0, 0, 1);

            Assert.That(dictionaryCount > map.Chunks.Count);
        }
    }
}