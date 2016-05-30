using System;
using System.Threading;
using System.Threading.Tasks;
#if !Android
using System.Web;
#endif

namespace NewLife.Log
{
    /// <summary>写日志事件参数</summary>
    public class WriteLogEventArgs : EventArgs
    {
        #region 属性
        /// <summary>日志等级</summary>
        public LogLevel Level { get; set; }

        /// <summary>日志信息</summary>
        public String Message { get; set; }

        /// <summary>异常</summary>
        public Exception Exception { get; set; }

        /// <summary>时间</summary>
        public DateTime Time { get; set; }

        /// <summary>线程编号</summary>
        public Int32 ThreadID { get; set; }

        /// <summary>是否线程池线程</summary>
        public Boolean IsPool { get; set; }

        /// <summary>是否Web线程</summary>
        public Boolean IsWeb { get; set; }

        /// <summary>线程名</summary>
        public String ThreadName { get; set; }

        /// <summary>任务编号</summary>
        public Int32 TaskID { get; set; }
        #endregion

        #region 构造
        /// <summary>实例化一个日志事件参数</summary>
        internal WriteLogEventArgs() { }
        #endregion

        #region 线程专有实例
        /*2015-06-01 @宁波-小董
         * 将Current以及Set方法组从internal修改为Public
         * 原因是 Logger在进行扩展时，重载OnWrite需要用到该静态属性以及方法，internal无法满足扩展要求
         * */
        [ThreadStatic]
        private static WriteLogEventArgs _Current;
        /// <summary>线程专有实例。线程静态，每个线程只用一个，避免GC浪费</summary>
        public static WriteLogEventArgs Current { get { return _Current ?? (_Current = new WriteLogEventArgs()); } }
        #endregion

        #region 方法
        /// <summary>初始化为新日志</summary>
        /// <param name="level">日志等级</param>
        /// <returns>返回自身，链式写法</returns>
        public WriteLogEventArgs Set(LogLevel level)
        {
            Level = level;

            return this;
        }

        /// <summary>初始化为新日志</summary>
        /// <param name="message">日志</param>
        /// <param name="exception">异常</param>
        /// <returns>返回自身，链式写法</returns>
        public WriteLogEventArgs Set(String message, Exception exception)
        {
            Message = message;
            Exception = exception;

            Init();

            return this;
        }

        void Init()
        {
            Time = DateTime.Now;
            var thread = Thread.CurrentThread;
            ThreadID = thread.ManagedThreadId;
            IsPool = thread.IsThreadPoolThread;
            ThreadName = thread.Name;

            var tid = Task.CurrentId;
            TaskID = tid != null ? tid.Value : -1;

#if !Android
            IsWeb = HttpContext.Current != null;
#endif
        }

        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override string ToString()
        {
            if (Exception != null) Message += Exception.ToString();

            var name = ThreadName;
            if (name.IsNullOrEmpty()) name = TaskID >= 0 ? TaskID + "" : "-";
#if Android
            if (name.EqualIgnoreCase("Threadpool worker")) name = "P";
            if (name.EqualIgnoreCase("IO Threadpool worker")) name = "IO";
#endif

            return String.Format("{0:HH:mm:ss.fff} {1,2} {2} {3} {4}", Time, ThreadID, IsPool ? (IsWeb ? 'W' : 'Y') : 'N', name, Message);
        }
        #endregion
    }
}