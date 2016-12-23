using System.IO;
using NewLife.Queue.Utilities;

namespace NewLife.Queue.Storage
{
    public class ChunkFooter
    {
        public const int Size = 128;
        public readonly int ChunkDataTotalSize;

        public ChunkFooter(int chunkDataTotalSize)
        {
            Ensure.Nonnegative(chunkDataTotalSize, "chunkDataTotalSize");
            ChunkDataTotalSize = chunkDataTotalSize;
        }

        public byte[] AsByteArray()
        {
            var array = new byte[Size];
            using (var stream = new MemoryStream(array))
            {
                using (var writer = new BinaryWriter(stream))
                {
                    writer.Write(ChunkDataTotalSize);
                }
            }
            return array;
        }

        public static ChunkFooter FromStream(BinaryReader reader, Stream stream)
        {
            var chunkDataTotalSize = reader.ReadInt32();
            return new ChunkFooter(chunkDataTotalSize);
        }

        public override string ToString()
        {
            return string.Format("[ChunkDataTotalSize:{0}]", ChunkDataTotalSize);
        }
    }
}
