using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml.Serialization;
using NewLife.Reflection;
using NewLife.Serialization;

namespace NewLife.Messaging
{
    /// <summary>消息基类</summary>
    public abstract class MessageBase : IMessage
    {
        #region 核心读写方法
        /// <summary>从数据流中读取消息</summary>
        /// <param name="stream">数据流</param>
        /// <param name="context">上下文</param>
        /// <returns>是否成功</returns>
        public virtual Boolean Read(Stream stream, Object context)
        {
            var fm = CreateFormatter(true);
            fm.Stream = stream;
#if DEBUG
            if (fm is Binary)
                (fm as Binary).EnableTrace();
            else
                stream = new NewLife.Log.TraceStream(stream);
            fm.Log = NewLife.Log.XTrace.Log;
#endif
            Object obj = this;
            return fm.TryRead(GetType(), ref obj);
        }

        /// <summary>把消息写入到数据流中</summary>
        /// <param name="stream">数据流</param>
        /// <param name="context">上下文</param>
        public virtual void Write(Stream stream, Object context)
        {
            var fm = CreateFormatter(false);
            fm.Stream = stream;
#if DEBUG
            if (fm is Binary)
                (fm as Binary).EnableTrace();
            else
                stream = new NewLife.Log.TraceStream(stream);
            fm.Log = NewLife.Log.XTrace.Log;
#endif
            fm.Write(this);
        }

        /// <summary>消息转为字节数组</summary>
        /// <returns></returns>
        public virtual Byte[] ToArray()
        {
            var ms = new MemoryStream();
            Write(ms, null);
            return ms.ToArray();
        }

        /// <summary>创建序列化器</summary>
        /// <param name="isRead"></param>
        /// <returns></returns>
        protected virtual IFormatterX CreateFormatter(Boolean isRead)
        {
            var fn = new Binary();
            fn.EncodeInt = true;
            fn.UseFieldSize = true;
            fn.UseProperty = false;

            return fn;
        }
        #endregion

        #region 辅助
        /// <summary>输出消息实体</summary>
        /// <returns></returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            foreach (var pi in GetType().GetProperties(true))
            {
                GetMember(pi, -16, sb);
            }
            return sb.ToString();
        }

        /// <summary>获取成员输出</summary>
        /// <param name="pi"></param>
        /// <param name="len"></param>
        /// <param name="sb"></param>
        protected virtual void GetMember(PropertyInfo pi, Int32 len, StringBuilder sb)
        {
            if (sb.Length > 0) sb.AppendLine();

            var v = GetMemberValue(pi);
            sb.AppendFormat("{0," + len + "}: {1}", pi.Name, v);
        }

        /// <summary>获取用于输出的成员值</summary>
        /// <param name="pi"></param>
        /// <returns></returns>
        protected virtual Object GetMemberValue(PropertyInfo pi)
        {
            var v = this.GetValue(pi);

            if (pi.PropertyType == typeof(Byte[]) && v != null)
            {
                var buf = (Byte[])v;
                var len = buf.Length;

                var att = pi.GetCustomAttribute<FieldSizeAttribute>();
                if (att != null && att.Size > 0) len = att.Size;
                v = buf.ToHex("-", 0, len);
            }

            return v;
        }
        #endregion
    }

    /// <summary>消息泛型基类</summary>
    /// <typeparam name="TMessage"></typeparam>
    public abstract class Message<TMessage> : MessageBase where TMessage : Message<TMessage>, new()
    {
        #region 读写
        /// <summary>从流中读取消息</summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static TMessage Read(Stream stream)
        {
            var obj = new TMessage();
            if (!obj.Read(stream, null)) return default(TMessage);

            return (TMessage)obj;
        }

        /// <summary>从字节数组中读取消息</summary>
        /// <param name="buf"></param>
        /// <returns></returns>
        public static TMessage Read(Byte[] buf) { return Read(new MemoryStream(buf)); }
        #endregion
    }
}