using System;
using System.Configuration;
using System.Threading;
using NewLife.Log;
using NewLife.Configuration;

namespace NewLife.Threading
{
    /// <summary>
    /// 线程扩展
    /// </summary>
    class ThreadX : IDisposable
    {
        #region 属性
        private String _Name;
        /// <summary>名称</summary>
        public String Name
        {
            get { return _Name; }
            set { _Name = value; }
        }

        private ThreadTask _Task;
        /// <summary>任务</summary>
        public ThreadTask Task
        {
            get { return _Task; }
            set { _Task = value; }
        }

        //private WaitCallback _Method;
        ///// <summary>任务方法</summary>
        //public WaitCallback Method
        //{
        //    get { return _Method; }
        //    set { _Method = value; }
        //}

        //private Object _Argument;
        ///// <summary>任务参数</summary>
        //public Object Argument
        //{
        //    get { return _Argument; }
        //    set { _Argument = value; }
        //}

        private Exception _LastError;
        /// <summary>最后错误</summary>
        public Exception LastError
        {
            get { return _LastError; }
            private set { _LastError = value; }
        }

        private Boolean _Running;
        /// <summary>是否正在处理任务</summary>
        public Boolean Running
        {
            get { return _Running; }
            set { _Running = value; }
        }

        //private Boolean _Alive = true;
        /// <summary>是否活动</summary>
        public Boolean IsAlive
        {
            get
            {
                //虽然还没有分配，但是仍然把它当作活动的，因为它“可用”
                if (Thread == null)
                    return true;
                else
                    return Thread.IsAlive;
                //return _Alive;
            }
            //set { _Alive = value; }
        }

        /// <summary>
        /// 是否能够释放
        /// </summary>
        public Boolean CanRelease
        {
            get
            {
                //正在处理任务的不允许释放
                if (Running) return false;

                //半分钟不活动的可以释放
                if (AliveTime.AddSeconds(180) < DateTime.Now) return true;
                return false;
            }
        }
        #endregion

        #region 私有属性
        private Thread _Thread;
        /// <summary>线程</summary>
        private Thread Thread
        {
            get { return _Thread; }
            set { _Thread = value; }
        }

        private DateTime _StartTime;
        /// <summary>开始时间</summary>
        private DateTime StartTime
        {
            get { return _StartTime; }
            set { _StartTime = value; }
        }

        private DateTime _AliveTime;
        /// <summary>活动时间</summary>
        private DateTime AliveTime
        {
            get { return _AliveTime; }
            set { _AliveTime = value; }
        }

        //private Int32 _CreateTimes;
        ///// <summary>线程创建次数</summary>
        //private Int32 CreateTimes
        //{
        //    get { return _CreateTimes; }
        //    set
        //    {
        //        _CreateTimes = value;
        //        if (_CreateTimes > 1) throw new Exception("理论上来说，线程池的线程不会出现重新创建的情况，除非曾经被中断或取消过！");
        //    }
        //}

        /// <summary>
        /// 内部控制事件
        /// </summary>
        private AutoResetEvent internalEvent = new AutoResetEvent(false);
        #endregion

        #region 方法
        /// <summary>
        /// 开始
        /// </summary>
        public void Start()
        {
            if (internalEvent == null) internalEvent = new AutoResetEvent(false);

            if (Thread == null || (Thread.ThreadState & ThreadState.Stopped) == ThreadState.Stopped)
            {
                Thread = new Thread(Work);
                Thread.Name = Name;
                Thread.IsBackground = true;
                Thread.Priority = ThreadPriority.AboveNormal;
                //CreateTimes++;
                Thread.Start();

                //Thread.Sleep(1);//停一会，可能线程还没建好
            }
            else if ((Thread.ThreadState & ThreadState.Unstarted) == ThreadState.Unstarted)
            {
                //CreateTimes++;
                Thread.Start();

                //Thread.Sleep(1);
            }

            Running = true;
            internalEvent.Set();
        }

        /// <summary>
        /// 取消
        /// </summary>
        /// <param name="onlytask">是否仅仅取消任务</param>
        public void Abort(Boolean onlytask)
        {
            WriteLog("取消");
            if (Thread == null) return;

            //取消参数表示是否终止线程，如果只是取消任务，就传false进去
            Thread.Abort(!onlytask);
            if (internalEvent != null) internalEvent.Set();
        }

