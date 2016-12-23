using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using NewLife.Queue.Protocols.Brokers;
using NewLife.Queue.Storage;
using NewLife.Queue.Storage.FileNamingStrategies;
using NewLife.Queue.Utilities;

namespace NewLife.Queue.Broker
{
    public class BrokerSetting
    {
        /// <summary>Broker基本配置信息
        /// </summary>
        public BrokerInfo BrokerInfo { get; set; }
        /// <summary>NameServer地址列表
        /// </summary>
        public IEnumerable<IPEndPoint> NameServerList { get; set; }
        /// <summary>消息到达时是否立即通知相关的PullRequest，默认为true；
        /// <remarks>
        /// 如果希望当前场景消息吞吐量不大且要求消息消费的实时性更高，可以考虑设置为true；设置为false时，最多在<see cref="CheckBlockingPullRequestMilliseconds"/>
        /// 的时间后，PullRequest会被通知到有新消息；也就是说，设置为false时，消息最多延迟<see cref="CheckBlockingPullRequestMilliseconds"/>。
        /// </remarks>
        /// </summary>
        public bool NotifyWhenMessageArrived { get; set; }
        /// <summary>Broker定期向NameServer注册信息的时间间隔，默认为5秒钟；
        /// </summary>
        public int RegisterBrokerToNameServerInterval { get; set; }
        /// <summary>删除符合删除条件的消息的定时间隔，默认为10秒钟；
        /// </summary>
        public int DeleteMessagesInterval { get; set; }
        /// <summary>删除符合删除条件的队列中的消息索引的定时间隔；一定是消息先被删除后，该消息索引才会从队列中删除；默认为10秒钟；
        /// </summary>
        public int DeleteQueueMessagesInterval { get; set; }
        /// <summary>表示删除消息文件时，是否需要忽略未消费过的消息，也就是说一满足删除策略条件，就直接删除，不关心这些消息是否还没消费过；默认值未True；
        /// </summary>
        public bool DeleteMessageIgnoreUnConsumed { get; set; }
        /// <summary>持久化消息消费进度的间隔，默认为1s；
        /// </summary>
        public int PersistConsumeOffsetInterval { get; set; }
        /// <summary>扫描PullRequest对应的队列是否有新消息的时间间隔，默认为1s；
        /// </summary>
        public int CheckBlockingPullRequestMilliseconds { get; set; }
        /// <summary>判断生产者者不在线的超时时间，默认为10s；即如果一个生产者10s不发送心跳到Broker，则认为不在线；Broker自动会关闭该生产者的连接并从生产者列表中移除；
        /// </summary>
        public int ProducerExpiredTimeout { get; set; }
        /// <summary>判断消费者心跳的超时时间，默认为10s；即如果一个消费者10s不发送心跳到Broker，则认为不在线；Broker自动会关闭该消费者的连接并从消费者列表中移除；
        /// </summary>
        public int ConsumerExpiredTimeout { get; set; }
        /// <summary>当消费者的链接断开时，是否需要立即将消费者从消费者列表中移除；如果为True，则消费者链接断开时立即移除；否则，会等到消费者心跳的超时时间到达后才会移除该消费者。默认为True
        /// </summary>
        public bool RemoveConsumerWhenDisconnect { get; set; }
        /// <summary>是否自动创建Topic，默认为true；线上环境建议设置为false，Topic应该总是由后台管理控制台来创建；
        /// </summary>
        public bool AutoCreateTopic { get; set; }
        /// <summary>创建Topic时，默认创建的队列数，默认为4；
        /// </summary>
        public int TopicDefaultQueueCount { get; set; }
        /// <summary>一个Topic下最多允许的队列数，默认为256；
        /// </summary>
        public int TopicMaxQueueCount { get; set; }
        /// <summary>消息最大允许的字节数，默认为4MB；
        /// </summary>
        public int MessageMaxSize { get; set; }
        /// <summary>消息写入缓冲队列限流阈值，默认为20000；
        /// </summary>
        public int MessageWriteQueueThreshold { get; set; }
        /// <summary>批量消息写入缓冲队列限流阈值，默认为10000；
        /// </summary>
        public int BatchMessageWriteQueueThreshold { get; set; }
        /// <summary>表示是否在内存模式下运行，内存模式下消息都不存储到文件，仅保存在内存；默认为False；
        /// </summary>
        public bool IsMessageStoreMemoryMode { get; set; }
        /// <summary>EQueue存储文件的根目录
        /// </summary>
        public string FileStoreRootPath { get; private set; }
        /// <summary>最新消息显示个数，默认为100个；
        /// </summary>
        public int LatestMessageShowCount { get; set; }

