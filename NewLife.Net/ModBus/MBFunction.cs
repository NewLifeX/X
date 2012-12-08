
namespace NewLife.Net.ModBus
{
    /// <summary>Modbus支持的功能码</summary>
    public enum MBFunction : byte
    {
        /// <summary>读取线圈状态。取得一组逻辑线圈的当前状态（ON/OFF) </summary>
        ReadCoils = 1,

        /// <summary>读取输入状态。取得一组开关输入的当前状态（ON/OFF) </summary>
        ReadInputs = 2,

        /// <summary>读取保持寄存器。在一个或多个保持寄存器中取得当前的二进制值 </summary>
        ReadHoldingRegisters = 3,

        /// <summary>读取输入寄存器。在一个或多个输入寄存器中取得当前的二进制值</summary>
        ReadInputRegisters = 4,

        /// <summary>强置单线圈。强置一个逻辑线圈的通断状态</summary>
        WriteSingleCoil = 5,

        /// <summary>预置单寄存器。把具体二进值装入一个保持寄存器</summary>
        WriteSingleRegister = 6,

        /// <summary>读取异常状态。取得8个内部线圈的通断状态，这8个线圈的地址由控制器决定 </summary>
        ReadError = 7,

        /// <summary>回送诊断校验。把诊断校验报文送从机，以对通信处理进行评鉴</summary>
        Diagnostics = 8,

        /// <summary>编程（只用于484）。使主机模拟编程器作用，修改PC从机逻辑 </summary>
        Program484 = 9,

        /// <summary>控询（只用于484）。可使主机与一台正在执行长程序任务从机通信，探询该从机是否已完成其操作任务，仅在含有功能码9的报文发送后，本功能码才发送 </summary>
        Ask484 = 10,

        /// <summary>读取事件计数。可使主机发出单询问，并随即判定操作是否成功，尤其是该命令或其他应答产生通信错误时 </summary>
        ReadEventCount = 11,

        /// <summary>读取通信事件记录。可是主机检索每台从机的ModBus事务处理通信事件记录。如果某项事务处理完成，记录会给出有关错误 </summary>
        ReadEventRecord = 12,

        /// <summary>编程（184/384 484 584）。可使主机模拟编程器功能修改PC从机逻辑 </summary>
        Program584 = 13,

        /// <summary>探询（184/384 484 584）。可使主机与正在执行任务的从机通信，定期控询该从机是否已完成其程序操作，仅在含有功能13的报文发送后，本功能码才得发送 </summary>
        Ask584 = 14,

        ///// <summary></summary>
        //DiagnosticsReturnQueryData = 0,

        /// <summary>强置多线圈。强置一串连续逻辑线圈的通断</summary>
        WriteMultipleCoils = 15,

        /// <summary>预置多寄存器。把具体的二进制值装入一串连续的保持寄存器</summary>
        WriteMultipleRegisters = 16,

        /// <summary>报告从机标识。可使主机判断编址从机的类型及该从机运行指示灯的状态</summary>
        ReportIdentity = 17,

        /// <summary>（884和MICRO 84）。可使主机模拟编程功能，修改PC状态逻辑</summary>
        Program884 = 18,

        /// <summary>重置通信链路。发生非可修改错误后，是从机复位于已知状态，可重置顺序字节</summary>
        Reset = 19,

        ///// <summary></summary>
        //ReadWriteMultipleRegisters = 23,
    }
}