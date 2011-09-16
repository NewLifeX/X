using System;
using System.Collections;
using System.Collections.Generic;

namespace XCode
{
    /// <summary>实体列表接口</summary>
    public interface IEntityList : /*IList, */IList<IEntity>
    {
        #region 对象查询
        /// <summary>
        /// 根据指定项查找
        /// </summary>
        /// <param name="name">属性名</param>
        /// <param name="value">属性值</param>
        /// <returns></returns>
        IEntityList FindAll(String name, Object value);

        /// <summary>
        /// 根据指定项查找
        /// </summary>
        /// <param name="names">属性名</param>
        /// <param name="values">属性值</param>
        /// <returns></returns>
        IEntityList FindAll(String[] names, Object[] values);

        /// <summary>
        /// 根据指定项查找
        /// </summary>
        /// <param name="name">属性名</param>
        /// <param name="value">属性值</param>
        /// <returns></returns>
        IEntity Find(String name, Object value);

        /// <summary>
        /// 根据指定项查找字符串。忽略大小写
        /// </summary>
        /// <param name="name">属性名</param>
        /// <param name="value">属性值</param>
        /// <returns></returns>
        IEntityList FindAllIgnoreCase(String name, String value);

        /// <summary>
        /// 根据指定项查找字符串。忽略大小写
        /// </summary>
        /// <param name="name">属性名</param>
        /// <param name="value">属性值</param>
        /// <returns></returns>
        IEntity FindIgnoreCase(String name, String value);

        /// <summary>
        /// 集合是否包含指定项
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        Boolean Exists(String name, Object value);

        /// <summary>
        /// 按指定字段排序
        /// </summary>
        /// <param name="name">字段</param>
        /// <param name="isDesc">是否降序</param>
        void Sort(String name, Boolean isDesc);

        /// <summary>
        /// 按指定字段数组排序
        /// </summary>
        /// <param name="names">字段</param>
        /// <param name="isDescs">是否降序</param>
        void Sort(String[] names, Boolean[] isDescs);
        #endregion

        #region 对象操作
        /// <summary>
        /// 把整个集合插入到数据库
        /// </summary>
        /// <param name="useTransition">是否使用事务保护</param>
        /// <returns></returns>
        Int32 Insert(Boolean useTransition);

        /// <summary>
        /// 把整个集合更新到数据库
        /// </summary>
        /// <param name="useTransition">是否使用事务保护</param>
        /// <returns></returns>
        Int32 Update(Boolean useTransition);

        /// <summary>
        /// 把整个保存更新到数据库
        /// </summary>
        /// <param name="useTransition">是否使用事务保护</param>
        /// <returns></returns>
        Int32 Save(Boolean useTransition);

        /// <summary>
        /// 把整个集合从数据库中删除
        /// </summary>
        /// <param name="useTransition">是否使用事务保护</param>
        /// <returns></returns>
        Int32 Delete(Boolean useTransition);

        /// <summary>
        /// 设置所有实体中指定项的值
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        void SetItem(String name, Object value);

        /// <summary>
        /// 获取所有实体中指定项的值
        /// </summary>
        /// <typeparam name="TResult">指定项的类型</typeparam>
        /// <param name="name"></param>
        /// <returns></returns>
        List<TResult> GetItem<TResult>(String name);

        /// <summary>
        /// 串联指定成员，方便由实体集合构造用于查询的子字符串
        /// </summary>
        /// <param name="name"></param>
        /// <param name="separator"></param>
        /// <returns></returns>
        String Join(String name, String separator);

        /// <summary>
        /// 串联
        /// </summary>
        /// <param name="separator"></param>
        /// <returns></returns>
        String Join(String separator);
        #endregion

        #region 导入导出
        /// <summary>
        /// 导出Xml文本
        /// </summary>
        /// <returns></returns>
        String ToXml();

        /// <summary>
        /// 导入Xml文本
        /// </summary>
        /// <param name="xml"></param>
        void FromXml(String xml);

        /// <summary>
        /// 导出Json
        /// </summary>
        /// <returns></returns>
        String ToJson();

        ///// <summary>
        ///// 导入Json
        ///// </summary>
        ///// <param name="json"></param>
        ///// <returns></returns>
        //IEntityList FromJson(String json);
        #endregion
    }
}