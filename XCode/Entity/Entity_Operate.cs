using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using XCode.Cache;
using XCode.Configuration;

namespace XCode
{
    partial class Entity<TEntity>
    {
        internal class EntityOperate : IEntityOperate
        {
            #region 属性
            private IEntity _Default;
            /// <summary>默认实体</summary>
            public IEntity Default
            {
                get { return _Default ?? (_Default = new TEntity()); }
                set { _Default = value; }
            }

            /// <summary>
            /// 数据表元数据
            /// </summary>
            public TableItem Table { get { return Meta.Table; } }

            /// <summary>
            /// 所有数据属性
            /// </summary>
            public FieldItem[] AllFields { get { return Meta.AllFields; } }

            /// <summary>
            /// 所有绑定到数据表的属性
            /// </summary>
            public FieldItem[] Fields { get { return Meta.Fields; } }

            /// <summary>
            /// 字段名列表
            /// </summary>
            public IList<String> FieldNames { get { return Meta.FieldNames; } }

            /// <summary>连接名</summary>
            public String ConnName { get { return Meta.ConnName; } set { Meta.ConnName = value; } }

            /// <summary>表名</summary>
            public String TableName { get { return Meta.TableName; } set { Meta.TableName = value; } }

            /// <summary>实体缓存</summary>
            public IEntityCache Cache { get { return Meta.Cache; } }

            /// <summary>单对象实体缓存</summary>
            public ISingleEntityCache SingleCache { get { return Meta.SingleCache; } }

            /// <summary>总记录数</summary>
            public Int32 Count { get { return Meta.Count; } }
            #endregion

            #region 创建实体
            /// <summary>
            /// 创建一个实体对象
            /// </summary>
            /// <returns></returns>
            public IEntity Create() { return (Default as TEntity).CreateInstance(); }

            //public void InitData() { (Default as TEntity).InitData(); }
            #endregion

            #region 填充数据
            /// <summary>
            /// 加载记录集
            /// </summary>
            /// <param name="ds">记录集</param>
            /// <returns>实体数组</returns>
            public IEntityList LoadData(DataSet ds) { return ToList(Entity<TEntity>.LoadData(ds)); }

            /// <summary>
            /// 把一个FindAll返回的集合转为实体接口列表集合
            /// </summary>
            /// <param name="collection"></param>
            /// <returns></returns>
            IEntityList ToList(IEntityList collection)
            {
                //if (collection == null || collection.Count < 1) return new IEntityList();

                //IEntityList list = new IEntityList();
                //foreach (IEntity item in collection)
                //{
                //    list.Add(item);
                //}

                //return list;

                return collection as IEntityList;
            }
            #endregion

            #region 查找单个实体
            /// <summary>
            /// 根据属性以及对应的值，查找单个实体
            /// </summary>
            /// <param name="name"></param>
            /// <param name="value"></param>
            /// <returns></returns>
            public IEntity Find(String name, Object value) { return Entity<TEntity>.Find(name, value); }

            /// <summary>
            /// 根据条件查找单个实体
            /// </summary>
            /// <param name="whereClause"></param>
            /// <returns></returns>
            public IEntity Find(String whereClause) { return Entity<TEntity>.Find(whereClause); }

            /// <summary>
            /// 根据主键查找单个实体
            /// </summary>
            /// <param name="key"></param>
            /// <returns></returns>
            public IEntity FindByKey(Object key) { return Entity<TEntity>.FindByKey(key); }

            /// <summary>
            /// 根据主键查询一个实体对象用于表单编辑
            /// </summary>
            /// <param name="key"></param>
            /// <returns></returns>
            public IEntity FindByKeyForEdit(Object key) { return Entity<TEntity>.FindByKeyForEdit(key); }
            #endregion

            #region 静态查询
            /// <summary>
            /// 获取所有实体对象。获取大量数据时会非常慢，慎用
            /// </summary>
            /// <returns>实体数组</returns>
            public IEntityList FindAll() { return ToList(Entity<TEntity>.FindAll()); }

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
            public IEntityList FindAll(String whereClause, String orderClause, String selects, Int32 startRowIndex, Int32 maximumRows)
            {
                return ToList(Entity<TEntity>.FindAll(whereClause, orderClause, selects, startRowIndex, maximumRows));
            }

