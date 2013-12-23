using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using XCode.Cache;
using XCode.Configuration;
using XCode.DataAccessLayer;

namespace XCode
{
    partial class Entity<TEntity>
    {
        /// <summary>默认的实体操作者</summary>
        public class EntityOperate : IEntityOperate
        {
            #region 属性
            /// <summary>实体类型</summary>
            public virtual Type EntityType { get { return typeof(TEntity); } }

            private IEntity _Default;
            /// <summary>默认实体</summary>
            public virtual IEntity Default
            {
                get { return _Default ?? (_Default = new TEntity()); }
                set { _Default = value; }
            }

            /// <summary>数据表元数据</summary>
            public virtual TableItem Table { get { return Meta.Table; } }

            /// <summary>所有数据属性</summary>
            public virtual FieldItem[] AllFields { get { return Meta.AllFields; } }

            /// <summary>所有绑定到数据表的属性</summary>
            public virtual FieldItem[] Fields { get { return Meta.Fields; } }

            /// <summary>字段名列表</summary>
            public virtual IList<String> FieldNames { get { return Meta.FieldNames; } }

            /// <summary>唯一键，返回第一个标识列或者唯一的主键</summary>
            public virtual FieldItem Unique { get { return Meta.Unique; } }

            /// <summary>连接名</summary>
            public virtual String ConnName { get { return Meta.ConnName; } set { Meta.ConnName = value; } }

            /// <summary>表名</summary>
            public virtual String TableName { get { return Meta.TableName; } set { Meta.TableName = value; } }

            /// <summary>已格式化的表名，带有中括号等</summary>
            public virtual String FormatedTableName { get { return Meta.FormatName(Meta.TableName); } }

            /// <summary>实体缓存</summary>
            public virtual IEntityCache Cache { get { return Meta.Cache; } }

            /// <summary>单对象实体缓存</summary>
            public virtual ISingleEntityCache SingleCache { get { return Meta.SingleCache; } }

            /// <summary>总记录数</summary>
            public virtual Int32 Count { get { return Meta.Count; } }
            #endregion

            #region 创建实体
            /// <summary>创建一个实体对象</summary>
            /// <param name="forEdit">是否为了编辑而创建，如果是，可以再次做一些相关的初始化工作</param>
            /// <returns></returns>
            public virtual IEntity Create(Boolean forEdit = false) { return (Default as TEntity).CreateInstance(forEdit); }
            #endregion

            #region 填充数据
            /// <summary>加载记录集</summary>
            /// <param name="ds">记录集</param>
            /// <returns>实体数组</returns>
            public virtual IEntityList LoadData(DataSet ds) { return Entity<TEntity>.LoadData(ds); }
            #endregion

            #region 查找单个实体
            /// <summary>根据属性以及对应的值，查找单个实体</summary>
            /// <param name="name">名称</param>
            /// <param name="value">数值</param>
            /// <returns></returns>
            public virtual IEntity Find(String name, Object value) { return Entity<TEntity>.Find(name, value); }

            /// <summary>根据条件查找单个实体</summary>
            /// <param name="whereClause"></param>
            /// <returns></returns>
            public virtual IEntity Find(String whereClause) { return Entity<TEntity>.Find(whereClause); }

            /// <summary>根据主键查找单个实体</summary>
            /// <param name="key"></param>
            /// <returns></returns>
            public virtual IEntity FindByKey(Object key) { return Entity<TEntity>.FindByKey(key); }

            /// <summary>根据主键查询一个实体对象用于表单编辑</summary>
            /// <param name="key"></param>
            /// <returns></returns>
            public virtual IEntity FindByKeyForEdit(Object key) { return Entity<TEntity>.FindByKeyForEdit(key); }
            #endregion

            #region 静态查询
            /// <summary>获取所有实体对象。获取大量数据时会非常慢，慎用</summary>
            /// <returns>实体数组</returns>
            public virtual IEntityList FindAll() { return Entity<TEntity>.FindAll(); }

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
            public virtual IEntityList FindAll(String whereClause, String orderClause, String selects, Int32 startRowIndex, Int32 maximumRows)
            {
                return Entity<TEntity>.FindAll(whereClause, orderClause, selects, startRowIndex, maximumRows);
            }

            /// <summary>根据属性列表以及对应的值列表，获取所有实体对象</summary>
            /// <param name="names">属性列表</param>
            /// <param name="values">值列表</param>
            /// <returns>实体数组</returns>
            public virtual IEntityList FindAll(String[] names, Object[] values)
            {
                return Entity<TEntity>.FindAll(names, values);
            }

            /// <summary>根据属性以及对应的值，获取所有实体对象</summary>
            /// <param name="name">属性</param>
            /// <param name="value">值</param>
            /// <returns>实体数组</returns>
            public virtual IEntityList FindAll(String name, Object value)
            {
                return Entity<TEntity>.FindAll(name, value);
            }

