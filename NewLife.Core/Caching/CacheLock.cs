using System;
using System.Threading;
using NewLife.Log;

namespace NewLife.Caching
{
    /// <summary>分布式锁</summary>
    public class CacheLock : DisposeBase
    {
        private ICache Client { get; set; }

        /// <summary>
        /// 是否持有锁
        /// </summary>
        private Boolean _hasLock = false;

        /// <summary>键</summary>
        public String Key { get; set; }

        /// <summary>实例化</summary>
        /// <param name="client"></param>
        /// <param name="key"></param>
        public CacheLock(ICache client, String key)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));
            if (key.IsNullOrEmpty()) throw new ArgumentNullException(nameof(key));

            Client = client;
            Key = key;
        }

        /// <summary>申请锁</summary>
        /// <param name="msTimeout">锁等待时间，单位毫秒</param>
        /// <param name="msExpire">锁过期时间，单位毫秒</param>
        /// <returns></returns>
        public Boolean Acquire(Int32 msTimeout, Int32 msExpire)
        {
            var ch = Client;
            var now = Runtime.TickCount64;

            // 循环等待
            var end = now + msTimeout;
            while (now < end)
            {
                // 申请加锁。没有冲突时可以直接返回
                var rs = ch.Add(Key, now + msExpire, msExpire / 1000);
                if (rs) return _hasLock = true;

                // 死锁超期检测
                var dt = ch.Get<Int64>(Key);
                if (dt <= now)
                {
                    // 开抢死锁。所有竞争者都会修改该锁的时间戳，但是只有一个能拿到旧的超时的值
                    var old = ch.Replace(Key, now + msExpire);
                    // 如果拿到超时值，说明抢到了锁。其它线程会抢到一个为超时的值
                    if (old <= dt)
                    {
                        ch.SetExpire(Key, TimeSpan.FromMilliseconds(msExpire));
                        return _hasLock = true;
                    }
                }

                // 没抢到，继续
                Thread.Sleep(200);

                now = Runtime.TickCount64;
            }

            return false;
        }

        /// <summary>销毁</summary>
        /// <param name="disposing"></param>
        protected override void Dispose(Boolean disposing)
        {
            base.Dispose(disposing);

            // 如果客户端已释放，则不删除
            if (Client is DisposeBase db && db.Disposed)
            {
            }
            else
            {
                if (_hasLock)
                {
                    Client.Remove(Key);
                }
            }
        }
    }
}