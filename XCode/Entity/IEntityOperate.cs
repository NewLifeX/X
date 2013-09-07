using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using XCode.Cache;
using XCode.Configuration;

namespace XCode
{
    /// <summary>数据实体操作接口</summary>
    public interface IEntityOperate
    {
        #region 属性
        /// <summary>默认实体</summary>
        IEntity Default { get; set; }

        /// <summary>数据表元数据</summary>
        TableItem Table { get; }

        /// <summary>所有数据属性</summary>
        FieldItem[] AllFields { get; }

        /// <summary>所有绑定到数据表的属性</summary>
        FieldItem[] Fields { get; }

        /// <summary>字段名列表</summary>
        IList<String> FieldNames { get; }

        /// <summary>唯一键，返回第一个标识列或者唯一的主键</summary>
        FieldItem Unique { get; }

        /// <summary>连接名</summary>
        String ConnName { get; set; }

        /// <summary>表名</summary>
        String TableName { get; set; }

        /// <summary>已格式化的表名，带有中括号等</summary>
        String FormatedTableName { get; }

        /// <summary>实体缓存</summary>
        IEntityCache Cache { get; }

        /// <summary>单对象实体缓存</summary>
        ISingleEntityCache SingleCache { get; }

        /// <summary>总记录数</summary>
        Int32 Count { get; }
        #endregion

        #region 创建实体
        /// <summary>创建一个实体对象</summary>
        /// <param name="forEdit">是否为了编辑而创建，如果是，可以再次做一些相关的初始化工作</param>
        /// <returns></returns>
        IEntity Create(Boolean forEdit = false);
        #endregion

        #region 填充数据
        /// <summary>加载记录集</summary>
        /// <param name="ds">记录集</param>
        /// <returns>实体数组</returns>
        IEntityList LoadData(DataSet ds);
        #endregion

        #region 查找单个实体
        /// <summary>根据属性以及对应的值，查找单个实体</summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        IEntity Find(String name, Object value);

        /// <summary>根据条件查找单个实体</summary>
        /// <param name="whereClause"></param>
        /// <returns></returns>
        IEntity Find(String whereClause);

        /// <summary>根据主键查找单个实体</summary>
        /// <param name="key"></param>
        /// <returns></returns>
        IEntity FindByKey(Object key);

        /// <summary>根据主键查询一个实体对象用于表单编辑</summary>
        /// <param name="key"></param>
        /// <returns></returns>
        IEntity FindByKeyForEdit(Object key);
        #endregion

        #region 静态查询
        /// <summary>获取所有实体对象。获取大量数据时会非常慢，慎用</summary>
        /// <returns>实体数组</returns>
        IEntityList FindAll();

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
        IEntityList FindAll(String whereClause, String orderClause, String selects, Int32 startRowIndex, Int32 maximumRows);

        /// <summary>根据属性列表以及对应的值列表，获取所有实体对象</summary>
        /// <param name="names">属性列表</param>
        /// <param name="values">值列表</param>
        /// <returns>实体数组</returns>
        IEntityList FindAll(String[] names, Object[] values);

        /// <summary>根据属性以及对应的值，获取所有实体对象</summary>
        /// <param name="name">属性</param>
        /// <param name="value">值</param>
        /// <returns>实体数组</returns>
        IEntityList FindAll(String name, Object value);

        ///// <summary>根据属性以及对应的值，获取所有实体对象</summary>
        ///// <param name="name">属性</param>
        ///// <param name="value">值</param>
        ///// <param name="startRowIndex">开始行，0表示第一行</param>
        ///// <param name="maximumRows">最大返回行数，0表示所有行</param>
        ///// <returns>实体数组</returns>
        //[Obsolete("请改用FindAllByName！这个FindAll跟5参数那个太容易搞混了，害人不浅！")]
        //IEntityList FindAll(String name, Object value, Int32 startRowIndex, Int32 maximumRows);

        /// <summary>根据属性以及对应的值，获取所有实体对象</summary>
        /// <param name="name">属性</param>
        /// <param name="value">值</param>
        /// <param name="orderClause">排序，不带Order By</param>
        /// <param name="startRowIndex">开始行，0表示第一行</param>
        /// <param name="maximumRows">最大返回行数，0表示所有行</param>
        /// <returns>实体数组</returns>
        IEntityList FindAllByName(String name, Object value, String orderClause, Int32 startRowIndex, Int32 maximumRows);
        #endregion

        #region 缓存查询
        /// <summary>根据属性以及对应的值，在缓存中查找单个实体</summary>
        /// <param name="name">属性名称</param>
        /// <param name="value">属性值</param>
        /// <returns></returns>
        IEntity FindWithCache(String name, Object value);

        /// <summary>查找所有缓存</summary>
        /// <returns></returns>
        IEntityList FindAllWithCache();

        /// <summary>根据属性以及对应的值，在缓存中获取所有实体对象</summary>
        /// <param name="name">属性</param>
        /// <param name="value">值</param>
        /// <returns>实体数组</returns>
        IEntityList FindAllWithCache(String name, Object value);
        #endregion

        #region 取总记录数
        /// <summary>返回总记录数</summary>
        /// <returns></returns>
        Int32 FindCount();

        /// <summary>返回总记录数</summary>
        /// <param name="whereClause">条件，不带Where</param>
        /// <param name="orderClause">排序，不带Order By</param>
        /// <param name="selects">查询列</param>
        /// <param name="startRowIndex">开始行，0表示第一行</param>
        /// <param name="maximumRows">最大返回行数，0表示所有行</param>
        /// <returns>总行数</returns>
        Int32 FindCount(String whereClause, String orderClause, String selects, Int32 startRowIndex, Int32 maximumRows);

