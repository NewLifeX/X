using System;
using NewLife.Queue.Storage.FileNamingStrategies;
using NewLife.Queue.Utilities;

namespace NewLife.Queue.Storage
{
    public class ChunkManagerConfig
    {
        /// <summary>Chunk文件存储的根目录；
        /// </summary>
        public readonly string BasePath;
        /// <summary>Chunk文件命名规则策略；
        /// </summary>
        public readonly IFileNamingStrategy FileNamingStrategy;
        /// <summary>Chunk文件大小，字节为单位，适用于文件内记录大小不固定的场景，如果是固定大小，则设置为0；
        /// </summary>
        public readonly int ChunkDataSize;
        /// <summary>Chunk文件单条数据大小，字节为单位，适用于每条数据固定大小的场景；
        /// </summary>
        public readonly int ChunkDataUnitSize;
        /// <summary>Chunk文件总数据数，适用于每个数据固定大小的场景；
        /// </summary>
        public readonly int ChunkDataCount;
        /// <summary>Chunk文件刷磁盘的间隔，毫秒为单位；
        /// </summary>
        public readonly int FlushChunkIntervalMilliseconds;
        /// <summary>表示是否同步刷盘；
        /// </summary>
        public readonly bool SyncFlush;
        /// <summary>刷盘方式，是直接将数据刷入磁盘还是先刷入OS；
        /// </summary>
        public FlushOption FlushOption;
        /// <summary>表示是否缓存Chunk，缓存Chunk可以提高消费速度；
        /// </summary>
        public readonly bool EnableCache;
        /// <summary>Chunk文件的BinaryReader的个数；
        /// </summary>
        public readonly int ChunkReaderCount;
        /// <summary>Chunk文件允许最大的记录的大小，字节为单位；
        /// </summary>
        public readonly int MaxLogRecordSize;
        /// <summary>Chunk写入时的缓冲大小，字节为单位；
        /// </summary>
        public readonly int ChunkWriteBuffer;
        /// <summary>Chunk读取时的缓冲大小，字节为单位；
        /// </summary>
        public readonly int ChunkReadBuffer;
        /// <summary>使用的总物理内存上限，如果超过上限，则不允许创建新的Chunk文件；
        /// </summary>
        public readonly int ChunkCacheMaxPercent;
        /// <summary>Chunk文件使用内存的安全水位；低于这个水位，则不需要进行Chunk文件的非托管内存释放处理；高于这个水位，则开始进行Chunk文件的非托管内存释放处理；
        /// </summary>
        public readonly int ChunkCacheMinPercent;
        /// <summary>应用启动时，需要预加载到非托管内存的Chunk文件数；
        /// </summary>
        public readonly int PreCacheChunkCount;
        /// <summary>Chunk文件非活跃时间，单位为秒；
        /// <remarks>
        /// 在释放已完成的Chunk文件的非托管内存时，会根据这个非活跃时间来判断当前Chunk文件是否允许释放内存；
        /// 如果某个已完成并已经有对应非托管内存的Chunk文件有超过这个时间都不活跃，则可以进行非托管内存的释放；
        /// 是否活跃的依据是，只要该Chunk文件有发生读取或写入，就更新活跃时间；
        /// 释放时，根据非活跃时间的长短作为顺序，总是先把非活跃时间最大的Chunk文件的非托管内存释放。
        /// </remarks>
        /// </summary>
        public readonly int ChunkInactiveTimeMaxSeconds;
        /// <summary>表示当Chunk文件无法分配非托管内存时，使用本地的环形数组进行缓存最新的记录。此属性指定本地环形数组的大小；
        /// </summary>
        public readonly int ChunkLocalCacheSize;
        /// <summary>表示是否需要统计Chunk的写入和读取情况
        /// </summary>
        public readonly bool EnableChunkStatistic;

        public ChunkManagerConfig(string basePath,
                               IFileNamingStrategy fileNamingStrategy,
                               int chunkDataSize,
                               int chunkDataUnitSize,
                               int chunkDataCount,
                               int flushChunkIntervalMilliseconds,
                               bool enableCache,
                               bool syncFlush,
                               FlushOption flushOption,
                               int chunkReaderCount,
                               int maxLogRecordSize,
                               int chunkWriteBuffer,
                               int chunkReadBuffer,
                               int chunkCacheMaxPercent,
                               int chunkCacheMinPercent,
                               int preCacheChunkCount,
                               int chunkInactiveTimeMaxSeconds,
                               int chunkLocalCacheSize,
                               bool enableChunkStatistic)
        {
            Ensure.NotNullOrEmpty(basePath, "basePath");
            Ensure.NotNull(fileNamingStrategy, "fileNamingStrategy");
            Ensure.Nonnegative(chunkDataSize, "chunkDataSize");
            Ensure.Nonnegative(chunkDataUnitSize, "chunkDataUnitSize");
            Ensure.Nonnegative(chunkDataCount, "chunkDataCount");
            Ensure.Positive(flushChunkIntervalMilliseconds, "flushChunkIntervalMilliseconds");
            Ensure.Positive(maxLogRecordSize, "maxLogRecordSize");
            Ensure.Positive(chunkWriteBuffer, "chunkWriteBuffer");
            Ensure.Positive(chunkReadBuffer, "chunkReadBuffer");
            Ensure.Positive(chunkCacheMaxPercent, "chunkCacheMaxPercent");
            Ensure.Positive(chunkCacheMinPercent, "chunkCacheMinPercent");
            Ensure.Nonnegative(preCacheChunkCount, "preCacheChunkCount");
            Ensure.Nonnegative(chunkInactiveTimeMaxSeconds, "chunkInactiveTimeMaxSeconds");
            Ensure.Positive(chunkLocalCacheSize, "chunkLocalCacheSize");

            if (chunkDataSize <= 0 && (chunkDataUnitSize <= 0 || chunkDataCount <= 0))
            {
                throw new ArgumentException(string.Format("Invalid chunk data size arugment. chunkDataSize: {0}, chunkDataUnitSize: {1}, chunkDataCount: {2}", chunkDataSize, chunkDataUnitSize, chunkDataCount));
            }

            BasePath = basePath;
            FileNamingStrategy = fileNamingStrategy;
            ChunkDataSize = chunkDataSize;
            ChunkDataUnitSize = chunkDataUnitSize;
            ChunkDataCount = chunkDataCount;
            FlushChunkIntervalMilliseconds = flushChunkIntervalMilliseconds;
            EnableCache = enableCache;
            SyncFlush = syncFlush;
            FlushOption = flushOption;
            ChunkReaderCount = chunkReaderCount;
            MaxLogRecordSize = maxLogRecordSize;
            ChunkWriteBuffer = chunkWriteBuffer;
            ChunkReadBuffer = chunkReadBuffer;
            ChunkCacheMaxPercent = chunkCacheMaxPercent;
            ChunkCacheMinPercent = chunkCacheMinPercent;
            PreCacheChunkCount = preCacheChunkCount;
            ChunkInactiveTimeMaxSeconds = chunkInactiveTimeMaxSeconds;
            ChunkLocalCacheSize = chunkLocalCacheSize;
            EnableChunkStatistic = enableChunkStatistic;

            if (GetChunkDataSize() > 1024 * 1024 * 1024)
            {
                throw new ArgumentException("Chunk data size cannot bigger than 1G");
            }
        }

        public int GetChunkDataSize()
        {
            if (ChunkDataSize > 0)
            {
                return ChunkDataSize;
            }
            return ChunkDataUnitSize * ChunkDataCount;
        }
    }
}
