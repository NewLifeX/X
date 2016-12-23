using System;
using System.Collections.Generic;
using NewLife.Log;
using NewLife.Queue.Broker.DeleteMessageStrategies;
using NewLife.Queue.Center.Controllers;
using NewLife.Queue.Protocols;
using NewLife.Queue.Scheduling;
using NewLife.Queue.Storage;
using NewLife.Queue.Utilities;

namespace NewLife.Queue.Broker
{
    public class DefaultMessageStore : IMessageStore, IDisposable
    {
        private ChunkManager _chunkManager;
        private ChunkWriter _chunkWriter;
        private ChunkReader _chunkReader;
        private readonly IDeleteMessageStrategy _deleteMessageStragegy;
        private readonly IScheduleService _scheduleService;
        private readonly ILog _logger;
        private long _minConsumedMessagePosition = -1;
        private BufferQueue<MessageLogRecord> _bufferQueue;
        private BufferQueue<BatchMessageLogRecord> _batchMessageBufferQueue;
        private readonly object _lockObj = new object();

        public long MinMessagePosition
        {
            get
            {
                return _chunkManager.GetFirstChunk().ChunkHeader.ChunkDataStartPosition;
            }
        }
        public long CurrentMessagePosition
        {
            get
            {
                return _chunkWriter.CurrentChunk.GlobalDataPosition;
            }
        }
        public int ChunkCount
        {
            get { return _chunkManager.GetChunkCount(); }
        }
        public int MinChunkNum
        {
            get { return _chunkManager.GetFirstChunk().ChunkHeader.ChunkNumber; }
        }
        public int MaxChunkNum
        {
            get { return _chunkManager.GetLastChunk().ChunkHeader.ChunkNumber; }
        }

        public DefaultMessageStore(IDeleteMessageStrategy deleteMessageStragegy )
        {
            _deleteMessageStragegy = deleteMessageStragegy;
            _scheduleService = QueueService.ScheduleService;
            _logger = QueueService.Log;
        }

        public void Load()
        {
            _bufferQueue = new BufferQueue<MessageLogRecord>("MessageBufferQueue", BrokerService.Instance.Setting.MessageWriteQueueThreshold, PersistMessages);
            _batchMessageBufferQueue = new BufferQueue<BatchMessageLogRecord>("BatchMessageBufferQueue", BrokerService.Instance.Setting.BatchMessageWriteQueueThreshold, BatchPersistMessages);
            _chunkManager = new ChunkManager("MessageChunk", BrokerService.Instance.Setting.MessageChunkConfig, BrokerService.Instance.Setting.IsMessageStoreMemoryMode);
            _chunkWriter = new ChunkWriter(_chunkManager);
            _chunkReader = new ChunkReader(_chunkManager, _chunkWriter);

            _chunkManager.Load(ReadMessage);
        }
        public void Start()
        {
            _chunkWriter.Open();
            _scheduleService.StartTask("DeleteMessages", DeleteMessages, 5 * 1000, BrokerService.Instance.Setting.DeleteMessagesInterval);
        }
        public void Shutdown()
        {
            _scheduleService.StopTask("DeleteMessages");
            _chunkWriter.Close();
            _chunkManager.Close();
        }
        public void StoreMessageAsync(IQueue queue, Message message, Action<MessageLogRecord, object> callback, object parameter, string producerAddress)
        {
            lock (_lockObj)
            {
                var record = new MessageLogRecord(
                    message.Topic,
                    message.Code,
                    message.Body,
                    queue.QueueId,
                    queue.NextOffset,
                    message.CreatedTime,
                    DateTime.Now,
                    message.Tag,
                    producerAddress ?? string.Empty,
                    callback,
                    parameter);
                _bufferQueue.EnqueueMessage(record);
                queue.IncrementNextOffset();
            }
        }
        public void BatchStoreMessageAsync(IQueue queue, IEnumerable<Message> messages, Action<BatchMessageLogRecord, object> callback, object parameter, string producerAddress)
        {
            lock (_lockObj)
            {
                var recordList = new List<MessageLogRecord>();
                foreach (var message in messages)
                {
                    var record = new MessageLogRecord(
                        queue.Topic,
                        message.Code,
                        message.Body,
                        queue.QueueId,
                        queue.NextOffset,
                        message.CreatedTime,
                        DateTime.Now,
                        message.Tag,
                        producerAddress ?? string.Empty,
                        null,
                        null);
                    recordList.Add(record);
                    queue.IncrementNextOffset();
                }
                var batchRecord = new BatchMessageLogRecord(recordList, callback, parameter);
                _batchMessageBufferQueue.EnqueueMessage(batchRecord);
            }
        }
        public byte[] GetMessageBuffer(long position)
        {
            var record = _chunkReader.TryReadRecordBufferAt(position);
            if (record != null)
            {
                return record.RecordBuffer;
            }
            return null;
        }
        public QueueMessage GetMessage(long position)
        {
            var buffer = GetMessageBuffer(position);
            if (buffer != null)
            {
                var nextOffset = 0;
                var messageLength = ByteUtil.DecodeInt(buffer, nextOffset, out nextOffset);
                if (messageLength > 0)
                {
                    var message = new QueueMessage();
                    var messageBytes = new byte[messageLength];
                    Buffer.BlockCopy(buffer, nextOffset, messageBytes, 0, messageLength);
                    message.ReadFrom(messageBytes);
                    return message;
                }
            }
            return null;
        }
        public bool IsMessagePositionExist(long position)
        {
            var chunk = _chunkManager.GetChunkFor(position);
            return chunk != null;
        }
        public void UpdateMinConsumedMessagePosition(long minConsumedMessagePosition)
        {
            _minConsumedMessagePosition = minConsumedMessagePosition;
        }

        private void PersistMessages(MessageLogRecord record)
        {
            _chunkWriter.Write(record);
            record.OnPersisted();
        }
        private void BatchPersistMessages(BatchMessageLogRecord batchRecord)
        {
            foreach (var record in batchRecord.Records)
            {
                _chunkWriter.Write(record);
            }
            batchRecord.OnPersisted();
        }

        private void DeleteMessages()
        {
            var chunks = _deleteMessageStragegy.GetAllowDeleteChunks(_chunkManager, _minConsumedMessagePosition);
            foreach (var chunk in chunks)
            {
                if (_chunkManager.RemoveChunk(chunk))
                {
                    _logger.Info("Message chunk #{0} is deleted, chunkPositionScale: [{1}, {2}], minConsumedMessagePosition: {3}",
                        chunk.ChunkHeader.ChunkNumber,
                        chunk.ChunkHeader.ChunkDataStartPosition,
                        chunk.ChunkHeader.ChunkDataEndPosition,
                        _minConsumedMessagePosition);
                }
            }
        }
        private MessageLogRecord ReadMessage(byte[] recordBuffer)
        {
            var record = new MessageLogRecord();
            record.ReadFrom(recordBuffer);
            return record;
        }

        public void Dispose()
        {
            Shutdown();
        }
    }
}
