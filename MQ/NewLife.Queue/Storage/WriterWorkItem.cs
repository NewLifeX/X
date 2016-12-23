using System.IO;

namespace NewLife.Queue.Storage
{
    internal class WriterWorkItem
    {
        public readonly MemoryStream BufferStream;
        public readonly BinaryWriter BufferWriter;
        public readonly IStream WorkingStream;
        public long LastFlushedPosition;

        public WriterWorkItem(IStream stream)
        {
            WorkingStream = stream;
            BufferStream = new MemoryStream(8192);
            BufferWriter = new BinaryWriter(BufferStream);
        }

        public void AppendData(byte[] buf, int offset, int len)
        {
            WorkingStream.Write(buf, offset, len);
        }
        public void FlushToDisk()
        {
            WorkingStream.Flush();
            LastFlushedPosition = WorkingStream.Position;
        }
        public void ResizeStream(long length)
        {
            WorkingStream.SetLength(length);
        }
        public void Dispose()
        {
            WorkingStream.Dispose();
        }
    }
}
