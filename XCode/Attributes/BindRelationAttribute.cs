using System;
using XCode.DataAccessLayer;

namespace XCode
{
    /// <summary>用于指定数据类所绑定到的关系</summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = true)]
    public class BindRelationAttribute : Attribute
    {
        #region 属性
        private String _Column;
        /// <summary>数据列</summary>
        public String Column { get { return _Column; } set { _Column = value; } }

        private String _RelationTable;
        /// <summary>引用表</summary>
        public String RelationTable { get { return _RelationTable; } set { _RelationTable = value; } }

        private String _RelationColumn;
        /// <summary>引用列</summary>
        public String RelationColumn { get { return _RelationColumn; } set { _RelationColumn = value; } }

        private Boolean _Unique;
        /// <summary>是否唯一</summary>
        public Boolean Unique { get { return _Unique; } set { _Unique = value; } }
        #endregion

        #region 构造
        /// <summary>指定一个关系</summary>
        /// <param name="column"></param>
        /// <param name="unique"></param>
        /// <param name="relationtable"></param>
        /// <param name="relationcolumn"></param>
        public BindRelationAttribute(String column, Boolean unique, String relationtable, String relationcolumn)
        {
            Column = column;
            Unique = unique;
            RelationTable = relationtable;
            RelationColumn = relationcolumn;
        }
        #endregion

        #region 方法
        /// <summary>填充关系</summary>
        /// <param name="relation"></param>
        internal void Fill(IDataRelation relation)
        {
            relation.Column = Column;
            relation.Unique = Unique;
            relation.RelationTable = RelationTable;
            relation.RelationColumn = RelationColumn;
        }
        #endregion
    }
}