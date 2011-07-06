using System;
using System.Collections.Generic;
using System.Text;

namespace XCode.DataAccessLayer
{
    /// <summary>
    /// 索引
    /// </summary>
    [Serializable]
    class XIndex : IDataIndex
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

        private String[] _Columns;
        /// <summary>数据列集合</summary>
        public String[] Columns
        {
            get { return _Columns; }
            set { _Columns = value; Name = null; }
        }

        private Boolean _Unique;
        /// <summary>是否唯一</summary>
        public Boolean Unique
        {
            get { return _Unique; }
            set { _Unique = value; }
        }
        #endregion

        #region 扩展属性
        private IDataTable _Table;
        /// <summary>表</summary>
        public IDataTable Table
        {
            get { return _Table; }
            set { _Table = value; Name = null; }
        }
        #endregion

        #region 方法名
        String GetIndexName()
        {
            if (Columns == null || Columns.Length < 1) return null;

            String indexName = "IX";
            if (Table != null) indexName += Table.Name + "_";
            for (int i = 0; i < Columns.Length; i++)
            {
                indexName += "_" + Columns[i];
            }
            return indexName;
        }
        #endregion
    }
}