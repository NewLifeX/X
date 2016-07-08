using System;
using XCode.DataAccessLayer;

namespace XCode
{
    /// <summary>用于指定数据类所绑定到的关系</summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class BindRelationAttribute : Attribute
    {
        #region 属性
        /// <summary>数据列</summary>
        public String Column { get; set; }

        /// <summary>引用表</summary>
        public String RelationTable { get; set; }

        /// <summary>引用列</summary>
        public String RelationColumn { get; set; }

        /// <summary>是否唯一</summary>
        public Boolean Unique { get; set; }
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

        /// <summary>指定一个表内关联关系</summary>
        /// <param name="relationcolumn"></param>
        public BindRelationAttribute(String relationcolumn)
        {
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