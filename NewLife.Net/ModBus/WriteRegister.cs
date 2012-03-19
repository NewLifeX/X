using System;
using NewLife.Model;

namespace NewLife.Net.ModBus
{
    /// <summary>写入数据到寄存器</summary>
    public class WriteRegister : MBEntity, IModBusRequest, IModBusResponse
    {
        #region 属性
        private UInt16 _DataAddress;
        /// <summary>数据地址</summary>
        public UInt16 DataAddress { get { return _DataAddress; } set { _DataAddress = value; } }

        private UInt16 _Data;
        /// <summary>数据内容</summary>
        public UInt16 Data { get { return _Data; } set { _Data = value; } }
        #endregion

        /// <summary>实例化</summary>
        public WriteRegister() { Function = MBFunction.WriteSingleCoil; }

        static WriteRegister()
        {
            // 基类自动注册WriteSingleCoil时，会引发更多的注册
            ObjectContainer.Current
                .Register<IModBusRequest, WriteRegister>(MBFunction.WriteSingleCoil)
                .Register<IModBusRequest, WriteRegister>(MBFunction.WriteSingleRegister)

                .Register<IModBusResponse, WriteRegister>(MBFunction.WriteSingleCoil)
                .Register<IModBusResponse, WriteRegister>(MBFunction.WriteSingleRegister);
        }
    }
}