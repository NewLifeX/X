
namespace NewLife.Net.Modbus
{
    /// <summary>Modbus功能码</summary>
    public enum MBFunction : byte
    {
        /// <summary>读取线圈状态。取得一组逻辑线圈的当前状态（ON/OFF) </summary>
        ReadCoils = 1,

        /// <summary>读取离散量输入状态。取得一组开关输入的当前状态（ON/OFF) </summary>
        ReadInputs = 2,

        /// <summary>读取保持寄存器。在一个或多个保持寄存器中取得当前的二进制值 </summary>
        ReadHoldingRegisters = 3,

        /// <summary>读取输入寄存器。在一个或多个输入寄存器中取得当前的二进制值</summary>
        ReadInputRegisters = 4,

        /// <summary>强置单线圈。强置一个逻辑线圈的通断状态</summary>
        WriteSingleCoil = 5,

        /// <summary>预置单寄存器。把具体二进值装入一个保持寄存器</summary>
        WriteSingleRegister = 6,

        ///// <summary>读取异常状态。取得8个内部线圈的通断状态，这8个线圈的地址由控制器决定 </summary>
        //ReadError = 7,

        /// <summary>回送诊断校验。把诊断校验报文送从机，以对通信处理进行评鉴</summary>
        Diagnostics = 8,

        /// <summary>强置多线圈。强置一串连续逻辑线圈的通断</summary>
        WriteMultipleCoils = 15,

        /// <summary>预置多寄存器。把具体的二进制值装入一串连续的保持寄存器</summary>
        WriteMultipleRegisters = 16,

        /// <summary>报告从机标识。可使主机判断编址从机的类型及该从机运行指示灯的状态</summary>
        ReportIdentity = 17,
    }
}