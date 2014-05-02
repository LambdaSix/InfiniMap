using System;
using System.IO;

namespace InfiniMap
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Map map = new Map(128, 128);

            // Negative access!

            for (int x = -512; x < 1024; x++)
            {
                for (int y = -1024; y < 256; y++)
                {
                    map[x, y] = new Block {blockId = 1, flags = fromId(1)};
                }
            }

            // Look ma, sparse access!

            for (int x = 2048; x < 2048 + (128*3); x++)
            {
                for (int y = 2048; y < 2048 + (127*3); y++)
                {
                    map[x, y] = new Block {blockId = 1, flags = fromId(1)};
                }
            }

            WriteMap(map);

            // ~ 16MB per chunk, 160MB for 9 chunks (3x3 grid) (384x384x384)
        }

        public static void WriteMap(Map map)
        {
            foreach (var chunk in map.Chunks)
            {
                WriteChunk(
                    new FileStream(String.Format("map/chunk_{0}_{1}.bin", chunk.Key.Item1, chunk.Key.Item2), FileMode.Create),
                    chunk.Value);
            }
        }

        public static void WriteChunk(Stream fileStream, Chunk chunk)
        {
            using (var stream = new BinaryWriter(fileStream))
            {
                for (int x = 0; x < chunk.Height; x++)
                {
                    for (int y = 0; y < chunk.Width; y++)
                    {
                        stream.Write(chunk[x, y].blockData);
                        stream.Write((Int32) chunk[x, y].flags);
                        stream.Write(chunk[x, y].TagDataLocation);
                    }
                }
            }
        }

        public static uint fromId(short id)
        {
            switch (id)
            {
                default:
                    return (uint)BlockFlags.Air;
            }
        }
    }
}