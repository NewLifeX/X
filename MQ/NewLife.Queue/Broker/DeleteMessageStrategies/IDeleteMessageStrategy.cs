using System.Collections.Generic;
using NewLife.Queue.Storage;

namespace NewLife.Queue.Broker.DeleteMessageStrategies
{
    public interface IDeleteMessageStrategy
    {
        IEnumerable<Chunk> GetAllowDeleteChunks(ChunkManager chunkManager, long maxMessagePosition);
    }
}
