using System;
using NewLife.Linq;
using System.Xml.Serialization;
using System.Collections.Generic;
using System.IO;
using System.Collections;
using NewLife.Exceptions;

namespace NewLife.Messaging
{
    /// <summary>组消息</summary>
    /// <remarks>
    /// 对于超长消息，可拆分为多个组消息进行传输，然后在目的地重组。
    /// </remarks>
    public class GroupMessage : Message
    {
        #region 属性
        /// <summary>消息类型</summary>
        [XmlIgnore]
        public override MessageKind Kind { get { return MessageKind.Group; } }

        private Int32 _Identity;
        /// <summary>唯一标识</summary>
        public Int32 Identity { get { return _Identity; } set { _Identity = value; } }

        private Int32 _Index;
        /// <summary>在组中的索引位置</summary>
        public Int32 Index { get { return _Index; } set { _Index = value; } }

        private Int32 _Count;
        /// <summary>分组数</summary>
        public Int32 Count { get { return _Count; } set { _Count = value; } }

        private Byte[] _Data;
        /// <summary>数据</summary>
        public Byte[] Data { get { return _Data; } set { _Data = value; } }
        #endregion

        #region 方法
        #endregion

        #region 辅助
        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override string ToString()
        {
            var len = Data == null ? 0 : Data.Length;
            return String.Format("{0} {1} {2}/{3} Length={4}", base.ToString(), Identity, Index, Count, len);
        }
        #endregion
    }

    /// <summary>消息组。</summary>
    public class MessageGroup : IEnumerable<GroupMessage>
    {
        #region 属性
        private static Int32 _gid = 1;
        private Int32 _Identity = _gid++;
        /// <summary>唯一标识</summary>
        public Int32 Identity { get { return _Identity; } set { _Identity = value; } }

        private List<GroupMessage> _Items;
        /// <summary>消息集合</summary>
        List<GroupMessage> Items { get { return _Items ?? (_Items = new List<GroupMessage>()); } /*set { _Items = value; }*/ }
        #endregion

        #region 拆分
        /// <summary>拆分数据流为多个消息</summary>
        /// <param name="stream"></param>
        /// <param name="size"></param>
        public void Split(Stream stream, Int32 size)
        {
            var count = (Int32)Math.Ceiling((Double)(stream.Length - stream.Position) / size);

            // 估计组消息头部长度。
            var len = 1 + 1 + 1 + 1;
            // 如果标识大于128，增加
            if (Identity >= 128) len += 3;
            // 如果数据包个数大于128时，增加
            if (count >= 128) len += 3 + 3;

            // 加上数据大小
            len += size >= 128 ? 4 : 1;

            // 计算数据部分大小
            var index = 0;
            while (stream.Position < stream.Length)
            {
                var msg = new GroupMessage();
                msg.Identity = Identity;
                msg.Index = ++index;
                msg.Count = count;

                var len2 = stream.Length - stream.Position;
                if (len2 > size - len) len2 = size - len;
                var buffer = new Byte[len2];
                stream.Read(buffer, 0, buffer.Length);
                msg.Data = buffer;

                Items.Add(msg);
            }
        }
        #endregion

        #region 重组
        /// <summary>添加组消息，返回是否已收到所有组消息。</summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public Boolean Add(GroupMessage message)
        {
            if (Items.Count <= 0)
                Identity = message.Identity;
            else if (message.Identity != Identity)
                throw new ArgumentException("组消息的标识不匹配！", "message");

            if (!Items.Any(e => e.Index == message.Index)) Items.Add(message);

            return Items.Count > 0 && Items.Count == Items[0].Count;
        }
        #endregion

        #region 输出
        /// <summary>获取整个数据流</summary>
        /// <returns></returns>
        public Stream GetStream()
        {
            var ms = new MemoryStream();
            foreach (var item in Items.OrderBy(e => e.Index))
            {
                ms.Write(item.Data, 0, item.Data.Length);
            }
            ms.Position = 0;
            return ms;
        }

        /// <summary>获取整个消息</summary>
        /// <returns></returns>
        public Message GetMessage() { return Message.Read(GetStream()); }
        #endregion

        #region 辅助
        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override string ToString()
        {
            var ts = Items;
            var count = ts.Count > 0 ? ts[0].Count : 0;
            return String.Format("{0} {1}/{2}", Identity, ts.Count, count);
        }
        #endregion

        #region IEnumerable<GroupMessage> 成员
        IEnumerator<GroupMessage> IEnumerable<GroupMessage>.GetEnumerator() { return Items.GetEnumerator(); }

        IEnumerator IEnumerable.GetEnumerator() { return (this as IEnumerable<GroupMessage>).GetEnumerator(); }
        #endregion
    }
}