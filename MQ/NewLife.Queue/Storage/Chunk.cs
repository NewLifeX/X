using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using ECommon.Storage;
using NewLife.Log;
using NewLife.Queue.Storage.Exceptions;
using NewLife.Queue.Utilities;


namespace NewLife.Queue.Storage
{
    public unsafe class Chunk : IDisposable
    {
        #region Private Variables

        private ChunkHeader _chunkHeader;
        private ChunkFooter _chunkFooter;

        private readonly string _filename;
        private readonly ChunkManager _chunkManager;
        private readonly ChunkManagerConfig _chunkConfig;
        private readonly bool _isMemoryChunk;
        private readonly ConcurrentQueue<ReaderWorkItem> _readerWorkItemQueue = new ConcurrentQueue<ReaderWorkItem>();

        private readonly object _writeSyncObj = new object();
        private readonly object _cacheSyncObj = new object();
        private readonly object _freeMemoryObj = new object();

        private int _dataPosition;
        private bool _isCompleted;
        private bool _isDestroying;
        private bool _isMemoryFreed;
        private int _cachingChunk;
        private DateTime _lastActiveTime;
        private bool _isReadersInitialized;
        private int _flushedDataPosition;

        private Chunk _memoryChunk;
        private CacheItem[] _cacheItems;
        private IntPtr _cachedData;
        private int _cachedLength;

        private WriterWorkItem _writerWorkItem;

        #endregion

        #region Public Properties

        public string FileName { get { return _filename; } }
        public ChunkHeader ChunkHeader { get { return _chunkHeader; } }
        public ChunkFooter ChunkFooter { get { return _chunkFooter; } }
        public ChunkManagerConfig Config { get { return _chunkConfig; } }
        public bool IsCompleted { get { return _isCompleted; } }
        public DateTime LastActiveTime
        {
            get
            {
                var lastActiveTimeOfMemoryChunk = DateTime.MinValue;
                if (_memoryChunk != null)
                {
                    lastActiveTimeOfMemoryChunk = _memoryChunk.LastActiveTime;
                }
                return lastActiveTimeOfMemoryChunk >= _lastActiveTime ? lastActiveTimeOfMemoryChunk : _lastActiveTime;
            }
        }
        public bool IsMemoryChunk { get { return _isMemoryChunk; } }
        public bool HasCachedChunk { get { return _memoryChunk != null; } }
        public int DataPosition { get { return _dataPosition; } }
        public long GlobalDataPosition
        {
            get
            {
                return ChunkHeader.ChunkDataStartPosition + DataPosition;
            }
        }
        public bool IsFixedDataSize()
        {
            return _chunkConfig.ChunkDataUnitSize > 0 && _chunkConfig.ChunkDataCount > 0;
        }

        #endregion

        #region Constructors

        private Chunk(string filename, ChunkManager chunkManager, ChunkManagerConfig chunkConfig, bool isMemoryChunk)
        {
            Ensure.NotNullOrEmpty(filename, "filename");
            Ensure.NotNull(chunkManager, "chunkManager");
            Ensure.NotNull(chunkConfig, "chunkConfig");

            _filename = filename;
            _chunkManager = chunkManager;
            _chunkConfig = chunkConfig;
            _isMemoryChunk = isMemoryChunk;
            _lastActiveTime = DateTime.Now;
        }
        ~Chunk()
        {
            UnCacheFromMemory();
        }

        #endregion

        #region Factory Methods

        public static Chunk CreateNew(string filename, int chunkNumber, ChunkManager chunkManager, ChunkManagerConfig config, bool isMemoryChunk)
        {
            var chunk = new Chunk(filename, chunkManager, config, isMemoryChunk);

            try
            {
                chunk.InitNew(chunkNumber);
            }
            catch (OutOfMemoryException)
            {
                chunk.Dispose();
                throw;
            }
            catch (Exception ex)
            {
                QueueService.Log.Info(string.Format("Chunk {0} create failed.", chunk));
                XTrace.WriteException(ex);
                chunk.Dispose();
                throw;
            }

            return chunk;
        }
        public static Chunk FromCompletedFile(string filename, ChunkManager chunkManager, ChunkManagerConfig config, bool isMemoryChunk)
        {
            var chunk = new Chunk(filename, chunkManager, config, isMemoryChunk);

            try
            {
                chunk.InitCompleted();
            }
            catch (OutOfMemoryException)
            {
                chunk.Dispose();
                throw;
            }
            catch (Exception ex)
            {
                QueueService.Log.Info(string.Format("Chunk {0} init from completed file failed.", chunk));
                XTrace.WriteException(ex);
                chunk.Dispose();
                throw;
            }

            return chunk;
        }
        public static Chunk FromOngoingFile<T>(string filename, ChunkManager chunkManager, ChunkManagerConfig config, Func<byte[], T> readRecordFunc, bool isMemoryChunk) where T : ILogRecord
        {
            var chunk = new Chunk(filename, chunkManager, config, isMemoryChunk);

            try
            {
                chunk.InitOngoing(readRecordFunc);
            }
            catch (OutOfMemoryException)
            {
                chunk.Dispose();
                throw;
            }
            catch (Exception ex)
            {
                QueueService.Log.Info(string.Format("Chunk {0} init from ongoing file failed.", chunk));
                XTrace.WriteException(ex);
                chunk.Dispose();
                throw;
            }

            return chunk;
        }

