using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NewLife.Serialization;

namespace NewLife.Net.ISO
{
    /// <summary>ISO transport services on top of the TCP</summary>
    public class TPKT : Protocol<TPKT>
    {
        #region 属性
        private Byte _Version;
        /// <summary>版本</summary>
        public Byte Version { get { return _Version; } set { _Version = value; } }

        private Byte _Reserved;
        /// <summary>保留</summary>
        public Byte Reserved { get { return _Reserved; } set { _Reserved = value; } }

        private Int16 _Length;
        /// <summary>长度</summary>
        public Int16 Length { get { return _Length; } set { _Length = value; } }
        #endregion
    }
}