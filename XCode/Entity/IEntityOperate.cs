using System;
using System.Collections.Generic;
using System.Data;
using XCode.Cache;
using XCode.Configuration;

namespace XCode
{
    /// <summary>数据实体操作接口</summary>
    public interface IEntityOperate
    {
        #region 主要属性
        /// <summary>实体类型</summary>
        Type EntityType { get; }

        /// <summary>实体会话</summary>
        IEntitySession Session { get; }
        #endregion

        #region 属性
        /// <summary>默认实体</summary>
        IEntity Default { get; set; }

        /// <summary>数据表元数据</summary>
        TableItem Table { get; }

        /// <summary>所有数据属性</summary>
        FieldItem[] AllFields { get; }

        /// <summary>所有绑定到数据表的属性</summary>
        FieldItem[] Fields { get; }

        /// <summary>字段名集合，不区分大小写的哈希表存储，外部不要修改元素数据</summary>
        ICollection<String> FieldNames { get; }

        /// <summary>唯一键，返回第一个标识列或者唯一的主键</summary>
        FieldItem Unique { get; }

        /// <summary>主字段。主字段作为业务主要字段，代表当前数据行意义</summary>
        FieldItem Master { get; }

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

        #region 创建实体、填充数据
        /// <summary>创建一个实体对象</summary>
        /// <param name="forEdit">是否为了编辑而创建，如果是，可以再次做一些相关的初始化工作</param>
        /// <returns></returns>
        IEntity Create(Boolean forEdit = false);

        /// <summary>加载记录集</summary>
        /// <param name="ds">记录集</param>
        /// <returns>实体数组</returns>
        IList<IEntity> LoadData(DataSet ds);
        #endregion

        #region 查找单个实体
        /// <summary>根据属性以及对应的值，查找单个实体</summary>
        /// <param name="name">名称</param>
        /// <param name="value">数值</param>
        /// <returns></returns>
        IEntity Find(String name, Object value);

        /// <summary>根据条件查找单个实体</summary>
        /// <param name="where"></param>
        /// <returns></returns>
        IEntity Find(Expression where);

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
        IList<IEntity> FindAll();

        /// <summary>
        /// 查询并返回实体对象集合。
        /// 表名以及所有字段名，请使用类名以及字段对应的属性名，方法内转换为表名和列名
        /// </summary>
        /// <param name="where">条件，不带Where</param>
        /// <param name="order">排序，不带Order By</param>
        /// <param name="selects">查询列</param>
        /// <param name="startRowIndex">开始行，0表示第一行</param>
        /// <param name="maximumRows">最大返回行数，0表示所有行</param>
        /// <returns>实体数组</returns>
        IList<IEntity> FindAll(String where, String order, String selects, Int64 startRowIndex, Int64 maximumRows);

        /// <summary>
        /// 查询并返回实体对象集合。
        /// 表名以及所有字段名，请使用类名以及字段对应的属性名，方法内转换为表名和列名
        /// </summary>
        /// <param name="where">条件，不带Where</param>
        /// <param name="order">排序，不带Order By</param>
        /// <param name="selects">查询列</param>
        /// <param name="startRowIndex">开始行，0表示第一行</param>
        /// <param name="maximumRows">最大返回行数，0表示所有行</param>
        /// <returns>实体数组</returns>
        IList<IEntity> FindAll(Expression where, String order, String selects, Int64 startRowIndex, Int64 maximumRows);
        #endregion

        #region 缓存查询
        /// <summary>查找所有缓存</summary>
        /// <returns></returns>
        IList<IEntity> FindAllWithCache();
        #endregion

        #region 取总记录数
        /// <summary>返回总记录数</summary>
        /// <returns></returns>
        Int64 FindCount();

        /// <summary>返回总记录数</summary>
        /// <param name="where">条件，不带Where</param>
        /// <param name="order">排序，不带Order By</param>
        /// <param name="selects">查询列</param>
        /// <param name="startRowIndex">开始行，0表示第一行</param>
        /// <param name="maximumRows">最大返回行数，0表示所有行</param>
        /// <returns>总行数</returns>
        Int32 FindCount(String where, String order, String selects, Int64 startRowIndex, Int64 maximumRows);

        /// <summary>返回总记录数</summary>
        /// <param name="where">条件，不带Where</param>
        /// <returns>总行数</returns>
        Int64 FindCount(Expression where);
        #endregion

        #region 高并发
        /// <summary>获取 或 新增 对象，常用于统计等高并发更新的情况，一般配合SaveAsync</summary>
        /// <typeparam name="TKey"></typeparam>
        /// <param name="key">业务主键</param>
        /// <param name="find">查找函数</param>
        /// <param name="create">创建对象</param>
        /// <returns></returns>
        IEntity GetOrAdd<TKey>(TKey key, Func<TKey, Boolean, IEntity> find = null, Func<TKey, IEntity> create = null);
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

        /// <summary>创建事务</summary>
        EntityTransaction CreateTrans();
        #endregion

        #region 辅助方法
        /// <summary>格式化关键字</summary>
        /// <param name="name">名称</param>
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
        /// <param name="field">字段</param>
        /// <param name="value">数值</param>
        /// <returns></returns>
        String FormatValue(FieldItem field, Object value);
        #endregion

        #region 一些设置
        /// <summary>是否自增获取自增返回值。默认启用</summary>
        Boolean AutoIdentity { get; set; }

        /// <summary>是否允许向自增列插入数据。为免冲突，仅本线程有效</summary>
        Boolean AllowInsertIdentity { get; set; }

        /// <summary>自动设置Guid的字段。对实体类有效，可在实体类类型构造函数里面设置</summary>
        FieldItem AutoSetGuidField { get; set; }

        /// <summary>默认累加字段</summary>
        ICollection<String> AdditionalFields { get; }

        /// <summary>主时间字段。代表当前数据行更新时间</summary>
        FieldItem MasterTime { get; set; }

        /// <summary>默认选择的字段</summary>
        String Selects { get; set; }

        /// <summary>实体模块集合</summary>
        EntityModules Modules { get; }
        #endregion
    }
}