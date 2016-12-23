using System.Collections.Generic;
using System.Linq;
using NewLife.Queue.Center.Controllers;
using NewLife.Queue.Storage;
using NewLife.Queue.Utilities;

namespace NewLife.Queue.Broker.DeleteMessageStrategies
{
    public class DeleteMessageByCountStrategy : IDeleteMessageStrategy
    {
        /// <summary>表示磁盘上可以保存的最多的Chunk文件的个数；
        /// <remarks>
        /// 比如设置为100，则磁盘上可以保存的最多的Chunk文件的个数为100，如果现在总的个数超过100，则最先产生的Chunk文件就会被删除。
        /// 默认值为100，即如果每个Chunk文件的大小为256MB的话，则100 * 256 = 25GB，即磁盘总共会保存最多默认25GB的消息。
        /// </remarks>
        /// </summary>
        public int MaxChunkCount { get; private set; }

        public DeleteMessageByCountStrategy(int maxChunkCount = 100)
        {
            Ensure.Positive(maxChunkCount, "maxChunkCount");
            MaxChunkCount = maxChunkCount;
        }

        public IEnumerable<Chunk> GetAllowDeleteChunks(ChunkManager chunkManager, long maxMessagePosition)
        {
            var chunks = new List<Chunk>();
            var allCompletedChunks = chunkManager
                .GetAllChunks()
                .Where(x => x.IsCompleted && CheckMessageConsumeOffset(x, maxMessagePosition))
                .OrderBy(x => x.ChunkHeader.ChunkNumber)
                .ToList();

            var exceedCount = allCompletedChunks.Count - MaxChunkCount;
            if (exceedCount <= 0)
            {
                return chunks;
            }

            for (var i = 0; i < exceedCount; i++)
            {
                chunks.Add(allCompletedChunks[i]);
            }

            return chunks;
        }

        private bool CheckMessageConsumeOffset(Chunk currentChunk, long maxMessagePosition)
        {
            if (BrokerService.Instance.Setting.DeleteMessageIgnoreUnConsumed)
            {
                return true;
            }
            return currentChunk.ChunkHeader.ChunkDataEndPosition <= maxMessagePosition;
        }
    }
}