            /// <summary>根据属性以及对应的值，获取所有实体对象</summary>
            /// <param name="name">属性</param>
            /// <param name="value">值</param>
            /// <param name="orderClause">排序，不带Order By</param>
            /// <param name="startRowIndex">开始行，0表示第一行</param>
            /// <param name="maximumRows">最大返回行数，0表示所有行</param>
            /// <returns>实体数组</returns>
            public virtual IEntityList FindAllByName(String name, Object value, String orderClause, Int32 startRowIndex, Int32 maximumRows)
            {
                return Entity<TEntity>.FindAllByName(name, value, orderClause, startRowIndex, maximumRows);
            }
            #endregion

            #region 缓存查询
            /// <summary>根据属性以及对应的值，在缓存中查找单个实体</summary>
            /// <param name="name">属性名称</param>
            /// <param name="value">属性值</param>
            /// <returns></returns>
            public virtual IEntity FindWithCache(String name, Object value) { return Entity<TEntity>.FindWithCache(name, value); }

            /// <summary>查找所有缓存</summary>
            /// <returns></returns>
            public virtual IEntityList FindAllWithCache() { return Entity<TEntity>.FindAllWithCache(); }

            /// <summary>根据属性以及对应的值，在缓存中获取所有实体对象</summary>
            /// <param name="name">属性</param>
            /// <param name="value">值</param>
            /// <returns>实体数组</returns>
            public virtual IEntityList FindAllWithCache(String name, Object value) { return Entity<TEntity>.FindAllWithCache(name, value); }
            #endregion

            #region 取总记录数
            /// <summary>返回总记录数</summary>
            /// <returns></returns>
            public virtual Int32 FindCount() { return Entity<TEntity>.FindCount(); }

            /// <summary>返回总记录数</summary>
            /// <param name="whereClause">条件，不带Where</param>
            /// <param name="orderClause">排序，不带Order By</param>
            /// <param name="selects">查询列</param>
            /// <param name="startRowIndex">开始行，0表示第一行</param>
            /// <param name="maximumRows">最大返回行数，0表示所有行</param>
            /// <returns>总行数</returns>
            public virtual Int32 FindCount(String whereClause, String orderClause, String selects, Int32 startRowIndex, Int32 maximumRows)
            {
                return Entity<TEntity>.FindCount(whereClause, orderClause, selects, startRowIndex, maximumRows);
            }

            /// <summary>根据属性列表以及对应的值列表，返回总记录数</summary>
            /// <param name="names">属性列表</param>
            /// <param name="values">值列表</param>
            /// <returns>总行数</returns>
            public virtual Int32 FindCount(String[] names, Object[] values)
            {
                return Entity<TEntity>.FindCount(names, values);
            }

            /// <summary>根据属性以及对应的值，返回总记录数</summary>
            /// <param name="name">属性</param>
            /// <param name="value">值</param>
            /// <returns>总行数</returns>
            public virtual Int32 FindCount(String name, Object value)
            {
                return Entity<TEntity>.FindCount(name, value);
            }

            /// <summary>根据属性以及对应的值，返回总记录数</summary>
            /// <param name="name">属性</param>
            /// <param name="value">值</param>
            /// <param name="orderClause">排序，不带Order By</param>
            /// <param name="startRowIndex">开始行，0表示第一行</param>
            /// <param name="maximumRows">最大返回行数，0表示所有行</param>
            /// <returns>总行数</returns>
            public virtual Int32 FindCountByName(String name, Object value, String orderClause, int startRowIndex, int maximumRows)
            {
                return Entity<TEntity>.FindCountByName(name, value, orderClause, startRowIndex, maximumRows);
            }
            #endregion

            #region 导入导出XML
            /// <summary>导入</summary>
            /// <param name="xml"></param>
            /// <returns></returns>
            [Obsolete("该成员在后续版本中将不再被支持！请使用实体访问器IEntityAccessor替代！")]
            public virtual IEntity FromXml(String xml) { return Entity<TEntity>.FromXml(xml); }
            #endregion

            #region 导入导出Json
            /// <summary>导入</summary>
            /// <param name="json"></param>
            /// <returns></returns>
            [Obsolete("该成员在后续版本中将不再被支持！请使用实体访问器IEntityAccessor替代！")]
            public virtual IEntity FromJson(String json) { return Entity<TEntity>.FromJson(json); }
            #endregion

            #region 数据库操作
            /// <summary>查询</summary>
            /// <param name="sql">SQL语句</param>
            /// <returns>结果记录集</returns>
            public virtual DataSet Query(String sql) { return Meta.Session.Query(sql); }

            /// <summary>查询记录数</summary>
            /// <param name="sql">SQL语句</param>
            /// <returns>记录数</returns>
            public virtual Int32 QueryCount(String sql)
            {
                var sb = new SelectBuilder();
                sb.Parse(sql);
                return Meta.QueryCount(sb);
            }

