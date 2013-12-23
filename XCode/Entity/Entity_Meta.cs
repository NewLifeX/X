using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using NewLife;
using XCode.Cache;
using XCode.Configuration;
using XCode.DataAccessLayer;

namespace XCode
{
    partial class Entity<TEntity>
    {
        /// <summary>实体元数据</summary>
        public static class Meta
        {
            #region 基本属性
            /// <summary>实体类型</summary>
            internal static Type ThisType { get { return typeof(TEntity); } }

            /// <summary>表信息</summary>
            public static TableItem Table { get { return TableItem.Create(ThisType); } }

            [ThreadStatic]
            private static String _ConnName;
            /// <summary>链接名。线程内允许修改，修改者负责还原。若要还原默认值，设为null即可</summary>
            public static String ConnName
            {
                get { return _ConnName ?? (_ConnName = Table.ConnName); }
                set
                {
                    ////修改链接名，挂载当前表
                    //if (!String.IsNullOrEmpty(value) && !_ConnName.EqualIgnoreCase(value))
                    //{
                    //    //try
                    //    //{
                    //    //    CheckTable(value, TableName);
                    //    //}
                    //    //catch { }

                    //    // 清空记录数缓存
                    //    ClearCountCache();
                    //}
                    _ConnName = value;

                    if (String.IsNullOrEmpty(_ConnName)) _ConnName = Table.ConnName;
                }
            }

            [ThreadStatic]
            private static String _TableName;
            /// <summary>表名。线程内允许修改，修改者负责还原</summary>
            public static String TableName
            {
                get { return _TableName ?? (_TableName = Table.TableName); }
                set
                {
                    ////修改表名
                    //if (!String.IsNullOrEmpty(value) && !_TableName.EqualIgnoreCase(value))
                    //{
                    //    //try
                    //    //{
                    //    //    CheckTable(ConnName, value);
                    //    //}
                    //    //catch { }

                    //    // 清空记录数缓存
                    //    ClearCountCache();
                    //}
                    _TableName = value;

                    if (String.IsNullOrEmpty(_TableName)) _TableName = Table.TableName;
                }
            }

            /// <summary>所有数据属性</summary>
            public static FieldItem[] AllFields { get { return Table.AllFields; } }

            /// <summary>所有绑定到数据表的属性</summary>
            public static FieldItem[] Fields { get { return Table.Fields; } }

            /// <summary>字段名列表</summary>
            public static IList<String> FieldNames { get { return Table.FieldNames; } }

            /// <summary>唯一键，返回第一个标识列或者唯一的主键</summary>
            public static FieldItem Unique
            {
                get
                {
                    if (Table.Identity != null) return Table.Identity;
                    if (Table.PrimaryKeys != null && Table.PrimaryKeys.Length > 0) return Table.PrimaryKeys[0];
                    return null;
                }
            }

            /// <summary>实体操作者</summary>
            public static IEntityOperate Factory
            {
                get
                {
                    Type type = ThisType;
                    if (type.IsInterface) return null;

                    return EntityFactory.CreateOperate(type);
                }
            }
            #endregion

            #region 实体会话
            /// <summary>实体会话</summary>
            static EntitySession<TEntity> Session { get { return EntitySession<TEntity>.Create(ConnName, TableName); } }
            #endregion

            #region 数据库操作
            /// <summary>数据操作对象。</summary>
            public static DAL DBO { get { return DAL.Create(ConnName); } }

            /// <summary>执行SQL查询，返回记录集</summary>
            /// <param name="builder">SQL语句</param>
            /// <param name="startRowIndex">开始行，0表示第一行</param>
            /// <param name="maximumRows">最大返回行数，0表示所有行</param>
            /// <returns></returns>
            public static DataSet Query(SelectBuilder builder, Int32 startRowIndex, Int32 maximumRows)
            {
                WaitForInitData();

                return DBO.Select(builder, startRowIndex, maximumRows, Meta.TableName);
            }

            /// <summary>查询</summary>
            /// <param name="sql">SQL语句</param>
            /// <returns>结果记录集</returns>
            //[Obsolete("请优先考虑使用SelectBuilder参数做查询！")]
            public static DataSet Query(String sql)
            {
                WaitForInitData();

                return DBO.Select(sql, Meta.TableName);
            }

