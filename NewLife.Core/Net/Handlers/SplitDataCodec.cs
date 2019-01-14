using System;
using System.Collections.Generic;
using System.IO;
using NewLife.Data;
using NewLife.Model;
using NewLife.Threading;

namespace NewLife.Net.Handlers
{
    /// <summary>
    /// 按指定分割字节来处理粘包的处理器
    /// </summary>
    /// <remarks>
    /// 默认以"0x0D 0x0A"即换行来分割，分割的包包含分割字节本身，使用时请注意。
    /// 默认分割方式：ISocket.Add&lt;SplitDataCodec&gt;()
    /// 自定义分割方式：ISocket.Add(new SplitDataHandler { SplitData = 自定义分割字节数组 })
    /// 自定义最大缓存大小方式：ISocket.Add(new SplitDataHandler { MaxCacheDataLength = 2048 })
    /// 自定义方式：ISocket.Add(new SplitDataHandler { MaxCacheDataLength = 2048, SplitData = 自定义分割字节数组 })
    /// </remarks>
    public class SplitDataCodec : Handler
    {
        /// <summary>
        /// 粘包分割字节数据（默认0x0D,0x0A）
        /// </summary>
        public Byte[] SplitData { get; set; } = new Byte[] { 0x0D, 0x0A };

        /// <summary>
        /// 最大缓存待处理数据（字节）
        /// </summary>
        public Int32 MaxCacheDataLength { get; set; } = 1024;

        /// <summary>读取数据</summary>
        /// <param name="context"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public override Object Read(IHandlerContext context, Object message)
        {
            if (!(message is Packet pk)) return base.Read(context, message);

            // 解码得到多个消息
            var list = Decode(context, pk);
            if (list == null) return null;

            foreach (var msg in list)
            {
                // 把数据发送给后续处理器
                var rs = base.Read(context, msg);
            }

            return null;
        }

        #region 粘包处理
        /// <summary>解码</summary>
        /// <param name="context"></param>
        /// <param name="pk">包</param>
        /// <returns></returns>
        protected IList<Packet> Decode(IHandlerContext context, Packet pk)
        {
            var ss = context.Owner as IExtend;
            var mcp = ss["CodecItem"] as CodecItem;
            if (mcp == null) ss["CodecItem"] = mcp = new CodecItem();

            return Parse(pk, mcp, pk1 => GetLineLength(pk1));
        }
        
        /// <summary>分析数据流，得到一帧数据</summary>
        /// <param name="pk">待分析数据包</param>
        /// <param name="codec">参数</param>
        /// <param name="getLength">获取长度</param>
        /// <param name="expire">缓存有效期</param>
        /// <returns></returns>
        protected virtual IList<Packet> Parse(Packet pk, CodecItem codec, Func<Packet, Int32> getLength, Int32 expire = 5000)
        {
            var ms = codec.Stream;
            var nodata = ms == null || ms.Position < 0 || ms.Position >= ms.Length;

            var list = new List<Packet>();
            // 内部缓存没有数据，直接判断输入数据流是否刚好一帧数据，快速处理，绝大多数是这种场景
            if (nodata)
            {
                if (pk == null) return list.ToArray();

                var idx = 0;
                while (idx < pk.Total)
                {
                    // 切出来一片，计算长度
                    var pk2 = pk.Slice(idx);
                    var len = getLength(pk2);
                    if (len <= 0 || len > pk2.Count) break;

                    // 根据计算得到的长度，重新设置数据片正确长度
                    pk2.Set(pk2.Data, pk2.Offset, len);
                    list.Add(pk2);
                    idx += len;
                }
                // 如果没有剩余，可以返回
                if (idx == pk.Total) return list.ToArray();

                // 剩下的
                pk = pk.Slice(idx);
            }

            if (ms == null) codec.Stream = ms = new MemoryStream();

            // 加锁，避免多线程冲突
            lock (ms)
            {
                // 超过该时间后按废弃数据处理  2019-1-9 +待处理数据超过设定值也按废弃数据处理【重要】
                var now = TimerX.Now;
                if ((ms.Length > ms.Position && codec.Last.AddMilliseconds(expire) < now) || ms.Length >= MaxCacheDataLength)
                {
                    ms.SetLength(0);
                    ms.Position = 0;
                }
                codec.Last = now;

                // 合并数据到最后面
                if (pk != null && pk.Total > 0)
                {
                    var p = ms.Position;
                    ms.Position = ms.Length;
                    pk.WriteTo(ms);
                    ms.Position = p;
                }

                // 尝试解包
                while (ms.Position < ms.Length)
                {
                    var pk2 = new Packet(ms);
                    var len = getLength(pk2);

                    // 资源不足一包
                    if (len <= 0 || len > pk2.Total) break;

                    // 解包成功
                    pk2.Set(pk2.Data, pk2.Offset, len);
                    list.Add(pk2);

                    ms.Seek(len, SeekOrigin.Current);
                }

                // 如果读完了数据，需要重置缓冲区
                if (ms.Position >= ms.Length)
                {
                    ms.SetLength(0);
                    ms.Position = 0;
                }

                return list;
            }
        }

        /// <summary>
        /// 获取包含分割字节在内的数据长度
        /// </summary>
        /// <param name="pk"></param>
        /// <returns></returns>
        protected Int32 GetLineLength(Packet pk)
        {
            var idx = pk.IndexOf(SplitData);
            if (idx < 0) return 0;

            return idx + SplitData.Length;
        }
        #endregion
    }
}