using System;

namespace NewLife.Net.ModBus
{
    /// <summary>回送诊断校验。把诊断校验报文送从机，以对通信处理进行评鉴</summary>
    public class Diagnostics : MBEntity, IModBusRequest, IModBusResponse
    {
        #region 属性
        private UInt16 _SubFunction;
        /// <summary>子功能码</summary>
        public UInt16 SubFunction { get { return _SubFunction; } set { _SubFunction = value; } }

        private UInt16 _Data;
        /// <summary>数据内容</summary>
        public UInt16 Data { get { return _Data; } set { _Data = value; } }
        #endregion

        /// <summary>实例化</summary>
        public Diagnostics() { Function = MBFunction.Diagnostics; }
    }
}