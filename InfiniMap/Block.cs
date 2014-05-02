using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;

namespace InfiniMap
{
    public struct StreamInfo
    {
        public readonly byte[] BsonBuffer;

        public StreamInfo(byte[] bytes)
        {
            BsonBuffer = bytes;
        }

        public UInt32 GetOffset(long startPosition)
        {
            return (uint)startPosition + (uint)BsonBuffer.Length;
        }
    }

    public class BlockMetadata : DynamicObject
    {
        public Dictionary<string, dynamic> Dictionary;

        public BlockMetadata()
        {
            Dictionary = new Dictionary<string, dynamic>();
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            return Dictionary.TryGetValue(binder.Name, out result);
        }

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            if (value.GetType().IsPrimitive || value is string || value is DateTime)
            {
                Dictionary[binder.Name] = value;
                return true;
            }

            throw new NotSupportedException("Can only store primitive or string types as a value");
        }

        public StreamInfo Write()
        {
            if (Dictionary.Count >= 1)
            {
                var stream = new MemoryStream();
                var serializer = new JsonSerializer();
                var writer = new BsonWriter(stream);

                serializer.Serialize(writer, Dictionary);
                stream.Seek(0, SeekOrigin.Begin);
                return new StreamInfo(stream.GetBuffer());
            }
            else
            {
                return new StreamInfo(Enumerable.Empty<byte>().ToArray());
            }
        }
    }

    public class Block : ISerialize, IDeserialize
    {
        /// <summary>
        /// Combined BlockId and BlockMeta.
        /// </summary>
        public UInt32 BlockData;

        /// <summary>
        /// Holds the block ID
        /// </summary>
        public UInt16 BlockId { get; set; }

        /// <summary>
        /// MetaId attached to the block.
        /// </summary>
        public UInt16 BlockMeta { get; set; }

        /// <summary>
        /// Quick access for a small set of block Flags
        /// </summary>
        public uint Flags;

        /// <summary>
        /// Location in chunk metadata file for optional data
        /// </summary>
        public uint TagDataLocation;

        /// <summary>
        /// Contains optional extended properties for this specific block instance.
        /// </summary>
        public BlockMetadata ExtendedMetadata = new BlockMetadata();

        public dynamic Metadata
        {
            get
            {
                if (ExtendedMetadata == null)
                {
                    throw new NullReferenceException("ExtendedMetaData was null");
                }
                return ExtendedMetadata;
            }
        }

        public Block() : this(0,0) { }

        public Block(UInt32 blockData, UInt32 flags)
        {
            BlockData = blockData;
            Flags = flags;
            TagDataLocation = 0;
        }

        public void SetTagOffset(uint position)
        {
            TagDataLocation = position;
        }

        public void Write(BinaryWriter stream)
        {
            stream.Write(BlockData);
            stream.Write(Flags);
            stream.Write(TagDataLocation);
        }

        public void Read(Stream stream)
        {
            using (var r = new BinaryReader(stream))
            {
                BlockData = r.ReadUInt32();
                Flags = r.ReadUInt32();
                TagDataLocation = r.ReadUInt32();
            }
        }
    }
}