            /// <summary>
            /// 根据属性列表以及对应的值列表，获取所有实体对象
            /// </summary>
            /// <param name="names">属性列表</param>
            /// <param name="values">值列表</param>
            /// <returns>实体数组</returns>
            public IEntityList FindAll(String[] names, Object[] values)
            {
                return ToList(Entity<TEntity>.FindAll(names, values));
            }

            /// <summary>
            /// 根据属性以及对应的值，获取所有实体对象
            /// </summary>
            /// <param name="name">属性</param>
            /// <param name="value">值</param>
            /// <returns>实体数组</returns>
            public IEntityList FindAll(String name, Object value)
            {
                return ToList(Entity<TEntity>.FindAll(name, value));
            }

            /// <summary>
            /// 根据属性以及对应的值，获取所有实体对象
            /// </summary>
            /// <param name="name">属性</param>
            /// <param name="value">值</param>
            /// <param name="startRowIndex">开始行，0表示第一行</param>
            /// <param name="maximumRows">最大返回行数，0表示所有行</param>
            /// <returns>实体数组</returns>
            public IEntityList FindAll(String name, Object value, Int32 startRowIndex, Int32 maximumRows)
            {
                return ToList(Entity<TEntity>.FindAll(name, value, startRowIndex, maximumRows));
            }

            /// <summary>
            /// 根据属性以及对应的值，获取所有实体对象
            /// </summary>
            /// <param name="name">属性</param>
            /// <param name="value">值</param>
            /// <param name="orderClause">排序，不带Order By</param>
            /// <param name="startRowIndex">开始行，0表示第一行</param>
            /// <param name="maximumRows">最大返回行数，0表示所有行</param>
            /// <returns>实体数组</returns>
            public IEntityList FindAllByName(String name, Object value, String orderClause, Int32 startRowIndex, Int32 maximumRows)
            {
                return ToList(Entity<TEntity>.FindAllByName(name, value, orderClause, startRowIndex, maximumRows));
            }
            #endregion

            #region 取总记录数
            /// <summary>
            /// 返回总记录数
            /// </summary>
            /// <returns></returns>
            public Int32 FindCount() { return Entity<TEntity>.FindCount(); }

            /// <summary>
            /// 返回总记录数
            /// </summary>
            /// <param name="whereClause">条件，不带Where</param>
            /// <param name="orderClause">排序，不带Order By</param>
            /// <param name="selects">查询列</param>
            /// <param name="startRowIndex">开始行，0表示第一行</param>
            /// <param name="maximumRows">最大返回行数，0表示所有行</param>
            /// <returns>总行数</returns>
            public Int32 FindCount(String whereClause, String orderClause, String selects, Int32 startRowIndex, Int32 maximumRows)
            {
                return Entity<TEntity>.FindCount(whereClause, orderClause, selects, startRowIndex, maximumRows);
            }

            /// <summary>
            /// 根据属性列表以及对应的值列表，返回总记录数
            /// </summary>
            /// <param name="names">属性列表</param>
            /// <param name="values">值列表</param>
            /// <returns>总行数</returns>
            public Int32 FindCount(String[] names, Object[] values)
            {
                return Entity<TEntity>.FindCount(names, values);
            }

            /// <summary>
            /// 根据属性以及对应的值，返回总记录数
            /// </summary>
            /// <param name="name">属性</param>
            /// <param name="value">值</param>
            /// <returns>总行数</returns>
            public Int32 FindCount(String name, Object value)
            {
                return Entity<TEntity>.FindCount(name, value);
            }

            /// <summary>
            /// 根据属性以及对应的值，返回总记录数
            /// </summary>
            /// <param name="name">属性</param>
            /// <param name="value">值</param>
            /// <param name="startRowIndex">开始行，0表示第一行</param>
            /// <param name="maximumRows">最大返回行数，0表示所有行</param>
            /// <returns>总行数</returns>
            public Int32 FindCount(String name, Object value, Int32 startRowIndex, Int32 maximumRows)
            {
                return Entity<TEntity>.FindCount(name, value, startRowIndex, maximumRows);
            }

