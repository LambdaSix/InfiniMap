using System.IO;

namespace InfiniMap
{
    public interface ISerialize
    {
        void Write(Stream stream);
    }

    public interface IDeserialize
    {
        void Read(Stream stream);
    }
}