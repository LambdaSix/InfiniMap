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
            md.UInt32Property = Int32.MaxValue;
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
    }
}
