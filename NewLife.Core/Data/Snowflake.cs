using System;
using System.Diagnostics;
using System.Threading;
using NewLife.Common;

namespace NewLife.Data
{
    /// <summary>流式Id</summary>
    [Obsolete("=>SnowFlake")]
    public class FlowId : Snowflake { }

    /// <summary>雪花算法。分布式Id</summary>
    /// <remarks>
    /// 文档 https://www.yuque.com/smartstone/nx/snow_flake
    /// 
    /// 使用一个 64 bit 的 long 型的数字作为全局唯一 id。在分布式系统中的应用十分广泛，且ID 引入了时间戳，基本上保持自增。
    /// 1bit保留 + 41bit时间戳 + 10bit机器 + 12bit序列号
    /// </remarks>
    public class Snowflake
    {
        #region 属性
        /// <summary>开始时间戳。首次使用前设置，否则无效，默认1970-1-1</summary>
        public DateTime StartTimestamp { get; set; } = new DateTime(1970, 1, 1);

        /// <summary>机器Id，取10位</summary>
        public Int32 WorkerId { get; set; }

        private Int32 _Sequence;
        /// <summary>序列号，取12位</summary>
        public Int32 Sequence { get => _Sequence; set => _Sequence = value; }

        private Int64 _msStart;
        private Stopwatch _watch;
        private Int64 _lastTime;
        #endregion

        #region 核心方法
        private void Init()
        {
            // 初始化WorkerId，取5位实例加上5位进程，确保同一台机器的WorkerId不同
            if (WorkerId <= 0)
            {
                var nodeId = SysConfig.Current.Instance;
                var pid = Process.GetCurrentProcess().Id;
                var tid = Thread.CurrentThread.ManagedThreadId;
                //WorkerId = ((nodeId & 0x1F) << 5) | (pid & 0x1F);
                //WorkerId = (nodeId ^ pid ^ tid) & 0x3FF;
                WorkerId = ((nodeId & 0x1F) << 5) | ((pid ^ tid) & 0x1F);
            }

            // 记录此时距离起点的毫秒数以及开机嘀嗒数
            if (_watch == null)
            {
                _msStart = (Int64)(DateTime.Now - StartTimestamp).TotalMilliseconds;
                _watch = Stopwatch.StartNew();
            }
        }

        /// <summary>获取下一个Id</summary>
        /// <returns></returns>
        public virtual Int64 NewId()
        {
            Init();

            // 此时嘀嗒数减去起点嘀嗒数，加上七点毫秒数
            //var ms = (Int64)(DateTime.Now - StartTimestamp).TotalMilliseconds;
            var ms = _watch.ElapsedMilliseconds + _msStart;
            var wid = WorkerId & 0x3FF;
            var seq = Interlocked.Increment(ref _Sequence) & 0x0FFF;

            //!!! 避免时间倒退
            if (ms < _lastTime) ms = _lastTime;

            // 相同毫秒内，如果序列号用尽，则可能超过4096，导致生成重复Id
            // 睡眠1毫秒，抢占它的位置 @656092719（广西-风吹面）
            if (_lastTime == ms && seq == 0)
            {
                //ms++;
                // spin等1000次耗时141us，10000次耗时397us，100000次耗时3231us。@i9-10900k
                //Thread.SpinWait(1000);
                while (_lastTime == ms) ms = _watch.ElapsedMilliseconds + _msStart;
            }
            _lastTime = ms;

            /*
             * 每个毫秒内_Sequence没有归零，主要是为了安全，避免被人猜测得到前后Id。
             * 而毫秒内的顺序，重要性不大。
             */

            return (ms << (10 + 12)) | (Int64)(wid << 12) | (Int64)seq;
        }

        /// <summary>获取指定时间的Id，带上节点和序列号。可用于根据业务时间构造插入Id</summary>
        /// <param name="time">时间</param>
        /// <returns></returns>
        public virtual Int64 NewId(DateTime time)
        {
            Init();

            var ms = (Int64)(time - StartTimestamp).TotalMilliseconds;
            var wid = WorkerId & 0x3FF;
            var seq = Interlocked.Increment(ref _Sequence) & 0x0FFF;

            return (ms << (10 + 12)) | (Int64)(wid << 12) | (Int64)seq;
        }

        /// <summary>时间转为Id，不带节点和序列号。可用于构建时间片段查询</summary>
        /// <param name="time">时间</param>
        /// <returns></returns>
        public virtual Int64 GetId(DateTime time)
        {
            var t = (Int64)(time - StartTimestamp).TotalMilliseconds;
            return t << (10 + 12);
        }

        /// <summary>尝试分析</summary>
        /// <param name="id"></param>
        /// <param name="time">时间</param>
        /// <param name="workerId">节点</param>
        /// <param name="sequence">序列号</param>
        /// <returns></returns>
        public virtual Boolean TryParse(Int64 id, out DateTime time, out Int32 workerId, out Int32 sequence)
        {
            time = StartTimestamp.AddMilliseconds(id >> (10 + 12));
            workerId = (Int32)((id >> 12) & 0x3FF);
            sequence = (Int32)(id & 0x0FFF);

            return true;
        }
        #endregion
    }
}