using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using NewLife.Collections;
using NewLife.Exceptions;
using NewLife.Log;

namespace NewLife.Threading
{
    /// <summary>
    /// 原子读写锁
    /// </summary>
    /// <remark>
    /// 任意多个读操作，只有一个写操作；
    /// 任意读操作阻塞写操作，同样任意写操作阻塞非本线程读操作和其它写操作；
    /// </remark>
    /// <remarks>
    /// do...while(Interlocked.CompareExchange(ref _lock, oldLock - 1, oldLock) != oldLock)形式的原子锁结构，
    /// 精髓在于do...while之间，里面才是真正的判断数据有效性核心，而CompareExchange仅仅是负责完成替换而已。
    /// 实际上，就类似于准备好各种资料等上级审批，而上级每次只能审批一个，如果这次别人抢到了，那么自己得再次准备资料。
    /// </remarks>
    public sealed class ReadWriteLock //: DisposeBase
    {
        #region 属性
        private Int32 _Max = 1;
        /// <summary>最大可独占资源数，默认1</summary>
        public Int32 Max
        {
            get { return _Max; }
            set { _Max = value; }
        }

        /// <summary>
        /// 锁计数
        /// </summary>
        private Int32 _lock = 0;

        private Int32 _ReadTimeout = 1000;
        /// <summary>读取锁等待超时时间，默认1秒</summary>
        public Int32 ReadTimeout
        {
            get { return _ReadTimeout; }
            set { _ReadTimeout = value; }
        }

        private Int32 _WriteTimeout = 5000;
        /// <summary>写入锁等待超时时间，默认5秒</summary>
        public Int32 WriteTimeout
        {
            get { return _WriteTimeout; }
            set { _WriteTimeout = value; }
        }

        ///// <summary>
        ///// 写入线程的ID。用于多次调用识别
        ///// </summary>
        //private Int32 _threadID = 0;

        ///// <summary>
        ///// 循环计数。多次调用时，实现递加或递减
        ///// </summary>
        //private Int32 _recursionCount = 0;
        #endregion

        #region 构造
        ///// <summary>
        ///// 实例化一个原子读写锁
        ///// </summary>
        //public ReadWriteLock() : this(1) { }

        ///// <summary>
        ///// 实例化一个原子读写锁，并制定最大写资源
        ///// </summary>
        ///// <param name="max"></param>
        //public ReadWriteLock(Int32 max) { Max = max; }

        static DictionaryCache<Object, ReadWriteLock> _cache = new DictionaryCache<object, ReadWriteLock>();
        /// <summary>
        /// 根据指定键值创建读写锁，一般读写锁需要针对指定资源唯一
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static ReadWriteLock Create(Object key)
        {
            if (key == null) throw new ArgumentNullException("key");

            return _cache.GetItem(key, delegate { return new ReadWriteLock(); });
        }
        #endregion

        #region 方法
        /// <summary>
        /// 请求读取锁
        /// </summary>
        public void AcquireRead()
        {
            // 1秒
            if (!AcquireRead(ReadTimeout)) throw new XException("原子读写锁实在太忙，无法取得读取锁！");
        }

        /// <summary>
        /// 请求读取锁，等待指定秒
        /// </summary>
        /// <param name="millisecondsTimeout"></param>
        /// <returns>是否取得锁</returns>
        public Boolean AcquireRead(Int32 millisecondsTimeout)
        {
            //Int32 currentThreadID = Thread.CurrentThread.ManagedThreadId;
            //if (currentThreadID == _threadID)
            //{
            //    Interlocked.Increment(ref _recursionCount);
            //    return;
            //}

            Int32 oldLock = 0;
            Double times = DateTime.Now.TimeOfDay.TotalMilliseconds + millisecondsTimeout;

            // 读锁使得锁计数递加
            while (true)
            {
                oldLock = _lock;

                // 负数表示写锁
                if (oldLock < 0 || _lock < 0)
                {
                    if (DateTime.Now.TimeOfDay.TotalMilliseconds > times) return false;

                    // 空转一下
                    Thread.SpinWait(1);
                    continue;
                }

                // 是否拿到了锁
                if (Interlocked.CompareExchange(ref _lock, oldLock + 1, oldLock) == oldLock) break;
            }

            Write("AcquireRead");
            return true;
        }

        /// <summary>
        /// 释放读取锁
        /// </summary>
        public void ReleaseRead()
        {
            //Int32 currentThreadID = Thread.CurrentThread.ManagedThreadId;
            //if (currentThreadID == _threadID)
            //{
            //    Interlocked.Decrement(ref _recursionCount);
            //    return;
            //}

            if (_lock <= 0) throw new InvalidOperationException("当前未处于读取锁定状态！");

            Interlocked.Decrement(ref _lock);
            Write("ReleaseRead");
        }

        /// <summary>
        /// 请求写入锁
        /// </summary>
        public void AcquireWrite()
        {
            // 10秒
            if (!AcquireWrite(WriteTimeout)) throw new XException("原子读写锁实在太忙，无法取得写入锁！");
        }

        /// <summary>
        /// 请求写入锁，等待指定秒
        /// </summary>
        /// <param name="millisecondsTimeout"></param>
        /// <returns>是否取得锁</returns>
        public Boolean AcquireWrite(Int32 millisecondsTimeout)
        {
            //Int32 currentThreadID = Thread.CurrentThread.ManagedThreadId;
            //if (currentThreadID == _threadID)
            //{
            //    Interlocked.Increment(ref _recursionCount);
            //    return;
            //}

            Int32 oldLock = 0;
            Int32 m = -1 * Max;
            Double times = DateTime.Now.TimeOfDay.TotalMilliseconds + millisecondsTimeout;

            // 读锁使得锁计数递加
            while (true)
            {
                oldLock = _lock;

                // 正数表示读锁，负数且不能大于最大可请求资源
                if (oldLock > 0 || oldLock <= m)
                //// 只能一个写操作进来
                //if (oldLock != 0 || _lock != 0)
                {
                    if (DateTime.Now.TimeOfDay.TotalMilliseconds > times) return false;

                    // 空转一下
                    Thread.SpinWait(1);
                    continue;
                }

                // 是否拿到了锁
                if (Interlocked.CompareExchange(ref _lock, oldLock - 1, oldLock) == oldLock) break;
            }

            //_threadID = currentThreadID;
            //_recursionCount = 1;

            Write("AcquireWrite");
            return true;
        }

        /// <summary>
        /// 释放写入锁
        /// </summary>
        public void ReleaseWrite()
        {
            //Int32 currentThreadID = Thread.CurrentThread.ManagedThreadId;
            //if (currentThreadID == _threadID && --_recursionCount == 0)
            {
                //_threadID = 0;

                if (_lock >= 0) throw new InvalidOperationException("当前未处于写入锁定状态！");

                Interlocked.Increment(ref _lock);
                Write("ReleaseWrite");
            }
        }
        #endregion

        #region 辅助
        void Write(String msg)
        {
            if (XTrace.Debug) XTrace.WriteLine(msg);
        }
        #endregion

        //#region 释放
        ///// <summary>
        ///// 已重载。
        ///// </summary>
        ///// <param name="disposing"></param>
        //protected override void OnDispose(bool disposing)
        //{
        //    base.OnDispose(disposing);

        //    _lock = 0;
        //}
        //#endregion
    }
}
