using System;
using System.Collections.Generic;
using System.Data;
using XCode.Configuration;

namespace XCode
{
    /// <summary>
    /// 数据实体操作接口
    /// </summary>
    public interface IEntityOperate
    {
        #region 创建实体
        /// <summary>
        /// 创建一个实体对象
        /// </summary>
        /// <returns></returns>
        IEntity Create();
        #endregion

        #region 填充数据
        /// <summary>
        /// 加载记录集
        /// </summary>
        /// <param name="ds">记录集</param>
        /// <returns>实体数组</returns>
        EntityList<IEntity> LoadData(DataSet ds);
        #endregion

        #region 查找单个实体
        /// <summary>
        /// 根据属性以及对应的值，查找单个实体
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        EntityBase Find(String name, Object value);

        /// <summary>
        /// 根据条件查找单个实体
        /// </summary>
        /// <param name="whereClause"></param>
        /// <returns></returns>
        EntityBase Find(String whereClause);

        /// <summary>
        /// 根据主键查找单个实体
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        EntityBase FindByKey(Object key);

        /// <summary>
        /// 根据主键查询一个实体对象用于表单编辑
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        EntityBase FindByKeyForEdit(Object key);
        #endregion

        #region 静态查询
        /// <summary>
        /// 获取所有实体对象。获取大量数据时会非常慢，慎用
        /// </summary>
        /// <returns>实体数组</returns>
        EntityList<IEntity> FindAll();

        /// <summary>
        /// 查询并返回实体对象集合。
        /// 表名以及所有字段名，请使用类名以及字段对应的属性名，方法内转换为表名和列名
        /// </summary>
        /// <param name="whereClause">条件，不带Where</param>
        /// <param name="orderClause">排序，不带Order By</param>
        /// <param name="selects">查询列</param>
        /// <param name="startRowIndex">开始行，0表示第一行</param>
        /// <param name="maximumRows">最大返回行数，0表示所有行</param>
        /// <returns>实体数组</returns>
        EntityList<IEntity> FindAll(String whereClause, String orderClause, String selects, Int32 startRowIndex, Int32 maximumRows);

        /// <summary>
        /// 根据属性列表以及对应的值列表，获取所有实体对象
        /// </summary>
        /// <param name="names">属性列表</param>
        /// <param name="values">值列表</param>
        /// <returns>实体数组</returns>
        EntityList<IEntity> FindAll(String[] names, Object[] values);

        /// <summary>
        /// 根据属性以及对应的值，获取所有实体对象
        /// </summary>
        /// <param name="name">属性</param>
        /// <param name="value">值</param>
        /// <returns>实体数组</returns>
        EntityList<IEntity> FindAll(String name, Object value);

        /// <summary>
        /// 根据属性以及对应的值，获取所有实体对象
        /// </summary>
        /// <param name="name">属性</param>
        /// <param name="value">值</param>
        /// <param name="startRowIndex">开始行，0表示第一行</param>
        /// <param name="maximumRows">最大返回行数，0表示所有行</param>
        /// <returns>实体数组</returns>
        EntityList<IEntity> FindAll(String name, Object value, Int32 startRowIndex, Int32 maximumRows);

        /// <summary>
        /// 根据属性以及对应的值，获取所有实体对象
        /// </summary>
        /// <param name="name">属性</param>
        /// <param name="value">值</param>
        /// <param name="orderClause">排序，不带Order By</param>
        /// <param name="startRowIndex">开始行，0表示第一行</param>
        /// <param name="maximumRows">最大返回行数，0表示所有行</param>
        /// <returns>实体数组</returns>
        EntityList<IEntity> FindAllByName(String name, Object value, String orderClause, Int32 startRowIndex, Int32 maximumRows);
        #endregion

        #region 取总记录数
        /// <summary>
        /// 返回总记录数
        /// </summary>
        /// <returns></returns>
        Int32 FindCount();

