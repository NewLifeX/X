using System;
using System.Collections.Generic;
using System.Net.Sockets;

namespace NewLife.Net.Common
{
    /// <summary>缓冲池</summary>
    /// <remarks>
    /// 频繁的分配小块内存，很容易形成内存碎片，并可能倒置GC无法回收而最后内存不足。
    /// 缓冲池采用一开始就分配一大块空间的策略，谁要使用内存再从池里申请，用完后自动归还。
    /// 这样整个缓冲池的生命周期内GC不来干涉。
    /// </remarks>
    public class BufferPool
    {
        #region 属性
        private Int32 _Count;
        /// <summary>内存块数</summary>
        public Int32 Count { get { return _Count; } }

        private Int32 _Size;
        /// <summary>内存分块大小</summary>
        public Int32 Size { get { return _Size; } }

        /// <summary>一大块预先申请的内存区域</summary>
        private Byte[] _Buffer;

        /// <summary>下一次可用的内存块偏移量</summary>
        private Int32 _Index;
        /// <summary>总内存大小</summary>
        private Int32 _TotalBytes;
        /// <summary>用过后归还的内存块索引，优先从这里借用</summary>
        private Stack<Int32> _Free = new Stack<Int32>();

        /// <summary>可用内存块数</summary>
        public int Available
        {
            get
            {
                lock (_Free)
                {
                    return (_TotalBytes - _Index) / _Size + _Free.Count;
                }
            }
        }
        #endregion

        #region 构造
        /// <summary>通过指定内存块数量和大小实例化一个内存池</summary>
        /// <param name="count">内存块数量</param>
        /// <param name="size">内存块大小</param>
        public BufferPool(Int32 count, Int32 size)
        {
            _Count = count;
            _Size = size;
            _TotalBytes = count * size;
            _Buffer = new Byte[_TotalBytes];
        }
        #endregion

        #region 方法
        /// <summary>借出内存区域</summary>
        /// <param name="args"></param>
        public void Pop(SocketAsyncEventArgs args)
        {
            lock (_Free)
            {
                if (_Free.Count > 0)
                    args.SetBuffer(_Buffer, _Free.Pop(), _Size);
                else
                {
                    // 判断是否已用完
                    if (_Index >= _TotalBytes - 1) throw new InvalidOperationException("内存不足！");

                    args.SetBuffer(_Buffer, _Index, _Size);
                    _Index += _Size;
                }
            }
        }

        /// <summary>归还内存块</summary>
        /// <param name="args"></param>
        public void Push(SocketAsyncEventArgs args)
        {
            lock (_Free)
            {
                _Free.Push(args.Offset);
                args.SetBuffer(null, 0, 0);
            }
        }
        #endregion
    }
}