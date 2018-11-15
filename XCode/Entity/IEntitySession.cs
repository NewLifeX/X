using System;
using System.Collections.Generic;
using System.Data;
using NewLife.Data;
using XCode.Cache;
using XCode.DataAccessLayer;

namespace XCode
{
    /// <summary>实体会话接口</summary>
    public interface IEntitySession
    {
        #region 属性
        /// <summary>连接名</summary>
        String ConnName { get; }

        /// <summary>表名</summary>
        String TableName { get; }

        /// <summary>用于标识会话的键值</summary>
        String Key { get; }

        /// <summary>已格式化的表名，带有中括号等</summary>
        String FormatedTableName { get; }

        /// <summary>数据操作层</summary>
        DAL Dal { get; }

        /// <summary>用户数据</summary>
        IDictionary<String, Object> Items { get; set; }
        #endregion

        #region 数据初始化
        /// <summary>检查并初始化数据。参数等待时间为0表示不等待</summary>
        /// <param name="ms">等待时间，-1表示不限，0表示不等待</param>
        /// <returns>如果等待，返回是否收到信号</returns>
        Boolean WaitForInitData(Int32 ms = 1000);
        #endregion

        #region 缓存
        /// <summary>实体缓存</summary>
        /// <returns></returns>
        IEntityCache Cache { get; }

        /// <summary>单对象实体缓存。
        /// 建议自定义查询数据方法，并从二级缓存中获取实体数据，以抵消因初次填充而带来的消耗。
        /// </summary>
        ISingleEntityCache SingleCache { get; }

        /// <summary>总记录数，小于1000时是精确的，大于1000时缓存10分钟</summary>
        Int32 Count { get; }

        /// <summary>总记录数，小于1000时是精确的，大于1000时缓存10分钟</summary>
        /// <remarks>
        /// 1，检查静态字段，如果有数据且小于1000，直接返回，否则=>3
        /// 2，如果有数据但大于1000，则返回缓存里面的有效数据
        /// 3，来到这里，有可能是第一次访问，静态字段没有缓存，也有可能是大于1000的缓存过期
        /// 4，检查模型
        /// 5，根据需要查询数据
        /// 6，如果大于1000，缓存数据
        /// 7，检查数据初始化
        /// </remarks>
        Int64 LongCount { get; }

        /// <summary>清除缓存</summary>
        /// <param name="reason">原因</param>
        void ClearCache(String reason);
        #endregion

        #region 数据库操作
        /// <summary>初始化数据</summary>
        void InitData();

        /// <summary>执行SQL查询，返回记录集</summary>
        /// <param name="builder">SQL语句</param>
        /// <param name="startRowIndex">开始行，0表示第一行</param>
        /// <param name="maximumRows">最大返回行数，0表示所有行</param>
        /// <returns></returns>
        DbTable Query(SelectBuilder builder, Int64 startRowIndex, Int64 maximumRows);

        /// <summary>查询记录数</summary>
        /// <param name="builder">查询生成器</param>
        /// <returns>记录数</returns>
        Int32 QueryCount(SelectBuilder builder);

        /// <summary>执行</summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="type">命令类型，默认SQL文本</param>
        /// <param name="ps">命令参数</param>
        /// <returns>影响的结果</returns>
        Int32 Execute(String sql, CommandType type = CommandType.Text, params IDataParameter[] ps);

        /// <summary>执行插入语句并返回新增行的自动编号</summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="type">命令类型，默认SQL文本</param>
        /// <param name="ps">命令参数</param>
        /// <returns>新增行的自动编号</returns>
        Int64 InsertAndGetIdentity(String sql, CommandType type = CommandType.Text, params IDataParameter[] ps);

        /// <summary>执行Truncate语句</summary>
        /// <returns>影响的结果</returns>
        Int32 Truncate();

        /// <summary>数据改变后触发。参数指定触发该事件的实体类</summary>
        event Action<Type> OnDataChange;
        #endregion

        #region 事务保护
        /// <summary>开始事务</summary>
        /// <returns>剩下的事务计数</returns>
        Int32 BeginTrans();

        /// <summary>提交事务</summary>
        /// <returns>剩下的事务计数</returns>
        Int32 Commit();

        /// <summary>回滚事务，忽略异常</summary>
        /// <returns>剩下的事务计数</returns>
        Int32 Rollback();
        #endregion

        #region 参数化
        ///// <summary>创建参数</summary>
        ///// <returns></returns>
        //IDataParameter CreateParameter();

        /// <summary>格式化参数名</summary>
        /// <param name="name">名称</param>
        /// <returns></returns>
        String FormatParameterName(String name);
        #endregion

        #region 实体操作
        /// <summary>把该对象持久化到数据库，添加/更新实体缓存。</summary>
        /// <param name="entity">实体对象</param>
        /// <returns></returns>
        Int32 Insert(IEntity entity);

        /// <summary>更新数据库，同时更新实体缓存</summary>
        /// <param name="entity">实体对象</param>
        /// <returns></returns>
        Int32 Update(IEntity entity);

        /// <summary>从数据库中删除该对象，同时从实体缓存中删除</summary>
        /// <param name="entity">实体对象</param>
        /// <returns></returns>
        Int32 Delete(IEntity entity);
        #endregion
    }
}