            /// <summary>执行</summary>
            /// <param name="sql">SQL语句</param>
            /// <returns>影响的结果</returns>
            public virtual Int32 Execute(String sql) { return Meta.Execute(sql); }

            /// <summary>执行插入语句并返回新增行的自动编号</summary>
            /// <param name="sql">SQL语句</param>
            /// <returns>新增行的自动编号</returns>
            public virtual Int64 InsertAndGetIdentity(String sql) { return Meta.InsertAndGetIdentity(sql); }

            /// <summary>执行</summary>
            /// <param name="sql">SQL语句</param>
            /// <param name="type">命令类型，默认SQL文本</param>
            /// <param name="ps">命令参数</param>
            /// <returns>影响的结果</returns>
            public virtual Int32 Execute(String sql, CommandType type, DbParameter[] ps) { return Meta.Execute(sql, type, ps); }

            /// <summary>执行插入语句并返回新增行的自动编号</summary>
            /// <param name="sql">SQL语句</param>
            /// <param name="type">命令类型，默认SQL文本</param>
            /// <param name="ps">命令参数</param>
            /// <returns>新增行的自动编号</returns>
            public virtual Int64 InsertAndGetIdentity(String sql, CommandType type, DbParameter[] ps) { return Meta.InsertAndGetIdentity(sql, type, ps); }
            #endregion

            #region 事务
            /// <summary>开始事务</summary>
            /// <returns></returns>
            public virtual Int32 BeginTransaction() { return Meta.BeginTrans(); }

            /// <summary>提交事务</summary>
            /// <returns></returns>
            public virtual Int32 Commit() { return Meta.Commit(); }

            /// <summary>回滚事务</summary>
            /// <returns></returns>
            public virtual Int32 Rollback() { return Meta.Rollback(); }
            #endregion

            #region 参数化
            /// <summary>创建参数</summary>
            /// <returns></returns>
            public virtual DbParameter CreateParameter() { return Meta.CreateParameter(); }

            /// <summary>格式化参数名</summary>
            /// <param name="name"></param>
            /// <returns></returns>
            public virtual String FormatParameterName(String name) { return Meta.FormatParameterName(name); }
            #endregion

            #region 辅助方法
            /// <summary>格式化关键字</summary>
            /// <param name="name">名称</param>
            /// <returns></returns>
            public virtual String FormatName(String name) { return Meta.FormatName(name); }

            /// <summary>
            /// 取得一个值的Sql值。
            /// 当这个值是字符串类型时，会在该值前后加单引号；
            /// </summary>
            /// <param name="name">字段</param>
            /// <param name="value">对象</param>
            /// <returns>Sql值的字符串形式</returns>
            public virtual String FormatValue(String name, Object value) { return Meta.FormatValue(name, value); }

            /// <summary>格式化数据为SQL数据</summary>
            /// <param name="field">字段</param>
            /// <param name="value">数值</param>
            /// <returns></returns>
            public virtual String FormatValue(FieldItem field, Object value) { return Meta.FormatValue(field, value); }

            /// <summary>
            /// 根据属性列表和值列表，构造查询条件。
            /// 例如构造多主键限制查询条件。
            /// </summary>
            /// <param name="names">属性列表</param>
            /// <param name="values">值列表</param>
            /// <param name="action">联合方式</param>
            /// <returns>条件子串</returns>
            public virtual String MakeCondition(String[] names, Object[] values, String action) { return Entity<TEntity>.MakeCondition(names, values, action); }

            /// <summary>构造条件</summary>
            /// <param name="name">名称</param>
            /// <param name="value">值</param>
            /// <param name="action">大于小于等符号</param>
            /// <returns></returns>
            public virtual String MakeCondition(String name, Object value, String action) { return Entity<TEntity>.MakeCondition(name, value, action); }

            /// <summary>构造条件</summary>
            /// <param name="field">名称</param>
            /// <param name="value">值</param>
            /// <param name="action">大于小于等符号</param>
            /// <returns></returns>
            public virtual String MakeCondition(FieldItem field, Object value, String action) { return Entity<TEntity>.MakeCondition(field, value, action); }
            #endregion

            #region 一些设置
            [ThreadStatic]
            private static Boolean _AllowInsertIdentity;
            /// <summary>是否允许向自增列插入数据。为免冲突，仅本线程有效</summary>
            public virtual Boolean AllowInsertIdentity { get { return _AllowInsertIdentity; } set { _AllowInsertIdentity = value; } }

            private FieldItem _AutoSetGuidField;
            /// <summary>自动设置Guid的字段。对实体类有效，可在实体类类型构造函数里面设置</summary>
            public virtual FieldItem AutoSetGuidField { get { return _AutoSetGuidField; } set { _AutoSetGuidField = value; } }
            #endregion
        }
    }
}