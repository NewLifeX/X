using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using NewLife.Linq;
using NewLife.Serialization;

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

        /// <summary>第一个组消息，上面有总记录数</summary>
        public GroupMessage First { get { return Items.Count > 0 ? Items[0] : null; } }

        private Int32 _Total;
        /// <summary>总的组消息数</summary>
        public Int32 Total { get { return _Total; } private set { _Total = value; } }

        /// <summary>组消息个数</summary>
        public Int32 Count { get { return Items.Count; } }
        #endregion

        #region 拆分
        /// <summary>拆分数据流为多个消息</summary>
        /// <param name="stream"></param>
        /// <param name="size"></param>
        /// <param name="header"></param>
        public void Split(Stream stream, Int32 size, MessageHeader header = null)
        {
            // 组包消息全部采用消息头长度，这里估算，方便内部预留大小，如果包大小刚好在边界上，可能会浪费一个字节空间，不过这个可能性很小
            if (header == null) header = new MessageHeader();
            header.Length = size;

            // 消息头大小
            var headerLength = 0;
            if (header != null) headerLength = header.ToArray().Length;

            // 先估算数据包个数
            var count = (Int32)Math.Ceiling((Double)(stream.Length - stream.Position) / (size - (1 + 4 * 3 + 1) - headerLength));

            // 估计组消息头部长度，最大化构造包。
            // Kind + 消息头
            var len = 1 + headerLength;
            // Identity
            len += GetBytesCount(Identity);
            // Count
            len += 1;

            // 加上数据包大小。因为压缩整数的存在，这里不是绝对准确，但是大多数时候不会有问题
            len += GetBytesCount(size);
            // !!!不要忘了数据部分的对象引用
            len += 1;

            // 计算数据部分大小
            var index = Items.Count;
            while (stream.Position < stream.Length)
            {
                var msg = new GroupMessage();
                msg.Header = header.Clone();
                msg.Identity = Identity;
                msg.Index = ++index;
                //msg.Count = count;

                // 加上索引长度，计算真正的头部长度
                var trueLen = len + GetBytesCount(msg.Index);
                // 第一个元素采用精确Count
                if (msg.Index == 1) trueLen += GetBytesCount(count) - 1;

                var len2 = stream.Length - stream.Position;
                if (len2 > size - trueLen) len2 = size - trueLen;
                //var buffer = new Byte[len2];
                //stream.Read(buffer, 0, buffer.Length);
                //msg.Data = buffer;
                msg.Data = stream.ReadBytes(len2);

                // 减去Header以外的全部长度
                msg.Header.Length = (Int32)(trueLen + len2) - headerLength;

                // 最后一个修正长度，因为数据量可能很少，前面的GetBytesCount(size)可能不对。
                if (len2 < size - trueLen) msg.Header.Length += GetBytesCount(len2) - GetBytesCount(size);

                Items.Add(msg);
            }

            //if (Items.Count > 0) Items[0].Count = Items.Count;
            // 第一个组消息的Count是准确的，总数Count小于128的全部使用实际总数Count，其它都是用0
            count = Items.Count;
            var isLittle = count < 128;
            foreach (var item in Items)
            {
                item.Count = isLittle ? count : 0;
            }
            if (count > 0) Items[0].Count = count;

            Total = count;
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

            if (message.Index == 1) Total = message.Count;

            return Items.Count > 0 && Items.Count == Total;
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

        static Int32 GetBytesCount(Int64 n) { return BinaryWriterX.GetEncodedIntSize(n); }
        #endregion

        #region IEnumerable<GroupMessage> 成员
        IEnumerator<GroupMessage> IEnumerable<GroupMessage>.GetEnumerator() { return Items.GetEnumerator(); }

        IEnumerator IEnumerable.GetEnumerator() { return (this as IEnumerable<GroupMessage>).GetEnumerator(); }
        #endregion
    }
}