            /// <summary>查询记录数</summary>
            /// <param name="sql">SQL语句</param>
            /// <returns>记录数</returns>
            [EditorBrowsable(EditorBrowsableState.Never)]
            [Obsolete("请优先考虑使用SelectBuilder参数做查询！")]
            public static Int32 QueryCount(String sql)
            {
                WaitForInitData();

                return DBO.SelectCount(sql, Meta.TableName);
            }

            /// <summary>查询记录数</summary>
            /// <param name="sb">查询生成器</param>
            /// <returns>记录数</returns>
            public static Int32 QueryCount(SelectBuilder sb)
            {
                WaitForInitData();

                return DBO.SelectCount(sb, new String[] { Meta.TableName });
            }

            /// <summary>执行</summary>
            /// <param name="sql">SQL语句</param>
            /// <returns>影响的结果</returns>
            public static Int32 Execute(String sql)
            {
                WaitForInitData();

                Int32 rs = DBO.Execute(sql, Meta.TableName);
                executeCount++;
                DataChange("修改数据");
                return rs;
            }

            /// <summary>执行插入语句并返回新增行的自动编号</summary>
            /// <param name="sql">SQL语句</param>
            /// <returns>新增行的自动编号</returns>
            public static Int64 InsertAndGetIdentity(String sql)
            {
                WaitForInitData();

                Int64 rs = DBO.InsertAndGetIdentity(sql, Meta.TableName);
                executeCount++;
                DataChange("修改数据");
                return rs;
            }

            /// <summary>执行</summary>
            /// <param name="sql">SQL语句</param>
            /// <param name="type">命令类型，默认SQL文本</param>
            /// <param name="ps">命令参数</param>
            /// <returns>影响的结果</returns>
            public static Int32 Execute(String sql, CommandType type = CommandType.Text, params DbParameter[] ps)
            {
                WaitForInitData();

                Int32 rs = DBO.Execute(sql, type, ps, Meta.TableName);
                executeCount++;
                DataChange("修改数据");
                return rs;
            }

            /// <summary>执行插入语句并返回新增行的自动编号</summary>
            /// <param name="sql">SQL语句</param>
            /// <param name="type">命令类型，默认SQL文本</param>
            /// <param name="ps">命令参数</param>
            /// <returns>新增行的自动编号</returns>
            public static Int64 InsertAndGetIdentity(String sql, CommandType type = CommandType.Text, params DbParameter[] ps)
            {
                WaitForInitData();

                Int64 rs = DBO.InsertAndGetIdentity(sql, type, ps, Meta.TableName);
                executeCount++;
                DataChange("修改数据");
                return rs;
            }

            static void DataChange(String reason = null)
            {
                // 还在事务保护里面，不更新缓存，最后提交或者回滚的时候再更新
                // 一般事务保护用于批量更新数据，频繁删除缓存将会打来巨大的性能损耗
                // 2012-07-17 当前实体类开启的事务保护，必须由当前类结束，否则可能导致缓存数据的错乱
                if (TransCount > 0) return;

                //Cache.Clear(reason);
                ////_Count = null;
                //ClearCountCache();
                Session.ClearCache();

                if (_OnDataChange != null) _OnDataChange(ThisType);
            }

            //private static WeakReference<Action<Type>> _OnDataChange = new WeakReference<Action<Type>>();
            private static Action<Type> _OnDataChange;
            /// <summary>数据改变后触发。参数指定触发该事件的实体类</summary>
            public static event Action<Type> OnDataChange
            {
                add
                {
                    if (value != null)
                    {
                        // 这里不能对委托进行弱引用，因为GC会回收委托，应该改为对对象进行弱引用
                        //WeakReference<Action<Type>> w = value;

                        _OnDataChange += new WeakAction<Type>(value, handler => { _OnDataChange -= handler; }, true);
                    }
                }
                remove { }
            }

            /// <summary>检查并初始化数据。参数等待时间为0表示不等待</summary>
            /// <param name="ms">等待时间，-1表示不限，0表示不等待</param>
            /// <returns>如果等待，返回是否收到信号</returns>
            public static Boolean WaitForInitData(Int32 ms = 1000) { return Session.WaitForInitData(ms); }
            #endregion

