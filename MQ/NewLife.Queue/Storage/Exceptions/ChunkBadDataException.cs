using System;

namespace NewLife.Queue.Storage.Exceptions
{
    public class ChunkBadDataException : Exception
    {
        public ChunkBadDataException(string message) : base(message)
        {
        }
    }
}
