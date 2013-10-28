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
using System.Threading;

namespace NewLife.Net.Modbus
{
    /// <summary>Modbus主站</summary>
    /// <example>
    /// <code>
    /// var master = new ModbusMaster();
    /// master.Transport = new UdpTransport("127.0.0.1", 502);
    /// 
    /// Assert.IsTrue(master.Diagnostics(), "诊断错误");
    /// 
    /// var ids = master.ReportIdentity();
    /// Assert.IsNotNull(ids, "标识不能为空");
    /// </code>
    /// </example>
    public class ModbusMaster : IDisposable
    {
        #region 属性
        private ITransport _Transport;
        /// <summary>传输接口</summary>
        public ITransport Transport { get { return _Transport; } set { _Transport = value; } }

        private Byte _Host = 1;
        /// <summary>主机地址。用于485编码</summary>
        public Byte Host { get { return _Host; } set { _Host = value; } }

        private Boolean _EnableDebug;
        /// <summary>启用调试</summary>
        public Boolean EnableDebug { get { return _EnableDebug; } set { _EnableDebug = value; } }

        private Int32 _Delay;
        /// <summary>发送数据后接收数据前的延迟时间，默认0毫秒</summary>
        public Int32 Delay { get { return _Delay; } set { _Delay = value; } }
        #endregion

        #region 构造
        /// <summary>析构</summary>
        ~ModbusMaster() { Dispose(false); }

        /// <summary>销毁</summary>
        public void Dispose() { Dispose(true); }

        /// <summary>销毁</summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(Boolean disposing)
        {
            if (disposing) GC.SuppressFinalize(this);

            if (Transport != null) Transport.Dispose();
        }
        #endregion

        #region 方法
#if MF
        Byte[] buf_receive = new Byte[256];
#else
        Byte[] buf_receive = new Byte[1024];
#endif

        /// <summary>处理指令</summary>
        /// <param name="entity">指令实体</param>
        /// <param name="expect">预期返回数据长度</param>
        /// <returns></returns>
        ModbusEntity Process(ModbusEntity entity, Int32 expect)
        {
            if (Transport == null) throw new ArgumentNullException("Transport");

            entity.Host = Host;

            // 发送
            var buf = entity.ToArray();

#if DEBUG
            var str = "Request :";
            for (int i = 0; i < buf.Length; i++)
            {
                str += " " + buf[i].ToString("X2");
            }
            WriteLine(str);
#endif

            // 预期返回指令长度，传入参数expect没有考虑头部和校验位
            Transport.FrameSize = expect + ModbusEntity.NO_DATA_LENGTH;
            Transport.Send(buf);

            // lscy 2013-7-29 
            // 发送后，休眠一段时间，避免设备数据未全部写到串口缓冲区中
            // 一般情况下，100ms 已足够
            if (Delay > 0) Thread.Sleep(Delay);

            // 读取
            var count = Transport.Receive(buf_receive);
            if (count <= 0) return null;

#if DEBUG
            str = "Response:";
            for (int i = 0; i < count; i++)
            {
                str += " " + buf_receive[i].ToString("X2");
            }
            WriteLine(str);
            WriteLine("");
#endif

            var rs = new ModbusEntity().Parse(buf_receive, 0, count);
            if (rs == null) return null;
            if (rs.IsException) throw new ModbusException(rs.Data != null && rs.Data.Length > 0 ? (Errors)rs.Data[0] : (Errors)0);
            return rs;
        }
        #endregion

        #region 线圈
        /// <summary>读取线圈状态</summary>
        /// <remarks>
        /// 请求：0x01|2字节起始地址|2字节线圈数量(1~2000)
        /// 响应：0x01|1字节字节计数|n字节线圈状态（n=输出数量/8，如果余数不为0，n=n+1）
        /// </remarks>
        /// <param name="addr"></param>
        /// <returns></returns>
        public Boolean ReadCoil(Int32 addr)
        {
            return ReadInputs(MBFunction.ReadCoils, addr, 1)[0];
        }

        /// <summary>读取线圈状态</summary>
        /// <remarks>
        /// 请求：0x01|2字节起始地址|2字节线圈数量(1~2000)
        /// 响应：0x01|1字节字节计数|n字节线圈状态（n=输出数量/8，如果余数不为0，n=n+1）
        /// </remarks>
        /// <param name="addr"></param>
        /// <param name="count">数量</param>
        /// <returns></returns>
        public Boolean[] ReadCoils(Int32 addr, UInt16 count)
        {
            return ReadInputs(MBFunction.ReadCoils, addr, count);
        }

