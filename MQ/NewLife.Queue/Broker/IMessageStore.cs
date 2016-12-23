using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NewLife.Queue.Protocols;

namespace NewLife.Queue.Broker
{
    public interface IMessageStore
    {
        /// <summary>
        /// 
        /// </summary>
        long MinMessagePosition { get; }
        /// <summary>
        /// 
        /// </summary>
        long CurrentMessagePosition { get; }
        /// <summary>
        /// 
        /// </summary>
        int ChunkCount { get; }
        /// <summary>
        /// 
        /// </summary>
        int MinChunkNum { get; }
        /// <summary>
        /// 
        /// </summary>
        int MaxChunkNum { get; }
        /// <summary>
        /// 
        /// </summary>
        void Load();
        /// <summary>
        /// 
        /// </summary>
        void Start();
        /// <summary>
        /// 
        /// </summary>
        void Shutdown();

        void StoreMessageAsync(IQueue queue, Message message, Action<MessageLogRecord, object> callback, object parameter, string producerAddress);
        void BatchStoreMessageAsync(IQueue queue, IEnumerable<Message> messages, Action<BatchMessageLogRecord, object> callback, object parameter, string producerAddress);
        byte[] GetMessageBuffer(long position);
        QueueMessage GetMessage(long position);
        bool IsMessagePositionExist(long position);
        void UpdateMinConsumedMessagePosition(long minConsumedMessagePosition);
    }
}