        /// <summary>消息文件存储的相关配置，默认一个消息文件的大小为256MB
        /// </summary>
        public ChunkManagerConfig MessageChunkConfig { get; set; }
        /// <summary>队列文件存储的相关配置，默认一个队列文件中存储100W个消息索引，每个消息索引8个字节
        /// </summary>
        public ChunkManagerConfig QueueChunkConfig { get; set; }

        public BrokerSetting(bool isMessageStoreMemoryMode = false, string chunkFileStoreRootPath = @"c:\equeue-store", int messageChunkDataSize = 1024 * 1024 * 1024, int chunkFlushInterval = 100, int chunkCacheMaxPercent = 75, int chunkCacheMinPercent = 40, int maxLogRecordSize = 5 * 1024 * 1024, int chunkWriteBuffer = 128 * 1024, int chunkReadBuffer = 128 * 1024, bool syncFlush = false, FlushOption flushOption = FlushOption.FlushToOS, bool enableCache = true, int messageChunkLocalCacheSize = 300000, int queueChunkLocalCacheSize = 10000)
        {
            BrokerInfo = new BrokerInfo(
                "DefaultBroker",
                "DefaultGroup",
                "DefaultCluster",
                BrokerRole.Master,
                new IPEndPoint(SocketUtils.GetLocalIPV4(), 5000).ToAddress(),
                new IPEndPoint(SocketUtils.GetLocalIPV4(), 5001).ToAddress(),
                new IPEndPoint(SocketUtils.GetLocalIPV4(), 5002).ToAddress());

            NameServerList = new List<IPEndPoint>()
            {
                new IPEndPoint(SocketUtils.GetLocalIPV4(), 9493)
            };

            NotifyWhenMessageArrived = true;
            RegisterBrokerToNameServerInterval = 1000 * 5;
            DeleteMessagesInterval = 1000 * 10;
            DeleteQueueMessagesInterval = 1000 * 10;
            DeleteMessageIgnoreUnConsumed = true;
            PersistConsumeOffsetInterval = 1000 * 1;
            CheckBlockingPullRequestMilliseconds = 1000 * 1;
            ProducerExpiredTimeout = 1000 * 10;
            ConsumerExpiredTimeout = 1000 * 10;
            RemoveConsumerWhenDisconnect = true;
            AutoCreateTopic = true;
            TopicDefaultQueueCount = 4;
            TopicMaxQueueCount = 256;
            MessageMaxSize = 1024 * 1024 * 4;
            MessageWriteQueueThreshold = 2 * 10000;
            BatchMessageWriteQueueThreshold = 1 * 10000;
            IsMessageStoreMemoryMode = isMessageStoreMemoryMode;
            FileStoreRootPath = chunkFileStoreRootPath;
            LatestMessageShowCount = 100;
            MessageChunkConfig = new ChunkManagerConfig(
                Path.Combine(chunkFileStoreRootPath, @"message-chunks"),
                new DefaultFileNamingStrategy("message-chunk-"),
                messageChunkDataSize,
                0,
                0,
                chunkFlushInterval,
                enableCache,
                syncFlush,
                flushOption,
                Environment.ProcessorCount * 8,
                maxLogRecordSize,
                chunkWriteBuffer,
                chunkReadBuffer,
                chunkCacheMaxPercent,
                chunkCacheMinPercent,
                1,
                5,
                messageChunkLocalCacheSize,
                true);
            QueueChunkConfig = new ChunkManagerConfig(
                Path.Combine(chunkFileStoreRootPath, @"queue-chunks"),
                new DefaultFileNamingStrategy("queue-chunk-"),
                0,
                12,
                1000000,
                chunkFlushInterval,
                enableCache,
                syncFlush,
                flushOption,
                Environment.ProcessorCount * 2,
                12,
                chunkWriteBuffer,
                chunkReadBuffer,
                chunkCacheMaxPercent,
                chunkCacheMinPercent,
                1,
                5,
                queueChunkLocalCacheSize,
                false);
        }
    }
}