        /// <summary>读取离散量输入</summary>
        /// <remarks>
        /// 请求：0x02|2字节起始地址|2字节输入数量(1~2000)
        /// 响应：0x02|1字节字节计数|n字节输入状态（n=输入数量/8，如果余数不为0，n=n+1）
        /// </remarks>
        /// <param name="addr"></param>
        /// <param name="count">数量</param>
        /// <returns></returns>
        public Boolean[] ReadInputs(Int32 addr, UInt16 count)
        {
            return ReadInputs(MBFunction.ReadInputs, addr, count);
        }

        Boolean[] ReadInputs(MBFunction func, Int32 addr, UInt16 count)
        {
            var cmd = new ModbusEntity();
            cmd.Function = func;
            var buf = new Byte[4];
            buf.WriteUInt16(0, addr);
            buf.WriteUInt16(2, count);
            cmd.Data = buf;

            var rLen = 1 + count / 8;
            if (count % 8 != 0) rLen++;
            var rs = Process(cmd, rLen);
            if (rs == null || rs.Data == null || rs.Data.Length < 1) return null;

            // 特殊处理单个读取，提高效率
            if (count == 1) return new Boolean[] { rs.Data[1] == 1 };

            var flags = new Boolean[count];

            // 元素存放于m字节n位
            Int32 m = 0, n = 0;
            for (var i = 0; i < flags.Length && 1 + m < rs.Data.Length; i++)
            {
                if (((rs.Data[1 + m] >> n) & 0x01) == 1) flags[i] = true;
                if (++n >= 8)
                {
                    m++;
                    n = 0;
                }
            }

            return flags;
        }

        /// <summary>写单个线圈</summary>
        /// <remarks>
        /// 请求：0x05|2字节输出地址|2字节输出值（0x0000/0xFF00）
        /// 响应：0x05|2字节输出地址|2字节输出值（0x0000/0xFF00）
        /// </remarks>
        /// <param name="addr"></param>
        /// <param name="flag"></param>
        /// <returns></returns>
        public Boolean WriteSingleCoil(Int32 addr, Boolean flag)
        {
            var cmd = new ModbusEntity();
            cmd.Function = MBFunction.WriteSingleCoil;
            var buf = new Byte[4];
            buf.WriteUInt16(0, addr);
            if (flag) buf.WriteUInt16(2, 0xFF00);
            cmd.Data = buf;

            var rs = Process(cmd, 2 + 2);
            if (rs == null) return false; ;

            return (rs.Data.ReadUInt16(2) != 0) == flag;
        }

        /// <summary>写多个线圈</summary>
        /// <remarks>
        /// 请求：0x0F|2字节起始地址|2字节输出数量（1~1698）|1字节字节计数|n字节输出值（n=输出数量/8，如果余数不为0，n=n+1）
        /// 响应：0x0F|2字节起始地址|2字节输出数量
        /// </remarks>
        /// <param name="addr"></param>
        /// <param name="flags"></param>
        /// <returns></returns>
        public Boolean WriteMultipleCoils(Int32 addr, params Boolean[] flags)
        {
            var cmd = new ModbusEntity();
            cmd.Function = MBFunction.WriteMultipleCoils;

            var n = flags.Length / 8;
            if (flags.Length % 8 != 0) n++;

            var buf = new Byte[4 + 1 + n];
            buf.WriteUInt16(0, addr);
            buf.WriteUInt16(2, (UInt16)flags.Length);

            buf[4] = (Byte)n;

            // 元素存放于m字节n位
            var m = n = 0;
            for (int i = 0; i < flags.Length; i++)
            {
                if (flags[i]) buf[5 + m] |= (Byte)(1 << n);

                if (++n >= 8)
                {
                    m++;
                    n = 0;
                }
            }

            cmd.Data = buf;

            var rs = Process(cmd, 2 + 2);
            if (rs == null) return false;

            return rs.Data.ReadUInt16(0) == addr && rs.Data.ReadUInt16(2) == flags.Length;
        }
        #endregion

        #region 寄存器
        /// <summary>读取保持寄存器</summary>
        /// <remarks>
        /// 请求：0x03|2字节起始地址|2字节寄存器数量（1~2000）
        /// 响应：0x03|1字节字节数|n*2字节寄存器值
        /// </remarks>
        /// <param name="addr"></param>
        /// <returns></returns>
        public UInt16 ReadHoldingRegister(Int32 addr)
        {
            return ReadRegisters(MBFunction.ReadHoldingRegisters, addr, 1)[0];
        }

        /// <summary>读取保持寄存器</summary>
        /// <remarks>
        /// 请求：0x03|2字节起始地址|2字节寄存器数量（1~2000）
        /// 响应：0x03|1字节字节数|n*2字节寄存器值
        /// </remarks>
        /// <param name="addr"></param>
        /// <param name="count">数量</param>
        /// <returns></returns>
        public UInt16[] ReadHoldingRegisters(Int32 addr, UInt16 count)
        {
            return ReadRegisters(MBFunction.ReadHoldingRegisters, addr, count);
        }

