using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
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
            #region 主要属性
            /// <summary>实体类型</summary>
            public virtual Type EntityType { get { return typeof(TEntity); } }

            /// <summary>实体会话</summary>
            public virtual IEntitySession Session { get { return Meta.Session; } }
            #endregion

            #region 属性
            private IEntity _Default;
            /// <summary>默认实体</summary>
            public virtual IEntity Default { get { return _Default ?? (_Default = new TEntity()); } set { _Default = value; } }

            /// <summary>数据表元数据</summary>
            public virtual TableItem Table { get { return Meta.Table; } }

            /// <summary>所有数据属性</summary>
            public virtual FieldItem[] AllFields { get { return Meta.AllFields; } }

            /// <summary>所有绑定到数据表的属性</summary>
            public virtual FieldItem[] Fields { get { return Meta.Fields; } }

            /// <summary>字段名集合，不区分大小写的哈希表存储，外部不要修改元素数据</summary>
            public virtual ICollection<String> FieldNames { get { return Meta.FieldNames; } }

            /// <summary>唯一键，返回第一个标识列或者唯一的主键</summary>
            public virtual FieldItem Unique { get { return Meta.Unique; } }

            /// <summary>主字段。主字段作为业务主要字段，代表当前数据行意义</summary>
            public virtual FieldItem Master { get { return Meta.Master; } }

            /// <summary>连接名</summary>
            public virtual String ConnName { get { return Meta.ConnName; } set { Meta.ConnName = value; } }

            /// <summary>表名</summary>
            public virtual String TableName { get { return Meta.TableName; } set { Meta.TableName = value; } }

            /// <summary>已格式化的表名，带有中括号等</summary>
            public virtual String FormatedTableName { get { return Session.FormatedTableName; } }

            /// <summary>实体缓存</summary>
            public virtual IEntityCache Cache { get { return Session.Cache; } }

            /// <summary>单对象实体缓存</summary>
            public virtual ISingleEntityCache SingleCache { get { return Session.SingleCache; } }

            /// <summary>总记录数</summary>
            public virtual Int32 Count { get { return Session.Count; } }
            #endregion

            #region 构造
            /// <summary>构造实体工厂</summary>
            public EntityOperate()
            {
                //MasterTime = GetMasterTime();
            }
            #endregion

            #region 创建实体、填充数据
            /// <summary>创建一个实体对象</summary>
            /// <param name="forEdit">是否为了编辑而创建，如果是，可以再次做一些相关的初始化工作</param>
            /// <returns></returns>
            public virtual IEntity Create(Boolean forEdit = false) => (Default as TEntity).CreateInstance(forEdit) as TEntity;

            /// <summary>加载记录集</summary>
            /// <param name="ds">记录集</param>
            /// <returns>实体数组</returns>
            public virtual IList<IEntity> LoadData(DataSet ds) => Entity<TEntity>.LoadData(ds).Cast<IEntity>().ToList();
            #endregion

            #region 查找单个实体
            /// <summary>根据属性以及对应的值，查找单个实体</summary>
            /// <param name="name">名称</param>
            /// <param name="value">数值</param>
            /// <returns></returns>
            public virtual IEntity Find(String name, Object value) { return Entity<TEntity>.Find(name, value); }

            /// <summary>根据条件查找单个实体</summary>
            /// <param name="where"></param>
            /// <returns></returns>
            public virtual IEntity Find(Expression where) { return Entity<TEntity>.Find(where); }

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
            public virtual IList<IEntity> FindAll() { return Entity<TEntity>.FindAll().Cast<IEntity>().ToList(); }

            /// <summary>查询并返回实体对象集合。
            /// 表名以及所有字段名，请使用类名以及字段对应的属性名，方法内转换为表名和列名
            /// </summary>
            /// <param name="where">条件，不带Where</param>
            /// <param name="order">排序，不带Order By</param>
            /// <param name="selects">查询列</param>
            /// <param name="startRowIndex">开始行，0表示第一行</param>
            /// <param name="maximumRows">最大返回行数，0表示所有行</param>
            /// <returns>实体数组</returns>
            public virtual IList<IEntity> FindAll(String where, String order, String selects, Int64 startRowIndex, Int64 maximumRows)
            {
                return Entity<TEntity>.FindAll(where, order, selects, startRowIndex, maximumRows).Cast<IEntity>().ToList();
            }

            /// <summary>查询并返回实体对象集合。
            /// 表名以及所有字段名，请使用类名以及字段对应的属性名，方法内转换为表名和列名
            /// </summary>
            /// <param name="where">条件，不带Where</param>
            /// <param name="order">排序，不带Order By</param>
            /// <param name="selects">查询列</param>
            /// <param name="startRowIndex">开始行，0表示第一行</param>
            /// <param name="maximumRows">最大返回行数，0表示所有行</param>
            /// <returns>实体数组</returns>
            public virtual IList<IEntity> FindAll(Expression where, String order, String selects, Int64 startRowIndex, Int64 maximumRows)
            {
                return Entity<TEntity>.FindAll(where, order, selects, startRowIndex, maximumRows).Cast<IEntity>().ToList();
            }
            #endregion

            #region 缓存查询
            /// <summary>查找所有缓存</summary>
            /// <returns></returns>
            public virtual IList<IEntity> FindAllWithCache() { return Entity<TEntity>.FindAllWithCache().Cast<IEntity>().ToList(); }
            #endregion

            #region 取总记录数
            /// <summary>返回总记录数</summary>
            /// <returns></returns>
            public virtual Int64 FindCount() { return Entity<TEntity>.FindCount(); }

            /// <summary>返回总记录数</summary>
            /// <param name="where">条件，不带Where</param>
            /// <param name="order">排序，不带Order By</param>
            /// <param name="selects">查询列</param>
            /// <param name="startRowIndex">开始行，0表示第一行</param>
            /// <param name="maximumRows">最大返回行数，0表示所有行</param>
            /// <returns>总行数</returns>
            public virtual Int32 FindCount(String where, String order, String selects, Int64 startRowIndex, Int64 maximumRows)
            {
                return Entity<TEntity>.FindCount(where, order, selects, startRowIndex, maximumRows);
            }

            /// <summary>返回总记录数</summary>
            /// <param name="where">条件，不带Where</param>
            /// <returns>总行数</returns>
            public virtual Int64 FindCount(Expression where) => Entity<TEntity>.FindCount(where);
            #endregion

            #region 高并发
            /// <summary>获取 或 新增 对象，常用于统计等高并发更新的情况，一般配合SaveAsync</summary>
            /// <typeparam name="TKey"></typeparam>
            /// <param name="key">业务主键</param>
            /// <param name="find">查找函数</param>
            /// <param name="create">创建对象</param>
            /// <returns></returns>
            public virtual IEntity GetOrAdd<TKey>(TKey key, Func<TKey, Boolean, IEntity> find = null, Func<TKey, IEntity> create = null)
            {
                if (find != null)
                {
                    if (create != null)
                        return Entity<TEntity>.GetOrAdd(key, (k, b) => find(k, b) as TEntity, k => create(k) as TEntity);
                    else
                        return Entity<TEntity>.GetOrAdd(key, (k, b) => find(k, b) as TEntity, null);
                }
                else
                {
                    if (create != null)
                        return Entity<TEntity>.GetOrAdd(key, null, k => create(k) as TEntity);
                    else
                        return Entity<TEntity>.GetOrAdd(key, null, null);
                }
                //return Entity<TEntity>.GetOrAdd(key,
                //    (k, b) => find?.Invoke(k, b) as TEntity,
                //    k => create?.Invoke(k) as TEntity);
            }
            #endregion

            #region 事务
            /// <summary>开始事务</summary>
            /// <returns></returns>
            public virtual Int32 BeginTransaction() { return Session.BeginTrans(); }

            /// <summary>提交事务</summary>
            /// <returns></returns>
            public virtual Int32 Commit() { return Session.Commit(); }

            /// <summary>回滚事务</summary>
            /// <returns></returns>
            public virtual Int32 Rollback() { return Session.Rollback(); }

            /// <summary>创建事务</summary>
            public virtual EntityTransaction CreateTrans() { return new EntityTransaction<TEntity>(); }
            #endregion

            #region 辅助方法
            /// <summary>格式化关键字</summary>
            /// <param name="name">名称</param>
            /// <returns></returns>
            public virtual String FormatName(String name) => Meta.FormatName(name);

            /// <summary>
            /// 取得一个值的Sql值。
            /// 当这个值是字符串类型时，会在该值前后加单引号；
            /// </summary>
            /// <param name="name">字段</param>
            /// <param name="value">对象</param>
            /// <returns>Sql值的字符串形式</returns>
            public virtual String FormatValue(String name, Object value) => Meta.FormatValue(name, value);

            /// <summary>格式化数据为SQL数据</summary>
            /// <param name="field">字段</param>
            /// <param name="value">数值</param>
            /// <returns></returns>
            public virtual String FormatValue(FieldItem field, Object value) => Meta.FormatValue(field, value);
            #endregion

            #region 一些设置
            /// <summary>是否自增获取自增返回值。默认启用</summary>
            public Boolean AutoIdentity { get; set; } = true;

            [ThreadStatic]
            private static Boolean _AllowInsertIdentity;
            /// <summary>是否允许向自增列插入数据。为免冲突，仅本线程有效</summary>
            public virtual Boolean AllowInsertIdentity { get { return _AllowInsertIdentity; } set { _AllowInsertIdentity = value; } }

            /// <summary>自动设置Guid的字段。对实体类有效，可在实体类类型构造函数里面设置</summary>
            public virtual FieldItem AutoSetGuidField { get; set; }

            /// <summary>默认累加字段</summary>
            public virtual ICollection<String> AdditionalFields { get; } = new HashSet<String>(StringComparer.OrdinalIgnoreCase);

            private Boolean _MasterTime_;
            private FieldItem _MasterTime;
            /// <summary>主时间字段。代表当前数据行更新时间</summary>
            public FieldItem MasterTime
            {
                get
                {
                    if (_MasterTime == null && !_MasterTime_)
                    {
                        _MasterTime = GetMasterTime();
                        _MasterTime_ = true;
                    }

                    return _MasterTime;
                }
                set { _MasterTime = value; _MasterTime_ = false; }
            }

            private FieldItem GetMasterTime()
            {
                var fis = Meta.Fields.Where(e => e.Type == typeof(DateTime)).ToArray();
                if (fis.Length == 0) return null;

                var dt = Meta.Table.DataTable;

                // 时间作为主键
                var fi = fis.FirstOrDefault(e => e.PrimaryKey);
                if (fi != null) return fi;

                // 第一个时间日期索引字段
                foreach (var di in dt.Indexes.OrderBy(e => e.Columns.Length).OrderByDescending(e => e.Unique).ThenByDescending(e => e.PrimaryKey))
                {
                    if (di.Columns == null || di.Columns.Length == 0) continue;

                    fi = fis.FirstOrDefault(e => di.Columns[0].EqualIgnoreCase(e.Name, e.ColumnName));
                    if (fi != null) return fi;
                }

                fi = fis.FirstOrDefault(e => e.Name.StartsWithIgnoreCase("UpdateTime", "Modify", "Modified"));

                return fi;
            }

            /// <summary>默认选择的字段</summary>
            public String Selects { get; set; }
            #endregion
        }
    }
}