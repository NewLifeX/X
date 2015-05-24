using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NewLife.Serialization;

namespace NewLife.Net.ISO
{
    /// <summary></summary>
    public class COTP : Protocol<COTP>
    {
        #region 属性
        private Byte _Length;
        /// <summary>长度</summary>
        public Byte Length { get { return _Length; } set { _Length = value; } }

        private Byte _PDUType;
        /// <summary>类型</summary>
        public Byte PDUType { get { return _PDUType; } set { _PDUType = value; } }

        private Byte _Flag;
        /// <summary>标识位</summary>
        public Byte Flag { get { return _Flag; } set { _Flag = value; } }
        #endregion
    }
}