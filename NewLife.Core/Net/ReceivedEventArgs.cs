using System;
using System.Collections.Generic;
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
            get { return _Data; }
            set
            {
                _Data = value;
                if (value != null)
                    Length = value.Length;
                else
                    Length = 0;
            }
        }

        private Int32 _Length;
        /// <summary>数据长度</summary>
        public Int32 Length { get { return _Length; } set { _Length = value; } }

        private Stream _Stream;
        /// <summary>数据流</summary>
        public Stream Stream { get { return _Stream ?? (_Stream = new MemoryStream(Data, 0, Length)); } set { _Stream = value; } }

        private Boolean _Feedback;
        /// <summary>是否把数据反馈给对方</summary>
        public Boolean Feedback { get { return _Feedback; } set { _Feedback = value; } }

        private Object _UserState;
        /// <summary>用户数据</summary>
        public Object UserState { get { return _UserState; } set { _UserState = value; } }

        //private IDictionary<String, Object> _Properties = new Dictionary<String, Object>(StringComparer.OrdinalIgnoreCase);
        ///// <summary>属性字典</summary>
        //public IDictionary<String, Object> Properties { get { return _Properties; } set { _Properties = value; } }
        #endregion

        #region 构造
        /// <summary>实例化一个数据事件参数</summary>
        public ReceivedEventArgs() { }

        /// <summary>使用字节数组实例化一个数据事件参数</summary>
        /// <param name="data"></param>
        public ReceivedEventArgs(Byte[] data)
        {
            Data = data;
            Length = data.Length;
        }
        #endregion
    }
}