        private void Work()
        {
            //Alive = true;
            while (true)
            {
                try
                {
                    //挂起自己，直到下一个任务到来
                    internalEvent.WaitOne(Timeout.Infinite, true);

                    Running = true;

                    //信号量复位
                    internalEvent.Reset();
                    if (Task != null)
                    {
                        ThreadTask task = Task;
                        WriteLog("新任务" + task.ID);
                        //task.Thread = this;
                        //WaitCallback method = Method;
                        //Object arg = Argument;
                        LastError = null;

                        StartTime = DateTime.Now;

                        //method(arg);
                        Task.Method(Task.Argument);
                    }
                }
                catch (ThreadInterruptedException ex)//中断异常，跳出
                {
                    LastError = ex;
                    Thread = null;
                    internalEvent.Close();
                    internalEvent = null;
                    break;
                }
                catch (ThreadAbortException ex)//取消异常，有可能是终止当前任务而已，不需要跳出
                {
                    LastError = ex;

                    //异常参数指明是否需要终止线程
                    if (ex.ExceptionState != null && (Boolean)ex.ExceptionState)
                    {
                        Thread = null;
                        internalEvent.Close();
                        internalEvent = null;
                        break;
                    }
                }
                catch (Exception ex)//其它异常，继续
                {
                    LastError = ex;

                    ThreadAbortException e = FindException<ThreadAbortException>(ex);
                    if (e == null)
                        XTrace.WriteException(ex);
                    else
                    {
                        //异常参数指明是否需要终止线程
                        if (e.ExceptionState != null && (Boolean)e.ExceptionState)
                        {
                            Thread = null;
                            internalEvent.Close();
                            internalEvent = null;
                            break;
                        }
                    }
                }
                finally
                {
                    //通知事件订阅者，任务已经完成
                    if (_OnTaskFinished != null)
                    {
                        //对不信任方法的调用，捕获所有异常，防止因外部方法的错误而导致线程自身崩溃
                        try
                        {
                            _OnTaskFinished(this, EventArgs.Empty);
                        }
                        catch { }
                    }

                    //清空任务，防止下一次重复执行
                    Task = null;

                    AliveTime = DateTime.Now;

                    //不管怎么样，都要标志线程不在运行
                    Running = false;
                }
            }
            //Alive = false;
        }
        #endregion

        #region 事件
        private EventHandler<EventArgs> _OnTaskFinished;
        /// <summary>任务完成时</summary>
        public event EventHandler<EventArgs> OnTaskFinished
        {
            add { _OnTaskFinished = value; }
            remove { _OnTaskFinished = null; }
        }
        #endregion

        #region IDisposable 成员
        private Boolean _isDisposed = false;

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            //上面的代码已经回收本对象的资源，GC不要再回收了
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 释放资源。非用户代码调用时，仅释放非托管资源
        /// </summary>
        /// <param name="disposing">是否用户代码调用</param>
        private void Dispose(Boolean disposing)
        {
            if (!_isDisposed)
            {
                _isDisposed = true;

                WriteLog("释放资源：" + Name);
                //释放托管资源
                //if (disposing)
                {
                    if (Thread != null)
                    {
                        if (Running) Thread.Abort(true);
                        Thread = null;
                    }
                    if (internalEvent != null) internalEvent.Close();
                }
                //释放非托管资源
            }
        }

        ~ThreadX()
        {
            Dispose(false);
        }
        #endregion

        #region 重载
        /// <summary>
        /// 已重载。
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return Name + " " + (Running ? "正在运行" : "休眠");
        }
        #endregion

        #region 辅助函数
        private static void WriteLog(String msg)
        {
            if (Debug) XTrace.WriteLine("线程：" + Thread.CurrentThread.Name + " 信息：" + msg);
        }

        private static Boolean? _Debug;
        /// <summary>
        /// 是否调试
        /// </summary>
        public static Boolean Debug
        {
            get
            {
                if (_Debug != null) return _Debug.Value;

                //String str = ConfigurationManager.AppSettings["ThreadPoolDebug"];
                //if (String.IsNullOrEmpty(str)) return false;
                //if (str == "1" || str.Equals(Boolean.TrueString, StringComparison.OrdinalIgnoreCase)) return true;
                //if (str == "0" || str.Equals(Boolean.FalseString, StringComparison.OrdinalIgnoreCase)) return false;
                //_Debug = Convert.ToBoolean(str);

                _Debug = Config.GetConfig<Boolean>("NewLife.Thread.Debug", Config.GetConfig<Boolean>("ThreadPoolDebug", false));

                return _Debug.Value;
            }
            set { _Debug = value; }
        }

        /// <summary>
        /// 查找指定类型的异常
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="ex"></param>
        /// <returns></returns>
        private static T FindException<T>(Exception ex) where T : Exception
        {
            if (ex == null) return null;

            if (ex is T) return ex as T;

            return FindException<T>(ex.InnerException);
        }
        #endregion
    }
}