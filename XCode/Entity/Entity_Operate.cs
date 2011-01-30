using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using XCode.Configuration;

namespace XCode
{
    partial class Entity<TEntity>
    {
        #region 填充数据
        internal override ICollection LoadDataInternal(DataSet ds) { return LoadData(ds); }
        #endregion

        #region 查找单个实体
        internal override EntityBase FindInternal(String name, Object value) { return Find(name, value); }
        internal override EntityBase FindInternal(String whereClause) { return Find(whereClause); }
        internal override EntityBase FindByKeyInternal(Object key) { return FindByKey(key); }
        internal override EntityBase FindByKeyForEditInternal(Object key) { return FindByKeyForEdit(key); }
        #endregion

        #region 静态查询
        internal override ICollection FindAllInternal() { return FindAll(); }
        internal override ICollection FindAllInternal(String whereClause, String orderClause, String selects, Int32 startRowIndex, Int32 maximumRows)
        {
            return FindAll(whereClause, orderClause, selects, startRowIndex, maximumRows);
        }
        internal override ICollection FindAllInternal(String[] names, Object[] values)
        {
            return FindAll(names, values);
        }
        internal override ICollection FindAllInternal(String name, Object value)
        {
            return FindAll(name, value);
        }
        internal override ICollection FindAllInternal(String name, Object value, Int32 startRowIndex, Int32 maximumRows)
        {
            return FindAll(name, value, startRowIndex, maximumRows);
        }
        internal override ICollection FindAllByNameInternal(String name, Object value, String orderClause, Int32 startRowIndex, Int32 maximumRows)
        {
            return FindAllByName(name, value, orderClause, startRowIndex, maximumRows);
        }
        #endregion

        #region 取总记录数
        internal override Int32 FindCountInternal() { return FindCount(); }

        internal override Int32 FindCountInternal(String whereClause, String orderClause, String selects, Int32 startRowIndex, Int32 maximumRows)
        {
            return FindCount(whereClause, orderClause, selects, startRowIndex, maximumRows);
        }

        internal override Int32 FindCountInternal(String[] names, Object[] values)
        {
            return FindCount(names, values);
        }

        internal override Int32 FindCountInternal(String name, Object value)
        {
            return FindCount(name, value);
        }

        internal override Int32 FindCountInternal(String name, Object value, Int32 startRowIndex, Int32 maximumRows)
        {
            return FindCount(name, value, startRowIndex, maximumRows);
        }

        internal override Int32 FindCountByNameInternal(String name, Object value, String orderClause, Int32 startRowIndex, Int32 maximumRows)
        {
            return FindCountByName(name, value, orderClause, startRowIndex, maximumRows);
        }
        #endregion

        #region 导入导出XML
        internal override EntityBase FromXmlInternal(string xml)
        {
            return FromXml(xml);
        }
        #endregion

        #region 事务
        internal override Int32 BeginTransactionInternal()
        {
            return Meta.BeginTrans();
        }

        internal override Int32 CommitInternal()
        {
            return Meta.Commit();
        }

        internal override Int32 RollbackInternal()
        {
            return Meta.Rollback();
        }
        #endregion

        #region 辅助方法
        internal override String FormatValueInternal(String name, Object value)
        {
            return Meta.FormatValue(name, value);
        }

        internal override String MakeConditionInternal(String[] names, Object[] values, String action)
        {
            return MakeCondition(names, values, action);
        }

        internal override String MakeConditionInternal(String name, Object value, String action)
        {
            return MakeCondition(name, value, action);
        }

        /// <summary>
        /// 所有绑定到数据表的属性
        /// </summary>
        internal override List<FieldItem> FieldsInternal { get { return Meta.Fields; } }

        /// <summary>
        /// 字段名列表
        /// </summary>
        internal override List<String> FieldNamesInternal { get { return Meta.FieldNames; } }
        #endregion
    }
}