        /// <summary>读取输入寄存器</summary>
        /// <remarks>
        /// 请求：0x04|2字节起始地址|2字节输入寄存器数量（1~2000）
        /// 响应：0x04|1字节字节数|n*2字节输入寄存器
        /// </remarks>
        /// <param name="addr"></param>
        /// <param name="count">数量</param>
        /// <returns></returns>
        public UInt16[] ReadInputRegisters(Int32 addr, UInt16 count)
        {
            return ReadRegisters(MBFunction.ReadInputRegisters, addr, count);
        }

        UInt16[] ReadRegisters(MBFunction func, Int32 addr, UInt16 count)
        {
            var cmd = new ModbusEntity();
            cmd.Function = func;
            var buf = new Byte[4];
            buf.WriteUInt16(0, addr);
            buf.WriteUInt16(2, count);
            cmd.Data = buf;

            var rs = Process(cmd, 1 + count * 2);
            if (rs == null) return null;

            count = rs.Data[0];
            if (1 + count > rs.Data.Length) count = (UInt16)(rs.Data.Length - 1);

            var ds = new UInt16[count / 2];
            for (int i = 0; i < ds.Length; i++)
            {
                ds[i] = rs.Data.ReadUInt16(1 + i * 2);
            }

            return ds;
        }

        /// <summary>写单个寄存器</summary>
        /// <remarks>
        /// 请求：0x06|2字节寄存器地址|2字节寄存器值
        /// 响应：0x06|2字节寄存器地址|2字节寄存器值
        /// </remarks>
        /// <param name="addr"></param>
        /// <param name="val"></param>
        /// <returns></returns>
        public Boolean WriteSingleRegister(Int32 addr, UInt16 val)
        {
            var cmd = new ModbusEntity();
            cmd.Function = MBFunction.WriteSingleRegister;
            var buf = new Byte[4];
            buf.WriteUInt16(0, addr);
            buf.WriteUInt16(2, val);
            cmd.Data = buf;

            var rs = Process(cmd, 2 + 2);
            if (rs == null) return false; ;

            return rs.Data.ReadUInt16(0) == addr && rs.Data.ReadUInt16(2) == val;
        }

        /// <summary>写多个寄存器</summary>
        /// <remarks>
        /// 请求：0x10|2字节起始地址|2字节寄存器数量（1~123）|1字节字节计数|n*2寄存器值
        /// 响应：0x10|2字节起始地址|2字节寄存器数量
        /// </remarks>
        /// <param name="addr"></param>
        /// <param name="vals"></param>
        /// <returns></returns>
        public Boolean WriteMultipleRegisters(Int32 addr, params UInt16[] vals)
        {
            var cmd = new ModbusEntity();
            cmd.Function = MBFunction.WriteMultipleRegisters;

            var buf = new Byte[4 + 1 + vals.Length * 2];
            buf.WriteUInt16(0, addr);
            buf.WriteUInt16(2, (UInt16)vals.Length);
            // 字节计数
            buf[4] = (Byte)(vals.Length * 2);

            for (int i = 0; i < vals.Length; i++)
            {
                buf.WriteUInt16(5 + i * 2, vals[i]);
            }

            cmd.Data = buf;

            var rs = Process(cmd, 2 + 2);
            if (rs == null) return false;

            return rs.Data.ReadUInt16(0) == addr && rs.Data.ReadUInt16(2) == vals.Length;
        }
        #endregion

        #region 诊断标识
        /// <summary>诊断</summary>
        /// <remarks>
        /// 请求：0x08|2字节子功能|n*2字节数据
        /// 响应：0x08|2字节子功能|n*2字节数据
        /// </remarks>
        /// <returns></returns>
        public Boolean Diagnostics()
        {
            var cmd = new ModbusEntity();
            cmd.Function = MBFunction.Diagnostics;

            // 子功能码
            var buf = new Byte[2];
            buf.WriteUInt16(0, 0);
            cmd.Data = buf;

            var rs = Process(cmd, 2);
            return rs != null;
        }

        /// <summary>返回标识</summary>
        /// <returns></returns>
        public Byte[] ReportIdentity()
        {
            var cmd = new ModbusEntity();
            cmd.Function = MBFunction.ReportIdentity;

            var rs = Process(cmd, 1 + 8);
            if (rs == null) return null;

            var count = (Int32)rs.Data[0];
            if (count > rs.Data.Length - 1) count = rs.Data.Length - 1;

            if (count <= 0) return new Byte[0];

            return rs.Data.ReadBytes(1, count);
        }
        #endregion

        #region 日志
        void WriteLine(String msg)
        {
#if MF
            if (EnableDebug) Microsoft.SPOT.Debug.Print(msg);
#else
            if (EnableDebug) NewLife.Log.XTrace.WriteLine(msg);
#endif
        }
        #endregion
    }
}