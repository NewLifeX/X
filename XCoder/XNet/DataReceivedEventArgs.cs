using System;
using System.Collections.Generic;
using System.Text;

namespace XCom
{
    class DataReceivedEventArgs : EventArgs
    {
        private Byte[] _Data;
        /// <summary>数据</summary>
        public Byte[] Data { get { return _Data; } set { _Data = value; } }
    }
}