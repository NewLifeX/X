using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace XCode.DataAccessLayer
{
    /// <summary>
    /// 数据关系
    /// </summary>
    [Serializable]
    [XmlRoot("Relation")]
    public class XRelation : IDataRelation
    {
        #region 属性
        private String _ForeignTable;
        /// <summary>外部表</summary>
        public String RelationTable
        {
            get { return _ForeignTable; }
            set { _ForeignTable = value; }
        }

        private String _ForeignColumn;
        /// <summary>外部列</summary>
        public String RelationColumn
        {
            get { return _ForeignColumn; }
            set { _ForeignColumn = value; }
        }

        [NonSerialized]
        private IDataTable _Table;
        /// <summary>表</summary>
        [XmlIgnore]
        public IDataTable Table
        {
            get { return _Table; }
            set { _Table = value; }
        }
        #endregion
    }
}