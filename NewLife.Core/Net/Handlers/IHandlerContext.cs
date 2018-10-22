using System;
using System.Collections.Generic;
using NewLife.Collections;
using NewLife.Data;

namespace NewLife.Net.Handlers
{
    /// <summary>处理器上下文</summary>
    public interface IHandlerContext : IExtend
    {
        /// <summary>管道</summary>
        IPipeline Pipeline { get; set; }

        /// <summary>远程连接</summary>
        ISocketRemote Session { get; set; }

        /// <summary>数据帧</summary>
        IData Data { get; set; }

        /// <summary>处理收到消息</summary>
        /// <param name="message"></param>
        void FireRead(Object message);

        /// <summary>处理消息</summary>
        /// <param name="message"></param>
        Boolean FireWrite(Object message);
    }

    /// <summary>处理器上下文</summary>
    public class HandlerContext : IHandlerContext
    {
        #region 属性
        /// <summary>管道</summary>
        public IPipeline Pipeline { get; set; }

        /// <summary>远程连接</summary>
        public ISocketRemote Session { get; set; }

        /// <summary>数据项</summary>
        public IDictionary<String, Object> Items { get; } = new NullableDictionary<String, Object>();

        /// <summary>设置 或 获取 数据项</summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public Object this[String key] { get => Items[key]; set => Items[key] = value; }

        /// <summary>数据帧</summary>
        public IData Data { get; set; }

        /// <summary>处理收到消息</summary>
        /// <param name="message"></param>
        public void FireRead(Object message)
        {
            var data = Data ?? new ReceivedEventArgs();
            data.Message = message;
            Session.Process(data);
        }

        /// <summary>处理消息</summary>
        /// <param name="message"></param>
        public Boolean FireWrite(Object message)
        {
            if (message == null) return false;

            var session = Session;

            // 发送一包数据
            if (message is Byte[] buf) return session.Send(buf);
            if (message is Packet pk) return session.Send(pk);
            if (message is String str) return session.Send(str.GetBytes());

            // 发送一批数据包
            if (message is IEnumerable<Packet> pks)
            {
                foreach (var item in pks)
                {
                    if (!session.Send(item)) return false;
                }

                return true;
            }

            throw new XException("无法识别消息[{0}]，可能缺少编码处理器", message?.GetType()?.FullName);
        }
        #endregion
    }
}
