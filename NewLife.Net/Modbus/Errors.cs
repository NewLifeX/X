
namespace NewLife.Net.Modbus
{
    /// <summary>错误代码</summary>
    public enum Errors : byte
    {
        /// <summary>错误的功能代码</summary>
        FunctionCode = 1,
        /// <summary>错误的数据地址</summary>
        Address = 2,
        /// <summary>错误的数据值</summary>
        Value = 3,
        /// <summary>错误的个数</summary>
        Count,

        /// <summary>处理出错</summary>
        ProcessError,

        /// <summary>错误的数据长度</summary>
        MessageLength,

        /// <summary>Crc校验错误</summary>
        CrcError
    }
}