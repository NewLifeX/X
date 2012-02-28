using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace NewLife.Net.Sockets
{
    /// <summary>数据接收事件参数</summary>
    public class DataReceiveEventArgs : EventArgs
    {
        private Stream _Stream;
        /// <summary>数据流</summary>
        public Stream Stream { get { return _Stream; } set { _Stream = value; } }

        /// <summary>实例化一个数据接收事件参数</summary>
        /// <param name="steam"></param>
        public DataReceiveEventArgs(Stream steam) { Stream = Stream; }
    }
}