using System;

namespace NewLife.Queue.Storage.Exceptions
{
    public class ChunkNotExistException : Exception
    {
        public ChunkNotExistException(long position, int chunkNum) : base(string.Format("Chunk not exist, position: {0}, chunkNum: {1}", position, chunkNum)) { }
    }
}