            /// <summary>
            /// 根据属性以及对应的值，返回总记录数
            /// </summary>
            /// <param name="name">属性</param>
            /// <param name="value">值</param>
            /// <param name="orderClause">排序，不带Order By</param>
            /// <param name="startRowIndex">开始行，0表示第一行</param>
            /// <param name="maximumRows">最大返回行数，0表示所有行</param>
            /// <returns>总行数</returns>
            public Int32 FindCountByName(String name, Object value, String orderClause, int startRowIndex, int maximumRows)
            {
                return Entity<TEntity>.FindCountByName(name, value, orderClause, startRowIndex, maximumRows);
            }
            #endregion

            #region 导入导出XML
            /// <summary>
            /// 导入
            /// </summary>
            /// <param name="xml"></param>
            /// <returns></returns>
            [Obsolete("该成员在后续版本中讲不再被支持！请使用实体访问器IEntityAccessor替代！")]
            public IEntity FromXml(String xml) { return Entity<TEntity>.FromXml(xml); }
            #endregion

            #region 导入导出Json
            /// <summary>
            /// 导入
            /// </summary>
            /// <param name="json"></param>
            /// <returns></returns>
            [Obsolete("该成员在后续版本中讲不再被支持！请使用实体访问器IEntityAccessor替代！")]
            public IEntity FromJson(String json) { return Entity<TEntity>.FromJson(json); }
            #endregion

            #region 事务
            /// <summary>
            /// 开始事务
            /// </summary>
            /// <returns></returns>
            public Int32 BeginTransaction() { return Meta.BeginTrans(); }

            /// <summary>
            /// 提交事务
            /// </summary>
            /// <returns></returns>
            public Int32 Commit() { return Meta.Commit(); }

            /// <summary>
            /// 回滚事务
            /// </summary>
            /// <returns></returns>
            public Int32 Rollback() { return Meta.Rollback(); }
            #endregion

            #region 辅助方法
            /// <summary>
            /// 格式化关键字
            /// </summary>
            /// <param name="name"></param>
            /// <returns></returns>
            public String FormatName(String name) { return Meta.FormatName(name); }

            /// <summary>
            /// 取得一个值的Sql值。
            /// 当这个值是字符串类型时，会在该值前后加单引号；
            /// </summary>
            /// <param name="name">字段</param>
            /// <param name="value">对象</param>
            /// <returns>Sql值的字符串形式</returns>
            public String FormatValue(String name, Object value) { return Meta.FormatValue(name, value); }

            /// <summary>
            /// 格式化数据为SQL数据
            /// </summary>
            /// <param name="field"></param>
            /// <param name="value"></param>
            /// <returns></returns>
            public String FormatValue(FieldItem field, Object value) { return Meta.FormatValue(field, value); }

            /// <summary>
            /// 根据属性列表和值列表，构造查询条件。
            /// 例如构造多主键限制查询条件。
            /// </summary>
            /// <param name="names">属性列表</param>
            /// <param name="values">值列表</param>
            /// <param name="action">联合方式</param>
            /// <returns>条件子串</returns>
            public String MakeCondition(String[] names, Object[] values, String action) { return Entity<TEntity>.MakeCondition(names, values, action); }

            /// <summary>
            /// 构造条件
            /// </summary>
            /// <param name="name">名称</param>
            /// <param name="value">值</param>
            /// <param name="action">大于小于等符号</param>
            /// <returns></returns>
            public String MakeCondition(String name, Object value, String action) { return Entity<TEntity>.MakeCondition(name, value, action); }

            /// <summary>
            /// 构造条件
            /// </summary>
            /// <param name="field">名称</param>
            /// <param name="value">值</param>
            /// <param name="action">大于小于等符号</param>
            /// <returns></returns>
            public String MakeCondition(FieldItem field, Object value, String action) { return Entity<TEntity>.MakeCondition(field, value, action); }
            #endregion
        }
    }
}
