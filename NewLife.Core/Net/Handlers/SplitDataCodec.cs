using System;
using System.Collections.Generic;
using NewLife.Data;
using NewLife.Messaging;
using NewLife.Model;

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

                // 匹配输入回调，让上层事件收到分包信息
                context.FireRead(rs);
            }

            return null;
        }

        /// <summary>连接关闭时，清空粘包编码器</summary>
        /// <param name="context"></param>
        /// <param name="reason"></param>
        /// <returns></returns>
        public override Boolean Close(IHandlerContext context, String reason)
        {
            if (context.Owner is IExtend ss) ss["Codec"] = null;

            return base.Close(context, reason);
        }

        #region 粘包处理
        /// <summary>解码</summary>
        /// <param name="context"></param>
        /// <param name="pk">包</param>
        /// <returns></returns>
        protected IList<Packet> Decode(IHandlerContext context, Packet pk)
        {
            var ss = context.Owner as IExtend;
            var pc = ss["Codec"] as PacketCodec;
            if (pc == null) ss["Codec"] = pc = new PacketCodec { MaxCache = MaxCacheDataLength, GetLength = GetLineLength };

            return pc.Parse(pk);
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