using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace XCode.DataAccessLayer
{
    /// <summary>
    /// 索引
    /// </summary>
    [Serializable]
    [XmlRoot("Index")]
    class XIndex : SerializableDataMember, IDataIndex, ICloneable
    {
        #region 属性
        private String _Name;
        /// <summary>名称</summary>
        public String Name
        {
            get
            {
                if (String.IsNullOrEmpty(_Name)) _Name = GetIndexName();
                return _Name;
            }
            set { _Name = value; }
        }

        private Boolean _Unique;
        /// <summary>是否唯一</summary>
        public Boolean Unique
        {
            get { return _Unique; }
            set { _Unique = value; }
        }

        private String[] _Columns;
        /// <summary>数据列集合</summary>
        public String[] Columns
        {
            get { return _Columns; }
            set { _Columns = value; }
        }
        #endregion

        #region 扩展属性
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

        #region 方法名
        String GetIndexName()
        {
            if (Columns == null || Columns.Length < 1) return null;

            String indexName = "IX";
            if (Table != null) indexName += "_" + Table.Name;
            for (int i = 0; i < Columns.Length; i++)
            {
                indexName += "_" + Columns[i];
            }
            return indexName;
        }
        #endregion

        #region ICloneable 成员
        /// <summary>
        /// 克隆
        /// </summary>
        /// <returns></returns>
        object ICloneable.Clone()
        {
            return Clone(Table);
        }

        /// <summary>
        /// 克隆
        /// </summary>
        /// <param name="table"></param>
        /// <returns></returns>
        public IDataIndex Clone(IDataTable table)
        {
            XIndex field = base.MemberwiseClone() as XIndex;
            field.Table = table;
            return field;
        }
        #endregion

        #region 辅助
        /// <summary>
        /// 已重载。
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            if (Columns != null && Columns.Length > 0)
                return String.Format("{0}=>{1} {2}", Name, String.Join(",", Columns), Unique ? "U" : "");
            else
                return String.Format("{0} {1}", Name, Unique ? "U" : "");
        }
        #endregion
    }
}