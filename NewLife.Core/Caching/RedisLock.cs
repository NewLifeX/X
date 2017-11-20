using System;
using System.Threading;

namespace NewLife.Caching
{
    /// <summary>Redis分布式锁</summary>
    public class RedisLock : DisposeBase
    {
        private Redis Client { get; set; }

        /// <summary>键</summary>
        public String Key { get; set; }

        /// <summary>实例化</summary>
        /// <param name="client"></param>
        /// <param name="key"></param>
        public RedisLock(Redis client, String key)
        {
            Client = client;
            Key = "lock:" + key;
        }

        /// <summary>申请锁</summary>
        /// <param name="msTimeout"></param>
        /// <returns></returns>
        public Boolean Acquire(Int32 msTimeout)
        {
            var rds = Client;
            var now = DateTime.Now;
            var expire = now.AddMilliseconds(msTimeout);

            // 申请加锁。没有冲突时可以直接返回
            var rs = rds.Add(Key, expire);
            if (rs) return true;

            // 循环等待
            var end = now.AddMilliseconds(msTimeout);
            while (true)
            {
                now = DateTime.Now;
                if (now > end) break;

                // 超期检测
                var dt = rds.Get<DateTime>(Key);
                if (dt <= now)
                {
                    // 开抢
                    expire = now.AddMilliseconds(msTimeout);
                    var old = rds.GetSet(Key, expire);
                    // 如果拿到超时值，说明抢到了锁。其它线程会抢到一个为超时的值
                    if (old <= now) return true;
                }

                // 没抢到，继续
                Thread.Sleep(20);
            }

            return false;
        }

        /// <summary>销毁</summary>
        /// <param name="disposing"></param>
        protected override void OnDispose(Boolean disposing)
        {
            base.OnDispose(disposing);

            Client.Remove(Key);
        }
    }
}