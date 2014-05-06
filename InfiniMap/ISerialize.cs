using System.IO;

namespace InfiniMap
{
    public interface ISerialize
    {
        void Write(BinaryWriter stream);
    }

    public interface IDeserialize<out T>
    {
        T Read(BinaryReader stream);
    }

    public interface ISerializeMetadata
    {
        StreamInfo GetMetadata();
    }
}