using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ECommon.Storage;
using NewLife.Queue.Utilities;
using NewLife.Log;
using NewLife.Model;
using NewLife.Queue.Scheduling;

namespace NewLife.Queue.Storage
{
    public class ChunkManager : IDisposable
    {
        private readonly object _lockObj = new object();
        private readonly ChunkManagerConfig _config;
        private readonly IDictionary<int, Chunk> _chunks;
        private readonly string _chunkPath;
        private readonly bool _isMemoryMode;
        private int _nextChunkNumber;
        private int _uncachingChunks;
        private int _isCachingNextChunk;
        private ConcurrentDictionary<int, BytesInfo> _bytesWriteDict;
        private ConcurrentDictionary<int, CountInfo> _fileReadDict;
        private ConcurrentDictionary<int, CountInfo> _unmanagedReadDict;
        private ConcurrentDictionary<int, CountInfo> _cachedReadDict;

        class BytesInfo
        {
            public long PreviousBytes;
            public long CurrentBytes;

            public long UpgradeBytes()
            {
                var incrementBytes = CurrentBytes - PreviousBytes;
                PreviousBytes = CurrentBytes;
                return incrementBytes;
            }
        }
        class CountInfo
        {
            public long PreviousCount;
            public long CurrentCount;

            public long UpgradeCount()
            {
                var incrementCount = CurrentCount - PreviousCount;
                PreviousCount = CurrentCount;
                return incrementCount;
            }
        }

        public string Name { get; private set; }
        public ChunkManagerConfig Config { get { return _config; } }
        public string ChunkPath { get { return _chunkPath; } }
        public bool IsMemoryMode { get { return _isMemoryMode; } }

        public ChunkManager(string name, ChunkManagerConfig config, bool isMemoryMode, string relativePath = null)
        {
            Ensure.NotNull(name, "name");
            Ensure.NotNull(config, "config");

            Name = name;
            _config = config;
            _isMemoryMode = isMemoryMode;
            if (string.IsNullOrEmpty(relativePath))
            {
                _chunkPath = _config.BasePath;
            }
            else
            {
                _chunkPath = Path.Combine(_config.BasePath, relativePath);
            }
            if (!Directory.Exists(_chunkPath))
            {
                Directory.CreateDirectory(_chunkPath);
            }
            _chunks = new ConcurrentDictionary<int, Chunk>();
            _bytesWriteDict = new ConcurrentDictionary<int, BytesInfo>();
            _fileReadDict = new ConcurrentDictionary<int, CountInfo>();
            _unmanagedReadDict = new ConcurrentDictionary<int, CountInfo>();
            _cachedReadDict = new ConcurrentDictionary<int, CountInfo>();
        }

