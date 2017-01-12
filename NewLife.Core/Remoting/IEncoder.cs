using System;
using System.Collections.Generic;
using NewLife.Data;
using NewLife.Log;

namespace NewLife.Remoting
{
    /// <summary>编码器</summary>
    public interface IEncoder
    {
        /// <summary>编码对象</summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        Byte[] Encode(Object obj);

        /// <summary>编码请求</summary>
        /// <param name="action"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        Byte[] Encode(String action, Object args);

        /// <summary>编码响应</summary>
        /// <param name="code"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        Byte[] Encode(Int32 code, Object result);

        /// <summary>解码成为字典</summary>
        /// <param name="pk">数据包</param>
        /// <returns></returns>
        IDictionary<String, Object> Decode(Packet pk);

        /// <summary>解码响应</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dic"></param>
        /// <returns></returns>
        T Decode<T>(IDictionary<String, Object> dic);

        /// <summary>解码请求</summary>
        /// <param name="dic"></param>
        /// <param name="action"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        Boolean TryGet(IDictionary<String, Object> dic, out String action, out Object args);

        /// <summary>解码响应</summary>
        /// <param name="dic"></param>
        /// <param name="code"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        Boolean TryGet(IDictionary<String, Object> dic, out Int32 code, out Object result);

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
    public abstract class EncoderBase : IEncoder
    {
        /// <summary>编码对象</summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public abstract Byte[] Encode(Object obj);

        /// <summary>编码请求</summary>
        /// <param name="action"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public virtual Byte[] Encode(String action, Object args)
        {
            var obj = new { action, args };
            return Encode(obj);
        }

        /// <summary>编码响应</summary>
        /// <param name="code"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public virtual Byte[] Encode(Int32 code, Object result)
        {
            // 不支持序列化异常
            var ex = result as Exception;
            if (ex != null) result = ex.GetTrue()?.Message;

            var obj = new { code, result };
            return Encode(obj);
        }

        /// <summary>解码成为字典</summary>
        /// <param name="pk"></param>
        /// <returns></returns>
        public abstract IDictionary<String, Object> Decode(Packet pk);

        /// <summary>解码响应</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dic"></param>
        /// <returns></returns>
        public virtual T Decode<T>(IDictionary<String, Object> dic)
        {
            if (dic == null) return default(T);

            // 是否成功
            var code = dic["code"].ToInt();
            var result = dic["result"];
            if (code != 0) throw new Exception(result + "");

            // 返回
            return Convert<T>(result);
        }

        /// <summary>解码请求</summary>
        /// <param name="dic"></param>
        /// <param name="action"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public virtual Boolean TryGet(IDictionary<String, Object> dic, out String action, out Object args)
        {
            action = null;
            args = null;

            Object act = null;
            if (!dic.TryGetValue("action", out act)) return false;

            // 参数可能不存在
            dic.TryGetValue("args", out args);

            action = act + "";

            return true;
        }

        /// <summary>解码响应</summary>
        /// <param name="dic"></param>
        /// <param name="code"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public virtual Boolean TryGet(IDictionary<String, Object> dic, out Int32 code, out Object result)
        {
            code = 0;
            result = null;

            Object cod = null;
            Object obj = null;
            if (!dic.TryGetValue("code", out cod)) return false;

            // 参数可能不存在
            dic.TryGetValue("result", out obj);

            code = (Int32)cod;
            result = obj;

            return true;
        }

        /// <summary>转换为对象</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <returns></returns>
        public virtual T Convert<T>(Object obj)
        {
            return (T)Convert(obj, typeof(T));
        }

        /// <summary>转换为目标类型</summary>
        /// <param name="obj"></param>
        /// <param name="targetType"></param>
        /// <returns></returns>
        public abstract Object Convert(Object obj, Type targetType);

        #region 日志
        /// <summary>日志提供者</summary>
        public ILog Log { get; set; } = Logger.Null;

        /// <summary>写日志</summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public virtual void WriteLog(String format, params Object[] args)
        {
            Log.Info(format, args);
        }
        #endregion
    }
}