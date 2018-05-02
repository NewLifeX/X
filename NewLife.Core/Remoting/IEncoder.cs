using System;
using System.IO;
using NewLife.Data;
using NewLife.Log;
using NewLife.Messaging;

namespace NewLife.Remoting
{
    /// <summary>编码器</summary>
    public interface IEncoder
    {
        /// <summary>编码</summary>
        /// <param name="action"></param>
        /// <param name="code"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        Packet Encode(String action, Int32 code, Object value);

        /// <summary>解码请求</summary>
        /// <param name="msg"></param>
        /// <param name="action"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        Boolean Decode(IMessage msg, out String action, out Object value);

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
        #region 方法
        /// <summary>编码</summary>
        /// <param name="action"></param>
        /// <param name="code"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public Packet Encode(String action, Int32 code, Object value)
        {
            var ms = new MemoryStream();
            ms.Seek(4, SeekOrigin.Begin);
            var writer = new BinaryWriter(ms);
            writer.Write(action);

            // 其它数据作为附加
            var len = 0;
            Packet pk2 = null;
            if (code != 0 || value != null)
            {
                pk2 = OnEncode(action, code, value);
                // 写入长度
                len = pk2.Total;
            }

            if (len == 0) WriteLog("{0}=>", action);

            // 不管有没有附加数据，都会写入长度
            ms.WriteEncodedInt(len);

            var pk = new Packet(ms.GetBuffer(), 4, (Int32)ms.Length - 4);
            if (pk2 != null) pk.Next = pk2;

            return pk;
        }

        /// <summary>编码</summary>
        /// <param name="action"></param>
        /// <param name="code"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        protected abstract Packet OnEncode(String action, Int32 code, Object value);

        /// <summary>解码</summary>
        /// <param name="msg"></param>
        /// <param name="action"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public Boolean Decode(IMessage msg, out String action, out Object value)
        {
            action = null;
            value = null;

            var ms = msg.Payload.GetStream();
            var reader = new BinaryReader(ms);
            action = reader.ReadString();
            if (action.IsNullOrEmpty()) return false;

            if (ms.Length > ms.Position)
            {
                //var pk = ms.ReadPacket();
                var len = ms.ReadEncodedInt();
                if (len > 0)
                {
                    var pk = msg.Payload.Sub((Int32)ms.Position, len);
                    value = OnDecode(action, pk);
                }
            }

            if (value == null) WriteLog("{0}=>", action);

            return true;
        }

        /// <summary>解码</summary>
        /// <param name="action"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        protected virtual Object OnDecode(String action, Packet data) => data;
        #endregion

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