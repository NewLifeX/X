using System;
using System.IO;
using System.Text;
using NewLife.Data;

namespace NewLife.Net
{
    /// <summary>收到数据时的事件参数</summary>
    public class ReceivedEventArgs : EventArgs
    {
        #region 属性
        /// <summary>数据包</summary>
        public Packet Packet { get; set; }

        /// <summary>数据</summary>
        public Byte[] Data
        {
            get { return Packet.ToArray(); }
            set { Packet.Set(value); }
        }

        /// <summary>数据长度</summary>
        public Int32 Length => Packet.Count;

        /// <summary>数据区对应的一个数据流实例</summary>
        public Stream Stream => Packet.GetStream();

        /// <summary>解码后的消息</summary>
        public Object Message { get; set; }

        /// <summary>用户数据。比如远程地址等</summary>
        public Object UserState { get; set; }
        #endregion

        #region 构造
        /// <summary>实例化一个数据事件参数</summary>
        public ReceivedEventArgs() { }

        /// <summary>使用字节数组实例化一个数据事件参数</summary>
        /// <param name="pk"></param>
        public ReceivedEventArgs(Packet pk) => Packet = pk;
        #endregion

        #region 方法
        /// <summary>以字符串表示</summary>
        /// <param name="encoding">字符串编码，默认URF-8</param>
        /// <returns></returns>
        public String ToStr(Encoding encoding = null) => Packet?.ToStr(encoding);

        /// <summary>以十六进制编码表示</summary>
        /// <param name="maxLength">最大显示多少个字节。默认-1显示全部</param>
        /// <param name="separate">分隔符</param>
        /// <param name="groupSize">分组大小，为0时对每个字节应用分隔符，否则对每个分组使用</param>
        /// <returns></returns>
        public String ToHex(Int32 maxLength = 32, String separate = "-", Int32 groupSize = 0) => Packet?.ToHex(maxLength, separate, groupSize);
        #endregion
    }
}