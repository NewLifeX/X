using System;
using System.Collections.Generic;
using System.Net;
using NewLife.Collections;
using NewLife.Data;

namespace NewLife.Net.Handlers
{
    /// <summary>处理器上下文</summary>
    public interface IHandlerContext
    {
        /// <summary>管道</summary>
        IPipeline Pipeline { get; set; }

        /// <summary>远程连接</summary>
        ISocketRemote Session { get; set; }

        /// <summary>数据项</summary>
        IDictionary<String, Object> Items { get; }

        /// <summary>设置 或 获取 数据项</summary>
        /// <param name="key"></param>
        /// <returns></returns>
        Object this[String key] { get; set; }

        /// <summary>数据帧</summary>
        IData Data { get; set; }

        ///// <summary>处理完成后的回调</summary>
        //Action<IHandlerContext, Object> Finish { get; set; }

        /// <summary>处理收到消息</summary>
        /// <param name="message"></param>
        void FireRead(Object message);
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

        ///// <summary>处理完成后的回调</summary>
        //public Action<IHandlerContext, Object> Finish { get; set; }

        /// <summary>处理收到消息</summary>
        /// <param name="message"></param>
        public void FireRead(Object message)
        {
            var data = Data ?? new ReceivedEventArgs();
            data.Message = message;
            Session.Receive(data);
        }
        #endregion
    }
}
