using System;
using System.Collections.Generic;
using System.Text;

namespace NewLife.Net
{
    /// <summary>收到数据时的事件参数</summary>
    public class ReceivedEventArgs : EventArgs
    {
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

        private Boolean _Feedback;
        /// <summary>是否把数据反馈给对方</summary>
        public Boolean Feedback { get { return _Feedback; } set { _Feedback = value; } }

        //private IDictionary<String, Object> _Properties = new Dictionary<String, Object>(StringComparer.OrdinalIgnoreCase);
        ///// <summary>属性字典</summary>
        //public IDictionary<String, Object> Properties { get { return _Properties; } set { _Properties = value; } }
    }
}