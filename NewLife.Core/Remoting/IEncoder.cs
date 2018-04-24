using System;
using System.Collections.Generic;
using NewLife.Data;
using NewLife.Log;
using NewLife.Messaging;

namespace NewLife.Remoting
{
    /// <summary>编码器</summary>
    public interface IEncoder
    {
        /// <summary>编码请求</summary>
        /// <param name="action"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        IMessage Encode(String action, Object args);

        /// <summary>编码响应</summary>
        /// <param name="action"></param>
        /// <param name="code"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        Packet Encode(String action, Int32 code, Object result);

        /// <summary>解码请求</summary>
        /// <param name="msg"></param>
        /// <param name="action"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        Boolean TryGetRequest(IMessage msg, out String action, out IDictionary<String, Object> args);

        /// <summary>解码响应</summary>
        /// <param name="msg"></param>
        /// <param name="code"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        Boolean TryGetResponse(IMessage msg, out Int32 code, out Object result);

        /// <summary>转换为对象</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <returns></returns>
        T Convert<T>(Object obj);

        /// <summary>转换为目标类型</summary>
        /// <param name="obj"></param>
        /// <param name="targetType"></param>
        /// <returns></returns>
        Object Convert(Object obj, Type targetType);

        /// <summary>日志提供者</summary>
        ILog Log { get; set; }
    }

    /// <summary>编码器基类</summary>
    public abstract class EncoderBase
    {
        #region 日志
        /// <summary>日志提供者</summary>
        public ILog Log { get; set; } = Logger.Null;

        /// <summary>写日志</summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public virtual void WriteLog(String format, params Object[] args) => Log.Info(format, args);
        #endregion
    }
}