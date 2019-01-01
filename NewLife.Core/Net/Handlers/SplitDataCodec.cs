using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
    /// </remarks>
    public class SplitDataCodec : Handler
    {
        /// <summary>
        /// 粘包分割字节数据（默认0x0D,0x0A）
        /// </summary>
        public byte[] SplitData { get; set; } = new byte[] { 0x0D, 0x0A };

        /// <summary>读取数据</summary>
        /// <param name="context"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public override object Read(IHandlerContext context, object message)
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

            var pks = Parse(pk, mcp, pk1 => GetLineLength(pk1));

            return pks;
        }


        /// <summary>分析数据流，得到一帧数据</summary>
        /// <param name="pk">待分析数据包</param>
        /// <param name="codec">参数</param>
        /// <param name="getLength">获取长度</param>
        /// <param name="expire">缓存有效期</param>
        /// <returns></returns>
        protected virtual IList<Packet> Parse(Packet pk, CodecItem codec, Func<Packet, Int32> getLength, Int32 expire = 5000)
        {
            var _ms = codec.Stream;
            var nodata = _ms == null || _ms.Position < 0 || _ms.Position >= _ms.Length;

            var list = new List<Packet>();
            // 内部缓存没有数据，直接判断输入数据流是否刚好一帧数据，快速处理，绝大多数是这种场景
            if (nodata)
            {
                if (pk == null) return list.ToArray();

                var idx = 0;
                while (idx < pk.Total)
                {
                    //var pk2 = new Packet(pk.Data, pk.Offset + idx, pk.Total - idx);
                    var pk2 = pk.Slice(idx);
                    var len = getLength(pk2);
                    if (len <= 0 || len > pk2.Count) break;

                    pk2.Set(pk2.Data, pk2.Offset, len);
                    //pk2.SetSub(0, len);
                    list.Add(pk2);
                    idx += len;
                }
                // 如果没有剩余，可以返回
                if (idx == pk.Total) return list.ToArray();

                // 剩下的
                pk = pk.Slice(idx);
            }

            if (_ms == null) codec.Stream = _ms = new MemoryStream();

            // 加锁，避免多线程冲突
            lock (_ms)
            {
                // 超过该时间后按废弃数据处理
                var now = TimerX.Now;
                if (_ms.Length > _ms.Position && codec.Last.AddMilliseconds(expire) < now)
                {
                    _ms.SetLength(0);
                    _ms.Position = 0;
                }
                codec.Last = now;

                // 合并数据到最后面
                if (pk != null && pk.Total > 0)
                {
                    var p = _ms.Position;
                    _ms.Position = _ms.Length;
                    pk.WriteTo(_ms);
                    _ms.Position = p;
                }

                // 尝试解包
                while (_ms.Position < _ms.Length)
                {
                    //var pk2 = new Packet(_ms.GetBuffer(), (Int32)_ms.Position, (Int32)_ms.Length);
                    var pk2 = new Packet(_ms);
                    var len = getLength(pk2);

                    // 资源不足一包
                    if (len <= 0 || len > pk2.Total) break;

                    // 解包成功
                    pk2.Set(pk2.Data, pk2.Offset, len);
                    //pk2.SetSub(0, len);
                    list.Add(pk2);

                    _ms.Seek(len, SeekOrigin.Current);
                }

                // 如果读完了数据，需要重置缓冲区
                if (_ms.Position >= _ms.Length)
                {
                    _ms.SetLength(0);
                    _ms.Position = 0;
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
            if (idx < 0)
                return 0;
            else
                return idx + SplitData.Length;

        }

        #endregion
    }
}
