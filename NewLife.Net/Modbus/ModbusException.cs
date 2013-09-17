using System;

namespace NewLife.Net.Modbus
{
    /// <summary>Modbus异常</summary>
    public class ModbusException : Exception
    {
        private Errors _Error;
        /// <summary>错误代码</summary>
        public Errors Error { get { return _Error; } private set { _Error = value; } }

        /// <summary>初始化</summary>
        /// <param name="error"></param>
        public ModbusException(Errors error) { Error = error; }

        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "Modbus Error " + Error;
        }
    }
}