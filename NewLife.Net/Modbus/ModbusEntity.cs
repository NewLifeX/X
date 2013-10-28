#region Modbus协议
/*
 * GB/T 19582.1-2008 基于Modbus协议的工业自动化网络规范
 * 请求响应：1字节功能码|n字节数据|2字节CRC校验
 * 异常响应：1字节功能码+0x80|1字节异常码
 * 
 * Modbus数据模型基本表
 * 基本表        对象类型   访问类型    注释
 * 离散量输入    单个位     只读        I/O系统可提供这种类型的数据
 * 线圈          单个位     读写        通过应用程序可改变这种类型的数据
 * 输入寄存器    16位字     只读        I/O系统可提供这种类型的数据
 * 保持寄存器    16位字     读写        通过应用程序可改变这种类型的数据
 * 
 */
#endregion

using System;

namespace NewLife.Net.Modbus
{
    /// <summary>Modbus实体</summary>
    public class ModbusEntity
    {
        #region 属性
        /// <summary>头部位移，RS232=0，RS485=1</summary>
        public const Int32 HEAD_OFFSET = 1;
        /// <summary>不包含数据部分的固定长度。地址、功能码、校验码</summary>
        public const Int32 NO_DATA_LENGTH = 1 + HEAD_OFFSET + 2;

        private Byte _Host = 1;
        /// <summary>主机地址。用于485编码</summary>
        public Byte Host { get { return _Host; } set { _Host = value; } }

        private MBFunction _Function;
        /// <summary>功能码</summary>
        public MBFunction Function { get { return _Function; } set { _Function = value; } }

        private Boolean _IsException;
        /// <summary>是否异常</summary>
        public Boolean IsException { get { return _IsException; } set { _IsException = value; } }

        private Byte[] _Data;
        /// <summary>数据</summary>
        public Byte[] Data { get { return _Data; } set { _Data = value; } }

        private UInt16 _Crc;
        /// <summary>校验数据</summary>
        public UInt16 Crc { get { return _Crc; } set { _Crc = value; } }
        #endregion

        //#region 扩展属性
        //public UInt16 Address { get { return Data.ReadUInt16(0); } }
        //public UInt16 Count { get { return Data.ReadUInt16(2); } }
        //#endregion

        #region 读写
        /// <summary>分析字节数组</summary>
        /// <param name="data"></param>
        /// <param name="offset">偏移</param>
        /// <param name="count">数量</param>
        /// <returns></returns>
        public ModbusEntity Parse(Byte[] data, Int32 offset = 0, Int32 count = -1)
        {
            if (data == null) return null;
            if (count <= 0) count = data.Length - offset;
            if (count < NO_DATA_LENGTH) return null;

            var mb = this;
            mb.Host = data[offset];
            // 读取功能码，最高一位代表是否异常
            var f = data[offset + HEAD_OFFSET];
            if ((f & 0x80) == 0)
                mb.Function = (MBFunction)f;
            else
            {
                // 过滤掉异常位
                mb.Function = (MBFunction)(f & 0x7F);
                mb.IsException = true;
            }
            var len = count - NO_DATA_LENGTH;
            if (len > 0) mb.Data = data.ReadBytes(offset + HEAD_OFFSET + 1, len);
            // 最后两个字节是Crc
            //mb.Crc = data.ReadUInt16(offset + count - 2);
            var ofs = offset + count - 2;
            mb.Crc = (UInt16)(data[ofs] + (data[ofs + 1] << 8));

            return mb;
        }

        /// <summary>转为字节数组</summary>
        /// <returns></returns>
        public Byte[] ToArray()
        {
            //// 先计算Crc
            //Crc = Data.Crc();

            var len = NO_DATA_LENGTH;
            var bts = Data;
            if (bts != null && bts.Length > 0) len += bts.Length;
            var buf = new Byte[len];
            buf[0] = Host;
            buf[HEAD_OFFSET] = (Byte)Function;
            // 异常加上0x80
            if (IsException) buf[HEAD_OFFSET] |= 0x80;
            // 复制头数据
            if (len > NO_DATA_LENGTH) Array.Copy(bts, 0, buf, HEAD_OFFSET + 1, bts.Length);

            // 计算Crc
            Crc = buf.Crc(0, len - 2);

            // 最后两个字节是Crc
            //buf.WriteUInt16(len - 2, Crc);
            buf[len - 2] = (Byte)(Crc & 0xFF);
            buf[len - 1] = (Byte)(Crc >> 8);

            return buf;
        }

        /// <summary>设置错误码</summary>
        /// <param name="errorCode"></param>
        /// <returns></returns>
        public ModbusEntity SetError(Errors errorCode)
        {
            IsException = true;
            Data = new Byte[] { (Byte)errorCode };

            return this;
        }
        #endregion

        #region 辅助
        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override string ToString()
        {
            return Function.ToString();
        }
        #endregion
    }
}