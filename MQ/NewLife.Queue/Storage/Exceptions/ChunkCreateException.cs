using System;

namespace NewLife.Queue.Storage.Exceptions
{
    public class ChunkCreateException : Exception
    {
        public ChunkCreateException(string message) : base(message) { }
    }
}
