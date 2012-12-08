
namespace NewLife.Serialization
{
    /// <summary>读取器事件参数</summary>
    public class ReaderEventArgs : ReaderWriterEventArgs
    {
        private ReadObjectCallback _Callback;
        /// <summary>处理成员的委托</summary>
        public ReadObjectCallback Callback { get { return _Callback; } set { _Callback = value; } }

        #region 构造
        /// <summary>实例化</summary>
        /// <param name="callback"></param>
        public ReaderEventArgs(ReadObjectCallback callback)
        {
            Callback = callback;
        }
        #endregion
    }
}