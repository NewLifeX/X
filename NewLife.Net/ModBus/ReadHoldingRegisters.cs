//using System;
//using System.Collections.Generic;
//using System.Text;
//using NewLife.Serialization;

//namespace NewLife.Net.ModBus
//{
//    /// <summary>读取保持寄存器。在一个或多个保持寄存器中取得当前的二进制值</summary>
//    public class ReadHoldingRegisters : MBEntity, IModBusRequest
//    {
//        #region 属性
//        private UInt16 _DataAddress;
//        /// <summary>数据地址</summary>
//        public UInt16 DataAddress { get { return _DataAddress; } set { _DataAddress = value; } }

//        private UInt16 _DataLength;
//        /// <summary>数据长度，单位是字，一个字两个字节</summary>
//        public UInt16 DataLength { get { return _DataLength; } set { _DataLength = value; } }
//        #endregion

//        /// <summary>实例化</summary>
//        public ReadHoldingRegisters() { Function = MBFunction.ReadHoldingRegisters; }
//    }

//    /// <summary>读取保持寄存器响应。在一个或多个保持寄存器中取得当前的二进制值</summary>
//    public class ReadHoldingRegistersResponse : MBEntity, IModBusResponse
//    {
//        #region 属性
//        private UInt16 _Length;

//        [FieldSize("_Length")]
//        private Byte[] _Data;
//        /// <summary>数据</summary>
//        public Byte[] Data { get { return _Data; } set { _Data = value; } }
//        #endregion

//        /// <summary>实例化</summary>
//        public ReadHoldingRegistersResponse() { Function = MBFunction.ReadHoldingRegisters; }
//    }
//}