        #endregion

        #region Init Methods

        private void InitCompleted()
        {
            var fileInfo = new FileInfo(_filename);
            if (!fileInfo.Exists)
            {
                throw new ChunkFileNotExistException(_filename);
            }

            _isCompleted = true;

            using (var fileStream = new FileStream(_filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, _chunkConfig.ChunkReadBuffer, FileOptions.None))
            {
                using (var reader = new BinaryReader(fileStream))
                {
                    _chunkHeader = ReadHeader(fileStream, reader);
                    _chunkFooter = ReadFooter(fileStream, reader);

                    CheckCompletedFileChunk();
                }
            }

            _dataPosition = _chunkFooter.ChunkDataTotalSize;
            _flushedDataPosition = _chunkFooter.ChunkDataTotalSize;

            if (_isMemoryChunk)
            {
                LoadFileChunkToMemory();
            }
            else
            {
                SetFileAttributes();
            }

            InitializeReaderWorkItems();

            _lastActiveTime = DateTime.Now;
        }
        private void InitNew(int chunkNumber)
        {
            var chunkDataSize = 0;
            if (_chunkConfig.ChunkDataSize > 0)
            {
                chunkDataSize = _chunkConfig.ChunkDataSize;
            }
            else
            {
                chunkDataSize = _chunkConfig.ChunkDataUnitSize * _chunkConfig.ChunkDataCount;
            }

            _chunkHeader = new ChunkHeader(chunkNumber, chunkDataSize);

            _isCompleted = false;

            var fileSize = ChunkHeader.Size + _chunkHeader.ChunkDataTotalSize + ChunkFooter.Size;

            var writeStream = default(Stream);
            var tempFilename = string.Format("{0}.{1}.tmp", _filename, Guid.NewGuid());
            var tempFileStream = default(FileStream);

            try
            {
                if (_isMemoryChunk)
                {
                    _cachedLength = fileSize;
                    _cachedData = Marshal.AllocHGlobal(_cachedLength);
                    writeStream = new UnmanagedMemoryStream((byte*)_cachedData, _cachedLength, _cachedLength, FileAccess.ReadWrite);
                    writeStream.Write(_chunkHeader.AsByteArray(), 0, ChunkHeader.Size);
                }
                else
                {
                    var fileInfo = new FileInfo(_filename);
                    if (fileInfo.Exists)
                    {
                        File.SetAttributes(_filename, FileAttributes.Normal);
                        File.Delete(_filename);
                    }

                    tempFileStream = new FileStream(tempFilename, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.Read, _chunkConfig.ChunkWriteBuffer, FileOptions.None);
                    tempFileStream.SetLength(fileSize);
                    tempFileStream.Write(_chunkHeader.AsByteArray(), 0, ChunkHeader.Size);
                    tempFileStream.Flush(true);
                    tempFileStream.Close();

                    File.Move(tempFilename, _filename);

                    writeStream = new FileStream(_filename, FileMode.Open, FileAccess.ReadWrite, FileShare.Read, _chunkConfig.ChunkWriteBuffer, FileOptions.SequentialScan);
                    SetFileAttributes();
                }

                writeStream.Position = ChunkHeader.Size;

                _dataPosition = 0;
                _flushedDataPosition = 0;
                _writerWorkItem = new WriterWorkItem(new ChunkFileStream(writeStream, _chunkConfig.FlushOption));

                InitializeReaderWorkItems();

                if (!_isMemoryChunk)
                {
                    if (_chunkConfig.EnableCache)
                    {
                        var chunkSize = (ulong)GetChunkSize(_chunkHeader);
                        if (ChunkUtil.IsMemoryEnoughToCacheChunk(chunkSize, (uint)_chunkConfig.ChunkCacheMaxPercent))
                        {
                            try
                            {
                                _memoryChunk = CreateNew(_filename, chunkNumber, _chunkManager, _chunkConfig, true);
                            }
                            catch (OutOfMemoryException)
                            {
                                _cacheItems = new CacheItem[_chunkConfig.ChunkLocalCacheSize];
                            }
                            catch (Exception ex)
                            {
                                Log.Error(string.Format("Failed to cache new chunk {0}", this), ex);
                                _cacheItems = new CacheItem[_chunkConfig.ChunkLocalCacheSize];
                            }
                        }
                        else
                        {
                            _cacheItems = new CacheItem[_chunkConfig.ChunkLocalCacheSize];
                        }
                    }
                    else
                    {
                        _cacheItems = new CacheItem[_chunkConfig.ChunkLocalCacheSize];
                    }
                }
            }
            catch
            {
                if (!_isMemoryChunk)
                {
                    if (tempFileStream != null)
                    {
                        Helper.EatException(() => tempFileStream.Close());
                    }
                    if (File.Exists(tempFilename))
                    {
                        Helper.EatException(() =>
                        {
                            File.SetAttributes(tempFilename, FileAttributes.Normal);
                            File.Delete(tempFilename);
                        });
                    }
                }
                throw;
            }

            _lastActiveTime = DateTime.Now;
        }
        private void InitOngoing<T>(Func<byte[], T> readRecordFunc) where T : ILogRecord
        {
            var fileInfo = new FileInfo(_filename);
            if (!fileInfo.Exists)
            {
                throw new ChunkFileNotExistException(_filename);
            }

            _isCompleted = false;

            if (!TryParsingDataPosition(readRecordFunc, out _chunkHeader, out _dataPosition))
            {
                throw new ChunkBadDataException(string.Format("Failed to parse chunk data, chunk file: {0}", _filename));
            }

            _flushedDataPosition = _dataPosition;

            var writeStream = default(Stream);

            if (_isMemoryChunk)
            {
                var fileSize = ChunkHeader.Size + _chunkHeader.ChunkDataTotalSize + ChunkFooter.Size;
                _cachedLength = fileSize;
                _cachedData = Marshal.AllocHGlobal(_cachedLength);
                writeStream = new UnmanagedMemoryStream((byte*)_cachedData, _cachedLength, _cachedLength, FileAccess.ReadWrite);

                writeStream.Write(_chunkHeader.AsByteArray(), 0, ChunkHeader.Size);

                if (_dataPosition > 0)
                {
                    using (var fileStream = new FileStream(_filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 8192, FileOptions.SequentialScan))
                    {
                        fileStream.Seek(ChunkHeader.Size, SeekOrigin.Begin);
                        var buffer = new byte[65536];
                        int toReadBytes = _dataPosition;

                        while (toReadBytes > 0)
                        {
                            int read = fileStream.Read(buffer, 0, Math.Min(toReadBytes, buffer.Length));
                            if (read == 0)
                            {
                                break;
                            }
                            toReadBytes -= read;
                            writeStream.Write(buffer, 0, read);
                        }
                    }
                }

                if (writeStream.Position != GetStreamPosition(_dataPosition))
                {
                    throw new InvalidOperationException(string.Format("UnmanagedMemoryStream position incorrect, expect: {0}, but: {1}", _dataPosition + ChunkHeader.Size, writeStream.Position));
                }
            }
            else
            {
                writeStream = new FileStream(_filename, FileMode.Open, FileAccess.ReadWrite, FileShare.Read, _chunkConfig.ChunkWriteBuffer, FileOptions.SequentialScan);
                writeStream.Position = GetStreamPosition(_dataPosition);
                SetFileAttributes();
            }

            _writerWorkItem = new WriterWorkItem(new ChunkFileStream(writeStream, _chunkConfig.FlushOption));

            InitializeReaderWorkItems();

            if (!_isMemoryChunk)
            {
                if (_chunkConfig.EnableCache)
                {
                    var chunkSize = (ulong)GetChunkSize(_chunkHeader);
                    if (ChunkUtil.IsMemoryEnoughToCacheChunk(chunkSize, (uint)_chunkConfig.ChunkCacheMaxPercent))
                    {
                        try
                        {
                            _memoryChunk = FromOngoingFile(_filename, _chunkManager, _chunkConfig, readRecordFunc, true);
                        }
                        catch (OutOfMemoryException)
                        {
                            _cacheItems = new CacheItem[_chunkConfig.ChunkLocalCacheSize];
                        }
                        catch (Exception ex)
                        {
                            Log.Error(string.Format("Failed to cache ongoing chunk {0}", this), ex);
                            _cacheItems = new CacheItem[_chunkConfig.ChunkLocalCacheSize];
                        }
                    }
                    else
                    {
                        _cacheItems = new CacheItem[_chunkConfig.ChunkLocalCacheSize];
                    }
                }
                else
                {
                    _cacheItems = new CacheItem[_chunkConfig.ChunkLocalCacheSize];
                }
            }

            _lastActiveTime = DateTime.Now;

            if (!_isMemoryChunk)
            {
                Log.Info("Ongoing chunk {0} initialized, _dataPosition: {1}", this, _dataPosition);
            }
        }

