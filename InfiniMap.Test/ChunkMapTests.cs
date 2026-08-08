using System.Linq;
using NUnit.Framework;

namespace InfiniMap.Test
{
    [TestFixture]
    public class ChunkMapTests
    {
        [Test]
        public void TestChunksWithin()
        {
            var map = new Map2D<int>(1, 1);
            
            map[0, 0] = 1; // 0,0
            map[1, 1] = 2; // 1,1
            map[2, 2] = 3; // 2,2
            map[3, 3] = 4; // 3,3

            map[-1, -1] = -1; //-1,-1
            map[-2, -2] = -2; // -2,-2
            map[-3, -3] = -3; // -3,-3
            map[-4, -4] = -4; // -4,-4

            var chunks = map.ChunksWithin(-4, -4, 4, 4, false);
            Assert.That(chunks.Count() == 8);
        }
    }
}