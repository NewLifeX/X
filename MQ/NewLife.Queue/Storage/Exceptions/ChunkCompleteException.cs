using System;

namespace NewLife.Queue.Storage.Exceptions
{
    public class ChunkCompleteException : Exception
    {
        public ChunkCompleteException(string message) : base(message) { }
    }
}
