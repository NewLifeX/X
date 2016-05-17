using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NewLife.MessageQueue
{
    /// <summary>
    /// 消息发布订阅
    /// </summary>
    public class PubSub<TMessage> : IDisposable2
    {
        /// <summary>
        /// 
        /// </summary>
        public Action<object, TMessage> OnMessage;
        /// <summary>
        /// 
        /// </summary>
        public Action<object[]> OnSuccess;
        /// <summary>
        /// 
        /// </summary>
        public Action<object> OnUnSubscribe;
        /// <summary>
        /// 
        /// </summary>
        public Action<Exception> OnError;
        /// <summary>
        /// 将信息 message 发送到指定的频道 channel 。
        /// </summary>
        /// <param name="channel">频道</param>
        /// <param name="message">信息</param>
        /// <returns>接收到信息 message 的订阅者数量</returns>
        public object Publish(string channel, TMessage message)
        {
            return null;
        }
        /// <summary>
        /// 将信息 message 发送到指定的频道 channel 。
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="message"></param>
        /// <returns>接收到信息 message 的订阅者数量</returns>
        public object Publish(string channel, string message)
        {
            
            return null;
        }
        /// <summary>
        /// 订阅指定的一个或多个频道的信息
        /// http://doc.redisfans.com/pub_sub/subscribe.html
        /// </summary>
        /// <param name="channelName">一个或多个频道的信息 new sub xxx</param>
        /// <returns>订阅成功失败相关信息</returns>
        public object Subscribe(string channelName)
        {
            return null;
        }
        /// <summary>
        /// 订阅一个或多个符合给定模式的频道。
        /// </summary>
        /// <param name="channelName">
        /// 每个模式以 * 作为匹配符，比如 it* 匹配所有以 it 开头的频道( it.news 、 it.blog 、 it.tweets 等等)， 
        /// news.* 匹配所有以 news. 开头的频道( news.it 、 news.global.today 等等)，诸如此类。
        /// </param>
        /// <returns>订阅成功失败相关信息</returns>
        public object PSubscribe(string channelName)
        {
            return null;
        }
        /// <summary>
        /// 退订指定的一个或多个频道的信息
        /// </summary>
        /// <param name="channelName">
        /// 如果没有频道被指定，也即是，一个无参数的 UNSUBSCRIBE 调用被执行，
        /// 那么客户端使用 SUBSCRIBE 命令订阅的所有频道都会被退订。在这种情况下，命令会返回一个信息，告知客户端所有被退订的频道。
        /// </param>
        /// <returns>退订成功失败相关信息</returns>
        public object UnSubscribe(string channelName)
        {
            return null;
        }
        /// <summary>
        /// 退订阅一个或多个符合给定模式的频道。
        /// </summary>
        /// <param name="channelName">
        /// 每个模式以 * 作为匹配符，比如 it* 匹配所有以 it 开头的频道( it.news 、 it.blog 、 it.tweets 等等)， 
        /// news.* 匹配所有以 news. 开头的频道( news.it 、 news.global.today 等等)，诸如此类。
        /// </param>
        /// <returns>退订成功失败相关信息</returns>
        public object UnPSubscribe(string channelName)
        {
            return null;
        }

        public void Dispose()
        {
   
        }

        public bool Disposed { get; }
        public event EventHandler OnDisposed;
    }
}
