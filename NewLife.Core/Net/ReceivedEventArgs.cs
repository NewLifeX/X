using System;
using System.IO;
using System.Text;

namespace NewLife.Net
{
    /// <summary>收到数据时的事件参数</summary>
    public class ReceivedEventArgs : EventArgs
    {
        #region 属性
        private Byte[] _Data;
        /// <summary>数据</summary>
        public Byte[] Data
        {
            get
            {
                if (_Data == null && Stream != null) _Data = GetData();
                return _Data;
            }
            set
            {
                _Data = value;
                _Stream = new MemoryStream(_Data, false);
            }
        }

        /// <summary>数据长度</summary>
        public Int32 Length { get { return (Int32)Stream.Length; } }

        private Stream _Stream;
        /// <summary>数据区对应的一个数据流实例</summary>
        public Stream Stream { get { return _Stream; } set { _Stream = value; _Data = null; } }

        /// <summary>用户数据。比如远程地址等</summary>
        public Object UserState { get; set; }
        #endregion

        #region 构造
        /// <summary>实例化一个数据事件参数</summary>
        public ReceivedEventArgs() { }

        /// <summary>使用字节数组实例化一个数据事件参数</summary>
        /// <param name="data"></param>
        public ReceivedEventArgs(Byte[] data)
        {
            Data = data;
            //Length = data.Length;
            //Stream = new MemoryStream(data, false);
        }
        #endregion

        #region 方法
        /// <summary>读取数据，不改变数据流指针</summary>
        /// <returns></returns>
        public Byte[] GetData()
        {
            if (Stream is MemoryStream)
                return (Stream as MemoryStream).ToArray();

            var ms = Stream;
            var p = ms.Position;
            //ms.Position = 0;
            var data = ms.ReadBytes();
            ms.Position = p;
            return data;
        }

        /// <summary>以字符串表示</summary>
        /// <param name="encoding">字符串编码，默认URF-8</param>
        /// <returns></returns>
        public String ToStr(Encoding encoding = null)
        {
            var ms = Stream;
            if (ms == null || ms.Length <= 0) return String.Empty;

            if (encoding == null) encoding = Encoding.UTF8;

            return Data.ToStr(encoding, 0, Length);
        }

        /// <summary>以十六进制编码表示</summary>
        /// <param name="maxLength">最大显示多少个字节。默认-1显示全部</param>
        /// <param name="separate">分隔符</param>
        /// <param name="groupSize">分组大小，为0时对每个字节应用分隔符，否则对每个分组使用</param>
        /// <returns></returns>
        public String ToHex(Int32 maxLength = 32, String separate = "-", Int32 groupSize = 0)
        {
            var ms = Stream;
            if (ms == null || ms.Length <= 0) return String.Empty;

            return Data.ToHex(separate, groupSize, Math.Min(Length, maxLength));
        }
        #endregion
    }
}