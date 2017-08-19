using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;

namespace XCode
{
    /// <summary>实体列表接口</summary>
    public interface IEntityList : /*IList, */ IEnumerable, IList<IEntity>
    {
        #region 对象查询
        /// <summary>根据指定项查找</summary>
        /// <param name="name">属性名</param>
        /// <param name="value">属性值</param>
        /// <returns></returns>
        IEntityList FindAll(String name, Object value);

        /// <summary>根据指定项查找</summary>
        /// <param name="names">属性名</param>
        /// <param name="values">属性值</param>
        /// <returns></returns>
        IEntityList FindAll(String[] names, Object[] values);

        /// <summary>根据指定项查找</summary>
        /// <param name="name">属性名</param>
        /// <param name="value">属性值</param>
        /// <returns></returns>
        IEntity Find(String name, Object value);

        /// <summary>根据指定项查找字符串。忽略大小写</summary>
        /// <param name="name">属性名</param>
        /// <param name="value">属性值</param>
        /// <returns></returns>
        IEntityList FindAllIgnoreCase(String name, String value);

        /// <summary>根据指定项查找字符串。忽略大小写</summary>
        /// <param name="name">属性名</param>
        /// <param name="value">属性值</param>
        /// <returns></returns>
        IEntity FindIgnoreCase(String name, String value);

        /// <summary>集合是否包含指定项</summary>
        /// <param name="name">名称</param>
        /// <param name="value">数值</param>
        /// <returns></returns>
        Boolean Exists(String name, Object value);
        #endregion

        #region 对象操作
        /// <summary>把整个集合插入到数据库</summary>
        /// <param name="useTransition">是否使用事务保护</param>
        /// <returns></returns>
        Int32 Insert(Boolean useTransition);

        /// <summary>把整个集合更新到数据库</summary>
        /// <param name="useTransition">是否使用事务保护</param>
        /// <returns></returns>
        Int32 Update(Boolean useTransition);

        /// <summary>把整个保存更新到数据库</summary>
        /// <param name="useTransition">是否使用事务保护</param>
        /// <returns></returns>
        Int32 Save(Boolean useTransition);

        /// <summary>把整个保存更新到数据库，保存时不需要验证</summary>
        /// <param name="useTransition">是否使用事务保护</param>
        /// <returns></returns>
        Int32 SaveWithoutValid(Boolean useTransition);

        /// <summary>把整个集合从数据库中删除</summary>
        /// <param name="useTransition">是否使用事务保护</param>
        /// <returns></returns>
        Int32 Delete(Boolean useTransition);
        #endregion

        #region 导入导出
        /// <summary>实体列表转为字典。主键为Key</summary>
        /// <param name="valueField">作为Value部分的字段，默认为空表示整个实体对象为值</param>
        /// <returns></returns>
        IDictionary ToDictionary(String valueField = null);
        #endregion

        #region 导出DataSet数据集
        /// <summary>转为DataTable</summary>
        /// <param name="allowUpdate">是否允许更新数据，如果允许，将可以对DataTable进行添删改等操作</param>
        /// <returns></returns>
        DataTable ToDataTable(Boolean allowUpdate = true);

        /// <summary>转为DataSet</summary>
        /// <returns></returns>
        DataSet ToDataSet();
        #endregion
    }
}