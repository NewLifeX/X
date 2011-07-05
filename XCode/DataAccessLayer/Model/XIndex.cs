using System;
using System.Collections.Generic;
using System.Text;

namespace XCode.DataAccessLayer
{
    /// <summary>
    /// 索引
    /// </summary>
    [Serializable]
    public class XIndex
    {
        #region 属性
        private XTable _Table;
        /// <summary>表</summary>
        public XTable Table
        {
            get { return _Table; }
            set { _Table = value; }
        }

        private Dictionary<XField, Boolean> _Fields;
        /// <summary>索引列</summary>
        public Dictionary<XField, Boolean> Fields
        {
            get { return _Fields; }
            set { _Fields = value; }
        }

        private Boolean _Unique;
        /// <summary>是否唯一</summary>
        public Boolean Unique
        {
            get { return _Unique; }
            set { _Unique = value; }
        }
        #endregion
    }
}
