using System;
using System.Collections.Generic;
using System.Text;

namespace NewLife.Net.ModBus
{
    /// <summary>预置单寄存器。把具体二进值装入一个保持寄存器</summary>
    public class WriteSingleRegister : MBEntity, IModBusRequest, IModBusResponse
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
        public WriteSingleRegister() { Function = MBFunction.WriteSingleRegister; }
    }
}