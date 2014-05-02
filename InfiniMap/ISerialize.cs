using System.IO;

namespace InfiniMap
{
    public interface ISerialize
    {
        void Write(BinaryWriter stream);
    }

    public interface IDeserialize
    {
        void Read(Stream stream);
    }
}