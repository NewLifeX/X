using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using NewLife;
using XCode.Cache;
using XCode.Configuration;
using XCode.Shards;

namespace XCode
{
    public partial class Entity<TEntity>
    {
        /// <summary>实体元数据</summary>
        public static class Meta
        {
            static Meta()
            {
                // 避免实际应用中，直接调用Entity.Meta的静态方法时，没有引发TEntity的静态构造函数。
                var entity = new TEntity();
            }

            #region 主要属性
            /// <summary>实体类型</summary>
            public static Type ThisType => typeof(TEntity);

            //private static IEntityFactory _Factory;
            /// <summary>实体操作者</summary>
            public static IEntityFactory Factory
            {
                get
                {
                    // 不能缓存Factory，因为后期可能会改变注册，比如Menu
                    //if (_Factory != null) return _Factory;

                    var type = ThisType;
                    if (type.IsInterface) return null;

                    return /*_Factory = */type.AsFactory();
                }
            }
            #endregion

            #region 基本属性
            private static readonly Lazy<TableItem> _Table = new(() => TableItem.Create(ThisType));
            /// <summary>表信息</summary>
            public static TableItem Table => _Table.Value;

#if NET40 || NET45
            private static readonly ThreadLocal<String> _ConnName = new();
#else
            private static readonly AsyncLocal<String> _ConnName = new();
#endif
            /// <summary>链接名。线程内允许修改，修改者负责还原。若要还原默认值，设为null即可</summary>
            public static String ConnName
            {
                get => _ConnName.Value ??= Table.ConnName;
                set { _Session.Value = null; _ConnName.Value = value; }
            }

#if NET40 || NET45
            private static readonly ThreadLocal<String> _TableName = new();
#else
            private static readonly AsyncLocal<String> _TableName = new();
#endif
            /// <summary>表名。线程内允许修改，修改者负责还原</summary>
            public static String TableName
            {
                get => _TableName.Value ??= Table.TableName;
                set { _Session.Value = null; _TableName.Value = value; }
            }

            /// <summary>所有数据属性</summary>
            public static FieldItem[] AllFields => Table.AllFields;

            /// <summary>所有绑定到数据表的属性</summary>
            public static FieldItem[] Fields => Table.Fields;

            /// <summary>字段名集合，不区分大小写的哈希表存储，外部不要修改元素数据</summary>
            public static ICollection<String> FieldNames => Table.FieldNames;

            /// <summary>唯一键，返回第一个标识列或者唯一的主键</summary>
            public static FieldItem Unique
            {
                get
                {
                    var dt = Table;
                    if (dt.PrimaryKeys.Length == 1) return dt.PrimaryKeys[0];
                    if (dt.Identity != null) return dt.Identity;
                    return null;
                }
            }

            /// <summary>主字段。主字段作为业务主要字段，代表当前数据行意义</summary>
            public static FieldItem Master => Table.Master ?? Unique;
            #endregion

            #region 会话
#if NET40 || NET45
            private static readonly ThreadLocal<EntitySession<TEntity>> _Session = new();
#else
            private static readonly AsyncLocal<EntitySession<TEntity>> _Session = new();
#endif
            /// <summary>实体会话。线程静态</summary>
            public static EntitySession<TEntity> Session => _Session.Value ??= EntitySession<TEntity>.Create(ConnName, TableName);
            #endregion

            #region 事务保护
            /// <summary>开始事务</summary>
            /// <returns>剩下的事务计数</returns>
            //[Obsolete("=>Session")]
            [EditorBrowsable(EditorBrowsableState.Never)]
            public static Int32 BeginTrans() => Session.BeginTrans();

            /// <summary>提交事务</summary>
            /// <returns>剩下的事务计数</returns>
            //[Obsolete("=>Session")]
            [EditorBrowsable(EditorBrowsableState.Never)]
            public static Int32 Commit() => Session.Commit();

            /// <summary>回滚事务，忽略异常</summary>
            /// <returns>剩下的事务计数</returns>
            //[Obsolete("=>Session")]
            [EditorBrowsable(EditorBrowsableState.Never)]
            public static Int32 Rollback() => Session.Rollback();

            /// <summary>创建事务</summary>
            public static EntityTransaction CreateTrans() => new EntityTransaction<TEntity>();
            #endregion

            #region 辅助方法
            ///// <summary>格式化关键字</summary>
            ///// <param name="name">名称</param>
            ///// <returns></returns>
            //public static String FormatName(String name) => Session.Dal.Db.FormatName(name);

            ///// <summary>格式化时间</summary>
            ///// <param name="dateTime"></param>
            ///// <returns></returns>
            //public static String FormatDateTime(DateTime dateTime) => Session.Dal.Db.FormatDateTime(dateTime);