        #endregion

        #region Public Methods

        public bool TryCacheInMemory(bool shouldCacheNextChunk)
        {
            lock (_cacheSyncObj)
            {
                if (!_chunkConfig.EnableCache || _isMemoryChunk || !_isCompleted || _memoryChunk != null)
                {
                    _cachingChunk = 0;
                    return false;
                }

                try
                {
                    var chunkSize = (ulong)GetChunkSize(_chunkHeader);
                    if (!ChunkUtil.IsMemoryEnoughToCacheChunk(chunkSize, (uint)_chunkConfig.ChunkCacheMaxPercent))
                    {
                        return false;
                    }
                    _memoryChunk = FromCompletedFile(_filename, _chunkManager, _chunkConfig, true);
                    if (shouldCacheNextChunk)
                    {
                        Task.Factory.StartNew(() => _chunkManager.TryCacheNextChunk(this));
                    }
                    return true;
                }
                catch (OutOfMemoryException) { return false; }
                catch (Exception ex)
                {
                    Log.Error(string.Format("Failed to cache completed chunk {0}", this), ex);
                    return false;
                }
                finally
                {
                    _cachingChunk = 0;
                }
            }
        }
        public bool UnCacheFromMemory()
        {
            lock (_cacheSyncObj)
            {
                if (!_chunkConfig.EnableCache || _isMemoryChunk || !_isCompleted || _memoryChunk == null)
                {
                    return false;
                }

                try
                {
                    var memoryChunk = _memoryChunk;
                    _memoryChunk = null;
                    memoryChunk.Dispose();
                    return true;
                }
                catch (Exception ex)
                {
                    Log.Error(string.Format("Failed to uncache completed chunk {0}", this), ex);
                    return false;
                }
            }
        }
        public T TryReadAt<T>(long dataPosition, Func<byte[], T> readRecordFunc, bool autoCache = true) where T : class, ILogRecord
        {
            if (_isDestroying)
            {
                throw new ChunkReadException(string.Format("Chunk {0} is being deleting.", this));
            }

            _lastActiveTime = DateTime.Now;

            if (!_isMemoryChunk)
            {
                if (_cacheItems != null)
                {
                    var index = dataPosition % _chunkConfig.ChunkLocalCacheSize;
                    var cacheItem = _cacheItems[index];
                    if (cacheItem != null && cacheItem.RecordPosition == dataPosition)
                    {
                        var record = readRecordFunc(cacheItem.RecordBuffer);
                        if (record == null)
                        {
                            throw new ChunkReadException(
                                string.Format("Cannot read a record from data position {0}. Something is seriously wrong in chunk {1}.",
                                              dataPosition, this));
                        }
                        if (_chunkConfig.EnableChunkStatistic)
                        {
                            _chunkManager.AddCachedReadCount(ChunkHeader.ChunkNumber);
                        }
                        return record;
                    }
                }
                else if (_memoryChunk != null)
                {
                    var record = _memoryChunk.TryReadAt(dataPosition, readRecordFunc);
                    if (record != null && _chunkConfig.EnableChunkStatistic)
                    {
                        _chunkManager.AddUnmanagedReadCount(ChunkHeader.ChunkNumber);
                    }
                    return record;
                }
            }

            if (_chunkConfig.EnableCache && autoCache && !_isMemoryChunk && _isCompleted && Interlocked.CompareExchange(ref _cachingChunk, 1, 0) == 0)
            {
                Task.Factory.StartNew(() => TryCacheInMemory(true));
            }

            var readerWorkItem = GetReaderWorkItem();
            try
            {
                var currentDataPosition = DataPosition;
                if (dataPosition >= currentDataPosition)
                {
                    return null;
                }

                try
                {
                    var record = IsFixedDataSize() ?
                        TryReadFixedSizeForwardInternal(readerWorkItem, dataPosition, readRecordFunc) :
                        TryReadForwardInternal(readerWorkItem, dataPosition, readRecordFunc);
                    if (!_isMemoryChunk && _chunkConfig.EnableChunkStatistic)
                    {
                        _chunkManager.AddFileReadCount(ChunkHeader.ChunkNumber);
                    }
                    return record;
                }
                catch
                {
                    if (!_isMemoryChunk && _writerWorkItem != null && _writerWorkItem.LastFlushedPosition < GetStreamPosition(_dataPosition))
                    {
                        return null;
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            finally
            {
                ReturnReaderWorkItem(readerWorkItem);
            }
        }
        public RecordWriteResult TryAppend(ILogRecord record)
        {
            if (_isCompleted)
            {
                throw new ChunkWriteException(this.ToString(), string.Format("Cannot write to a read-only chunk, isMemoryChunk: {0}, _dataPosition: {1}", _isMemoryChunk, _dataPosition));
            }

            _lastActiveTime = DateTime.Now;

            var writerWorkItem = _writerWorkItem;
            var bufferStream = writerWorkItem.BufferStream;
            var bufferWriter = writerWorkItem.BufferWriter;
            var recordBuffer = default(byte[]);

            if (IsFixedDataSize())
            {
                if (writerWorkItem.WorkingStream.Position + _chunkConfig.ChunkDataUnitSize > ChunkHeader.Size + _chunkHeader.ChunkDataTotalSize)
                {
                    return RecordWriteResult.NotEnoughSpace();
                }
                bufferStream.Position = 0;
                record.WriteTo(GlobalDataPosition, bufferWriter);
                var recordLength = (int)bufferStream.Length;
                if (recordLength != _chunkConfig.ChunkDataUnitSize)
                {
                    throw new ChunkWriteException(this.ToString(), string.Format("Invalid fixed data length, expected length {0}, but was {1}", _chunkConfig.ChunkDataUnitSize, recordLength));
                }

                if (_cacheItems != null)
                {
                    recordBuffer = new byte[recordLength];
                    Buffer.BlockCopy(bufferStream.GetBuffer(), 0, recordBuffer, 0, recordLength);
                }
            }
            else
            {
                bufferStream.SetLength(4);
                bufferStream.Position = 4;
                record.WriteTo(GlobalDataPosition, bufferWriter);
                var recordLength = (int)bufferStream.Length - 4;
                bufferWriter.Write(recordLength); // write record length suffix
                bufferStream.Position = 0;
                bufferWriter.Write(recordLength); // write record length prefix

                if (recordLength > _chunkConfig.MaxLogRecordSize)
                {
                    throw new ChunkWriteException(this.ToString(),
                        string.Format("Log record at data position {0} has too large length: {1} bytes, while limit is {2} bytes",
                                      _dataPosition, recordLength, _chunkConfig.MaxLogRecordSize));
                }

                if (writerWorkItem.WorkingStream.Position + recordLength + 2 * sizeof(int) > ChunkHeader.Size + _chunkHeader.ChunkDataTotalSize)
                {
                    return RecordWriteResult.NotEnoughSpace();
                }

                if (_cacheItems != null)
                {
                    recordBuffer = new byte[recordLength];
                    Buffer.BlockCopy(bufferStream.GetBuffer(), 4, recordBuffer, 0, recordLength);
                }
            }

            var writtenPosition = _dataPosition;
            var buffer = bufferStream.GetBuffer();

            lock (_writeSyncObj)
            {
                writerWorkItem.AppendData(buffer, 0, (int)bufferStream.Length);
            }

            _dataPosition = (int)writerWorkItem.WorkingStream.Position - ChunkHeader.Size;

            var position = ChunkHeader.ChunkDataStartPosition + writtenPosition;

            if (_chunkConfig.EnableCache)
            {
                if (_memoryChunk != null)
                {
                    var result = _memoryChunk.TryAppend(record);
                    if (!result.Success)
                    {
                        throw new ChunkWriteException(this.ToString(), "Append record to file chunk success, but append to memory chunk failed as memory space not enough, this should not be happened.");
                    }
                    else if (result.Position != position)
                    {
                        throw new ChunkWriteException(this.ToString(), string.Format("Append record to file chunk success, and append to memory chunk success, but the position is not equal, memory chunk write position: {0}, file chunk write position: {1}.", result.Position, position));
                    }
                }
                else if (_cacheItems != null && recordBuffer != null)
                {
                    var index = writtenPosition % _chunkConfig.ChunkLocalCacheSize;
                    _cacheItems[index] = new CacheItem { RecordPosition = writtenPosition, RecordBuffer = recordBuffer };
                }
            }
            else if (_cacheItems != null && recordBuffer != null)
            {
                var index = writtenPosition % _chunkConfig.ChunkLocalCacheSize;
                _cacheItems[index] = new CacheItem { RecordPosition = writtenPosition, RecordBuffer = recordBuffer };
            }

            if (!_isMemoryChunk && _chunkConfig.EnableChunkStatistic)
            {
                _chunkManager.AddWriteBytes(ChunkHeader.ChunkNumber, (int)bufferStream.Length);
            }

            return RecordWriteResult.Successful(position);
        }
        public void Flush()
        {
            if (_isMemoryChunk || _isCompleted) return;
            if (_writerWorkItem != null)
            {
                Helper.EatException(() => _writerWorkItem.FlushToDisk());
            }
        }
        public void Complete()
        {
            lock (_writeSyncObj)
            {
                if (_isCompleted) return;

                _chunkFooter = WriteFooter();
                if (!_isMemoryChunk)
                {
                    Flush();
                }

                _isCompleted = true;

                if (_writerWorkItem != null)
                {
                    Helper.EatException(() => _writerWorkItem.Dispose());
                    _writerWorkItem = null;
                }

                if (!_isMemoryChunk)
                {
                    if (_cacheItems != null)
                    {
                        _cacheItems = null;
                    }

                    SetFileAttributes();
                    if (_memoryChunk != null)
                    {
                        _memoryChunk.Complete();
                    }
                }
            }
        }
        public void Dispose()
        {
            Close();
        }
        public void Close()
        {
            lock (_writeSyncObj)
            {
                if (!_isCompleted)
                {
                    Flush();
                }

                if (_writerWorkItem != null)
                {
                    Helper.EatException(() => _writerWorkItem.Dispose());
                    _writerWorkItem = null;
                }

                if (!_isMemoryChunk)
                {
                    if (_cacheItems != null)
                    {
                        _cacheItems = null;
                    }
                }
                CloseAllReaderWorkItems();
                FreeMemory();
            }
        }
        public void Destroy()
        {
            if (_isMemoryChunk)
            {
                FreeMemory();
                return;
            }

            //检查当前chunk是否已完成
            if (!_isCompleted)
            {
                throw new InvalidOperationException(string.Format("Not allowed to delete a incompleted chunk {0}", this));
            }

            //首先设置删除标记
            _isDestroying = true;

            if (_cacheItems != null)
            {
                _cacheItems = null;
            }

            //释放缓存的内存
            UnCacheFromMemory();

            //关闭所有的ReaderWorkItem
            CloseAllReaderWorkItems();

            //删除Chunk文件
            File.SetAttributes(_filename, FileAttributes.Normal);
            File.Delete(_filename);
        }

        #endregion

        #region Helper Methods

        private void CheckCompletedFileChunk()
        {
            using (var fileStream = new FileStream(_filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, _chunkConfig.ChunkReadBuffer, FileOptions.None))
            {
                //检查Chunk文件的实际大小是否正确
                var chunkFileSize = ChunkHeader.Size + _chunkFooter.ChunkDataTotalSize + ChunkFooter.Size;
                if (chunkFileSize != fileStream.Length)
                {
                    throw new ChunkBadDataException(
                        string.Format("The size of chunk {0} should be equals with fileStream's length {1}, but instead it was {2}.",
                                        this,
                                        fileStream.Length,
                                        chunkFileSize));
                }

                //如果Chunk中的数据是固定大小的，则还需要检查数据总数是否正确
                if (IsFixedDataSize())
                {
                    if (_chunkFooter.ChunkDataTotalSize != _chunkHeader.ChunkDataTotalSize)
                    {
                        throw new ChunkBadDataException(
                            string.Format("For fixed-size chunk, the total data size of chunk {0} should be {1}, but instead it was {2}.",
                                            this,
                                            _chunkHeader.ChunkDataTotalSize,
                                            _chunkFooter.ChunkDataTotalSize));
                    }
                }
            }
        }
        private void LoadFileChunkToMemory()
        {
            using (var fileStream = new FileStream(_filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 8192, FileOptions.None))
            {
                var cachedLength = (int)fileStream.Length;
                var cachedData = Marshal.AllocHGlobal(cachedLength);

                try
                {
                    using (var unmanagedStream = new UnmanagedMemoryStream((byte*)cachedData, cachedLength, cachedLength, FileAccess.ReadWrite))
                    {
                        fileStream.Seek(0, SeekOrigin.Begin);
                        var buffer = new byte[65536];
                        int toRead = cachedLength;
                        while (toRead > 0)
                        {
                            int read = fileStream.Read(buffer, 0, Math.Min(toRead, buffer.Length));
                            if (read == 0)
                            {
                                break;
                            }
                            toRead -= read;
                            unmanagedStream.Write(buffer, 0, read);
                        }
                    }
                }
                catch
                {
                    Marshal.FreeHGlobal(cachedData);
                    throw;
                }

                _cachedData = cachedData;
                _cachedLength = cachedLength;
            }
        }
        private void FreeMemory()
        {
            if (_isMemoryChunk && !_isMemoryFreed)
            {
                lock (_freeMemoryObj)
                {
                    var cachedData = Interlocked.Exchange(ref _cachedData, IntPtr.Zero);
                    if (cachedData != IntPtr.Zero)
                    {
                        try
                        {
                            Marshal.FreeHGlobal(cachedData);
                        }
                        catch (Exception ex)
                        {
                            Log.Error(string.Format("Failed to free memory of chunk {0}", this), ex);
                        }
                    }
                    _isMemoryFreed = true;
                }
            }
        }

        private void InitializeReaderWorkItems()
        {
            for (var i = 0; i < _chunkConfig.ChunkReaderCount; i++)
            {
                _readerWorkItemQueue.Enqueue(CreateReaderWorkItem());
            }
            _isReadersInitialized = true;
        }
        private void CloseAllReaderWorkItems()
        {
            if (!_isReadersInitialized) return;

            var watch = Stopwatch.StartNew();
            var closedCount = 0;

            while (closedCount < _chunkConfig.ChunkReaderCount)
            {
                ReaderWorkItem readerWorkItem;
                while (_readerWorkItemQueue.TryDequeue(out readerWorkItem))
                {
                    readerWorkItem.Reader.Close();
                    closedCount++;
                }

                if (closedCount >= _chunkConfig.ChunkReaderCount)
                {
                    break;
                }

                Thread.Sleep(1000);

                if (watch.ElapsedMilliseconds > 30 * 1000)
                {
                    Log.Error("Close chunk reader work items timeout, expect close count: {0}, real close count: {1}", _chunkConfig.ChunkReaderCount, closedCount);
                    break;
                }
            }
        }
        private ReaderWorkItem CreateReaderWorkItem()
        {
            var stream = default(Stream);
            if (_isMemoryChunk)
            {
                stream = new UnmanagedMemoryStream((byte*)_cachedData, _cachedLength);
            }
            else
            {
                stream = new FileStream(_filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, _chunkConfig.ChunkReadBuffer, FileOptions.None);
            }
            return new ReaderWorkItem(stream, new BinaryReader(stream));
        }
        private ReaderWorkItem GetReaderWorkItem()
        {
            ReaderWorkItem readerWorkItem;
            while (!_readerWorkItemQueue.TryDequeue(out readerWorkItem))
            {
                Thread.Sleep(1);
            }
            return readerWorkItem;
        }
        private void ReturnReaderWorkItem(ReaderWorkItem readerWorkItem)
        {
            _readerWorkItemQueue.Enqueue(readerWorkItem);
        }

        private ChunkFooter WriteFooter()
        {
            var currentTotalDataSize = DataPosition;

            //如果是固定大小的数据，则检查总数据大小是否正确
            if (IsFixedDataSize())
            {
                if (currentTotalDataSize != _chunkHeader.ChunkDataTotalSize)
                {
                    throw new ChunkCompleteException(string.Format("Cannot write the chunk footer as the current total data size is incorrect. chunk: {0}, expectTotalDataSize: {1}, currentTotalDataSize: {2}",
                        this,
                        _chunkHeader.ChunkDataTotalSize,
                        currentTotalDataSize));
                }
            }

            var workItem = _writerWorkItem;
            var footer = new ChunkFooter(currentTotalDataSize);

            workItem.AppendData(footer.AsByteArray(), 0, ChunkFooter.Size);

            Flush(); // trying to prevent bug with resized file, but no data in it

            var oldStreamLength = workItem.WorkingStream.Length;
            var newStreamLength = ChunkHeader.Size + currentTotalDataSize + ChunkFooter.Size;

            if (newStreamLength != oldStreamLength)
            {
                workItem.ResizeStream(newStreamLength);
            }

            return footer;
        }
        private ChunkHeader ReadHeader(FileStream stream, BinaryReader reader)
        {
            if (stream.Length < ChunkHeader.Size)
            {
                throw new Exception(string.Format("Chunk file '{0}' is too short to even read ChunkHeader, its size is {1} bytes.", _filename, stream.Length));
            }
            stream.Seek(0, SeekOrigin.Begin);
            return ChunkHeader.FromStream(reader, stream);
        }
        private ChunkFooter ReadFooter(FileStream stream, BinaryReader reader)
        {
            if (stream.Length < ChunkFooter.Size)
            {
                throw new Exception(string.Format("Chunk file '{0}' is too short to even read ChunkFooter, its size is {1} bytes.", _filename, stream.Length));
            }
            stream.Seek(-ChunkFooter.Size, SeekOrigin.End);
            return ChunkFooter.FromStream(reader, stream);
        }
        private int GetChunkSize(ChunkHeader chunkHeader)
        {
            return ChunkHeader.Size + chunkHeader.ChunkDataTotalSize + ChunkFooter.Size;
        }

        private T TryReadForwardInternal<T>(ReaderWorkItem readerWorkItem, long dataPosition, Func<byte[], T> readRecordFunc) where T : ILogRecord
        {
            lock (_freeMemoryObj)
            {
                if (_isMemoryFreed)
                {
                    return default(T);
                }
                var currentDataPosition = DataPosition;

                if (dataPosition + 2 * sizeof(int) > currentDataPosition)
                {
                    throw new ChunkReadException(
                        string.Format("No enough space even for length prefix and suffix, data position: {0}, max data position: {1}, chunk: {2}",
                                      dataPosition, currentDataPosition, this));
                }

                readerWorkItem.Stream.Position = GetStreamPosition(dataPosition);

                var length = readerWorkItem.Reader.ReadInt32();
                if (length <= 0)
                {
                    throw new ChunkReadException(
                        string.Format("Log record at data position {0} has non-positive length: {1} in chunk {2}",
                                      dataPosition, length, this));
                }
                if (length > _chunkConfig.MaxLogRecordSize)
                {
                    throw new ChunkReadException(
                        string.Format("Log record at data position {0} has too large length: {1} bytes, while limit is {2} bytes, in chunk {3}",
                                      dataPosition, length, _chunkConfig.MaxLogRecordSize, this));
                }
                if (dataPosition + length + 2 * sizeof(int) > currentDataPosition)
                {
                    throw new ChunkReadException(
                        string.Format("There is not enough space to read full record (length prefix: {0}), data position: {1}, max data position: {2}, chunk: {3}",
                                      length, dataPosition, currentDataPosition, this));
                }

                var recordBuffer = readerWorkItem.Reader.ReadBytes(length);
                var record = readRecordFunc(recordBuffer);
                if (record == null)
                {
                    throw new ChunkReadException(
                        string.Format("Cannot read a record from data position {0}. Something is seriously wrong in chunk {1}.",
                                      dataPosition, this));
                }

                int suffixLength = readerWorkItem.Reader.ReadInt32();
                if (suffixLength != length)
                {
                    throw new ChunkReadException(
                        string.Format("Prefix/suffix length inconsistency: prefix length({0}) != suffix length ({1}), data position: {2}. Something is seriously wrong in chunk {3}.",
                                      length, suffixLength, dataPosition, this));
                }

                return record;
            }
        }
        private T TryReadFixedSizeForwardInternal<T>(ReaderWorkItem readerWorkItem, long dataPosition, Func<byte[], T> readRecordFunc) where T : ILogRecord
        {
            lock (_freeMemoryObj)
            {
                if (_isMemoryFreed)
                {
                    return default(T);
                }
                var currentDataPosition = DataPosition;

                if (dataPosition + _chunkConfig.ChunkDataUnitSize > currentDataPosition)
                {
                    throw new ChunkReadException(
                        string.Format("No enough space for fixed data record, data position: {0}, max data position: {1}, chunk: {2}",
                                      dataPosition, currentDataPosition, this));
                }

                var startStreamPosition = GetStreamPosition(dataPosition);
                readerWorkItem.Stream.Position = startStreamPosition;

                var recordBuffer = readerWorkItem.Reader.ReadBytes(_chunkConfig.ChunkDataUnitSize);
                var record = readRecordFunc(recordBuffer);
                if (record == null)
                {
                    throw new ChunkReadException(
                            string.Format("Read fixed record from data position: {0} failed, max data position: {1}. Something is seriously wrong in chunk {2}",
                                          dataPosition, currentDataPosition, this));
                }

                var recordLength = readerWorkItem.Stream.Position - startStreamPosition;
                if (recordLength != _chunkConfig.ChunkDataUnitSize)
                {
                    throw new ChunkReadException(
                            string.Format("Invalid fixed record length, expected length {0}, but was {1}, dataPosition: {2}. Something is seriously wrong in chunk {3}",
                                          _chunkConfig.ChunkDataUnitSize, recordLength, dataPosition, this));
                }

                return record;
            }
        }

        private bool TryParsingDataPosition<T>(Func<byte[], T> readRecordFunc, out ChunkHeader chunkHeader, out int dataPosition) where T : ILogRecord
        {
            using (var fileStream = new FileStream(_filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, _chunkConfig.ChunkReadBuffer, FileOptions.None))
            {
                using (var reader = new BinaryReader(fileStream))
                {
                    chunkHeader = ReadHeader(fileStream, reader);

                    fileStream.Position = ChunkHeader.Size;

                    var startStreamPosition = fileStream.Position;
                    var maxStreamPosition = fileStream.Length - ChunkFooter.Size;
                    var isFixedDataSize = IsFixedDataSize();

                    while (fileStream.Position < maxStreamPosition)
                    {
                        var success = false;
                        if (isFixedDataSize)
                        {
                            success = TryReadFixedSizeRecord(fileStream, reader, maxStreamPosition, readRecordFunc);
                        }
                        else
                        {
                            success = TryReadRecord(fileStream, reader, maxStreamPosition, readRecordFunc);
                        }

                        if (success)
                        {
                            startStreamPosition = fileStream.Position;
                        }
                        else
                        {
                            break;
                        }
                    }

                    if (startStreamPosition != fileStream.Position)
                    {
                        fileStream.Position = startStreamPosition;
                    }

                    dataPosition = (int)fileStream.Position - ChunkHeader.Size;

                    return true;
                }
            }
        }
        private bool TryReadRecord<T>(FileStream stream, BinaryReader reader, long maxStreamPosition, Func<byte[], T> readRecordFunc) where T : ILogRecord
        {
            try
            {
                var startStreamPosition = stream.Position;
                if (startStreamPosition + 2 * sizeof(int) > maxStreamPosition)
                {
                    return false;
                }

                var length = reader.ReadInt32();
                if (length <= 0 || length > _chunkConfig.MaxLogRecordSize)
                {
                    return false;
                }
                if (startStreamPosition + length + 2 * sizeof(int) > maxStreamPosition)
                {
                    return false;
                }

                var recordBuffer = reader.ReadBytes(length);
                var record = readRecordFunc(recordBuffer);
                if (record == null)
                {
                    return false;
                }

                int suffixLength = reader.ReadInt32();
                if (suffixLength != length)
                {
                    return false;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }
        private bool TryReadFixedSizeRecord<T>(FileStream stream, BinaryReader reader, long maxStreamPosition, Func<byte[], T> readRecordFunc) where T : ILogRecord
        {
            try
            {
                var startStreamPosition = stream.Position;
                if (startStreamPosition + _chunkConfig.ChunkDataUnitSize > maxStreamPosition)
                {
                    return false;
                }

                var recordBuffer = reader.ReadBytes(_chunkConfig.ChunkDataUnitSize);
                var record = readRecordFunc(recordBuffer);
                if (record == null)
                {
                    return false;
                }

                var recordLength = stream.Position - startStreamPosition;
                if (recordLength != _chunkConfig.ChunkDataUnitSize)
                {
                    Log.Error("Invalid fixed data length, expected length {0}, but was {1}", _chunkConfig.ChunkDataUnitSize, recordLength);
                    return false;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }
        private static long GetStreamPosition(long dataPosition)
        {
            return ChunkHeader.Size + dataPosition;
        }

        private void SetFileAttributes()
        {
            Helper.EatException(() => File.SetAttributes(_filename, FileAttributes.NotContentIndexed));
        }

        #endregion

        #region 日志
        /// <summary>日志</summary>
        public ILog Log { get; set; } = Logger.Null;
        #endregion

        class CacheItem
        {
            public long RecordPosition;
            public byte[] RecordBuffer;
        }
        class ChunkFileStream : IStream
        {
            public Stream Stream;
            public FlushOption FlushOption;

            public ChunkFileStream(Stream stream, FlushOption flushOption)
            {
                Stream = stream;
                FlushOption = flushOption;
            }

            public long Length
            {
                get
                {
                    return Stream.Length;
                }
            }

            public long Position
            {
                get
                {
                    return Stream.Position;
                }

                set
                {
                    Stream.Position = value;
                }
            }

            public void Dispose()
            {
                Stream.Dispose();
            }

            public void Flush()
            {
                var fileStream = Stream as FileStream;
                if (fileStream != null)
                {
                    if (FlushOption == FlushOption.FlushToDisk)
                    {
                        fileStream.Flush(true);
                    }
                    else
                    {
                        fileStream.Flush();
                    }
                }
                else
                {
                    Stream.Flush();
                }
            }

            public void SetLength(long value)
            {
                Stream.SetLength(value);
            }

            public void Write(byte[] buffer, int offset, int count)
            {
                Stream.Write(buffer, offset, count);
            }
        }
        public override string ToString()
        {
            return string.Format("({0}-#{1})", _chunkManager.Name, _chunkHeader.ChunkNumber);
        }
    }
}