        /// <summary>根据属性列表以及对应的值列表，返回总记录数</summary>
        /// <param name="names">属性列表</param>
        /// <param name="values">值列表</param>
        /// <returns>总行数</returns>
        Int32 FindCount(String[] names, Object[] values);

        /// <summary>根据属性以及对应的值，返回总记录数</summary>
        /// <param name="name">属性</param>
        /// <param name="value">值</param>
        /// <returns>总行数</returns>
        Int32 FindCount(String name, Object value);

        ///// <summary>根据属性以及对应的值，返回总记录数</summary>
        ///// <param name="name">属性</param>
        ///// <param name="value">值</param>
        ///// <param name="startRowIndex">开始行，0表示第一行</param>
        ///// <param name="maximumRows">最大返回行数，0表示所有行</param>
        ///// <returns>总行数</returns>
        //Int32 FindCount(String name, Object value, Int32 startRowIndex, Int32 maximumRows);

        /// <summary>根据属性以及对应的值，返回总记录数</summary>
        /// <param name="name">属性</param>
        /// <param name="value">值</param>
        /// <param name="orderClause">排序，不带Order By</param>
        /// <param name="startRowIndex">开始行，0表示第一行</param>
        /// <param name="maximumRows">最大返回行数，0表示所有行</param>
        /// <returns>总行数</returns>
        Int32 FindCountByName(String name, Object value, String orderClause, int startRowIndex, int maximumRows);
        #endregion

        #region 导入导出XML
        /// <summary>导入</summary>
        /// <param name="xml"></param>
        /// <returns></returns>
        [Obsolete("该成员在后续版本中将不再被支持！请使用实体访问器IEntityAccessor替代！")]
        IEntity FromXml(String xml);
        #endregion

        #region 导入导出Json
        /// <summary>导入</summary>
        /// <param name="json"></param>
        /// <returns></returns>
        [Obsolete("该成员在后续版本中将不再被支持！请使用实体访问器IEntityAccessor替代！")]
        IEntity FromJson(String json);
        #endregion

        #region 数据库操作
        /// <summary>查询</summary>
        /// <param name="sql">SQL语句</param>
        /// <returns>结果记录集</returns>
        DataSet Query(String sql);

        /// <summary>查询记录数</summary>
        /// <param name="sql">SQL语句</param>
        /// <returns>记录数</returns>
        Int32 QueryCount(String sql);

        /// <summary>执行</summary>
        /// <param name="sql">SQL语句</param>
        /// <returns>影响的结果</returns>
        Int32 Execute(String sql);

        /// <summary>执行插入语句并返回新增行的自动编号</summary>
        /// <param name="sql">SQL语句</param>
        /// <returns>新增行的自动编号</returns>
        Int64 InsertAndGetIdentity(String sql);

        /// <summary>执行</summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="type">命令类型，默认SQL文本</param>
        /// <param name="ps">命令参数</param>
        /// <returns>影响的结果</returns>
        Int32 Execute(String sql, CommandType type = CommandType.Text, params DbParameter[] ps);

        /// <summary>执行插入语句并返回新增行的自动编号</summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="type">命令类型，默认SQL文本</param>
        /// <param name="ps">命令参数</param>
        /// <returns>新增行的自动编号</returns>
        Int64 InsertAndGetIdentity(String sql, CommandType type = CommandType.Text, params DbParameter[] ps);
        #endregion

        #region 事务
        /// <summary>开始事务</summary>
        /// <returns></returns>
        Int32 BeginTransaction();

        /// <summary>提交事务</summary>
        /// <returns></returns>
        Int32 Commit();

        /// <summary>回滚事务</summary>
        /// <returns></returns>
        Int32 Rollback();
        #endregion

        #region 参数化
        /// <summary>创建参数</summary>
        /// <returns></returns>
        DbParameter CreateParameter();

        /// <summary>格式化参数名</summary>
        /// <param name="name"></param>
        /// <returns></returns>
        String FormatParameterName(String name);
        #endregion

        #region 辅助方法
        /// <summary>格式化关键字</summary>
        /// <param name="name"></param>
        /// <returns></returns>
        String FormatName(String name);

        /// <summary>
        /// 取得一个值的Sql值。
        /// 当这个值是字符串类型时，会在该值前后加单引号；
        /// </summary>
        /// <param name="name">字段</param>
        /// <param name="value">对象</param>
        /// <returns>Sql值的字符串形式</returns>
        String FormatValue(String name, Object value);

        /// <summary>格式化数据为SQL数据</summary>
        /// <param name="field"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        String FormatValue(FieldItem field, Object value);

        /// <summary>
        /// 根据属性列表和值列表，构造查询条件。
        /// 例如构造多主键限制查询条件。
        /// </summary>
        /// <param name="names">属性列表</param>
        /// <param name="values">值列表</param>
        /// <param name="action">联合方式</param>
        /// <returns>条件子串</returns>
        String MakeCondition(String[] names, Object[] values, String action);

        /// <summary>构造条件</summary>
        /// <param name="name">名称</param>
        /// <param name="value">值</param>
        /// <param name="action">大于小于等符号</param>
        /// <returns></returns>
        String MakeCondition(String name, Object value, String action);

        /// <summary>构造条件</summary>
        /// <param name="field">名称</param>
        /// <param name="value">值</param>
        /// <param name="action">大于小于等符号</param>
        /// <returns></returns>
        String MakeCondition(FieldItem field, Object value, String action);
        #endregion

        #region 一些设置
        /// <summary>是否允许向自增列插入数据。为免冲突，仅本线程有效</summary>
        Boolean AllowInsertIdentity { get; set; }

        /// <summary>自动设置Guid的字段。对实体类有效，可在实体类类型构造函数里面设置</summary>
        FieldItem AutoSetGuidField { get; set; }
        #endregion
    }
}