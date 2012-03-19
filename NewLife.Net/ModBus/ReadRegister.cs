using System;
using NewLife.Model;
using NewLife.Serialization;

namespace NewLife.Net.ModBus
{
    /// <summary>从寄存器读取数据</summary>
    public class ReadRegister : MBEntity, IModBusRequest
    {
        #region 属性
        private UInt16 _DataAddress;
        /// <summary>数据地址</summary>
        public UInt16 DataAddress { get { return _DataAddress; } set { _DataAddress = value; } }

        private UInt16 _DataLength;
        /// <summary>数据长度，单位是字，一个字两个字节</summary>
        public UInt16 DataLength { get { return _DataLength; } set { _DataLength = value; } }
        #endregion

        /// <summary>实例化</summary>
        public ReadRegister() { Function = MBFunction.ReadCoils; }

        static ReadRegister()
        {
            // 基类自动注册ReadCoils时，会引发更多的注册
            ObjectContainer.Current
                //.Register<IModBusRequest, ReadRegister>(MBFunction.ReadCoils)
                .Register<IModBusRequest, ReadRegister>(MBFunction.ReadInputs)
                .Register<IModBusRequest, ReadRegister>(MBFunction.ReadHoldingRegisters)
                .Register<IModBusRequest, ReadRegister>(MBFunction.ReadInputRegisters)
                ;
        }
    }

    /// <summary>读取线圈状态。取得一组逻辑线圈的当前状态（ON/OFF) </summary>
    public class ReadRegisterResponse : MBEntity, IModBusResponse
    {
        #region 属性
        private UInt16 _Length;

        [FieldSize("_Length")]
        private Byte[] _Data;
        /// <summary>数据</summary>
        public Byte[] Data { get { return _Data; } set { _Data = value; if (value != null)_Length = (UInt16)value.Length; } }
        #endregion

        /// <summary>实例化</summary>
        public ReadRegisterResponse() { Function = MBFunction.ReadCoils; }

        static ReadRegisterResponse()
        {
            // 基类自动注册ReadCoils时，会引发更多的注册
            ObjectContainer.Current
                //.Register<IModBusResponse, ReadRegisterResponse>(MBFunction.ReadCoils)
                .Register<IModBusResponse, ReadRegisterResponse>(MBFunction.ReadInputs)
                .Register<IModBusResponse, ReadRegisterResponse>(MBFunction.ReadHoldingRegisters)
                .Register<IModBusResponse, ReadRegisterResponse>(MBFunction.ReadInputRegisters)
                ;
        }
    }
}