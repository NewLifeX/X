using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace NewLife.Log
{
    /// <summary>统计代码的时间消耗</summary>
    public class TimeCost : DisposeBase
    {
        #region 属性
        Stopwatch _sw;

        /// <summary>名称</summary>
        public String Name { get; set; }

        /// <summary>最大时间。毫秒</summary>
        public Int32 Max { get; set; }

        /// <summary>日志输出</summary>
        public ILog Log { get; set; }
        #endregion

        #region 构造
        /// <summary>指定最大执行时间来构造一个代码时间统计</summary>
        /// <param name="name"></param>
        /// <param name="msMax"></param>
        public TimeCost(String name, Int32 msMax = 0)
        {
            Name = name;
            Max = msMax;
            Log = XTrace.Log;

            if (msMax >= 0) Start();
        }

        /// <summary>析构</summary>
        /// <param name="disposing"></param>
        protected override void OnDispose(bool disposing)
        {
            Stop();

            base.OnDispose(disposing);
        }
        #endregion

        #region 方法
        /// <summary>开始</summary>
        public void Start()
        {
            if (_sw == null)
                _sw = Stopwatch.StartNew();
            else if (!_sw.IsRunning)
                _sw.Start();
        }

        /// <summary>停止</summary>
        public void Stop()
        {
            if (_sw != null)
            {
                _sw.Stop();

                if (Log != null && Log != Logger.Null && Log.Enable)
                {
                    var ms = _sw.ElapsedMilliseconds;
                    if (ms > Max)
                    {
                        if (Max > 0)
                            Log.Warn("{0}执行过长警告 {1:n0}ms > {2:n0}ms", Name, ms, Max);
                        else
                            Log.Warn("{0}执行 {1:n0}ms", Name, ms);
                    }
                }
            }
        }
        #endregion
    }
}