            ///// <summary>格式化数据为SQL数据</summary>
            ///// <param name="name">名称</param>
            ///// <param name="value">数值</param>
            ///// <returns></returns>
            //public static String FormatValue(String name, Object value) => FormatValue(Table.FindByName(name), value);

            ///// <summary>格式化数据为SQL数据</summary>
            ///// <param name="field">字段</param>
            ///// <param name="value">数值</param>
            ///// <returns></returns>
            //public static String FormatValue(FieldItem field, Object value) => Session.Dal.Db.FormatValue(field?.Field, value);
            #endregion

            #region 缓存
            /// <summary>实体缓存</summary>
            /// <returns></returns>
            //[Obsolete("=>Session")]
            //[EditorBrowsable(EditorBrowsableState.Never)]
            public static EntityCache<TEntity> Cache => Session.Cache;

            /// <summary>单对象实体缓存。</summary>
            //[Obsolete("=>Session")]
            //[EditorBrowsable(EditorBrowsableState.Never)]
            public static ISingleEntityCache<Object, TEntity> SingleCache => Session.SingleCache;

            /// <summary>总记录数，小于1000时是精确的，大于1000时缓存10分钟</summary>
            //[Obsolete("=>Session")]
            //[EditorBrowsable(EditorBrowsableState.Never)]
            public static Int32 Count => (Int32)Session.LongCount;
            #endregion

            #region 分表分库
            /// <summary>自动分库回调，用于添删改操作</summary>
            [Obsolete("=>AutoShard")]
            public static Func<TEntity, String> ShardConnName { get; set; }

            /// <summary>自动分表回调，用于添删改操作</summary>
            [Obsolete("=>AutoShard")]
            public static Func<TEntity, String> ShardTableName { get; set; }

            /// <summary>分表分库策略</summary>
            public static IShardPolicy ShardPolicy { get; set; }

            /// <summary>在分库上执行操作，自动还原</summary>
            /// <param name="connName"></param>
            /// <param name="tableName"></param>
            /// <param name="func"></param>
            /// <returns></returns>
            [Obsolete("=>CreateSplit")]
            public static T ProcessWithSplit<T>(String connName, String tableName, Func<T> func)
            {
                using var split = CreateSplit(connName, tableName);
                return func();
            }

            /// <summary>创建分库会话，using结束时自动还原</summary>
            /// <param name="connName">连接名</param>
            /// <param name="tableName">表名</param>
            /// <returns></returns>
            public static IDisposable CreateSplit(String connName, String tableName) => new SplitPackge(connName, tableName);

            /// <summary>针对实体对象自动分库分表</summary>
            /// <param name="entity"></param>
            /// <returns></returns>
            public static IDisposable CreateShard(TEntity entity)
            {
                // 使用自动分表分库策略
                var model = ShardPolicy?.Shard(entity);
                if (model != null) return new SplitPackge(model.ConnName, model.TableName);

#pragma warning disable CS0618 // 类型或成员已过时
                var connName = ShardConnName?.Invoke(entity);
                var tableName = ShardTableName?.Invoke(entity);
                if (connName.IsNullOrEmpty() && tableName.IsNullOrEmpty()) return null;
#pragma warning restore CS0618 // 类型或成员已过时

                return new SplitPackge(connName, tableName);
            }

            /// <summary>为实体对象、时间、雪花Id等计算分表分库</summary>
            /// <param name="value"></param>
            /// <returns></returns>
            public static IDisposable CreateShard(Object value)
            {
                // 使用自动分表分库策略
                var model = ShardPolicy?.Shard(value);
                return new SplitPackge(model.ConnName, model.TableName);
            }

            /// <summary>针对时间区间自动分库分表，常用于多表顺序查询，支持倒序</summary>
            /// <param name="start"></param>
            /// <param name="end"></param>
            /// <param name="callback"></param>
            /// <returns></returns>
            public static IEnumerable<T> AutoShard<T>(DateTime start, DateTime end, Func<T> callback)
            {
                // 使用自动分表分库策略
                var models = ShardPolicy?.Shards(start, end);
                foreach (var model in models)
                {
                    using var shard = new SplitPackge(model.ConnName, model.TableName);
                    yield return callback();
                }
            }

            private class SplitPackge : IDisposable
            {
                /// <summary>连接名</summary>
                public String ConnName { get; set; }

                /// <summary>表名</summary>
                public String TableName { get; set; }

                public SplitPackge(String connName, String tableName)
                {
                    ConnName = Meta.ConnName;
                    TableName = Meta.TableName;

                    Meta.ConnName = connName;
                    Meta.TableName = tableName;
                }

                public void Dispose()
                {
                    Meta.ConnName = ConnName;
                    Meta.TableName = TableName;
                }
            }
            #endregion

            #region 模块
            internal static EntityModules _Modules = new(typeof(TEntity));
            /// <summary>实体模块集合</summary>
            public static EntityModules Modules => _Modules;
            #endregion
        }
    }
}