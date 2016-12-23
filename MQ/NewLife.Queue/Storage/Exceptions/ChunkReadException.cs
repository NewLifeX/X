using System;

namespace NewLife.Queue.Storage.Exceptions
{
    public class ChunkReadException : Exception
    {
        public ChunkReadException(string message) : base(message) { }
    }
}
