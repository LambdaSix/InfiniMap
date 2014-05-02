using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;

namespace InfiniMap.Test
{
    [TestFixture]
    public class BlockMetadataTests
    {
        [Test]
        public void OnlyStoresPrimitives()
        {
            dynamic metadata = new BlockMetadata();

            // int[16,32,64]
            Assert.DoesNotThrow(() => metadata.Int16 = Int16.MaxValue - 1);
            Assert.DoesNotThrow(() => metadata.Int32 = Int32.MaxValue - 1);
            Assert.DoesNotThrow(() => metadata.Int64 = Int64.MaxValue - 1);

            // float
            Assert.DoesNotThrow(() => metadata.Float = 1.4f);

            // double
            Assert.DoesNotThrow(() => metadata.Double = 1.8d);

            // string
            Assert.DoesNotThrow(() => metadata.String = "Hello World");

            // date
            Assert.DoesNotThrow(() => metadata.Date = DateTime.Now);

            // boolean
            Assert.DoesNotThrow(() => metadata.Boolean = true);

            // char
            Assert.DoesNotThrow(() => metadata.Char = 'c');

            foreach (var pair in ((BlockMetadata)metadata).Dictionary)
            {
                Console.WriteLine("{0}:({1}){2}", pair.Key, pair.Value.GetType().Name, pair.Value);
            }
        }

        [Test]
        public void DoesNotStoreObjects()
        {
            dynamic metadata = new BlockMetadata();

            Assert.Throws<NotSupportedException>(() => metadata.AnonymousObject = new {Property = "Property"});
        }

        [Test]
        public void MetdataWritesCorrectly()
        {
            dynamic md = new BlockMetadata();
            md.StringProperty = "String value";
            md.DoubleProperty = 1.3d;
            md.UInt32Property = (Int64)(UInt32.MaxValue);
            md.Int32Property = 8192;
            md.LongProperty = Int64.MaxValue;

            Console.WriteLine("Before BSON roundtrip");

            foreach (var pair in ((BlockMetadata)md).Dictionary)
            {
                Console.WriteLine("{0}:({1}){2}", pair.Key, pair.Value.GetType().Name, pair.Value);
            }

            var buffer = ((BlockMetadata) md).Write();

            Console.WriteLine("Size of BSON buffer: {0}bytes", buffer.BsonBuffer.Length);

            var serializer = new JsonSerializer();
            BsonReader reader = new BsonReader(new MemoryStream(buffer.BsonBuffer));
            var deserializedData = serializer.Deserialize<Dictionary<string, object>>(reader);

            ((BlockMetadata) md).Dictionary = deserializedData;

            Assert.That(md.StringProperty == "String value");
            Assert.That(md.DoubleProperty == 1.3d);
            Assert.That(md.UInt32Property == UInt32.MaxValue);
            Assert.That(md.Int32Property == 8192);
            Assert.That(md.LongProperty == Int64.MaxValue);

            Console.WriteLine("\nAfter BSON roundtrip");

            foreach (var pair in ((BlockMetadata)md).Dictionary)
            {
                Console.WriteLine("{0}:({1}){2}", pair.Key, pair.Value.GetType().Name, pair.Value);
            }
        }

        [Test]
        public void ChunkMetadataFitsInOffset()
        {
            // This is a terrible terrible test
            // TODO: Refactor

            Map map = new Map(128, 128);
            Random rand = new Random((int) DateTime.Now.Ticks);

            for (int x = 0; x <= 127; x++)
            {
                for (int y = 0; y <= 127; y++)
                {
                    map[x, y].BlockId = (ushort)rand.Next(0, Int32.MaxValue);
                    map[x, y].Flags = (uint)rand.Next();
                    map[x, y].Metadata.timeToNextTick = DateTime.Now + TimeSpan.FromMinutes(rand.Next(0,20));
                    map[x, y].Metadata.hasTicked = false;
                    map[x, y].Metadata.growthStage = rand.Next(0, 8);
                }
            }

            Console.WriteLine("Created {0} blocks over {1} chunks", map.Chunks.Sum(pair => pair.Value.Blocks.Length),
                              map.Chunks.Count);

            Console.WriteLine("Number of filled blocks: {0}",
                              map.Chunks.Select(pair => pair.Value.Blocks.Where(block => block.BlockData != 0)).Count());

            foreach (var chunk in map.Chunks)
            {
                var startRegionX = chunk.Key.Item1*chunk.Value.Width;
                var startRegionY = chunk.Key.Item2*chunk.Value.Height;

                var endRegionX = startRegionX + chunk.Value.Width;
                var endRegionY = startRegionY + chunk.Value.Height;

                Console.WriteLine("Chunk: {0},{1} ({2},{3} to {4},{5})", chunk.Key.Item1, chunk.Key.Item2, startRegionX,
                                  startRegionY, endRegionX, endRegionY);
            }

            var stream = new MemoryStream();

            foreach (var chunk in map.Chunks)
            {
                foreach (var block in chunk.Value.Blocks)
                {
                    var streamBuf = ((BlockMetadata) block.Metadata).Write();

                    Assert.That(stream.Position <= UInt32.MaxValue);
                    
                    block.SetTagOffset(streamBuf.GetOffset(stream.Position));
                    stream.Write(streamBuf.BsonBuffer, 0, streamBuf.BsonBuffer.Length);
                }
            }

            Console.WriteLine("Total length of metadata: {0} bytes", stream.Length);

            var outStream = new MemoryStream((int) stream.Length);
            using (var zipStream = new GZipStream(outStream, CompressionMode.Compress))
            {
                stream.Seek(0, SeekOrigin.Begin);
                stream.CopyTo(zipStream);
            }

            Console.WriteLine("Total length of zipped metadata: {0} bytes", outStream.ToArray().Length);
            using (var ms = new BinaryWriter(new MemoryStream()))
            {
                foreach (var chunk in map.Chunks)
                {
                    chunk.Value.Write(ms);
                }

                Console.WriteLine("Total length of block data: {0} bytes", ms.BaseStream.Length);

                var blocksOutStream = new MemoryStream((int) ms.BaseStream.Length);

                using (var blockZipStream = new GZipStream(blocksOutStream, CompressionMode.Compress))
                {
                    ms.Seek(0, SeekOrigin.Begin);
                    ms.BaseStream.CopyTo(blockZipStream);
                    blockZipStream.Flush();
                    blockZipStream.Close();
                }

                Console.WriteLine("Total length of zipped block data: {0} bytes", blocksOutStream.ToArray().Length);
            }
        }
    }
}
