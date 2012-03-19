using System;
using System.Collections.Generic;
using System.Text;
using NewLife.Serialization;

namespace NewLife.Net.ModBus
{
    /// <summary>读取线圈状态。取得一组逻辑线圈的当前状态（ON/OFF) </summary>
    public class ReadCoils : MBEntity, IModBusRequest
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
        public ReadCoils() { Function = MBFunction.ReadCoils; }
    }

    /// <summary>读取线圈状态。取得一组逻辑线圈的当前状态（ON/OFF) </summary>
    public class ReadCoilsResponse : MBEntity, IModBusResponse
    {
        #region 属性
        private UInt16 _Length;

        [FieldSize("_Length")]
        private Byte[] _Data;
        /// <summary>数据</summary>
        public Byte[] Data { get { return _Data; } set { _Data = value; } }
        #endregion

        /// <summary>实例化</summary>
        public ReadCoilsResponse() { Function = MBFunction.ReadCoils; }
    }
}