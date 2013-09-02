using System;
using System.Collections.Generic;
using System.Text;

namespace NewLife.Net
{
    /// <summary>异常事件参数</summary>
    public class ExceptionEventArgs : EventArgs
    {
        private Exception _Exception;
        /// <summary>异常</summary>
        public Exception Exception { get { return _Exception; } set { _Exception = value; } }
    }
}