        public void Load<T>(Func<byte[], T> readRecordFunc) where T : ILogRecord
        {
            if (_isMemoryMode) return;

            lock (_lockObj)
            {
                if (!Directory.Exists(_chunkPath))
                {
                    Directory.CreateDirectory(_chunkPath);
                }

                var tempFiles = _config.FileNamingStrategy.GetTempFiles(_chunkPath);
                foreach (var file in tempFiles)
                {
                    File.SetAttributes(file, FileAttributes.Normal);
                    File.Delete(file);
                }

                var files = _config.FileNamingStrategy.GetChunkFiles(_chunkPath);
                if (files.Length > 0)
                {
                    var cachedChunkCount = 0;
                    for (var i = files.Length - 2; i >= 0; i--)
                    {
                        var file = files[i];
                        var chunk = Chunk.FromCompletedFile(file, this, _config, _isMemoryMode);
                        if (_config.EnableCache && cachedChunkCount < _config.PreCacheChunkCount)
                        {
                            if (chunk.TryCacheInMemory(false))
                            {
                                cachedChunkCount++;
                            }
                        }
                        AddChunk(chunk);
                    }
                    var lastFile = files[files.Length - 1];
                    AddChunk(Chunk.FromOngoingFile(lastFile, this, _config, readRecordFunc, _isMemoryMode));
                }

                if (_config.EnableCache)
                {
                    QueueService.ScheduleService.StartTask("UncacheChunks", () => UncacheChunks(), 1000, 1000);
                }
            }
        }
        public int GetChunkCount()
        {
            return _chunks.Count;
        }
        public IList<Chunk> GetAllChunks()
        {
            return _chunks.Values.ToList();
        }
        public Chunk AddNewChunk()
        {
            lock (_lockObj)
            {
                var chunkNumber = _nextChunkNumber;
                var chunkFileName = _config.FileNamingStrategy.GetFileNameFor(_chunkPath, chunkNumber);
                var chunk = Chunk.CreateNew(chunkFileName, chunkNumber, this, _config, _isMemoryMode);

                AddChunk(chunk);

                return chunk;
            }
        }
        public Chunk GetFirstChunk()
        {
            lock (_lockObj)
            {
                if (_chunks.Count == 0)
                {
                    AddNewChunk();
                }
                var minChunkNum = _chunks.Keys.Min();
                return _chunks[minChunkNum];
            }
        }
        public Chunk GetLastChunk()
        {
            lock (_lockObj)
            {
                if (_chunks.Count == 0)
                {
                    AddNewChunk();
                }
                return _chunks[_nextChunkNumber - 1];
            }
        }
        public int GetChunkNum(long dataPosition)
        {
            return (int)(dataPosition / _config.GetChunkDataSize());
        }
        public Chunk GetChunkFor(long dataPosition)
        {
            var chunkNum = (int)(dataPosition / _config.GetChunkDataSize());
            return GetChunk(chunkNum);
        }
        public Chunk GetChunk(int chunkNum)
        {
            if (_chunks.ContainsKey(chunkNum))
            {
                return _chunks[chunkNum];
            }
            return null;
        }
        public bool RemoveChunk(Chunk chunk)
        {
            lock (_lockObj)
            {
                if (_chunks.Remove(chunk.ChunkHeader.ChunkNumber))
                {
                    try
                    {
                        chunk.Destroy();
                    }
                    catch (Exception ex)
                    {
                        Log.Error(string.Format("Destroy chunk {0} has exception.", chunk), ex);
                    }
                    return true;
                }
                return false;
            }
        }
        public void TryCacheNextChunk(Chunk currentChunk)
        {
            if (!_config.EnableCache) return;

            if (Interlocked.CompareExchange(ref _isCachingNextChunk, 1, 0) == 0)
            {
                try
                {
                    var nextChunkNumber = currentChunk.ChunkHeader.ChunkNumber + 1;
                    var nextChunk = GetChunk(nextChunkNumber);
                    if (nextChunk != null && !nextChunk.IsMemoryChunk && nextChunk.IsCompleted && !nextChunk.HasCachedChunk)
                    {
                        Task.Factory.StartNew(() => nextChunk.TryCacheInMemory(false));
                    }
                }
                finally
                {
                    Interlocked.Exchange(ref _isCachingNextChunk, 0);
                }
            }
        }

        public void Start()
        {
            if (_config.EnableChunkStatistic)
            {
                QueueService.ScheduleService.StartTask("LogChunkStatisticStatus", LogChunkStatisticStatus, 1000, 1000);
            }
        }
        public void Shutdown()
        {
            if (_config.EnableChunkStatistic)
            {
                QueueService.ScheduleService.StopTask("LogChunkStatisticStatus");
            }
        }
        public void AddWriteBytes(int chunkNum, int byteCount)
        {
            _bytesWriteDict.AddOrUpdate(chunkNum, GetDefaultBytesInfo, (chunkNumber, current) => UpdateBytesInfo(chunkNumber, current, byteCount));
        }
        public void AddFileReadCount(int chunkNum)
        {
            _fileReadDict.AddOrUpdate(chunkNum, GetDefaultCountInfo, UpdateCountInfo);
        }
        public void AddUnmanagedReadCount(int chunkNum)
        {
            _unmanagedReadDict.AddOrUpdate(chunkNum, GetDefaultCountInfo, UpdateCountInfo);
        }
        public void AddCachedReadCount(int chunkNum)
        {
            _cachedReadDict.AddOrUpdate(chunkNum, GetDefaultCountInfo, UpdateCountInfo);
        }

        public void Dispose()
        {
            Close();
        }
        public void Close()
        {
            lock (_lockObj)
            {
                QueueService.ScheduleService.StopTask("UncacheChunks");

                foreach (var chunk in _chunks.Values)
                {
                    try
                    {
                        chunk.Close();
                    }
                    catch (Exception ex)
                    {
                        Log.Error(string.Format("Chunk {0} close failed.", chunk), ex);
                    }
                }
            }
        }

