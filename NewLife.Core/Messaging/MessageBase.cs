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
        /// <param name="stream"></param>
        /// <returns>是否成功</returns>
        public virtual Boolean Read(Stream stream)
        {
            var fm = GetFormatter(true);
            fm.Stream = stream;
            Object obj = this;
            return fm.TryRead(this.GetType(), ref obj);
        }

        /// <summary>把消息写入到数据流中</summary>
        /// <param name="stream"></param>
        public virtual void Write(Stream stream)
        {
            var fm = GetFormatter(false);
            fm.Stream = stream;
            fm.Write(this);
        }

        /// <summary>消息转为字节数组</summary>
        /// <returns></returns>
        public virtual Byte[] ToArray()
        {
            var ms = new MemoryStream();
            Write(ms);
            return ms.ToArray();
        }

        /// <summary>获取序列化器</summary>
        /// <param name="isRead"></param>
        /// <returns></returns>
        protected virtual IFormatterX GetFormatter(Boolean isRead)
        {
            var binary = new Binary();

            return binary;
        }
        #endregion

        #region 辅助
        /// <summary>输出消息实体</summary>
        /// <returns></returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            foreach (var pi in this.GetType().GetProperties())
            {
                if (pi.GetCustomAttribute<XmlIgnoreAttribute>() != null) continue;

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
                v = buf.ToHex();
            }

            return v;
        }
        #endregion
    }

    ///// <summary>消息泛型基类</summary>
    ///// <typeparam name="TMessage"></typeparam>
    //public abstract class Message<TMessage> : MessageBase where TMessage : Message<TMessage>, new()
    //{
    //    public static TMessage Read(Stream stream)
    //    {
    //        var msg = new TMessage();
    //        if (msg.Read(stream)) return msg;

    //        return default(TMessage);
    //    }
    //}
}