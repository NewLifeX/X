using System;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace NewLife
{
    /// <summary>X组件异常</summary>
    [Serializable]
    public class XException : ApplicationException
    {
        #region 构造
        /// <summary>初始化</summary>
        public XException() { }

        /// <summary>初始化</summary>
        /// <param name="message"></param>
        public XException(String message) : base(message) { }

        /// <summary>初始化</summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public XException(String format, params Object[] args) : base(format.F(args)) { }

        /// <summary>初始化</summary>
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        public XException(String message, Exception innerException) : base(message, innerException) { }

        /// <summary>初始化</summary>
        /// <param name="innerException"></param>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public XException(Exception innerException, String format, params Object[] args) : base(format.F(args), innerException) { }

        /// <summary>初始化</summary>
        /// <param name="innerException"></param>
        public XException(Exception innerException) : base((innerException != null ? innerException.Message : null), innerException) { }

        /// <summary>初始化</summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected XException(SerializationInfo info, StreamingContext context) : base(info, context) { }
        #endregion
    }

    /// <summary>异常事件参数</summary>
    public class ExceptionEventArgs : EventArgs
    {
        private String _Action;
        /// <summary>发生异常时进行的动作</summary>
        public String Action { get { return _Action; } set { _Action = value; } }

        private Exception _Exception;
        /// <summary>异常</summary>
        public Exception Exception { get { return _Exception; } set { _Exception = value; } }
    }

    /// <summary>异常助手</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class ExceptionHelper
    {
        /// <summary>是否对象已被释放异常</summary>
        /// <param name="ex"></param>
        /// <returns></returns>
        public static Boolean IsDisposed(this Exception ex) { return ex is ObjectDisposedException; }
    }
}