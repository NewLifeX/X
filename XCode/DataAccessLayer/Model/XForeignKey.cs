using System;
using System.Collections.Generic;
using System.Text;

namespace XCode.DataAccessLayer
{
    /// <summary>
    /// 外键
    /// </summary>
    [Serializable]
    class XForeignKey : IDataForeignKey
    {
        #region IDataForeignKey 成员
        private String _ForeignTable;
        /// <summary>外部表</summary>
        public String ForeignTable
        {
            get { return _ForeignTable; }
            set { _ForeignTable = value; }
        }

        private String _ForeignColumn;
        /// <summary>外部列</summary>
        public String ForeignColumn
        {
            get { return _ForeignColumn; }
            set { _ForeignColumn = value; }
        }

        private IDataTable _Table;
        /// <summary>表</summary>
        public IDataTable Table
        {
            get { return _Table; }
            set { _Table = value; }
        }
        #endregion
    }
}