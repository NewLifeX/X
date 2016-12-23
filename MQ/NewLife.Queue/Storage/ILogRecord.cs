using System.IO;

namespace NewLife.Queue.Storage
{
    public interface ILogRecord
    {
        void WriteTo(long logPosition, BinaryWriter writer);
        void ReadFrom(byte[] recordBuffer);
    }
}
