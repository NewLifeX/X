using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace NewLife.IO
{
    /// <summary>读写流，继承自内存流，读写指针分开</summary>
    /// <remarks>
    /// 注意资源锁，读写不可同时进行，会出现抢锁的情况。
    /// </remarks>
    public class ReadWriteMemoryStream : MemoryStream
    {
        #region 属性
        private Int32 _ReadTimeout = Timeout.Infinite;
        /// <summary>获取或设置一个值（以毫秒为单位），该值确定流在超时前尝试读取多长时间。</summary>
        public override Int32 ReadTimeout { get { return _ReadTimeout; } set { _ReadTimeout = value; } }

        //private Int32 _WriteTimeout;
        ///// <summary>获取或设置一个值（以毫秒为单位），该值确定流在超时前尝试写入多长时间。</summary>
        //public override Int32 WriteTimeout
        //{
        //    get { return _WriteTimeout; }
        //    set { _WriteTimeout = value; }
        //}

        private Int64 _PositionForWrite;
        /// <summary>写位置</summary>
        public Int64 PositionForWrite { get { return _PositionForWrite; } set { _PositionForWrite = value; } }

        private Int64 _MaxLength = 1024 * 1024;
        /// <summary>最大长度，超过次长度时清空缓冲区</summary>
        public Int64 MaxLength { get { return _MaxLength; } set { _MaxLength = value; } }

        private AutoResetEvent dataArrived = new AutoResetEvent(false);
        #endregion

        #region 扩展属性
        /// <summary>可用数据</summary>
        public Int64 AvailableData { get { return PositionForWrite - Position; } }
        #endregion

        #region 方法
        /// <summary>已重载。</summary>
        /// <param name="offset"></param>
        /// <param name="loc"></param>
        /// <returns></returns>
        public long SeekForWrite(long offset, SeekOrigin loc)
        {
            Int64 r = 0;
            lock (rwLock)
            {
                Int64 p = Position;
                Position = PositionForWrite;

                r = base.Seek(offset, loc);

                PositionForWrite = Position;
                Position = p;
            }
            return r;
        }

        /// <summary>重设长度，</summary>
        void ResetLength()
        {
            // 写入指针必须超过最大长度
            if (PositionForWrite < MaxLength) return;
            //Int64 pos = Math.Min(Position, PositionForWrite);
            Int64 pos = Position;
            // 必须有剩余数据空间，并且剩余空间不能太小
            if (pos <= MaxLength / 2) return;

            Console.WriteLine("前移 {0}", pos);

            // 移动数据
            Byte[] buffer = GetBuffer();
            for (int i = 0; i < Length - pos; i++)
            {
                buffer[i] = buffer[pos + i];
            }

            SetLength(Length - pos);

            Position = 0;
            PositionForWrite -= pos;
        }
        #endregion

        #region 重载
        private Object rwLock = new Object();
        /// <summary>已重载。</summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public override int Read(byte[] buffer, int offset, int count)
        {
            Int32 rs = 0;
            Stopwatch sw = new Stopwatch();
            sw.Start();
            while (rs <= 0) rs = ReadEx(buffer, offset, count, sw);
            return rs;
        }

        Int32 ReadEx(byte[] buffer, int offset, int count, Stopwatch sw)
        {
            // 如果没有数据
            if (PositionForWrite <= Position) CheckReadTimeout(sw);
            // 即使得到事件量，也未必能读到值，因为可能在多线程里面，数据被别的线程读走了
            // 这种情况下，本线程就需要继续等

            lock (rwLock)
            {
                return base.Read(buffer, offset, count);
            }
        }

        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override int ReadByte()
        {
            Int32 rs = -1;
            Stopwatch sw = new Stopwatch();
            sw.Start();
            while (rs <= -1) rs = ReadByteEx(sw);
            return rs;

            //// 如果没有数据
            //if (PositionForWrite <= Position)
            //{
            //    Stopwatch sw = new Stopwatch();
            //    while (PositionForWrite <= Position)
            //    {
            //        sw.Start();
            //        if (!dataArrived.WaitOne(ReadTimeout - (Int32)sw.ElapsedMilliseconds)) throw new TimeoutException();
            //        sw.Stop();

            //        if (sw.ElapsedMilliseconds >= ReadTimeout) throw new TimeoutException();
            //    }
            //}

            //lock (rwLock)
            //{
            //    return base.ReadByte();
            //}
        }

        Int32 ReadByteEx(Stopwatch sw)
        {
            // 如果没有数据
            if (PositionForWrite <= Position) CheckReadTimeout(sw);
            // 即使得到事件量，也未必能读到值，因为可能在多线程里面，数据被别的线程读走了
            // 这种情况下，本线程就需要继续等

            lock (rwLock)
            {
                return base.ReadByte();
            }
        }

        void CheckReadTimeout(Stopwatch sw)
        {
            if (PositionForWrite <= Position)
            {
                while (PositionForWrite <= Position)
                {
                    if (!dataArrived.WaitOne(ReadTimeout - (Int32)sw.ElapsedMilliseconds)) throw new TimeoutException();

                    if (ReadTimeout > 0 && sw.ElapsedMilliseconds >= ReadTimeout) throw new TimeoutException();
                }
            }
        }

        /// <summary>已重载。</summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        public override void Write(byte[] buffer, int offset, int count)
        {
            lock (rwLock)
            {
                Int64 p = Position;
                Position = PositionForWrite;

                base.Write(buffer, offset, count);

                PositionForWrite = Position;
                Position = p;

                if (PositionForWrite >= MaxLength) ResetLength();
            }
            dataArrived.Set();
        }

        /// <summary>已重载。</summary>
        /// <param name="value"></param>
        public override void WriteByte(byte value)
        {
            lock (rwLock)
            {
                Int64 p = Position;
                Position = PositionForWrite;

                base.WriteByte(value);

                PositionForWrite = Position;
                Position = p;

                if (PositionForWrite >= MaxLength) ResetLength();
            }
            dataArrived.Set();
        }

        /// <summary>资源释放，关闭事件量</summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            dataArrived.Close();

            base.Dispose(disposing);
        }
        #endregion
    }
}