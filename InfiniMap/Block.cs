using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Runtime.InteropServices;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using Newtonsoft.Json.Linq;

namespace InfiniMap
{
    /// <summary>
    /// 
    /// </summary>
    public class BlockMetadata : DynamicObject
    {
        private Dictionary<string, dynamic> _dictionary;

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            return _dictionary.TryGetValue(binder.Name, out result);
        }

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            if (value.GetType().IsPrimitive || value is string || value is DateTime)
            {
                _dictionary[binder.Name] = value;
                return true;
            }

            throw new NotSupportedException("Can only store primitive or string types as a value");
        }

        public byte[] Write()
        {
            var stream = new MemoryStream();
            JsonSerializer ser = new JsonSerializer();

            BsonWriter writer = new BsonWriter(stream);

            ser.Serialize(writer, _dictionary);
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct Block : ISerialize, IDeserialize
    {
        /// <summary>
        /// Combined blockId and blockMeta.
        /// </summary>
        [FieldOffset(0)]
        public UInt32 blockData;

        /// <summary>
        /// 
        /// </summary>
        [FieldOffset(0)]
        public UInt16 blockId;

        /// <summary>
        /// MetaId attached to the block.
        /// </summary>
        [FieldOffset(2)]
        public UInt16 blockMeta;

        /// <summary>
        /// Quick access for a small set of block flags
        /// </summary>
        [FieldOffset(4)]
        public uint flags;

        /// <summary>
        /// Location in chunk metadata file for optional data
        /// </summary>
        [FieldOffset(8)]
        public uint TagDataLocation;

        /// <summary>
        /// Contains optional extended properties for this specific block instance.
        /// </summary>
        [FieldOffset(12)]
        private BlockMetadata ExtendedMetadata;

        public dynamic Metadata { get { return ExtendedMetadata; } }

        public void Write(Stream stream)
        {
            using (var w = new BinaryWriter(stream))
            {
                w.Write(blockData);
                w.Write(flags);
                w.Write(TagDataLocation);
            }
        }

        public void Read(Stream stream)
        {
            using (var r = new BinaryReader(stream))
            {
                blockData = r.ReadUInt32();
                flags = r.ReadUInt32();
                TagDataLocation = r.ReadUInt32();
            }
        }
    }
}