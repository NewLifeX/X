using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NewLife.Data;

namespace XCode.TDengine
{
    public class RpcHead
    {
        #region 属性
        public Byte Version { get; set; }

        public Byte Compression { get; set; }

        public Byte SecurityParameter { get; set; }

        public Byte Encrypt { get; set; }

        public UInt16 TranscationId { get; set; }

        public UInt32 LinkUid { get; set; }

        public Int64 Ahandle { get; set; }

        public UInt32 SourceId { get; set; }

        public UInt32 DestId { get; set; }

        public UInt32 DestIp { get; set; }

        public String Username { get; set; }

        public UInt16 Port { get; set; }

        public Byte Reserved { get; set; }

        public Byte Type { get; set; }

        public Int32 Length { get; set; }

        public UInt32 MsgVersion { get; set; }

        public Int32 Code { get; set; }

        public Packet Payload { get; set; }
        #endregion

        #region 读写
        public Boolean Read(Stream stream)
        {
            return true;
        }
        #endregion
    }
}