        /// <summary>
        /// 返回总记录数
        /// </summary>
        /// <param name="whereClause">条件，不带Where</param>
        /// <param name="orderClause">排序，不带Order By</param>
        /// <param name="selects">查询列</param>
        /// <param name="startRowIndex">开始行，0表示第一行</param>
        /// <param name="maximumRows">最大返回行数，0表示所有行</param>
        /// <returns>总行数</returns>
        Int32 FindCount(String whereClause, String orderClause, String selects, Int32 startRowIndex, Int32 maximumRows);

        /// <summary>
        /// 根据属性列表以及对应的值列表，返回总记录数
        /// </summary>
        /// <param name="names">属性列表</param>
        /// <param name="values">值列表</param>
        /// <returns>总行数</returns>
        Int32 FindCount(String[] names, Object[] values);

        /// <summary>
        /// 根据属性以及对应的值，返回总记录数
        /// </summary>
        /// <param name="name">属性</param>
        /// <param name="value">值</param>
        /// <returns>总行数</returns>
        Int32 FindCount(String name, Object value);

        /// <summary>
        /// 根据属性以及对应的值，返回总记录数
        /// </summary>
        /// <param name="name">属性</param>
        /// <param name="value">值</param>
        /// <param name="startRowIndex">开始行，0表示第一行</param>
        /// <param name="maximumRows">最大返回行数，0表示所有行</param>
        /// <returns>总行数</returns>
        Int32 FindCount(String name, Object value, Int32 startRowIndex, Int32 maximumRows);

        /// <summary>
        /// 根据属性以及对应的值，返回总记录数
        /// </summary>
        /// <param name="name">属性</param>
        /// <param name="value">值</param>
        /// <param name="orderClause">排序，不带Order By</param>
        /// <param name="startRowIndex">开始行，0表示第一行</param>
        /// <param name="maximumRows">最大返回行数，0表示所有行</param>
        /// <returns>总行数</returns>
        Int32 FindCountByName(String name, Object value, String orderClause, int startRowIndex, int maximumRows);
        #endregion

        #region 导入导出XML
        /// <summary>
        /// 导入
        /// </summary>
        /// <param name="xml"></param>
        /// <returns></returns>
        EntityBase FromXml(String xml);
        #endregion

        #region 事务
        /// <summary>
        /// 开始事务
        /// </summary>
        /// <returns></returns>
        Int32 BeginTransaction();

        /// <summary>
        /// 提交事务
        /// </summary>
        /// <returns></returns>
        Int32 Commit();

        /// <summary>
        /// 回滚事务
        /// </summary>
        /// <returns></returns>
        Int32 Rollback();
        #endregion

        #region 辅助方法
        /// <summary>
        /// 取得一个值的Sql值。
        /// 当这个值是字符串类型时，会在该值前后加单引号；
        /// </summary>
        /// <param name="name">字段</param>
        /// <param name="value">对象</param>
        /// <returns>Sql值的字符串形式</returns>
        String FormatValue(String name, Object value);

        /// <summary>
        /// 根据属性列表和值列表，构造查询条件。
        /// 例如构造多主键限制查询条件。
        /// </summary>
        /// <param name="names">属性列表</param>
        /// <param name="values">值列表</param>
        /// <param name="action">联合方式</param>
        /// <returns>条件子串</returns>
        String MakeCondition(String[] names, Object[] values, String action);

        /// <summary>
        /// 构造条件
        /// </summary>
        /// <param name="name">名称</param>
        /// <param name="value">值</param>
        /// <param name="action">大于小于等符号</param>
        /// <returns></returns>
        String MakeCondition(String name, Object value, String action);

        ///// <summary>
        ///// 把一个FindAll返回的集合转为实体接口列表集合
        ///// </summary>
        ///// <param name="collection"></param>
        ///// <returns></returns>
        //List<IEntity> ToList(ICollection collection);

        /// <summary>
        /// 所有绑定到数据表的属性
        /// </summary>
        List<FieldItem> Fields { get; }

        /// <summary>
        /// 字段名列表
        /// </summary>
        List<String> FieldNames { get; }
        #endregion
    }
}