        private void AddChunk(Chunk chunk)
        {
            _chunks.Add(chunk.ChunkHeader.ChunkNumber, chunk);
            _nextChunkNumber = chunk.ChunkHeader.ChunkNumber + 1;
        }
        private int UncacheChunks(int maxUncacheCount = 10)
        {
            var uncachedCount = 0;

            if (Interlocked.CompareExchange(ref _uncachingChunks, 1, 0) == 0)
            {
                try
                {
                    var usedMemoryPercent = ChunkUtil.GetUsedMemoryPercent();
                    if (usedMemoryPercent <= (ulong)_config.ChunkCacheMinPercent)
                    {
                        return 0;
                    }

                    if (Log.Level == LogLevel.Info)
                    {
                        Log.Debug("Current memory usage {0}% exceed the chunkCacheMinPercent {1}%, try to uncache chunks.", usedMemoryPercent, _config.ChunkCacheMinPercent);
                    }

                    var chunks = _chunks.Values.Where(x => x.IsCompleted && !x.IsMemoryChunk && x.HasCachedChunk).OrderBy(x => x.LastActiveTime).ToList();

                    foreach (var chunk in chunks)
                    {
                        if ((DateTime.Now - chunk.LastActiveTime).TotalSeconds >= _config.ChunkInactiveTimeMaxSeconds)
                        {
                            if (chunk.UnCacheFromMemory())
                            {
                                Thread.Sleep(100); //即便有内存释放了，由于通过API读取到的内存使用数可能不会立即更新，所以等待一定时间后检查内存是否足够
                                uncachedCount++;
                                if (uncachedCount >= maxUncacheCount || ChunkUtil.GetUsedMemoryPercent() <= (ulong)_config.ChunkCacheMinPercent)
                                {
                                    break;
                                }
                            }
                        }
                    }

                    if (Log.Level== LogLevel.Info)
                    {
                        if (uncachedCount > 0)
                        {
                            Log.Debug("Uncached {0} chunks, current memory usage: {1}%", uncachedCount, ChunkUtil.GetUsedMemoryPercent());
                        }
                        else
                        {
                            Log.Debug("No chunks uncached.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error("Uncaching chunks has exception.", ex);
                }
                finally
                {
                    Interlocked.Exchange(ref _uncachingChunks, 0);
                }
            }

            return uncachedCount;
        }
        private CountInfo GetDefaultCountInfo(int chunkNum)
        {
            return new CountInfo { CurrentCount = 1 };
        }
        private CountInfo UpdateCountInfo(int chunkNum, CountInfo countInfo)
        {
            Interlocked.Increment(ref countInfo.CurrentCount);
            return countInfo;
        }
        private BytesInfo GetDefaultBytesInfo(int chunkNum)
        {
            return new BytesInfo();
        }
        private BytesInfo UpdateBytesInfo(int chunkNum, BytesInfo bytesInfo, int bytesAdd)
        {
            Interlocked.Add(ref bytesInfo.CurrentBytes, bytesAdd);
            return bytesInfo;
        }
        private void LogChunkStatisticStatus()
        {
            if (Log.Level != LogLevel.Info) return;
            var bytesWriteStatus = UpdateWriteStatus(_bytesWriteDict);
            var unmanagedReadStatus = UpdateReadStatus(_unmanagedReadDict);
            var fileReadStatus = UpdateReadStatus(_fileReadDict);
            var cachedReadStatus = UpdateReadStatus(_cachedReadDict);
            Log.Debug("{0}, maxChunk:#{1}, write:{2}, unmanagedCacheRead:{3}, localCacheRead:{4}, fileRead:{5}", Name, GetLastChunk().ChunkHeader.ChunkNumber, bytesWriteStatus, unmanagedReadStatus, cachedReadStatus, fileReadStatus);
        }
        private string UpdateWriteStatus(ConcurrentDictionary<int, BytesInfo> dict)
        {
            var list = new List<string>();
            var toRemoveKeys = new List<int>();

            foreach (var entry in dict)
            {
                var chunkNum = entry.Key;
                var throughput = entry.Value.UpgradeBytes() / 1024;
                if (throughput > 0)
                {
                    list.Add(string.Format("[chunk:#{0},bytes:{1}KB]", chunkNum, throughput));
                }
                else
                {
                    toRemoveKeys.Add(chunkNum);
                }
            }

            foreach (var key in toRemoveKeys)
            {
                _bytesWriteDict.Remove(key);
            }

            return list.Count == 0 ? "[]" : string.Join(",", list);
        }
        private string UpdateReadStatus(ConcurrentDictionary<int, CountInfo> dict)
        {
            var list = new List<string>();

            foreach (var entry in dict)
            {
                var chunkNum = entry.Key;
                var throughput = entry.Value.UpgradeCount();
                if (throughput > 0)
                {
                    list.Add(string.Format("[chunk:#{0},count:{1}]", chunkNum, throughput));
                }
            }

            return list.Count == 0 ? "[]" : string.Join(",", list);
        }


        #region 日志
        /// <summary>日志</summary>
        public ILog Log { get; set; } = QueueService.Log;
        #endregion
    }
}