            #region 事务保护
            [ThreadStatic]
            private static Int32 TransCount = 0;
            [ThreadStatic]
            private static Int32 executeCount = 0;

            /// <summary>开始事务</summary>
            /// <returns>剩下的事务计数</returns>
            public static Int32 BeginTrans()
            {
                // 可能存在多层事务，这里不能把这个清零
                //executeCount = 0;
                return TransCount = DBO.BeginTransaction();
            }

            /// <summary>提交事务</summary>
            /// <returns>剩下的事务计数</returns>
            public static Int32 Commit()
            {
                TransCount = DBO.Commit();
                // 提交事务时更新数据，虽然不是绝对准确，但没有更好的办法
                // 即使提交了事务，但只要事务内没有执行更新数据的操作，也不更新
                // 2012-06-13 测试证明，修改数据后，提交事务后会更新缓存等数据
                if (TransCount <= 0 && executeCount > 0)
                {
                    DataChange("修改数据后提交事务");
                    // 回滚到顶层才更新数据
                    executeCount = 0;
                }
                return TransCount;
            }

            /// <summary>回滚事务，忽略异常</summary>
            /// <returns>剩下的事务计数</returns>
            public static Int32 Rollback()
            {
                TransCount = DBO.Rollback();
                // 回滚的时候貌似不需要更新缓存
                //if (TransCount <= 0 && executeCount > 0) DataChange();
                if (TransCount <= 0 && executeCount > 0)
                {
                    // 因为在事务保护中添加或删除实体时直接操作了实体缓存，所以需要更新
                    DataChange("修改数据后回滚事务");
                    executeCount = 0;
                }
                return TransCount;
            }

            /// <summary>是否在事务保护中</summary>
            internal static Boolean UsingTrans { get { return TransCount > 0; } }
            #endregion

            #region 参数化
            /// <summary>创建参数</summary>
            /// <returns></returns>
            public static DbParameter CreateParameter() { return DBO.Db.Factory.CreateParameter(); }

            /// <summary>格式化参数名</summary>
            /// <param name="name">名称</param>
            /// <returns></returns>
            public static String FormatParameterName(String name) { return DBO.Db.FormatParameterName(name); }
            #endregion

            #region 辅助方法
            /// <summary>格式化关键字</summary>
            /// <param name="name">名称</param>
            /// <returns></returns>
            public static String FormatName(String name) { return DBO.Db.FormatName(name); }

            /// <summary>格式化时间</summary>
            /// <param name="dateTime"></param>
            /// <returns></returns>
            public static String FormatDateTime(DateTime dateTime) { return DBO.Db.FormatDateTime(dateTime); }

            /// <summary>格式化数据为SQL数据</summary>
            /// <param name="name">名称</param>
            /// <param name="value">数值</param>
            /// <returns></returns>
            public static String FormatValue(String name, Object value) { return FormatValue(Table.FindByName(name), value); }

            /// <summary>格式化数据为SQL数据</summary>
            /// <param name="field">字段</param>
            /// <param name="value">数值</param>
            /// <returns></returns>
            public static String FormatValue(FieldItem field, Object value) { return DBO.Db.FormatValue(field != null ? field.Field : null, value); }
            #endregion

            #region 缓存
            /// <summary>实体缓存</summary>
            /// <returns></returns>
            public static EntityCache<TEntity> Cache { get { return Session.Cache; } }

            /// <summary>单对象实体缓存。
            /// 建议自定义查询数据方法，并从二级缓存中获取实体数据，以抵消因初次填充而带来的消耗。
            /// </summary>
            public static SingleEntityCache<Object, TEntity> SingleCache { get { return Session.SingleCache; } }

            /// <summary>总记录数，小于1000时是精确的，大于1000时缓存10分钟</summary>
            public static Int32 Count { get { return (Int32)LongCount; } }

            /// <summary>总记录数较小时，使用静态字段，较大时增加使用Cache</summary>
            private static Int64? _Count;
            /// <summary>总记录数，小于1000时是精确的，大于1000时缓存10分钟</summary>
            public static Int64 LongCount { get { return Session.LongCount; } }
            #endregion
        }
    }
}