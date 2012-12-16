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
        /// <param name="name"></param>
        /// <param name="value"></param>
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

        /// <summary>把整个集合从数据库中删除</summary>
        /// <param name="useTransition">是否使用事务保护</param>
        /// <returns></returns>
        Int32 Delete(Boolean useTransition);

        /// <summary>设置所有实体中指定项的值</summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        IEntityList SetItem(String name, Object value);

        /// <summary>获取所有实体中指定项的值</summary>
        /// <typeparam name="TResult">指定项的类型</typeparam>
        /// <param name="name"></param>
        /// <returns></returns>
        List<TResult> GetItem<TResult>(String name);

        /// <summary>串联指定成员，方便由实体集合构造用于查询的子字符串</summary>
        /// <param name="name"></param>
        /// <param name="separator"></param>
        /// <returns></returns>
        String Join(String name, String separator);

        /// <summary>串联</summary>
        /// <param name="separator"></param>
        /// <returns></returns>
        String Join(String separator);
        #endregion

        #region 排序
        /// <summary>按指定字段排序</summary>
        /// <param name="name">字段</param>
        /// <param name="isDesc">是否降序</param>
        IEntityList Sort(String name, Boolean isDesc);

        /// <summary>按指定字段数组排序</summary>
        /// <param name="names">字段</param>
        /// <param name="isDescs">是否降序</param>
        IEntityList Sort(String[] names, Boolean[] isDescs);

        /// <summary>提升指定实体在当前列表中的位置，加大排序键的值</summary>
        /// <param name="entity"></param>
        /// <param name="sortKey"></param>
        /// <returns></returns>
        IEntityList Up(IEntity entity, String sortKey);

        /// <summary>降低指定实体在当前列表中的位置，减少排序键的值</summary>
        /// <param name="entity"></param>
        /// <param name="sortKey"></param>
        /// <returns></returns>
        IEntityList Down(IEntity entity, String sortKey);
        #endregion

        #region 导入导出
        /// <summary>导出Xml文本</summary>
        /// <returns></returns>
        String ToXml();

        /// <summary>导入Xml文本</summary>
        /// <param name="xml"></param>
        IEntityList FromXml(String xml);

        /// <summary>导出Json</summary>
        /// <returns></returns>
        String ToJson();

        ///// <summary>
        ///// 导入Json
        ///// </summary>
        ///// <param name="json"></param>
        ///// <returns></returns>
        //IEntityList FromJson(String json);
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