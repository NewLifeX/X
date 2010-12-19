using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Text;
using System.Web.Services;
using System.Xml.Serialization;
using NewLife.Collections;
using NewLife.IO;
using NewLife.Reflection;
using XCode.Configuration;

namespace XCode
{
    /// <summary>
    /// 数据实体基类的基类
    /// </summary>
    public abstract class EntityBase : BinaryAccessor, IEntity, IEntityOperate
    {
        #region 创建实体
        /// <summary>
        /// 创建一个实体对象
        /// </summary>
        /// <returns></returns>
        IEntity IEntityOperate.Create() { return CreateInternal(); }

        internal abstract IEntity CreateInternal();
        #endregion

        #region 填充数据
        /// <summary>
        /// 加载记录集
        /// </summary>
        /// <param name="ds">记录集</param>
        /// <returns>实体数组</returns>
        EntityList<IEntity> IEntityOperate.LoadData(DataSet ds)
        {
            return ToList(LoadDataInternal(ds));
        }

        internal abstract ICollection LoadDataInternal(DataSet ds);

        /// <summary>
        /// 从一个数据行对象加载数据。不加载关联对象。
        /// </summary>
        /// <param name="dr">数据行</param>
        public abstract void LoadData(DataRow dr);
        #endregion

        #region 查找单个实体
        /// <summary>
        /// 根据属性以及对应的值，查找单个实体
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        EntityBase IEntityOperate.Find(String name, Object value) { return FindInternal(name, value); }

        /// <summary>
        /// 根据条件查找单个实体
        /// </summary>
        /// <param name="whereClause"></param>
        /// <returns></returns>
        EntityBase IEntityOperate.Find(String whereClause) { return FindInternal(whereClause); }

        /// <summary>
        /// 根据主键查找单个实体
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        EntityBase IEntityOperate.FindByKey(Object key) { return FindByKeyInternal(key); }

        /// <summary>
        /// 根据主键查询一个实体对象用于表单编辑
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        EntityBase IEntityOperate.FindByKeyForEdit(Object key) { return FindByKeyForEditInternal(key); }

        internal abstract EntityBase FindInternal(String name, Object value);
        internal abstract EntityBase FindInternal(String whereClause);
        internal abstract EntityBase FindByKeyInternal(Object key);
        internal abstract EntityBase FindByKeyForEditInternal(Object key);
        #endregion

        #region 操作
        /// <summary>
        /// 把该对象持久化到数据库
        /// </summary>
        /// <returns></returns>
        public abstract Int32 Insert();

        /// <summary>
        /// 更新数据库
        /// </summary>
        /// <returns></returns>
        public abstract Int32 Update();

        /// <summary>
        /// 从数据库中删除该对象
        /// </summary>
        /// <returns></returns>
        public abstract Int32 Delete();

        /// <summary>
        /// 保存。根据主键检查数据库中是否已存在该对象，再决定调用Insert或Update
        /// </summary>
        /// <returns></returns>
        public abstract Int32 Save();
        #endregion

        #region 静态查询
        /// <summary>
        /// 获取所有实体对象。获取大量数据时会非常慢，慎用
        /// </summary>
        /// <returns>实体数组</returns>
        EntityList<IEntity> IEntityOperate.FindAll()
        {
            return ToList(FindAllInternal());
        }

        /// <summary>
        /// 查询并返回实体对象集合。
        /// 表名以及所有字段名，请使用类名以及字段对应的属性名，方法内转换为表名和列名
        /// </summary>
        /// <param name="whereClause">条件，不带Where</param>
        /// <param name="orderClause">排序，不带Order By</param>
        /// <param name="selects">查询列</param>
        /// <param name="startRowIndex">开始行，0开始</param>
        /// <param name="maximumRows">最大返回行数</param>
        /// <returns>实体数组</returns>
        EntityList<IEntity> IEntityOperate.FindAll(String whereClause, String orderClause, String selects, Int32 startRowIndex, Int32 maximumRows)
        {
            return ToList(FindAllInternal(whereClause, orderClause, selects, startRowIndex, maximumRows));
        }

        /// <summary>
        /// 根据属性列表以及对应的值列表，获取所有实体对象
        /// </summary>
        /// <param name="names">属性列表</param>
        /// <param name="values">值列表</param>
        /// <returns>实体数组</returns>
        EntityList<IEntity> IEntityOperate.FindAll(String[] names, Object[] values)
        {
            return ToList(FindAllInternal(names, values));
        }

        /// <summary>
        /// 根据属性以及对应的值，获取所有实体对象
        /// </summary>
        /// <param name="name">属性</param>
        /// <param name="value">值</param>
        /// <returns>实体数组</returns>
        EntityList<IEntity> IEntityOperate.FindAll(String name, Object value)
        {
            return ToList(FindAllInternal(name, value));
        }

        /// <summary>
        /// 根据属性以及对应的值，获取所有实体对象
        /// </summary>
        /// <param name="name">属性</param>
        /// <param name="value">值</param>
        /// <param name="startRowIndex">起始行，0开始</param>
        /// <param name="maximumRows">最大返回行数</param>
        /// <returns>实体数组</returns>
        EntityList<IEntity> IEntityOperate.FindAll(String name, Object value, Int32 startRowIndex, Int32 maximumRows)
        {
            return ToList(FindAllInternal(name, value, startRowIndex, maximumRows));
        }

        /// <summary>
        /// 根据属性以及对应的值，获取所有实体对象
        /// </summary>
        /// <param name="name">属性</param>
        /// <param name="value">值</param>
        /// <param name="orderClause">排序，不带Order By</param>
        /// <param name="startRowIndex">起始行，0开始</param>
        /// <param name="maximumRows">最大返回行数</param>
        /// <returns>实体数组</returns>
        EntityList<IEntity> IEntityOperate.FindAllByName(String name, Object value, String orderClause, Int32 startRowIndex, Int32 maximumRows)
        {
            return ToList(FindAllByNameInternal(name, value, orderClause, startRowIndex, maximumRows));
        }

        internal abstract ICollection FindAllInternal();
        internal abstract ICollection FindAllInternal(String whereClause, String orderClause, String selects, Int32 startRowIndex, Int32 maximumRows);
        internal abstract ICollection FindAllInternal(String[] names, Object[] values);
        internal abstract ICollection FindAllInternal(String name, Object value);
        internal abstract ICollection FindAllInternal(String name, Object value, Int32 startRowIndex, Int32 maximumRows);
        internal abstract ICollection FindAllByNameInternal(String name, Object value, String orderClause, Int32 startRowIndex, Int32 maximumRows);
        #endregion

        #region 取总记录数
        /// <summary>
        /// 返回总记录数
        /// </summary>
        /// <returns></returns>
        Int32 IEntityOperate.FindCount() { return FindCountInternal(); }

        /// <summary>
        /// 返回总记录数
        /// </summary>
        /// <param name="whereClause">条件，不带Where</param>
        /// <param name="orderClause">排序，不带Order By</param>
        /// <param name="selects">查询列</param>
        /// <param name="startRowIndex">开始行，0开始</param>
        /// <param name="maximumRows">最大返回行数</param>
        /// <returns>总行数</returns>
        Int32 IEntityOperate.FindCount(String whereClause, String orderClause, String selects, Int32 startRowIndex, Int32 maximumRows)
        {
            return FindCountInternal(whereClause, orderClause, selects, startRowIndex, maximumRows);
        }

        /// <summary>
        /// 根据属性列表以及对应的值列表，返回总记录数
        /// </summary>
        /// <param name="names">属性列表</param>
        /// <param name="values">值列表</param>
        /// <returns>总行数</returns>
        Int32 IEntityOperate.FindCount(String[] names, Object[] values)
        {
            return FindCountInternal(names, values);
        }

        /// <summary>
        /// 根据属性以及对应的值，返回总记录数
        /// </summary>
        /// <param name="name">属性</param>
        /// <param name="value">值</param>
        /// <returns>总行数</returns>
        Int32 IEntityOperate.FindCount(String name, Object value)
        {
            return FindCountInternal(name, value);
        }

        /// <summary>
        /// 根据属性以及对应的值，返回总记录数
        /// </summary>
        /// <param name="name">属性</param>
        /// <param name="value">值</param>
        /// <param name="startRowIndex">开始行，0开始</param>
        /// <param name="maximumRows">最大返回行数</param>
        /// <returns>总行数</returns>
        Int32 IEntityOperate.FindCount(String name, Object value, Int32 startRowIndex, Int32 maximumRows)
        {
            return FindCountInternal(name, value, startRowIndex, maximumRows);
        }

        /// <summary>
        /// 根据属性以及对应的值，返回总记录数
        /// </summary>
        /// <param name="name">属性</param>
        /// <param name="value">值</param>
        /// <param name="orderClause">排序，不带Order By</param>
        /// <param name="startRowIndex">开始行，0开始</param>
        /// <param name="maximumRows">最大返回行数</param>
        /// <returns>总行数</returns>
        Int32 IEntityOperate.FindCountByName(String name, Object value, String orderClause, int startRowIndex, int maximumRows)
        {
            return FindCountByNameInternal(name, value, orderClause, startRowIndex, maximumRows);
        }

        internal abstract Int32 FindCountInternal();
        internal abstract Int32 FindCountInternal(String whereClause, String orderClause, String selects, Int32 startRowIndex, Int32 maximumRows);
        internal abstract Int32 FindCountInternal(String[] names, Object[] values);
        internal abstract Int32 FindCountInternal(String name, Object value);
        internal abstract Int32 FindCountInternal(String name, Object value, Int32 startRowIndex, Int32 maximumRows);
        internal abstract Int32 FindCountByNameInternal(String name, Object value, String orderClause, Int32 startRowIndex, Int32 maximumRows);
        #endregion

        #region 静态操作
        /// <summary>
        /// 把一个实体对象持久化到数据库
        /// </summary>
        /// <param name="obj">实体对象</param>
        /// <returns>返回受影响的行数</returns>
        [WebMethod(Description = "插入")]
        [DataObjectMethod(DataObjectMethodType.Insert, true)]
        public static Int32 Insert(EntityBase obj)
        {
            return obj.Insert();
        }

        /// <summary>
        /// 把一个实体对象更新到数据库
        /// </summary>
        /// <param name="obj">实体对象</param>
        /// <returns>返回受影响的行数</returns>
        [WebMethod(Description = "更新")]
        [DataObjectMethod(DataObjectMethodType.Update, true)]
        public static Int32 Update(EntityBase obj)
        {
            return obj.Update();
        }
        #endregion

        #region 获取/设置 字段值
        ///// <summary>
        ///// 获取/设置 字段值。不影响脏数据。
        ///// </summary>
        ///// <param name="name">字段名</param>
        ///// <returns></returns>
        //public abstract Object this[String name] { get; set; }

        /// <summary>
        /// 设置字段值，该方法影响脏数据。
        /// </summary>
        /// <param name="name">字段名</param>
        /// <param name="value">值</param>
        /// <returns>返回是否成功设置了数据</returns>
        public Boolean SetItem(String name, Object value)
        {
            Boolean b = OnPropertyChange(name, value);
            if (b) this[name] = value;
            return b;
        }
        #endregion

        #region 导入导出XML
        /// <summary>
        /// 建立Xml序列化器
        /// </summary>
        /// <returns></returns>
        protected virtual XmlSerializer CreateXmlSerializer()
        {
            return new XmlSerializer(this.GetType());
        }

        /// <summary>
        /// 导出XML
        /// </summary>
        /// <returns></returns>
        public virtual String ToXml()
        {
            XmlSerializer serial = CreateXmlSerializer();
            using (MemoryStream stream = new MemoryStream())
            {
                StreamWriter writer = new StreamWriter(stream, Encoding.UTF8);
                serial.Serialize(writer, this);
                Byte[] bts = stream.ToArray();
                String xml = Encoding.UTF8.GetString(bts);
                writer.Close();
                if (!String.IsNullOrEmpty(xml)) xml = xml.Trim();
                return xml;
            }
        }

        /// <summary>
        /// 导入
        /// </summary>
        /// <param name="xml"></param>
        /// <returns></returns>
        EntityBase IEntityOperate.FromXml(String xml) { return FromXmlInternal(xml); }

        internal abstract EntityBase FromXmlInternal(String xml);
        #endregion

        #region 事务
        /// <summary>
        /// 开始事务
        /// </summary>
        /// <returns></returns>
        Int32 IEntityOperate.BeginTransaction()
        {
            return BeginTransactionInternal();
        }

        /// <summary>
        /// 提交事务
        /// </summary>
        /// <returns></returns>
        Int32 IEntityOperate.Commit()
        {
            return CommitInternal();
        }

        /// <summary>
        /// 回滚事务
        /// </summary>
        /// <returns></returns>
        Int32 IEntityOperate.Rollback()
        {
            return RollbackInternal();
        }

        internal abstract Int32 BeginTransactionInternal();
        internal abstract Int32 CommitInternal();
        internal abstract Int32 RollbackInternal();
        #endregion

        #region 脏数据
        [NonSerialized]
        private DirtyCollection _Dirtys;
        /// <summary>脏属性。存储哪些属性的数据被修改过了。</summary>
        [XmlIgnore]
        protected DirtyCollection Dirtys
        {
            get
            {
                if (_Dirtys == null) _Dirtys = new DirtyCollection();
                return _Dirtys;
            }
            set { _Dirtys = value; }
        }

        /// <summary>
        /// 设置所有数据的脏属性
        /// </summary>
        /// <param name="isDirty">改变脏属性的属性个数</param>
        /// <returns></returns>
        protected virtual Int32 SetDirty(Boolean isDirty)
        {
            if (_Dirtys == null || Dirtys.Count < 1) return 0;

            Int32 count = 0;
            foreach (String item in Dirtys.Keys)
            {
                if (Dirtys[item] != isDirty)
                {
                    Dirtys[item] = isDirty;
                    count++;
                }
            }
            return count;
        }

        /// <summary>
        /// 属性改变。重载时记得调用基类的该方法，以设置脏数据属性，否则数据将无法Update到数据库。
        /// </summary>
        /// <param name="fieldName">字段名</param>
        /// <param name="newValue">新属性值</param>
        /// <returns>是否允许改变</returns>
        protected virtual Boolean OnPropertyChange(String fieldName, Object newValue)
        {
            Dirtys[fieldName] = true;
            return true;
        }
        #endregion

        #region 扩展属性
        [NonSerialized]
        private DictionaryCache<String, Object> _Extends;
        /// <summary>扩展属性</summary>
        [XmlIgnore]
        public DictionaryCache<String, Object> Extends
        {
            get { return _Extends ?? (_Extends = new DictionaryCache<String, Object>()); }
        }

        [NonSerialized]
        private Dictionary<Type, List<String>> _depends;
        /// <summary>
        /// 类型依赖
        /// </summary>
        [XmlIgnore]
        private Dictionary<Type, List<String>> Depends
        {
            get { return _depends ?? (_depends = new Dictionary<Type, List<String>>()); }
        }

        private static Boolean _StopExtend = false;
        /// <summary>
        /// 是否停止扩展属性，停止扩展属性后，可以避免扩展属性自动触发获取数据的功能
        /// </summary>
        public static Boolean StopExtend
        {
            get { return _StopExtend; }
            set { _StopExtend = value; }
        }

        /// <summary>
        /// 获取扩展属性，获取数据时向指定的依赖实体类注册数据更改事件
        /// </summary>
        /// <typeparam name="TDependEntity">依赖实体类，该实体类数据更改时清空所有依赖于实体类的扩展属性</typeparam>
        /// <typeparam name="TResult">返回类型</typeparam>
        /// <param name="key">键值</param>
        /// <param name="func">回调</param>
        /// <returns></returns>
        protected virtual TResult GetExtend<TDependEntity, TResult>(String key, Func<String, Object> func) where TDependEntity : Entity<TDependEntity>, new()
        {
            Object value = null;
            if (Extends.TryGetValue(key, out value)) return (TResult)value;

            if (StopExtend) return default(TResult);

            // 针对每个类型，仅注册一个事件
            Type type = typeof(TDependEntity);
            List<String> list = null;
            if (!Depends.TryGetValue(type, out list))
            {
                list = new List<String>();
                Depends.Add(type, list);
            }

            // 这里使用了成员方法GetExtend<TDependEntity>而不是匿名函数，为了避免生成包装类，且每次调用前实例化包装类带来较大开销
            return (TResult)Extends.GetItem<Object[]>(key, new Object[] { func, list }, new Func<String, Object[], Object>(GetExtend<TDependEntity>));
        }

        Object GetExtend<TDependEntity>(String key, Object[] args) where TDependEntity : Entity<TDependEntity>, new()
        {
            //if (Database.Debug) Database.WriteLog("GetExtend({0}, {1})", key, this);

            Func<String, Object> func = args[0] as Func<String, Object>;
            List<String> list = args[1] as List<String>;

            Object value = null;
            if (func != null) value = func(key);
            if (!list.Contains(key)) list.Add(key);
            if (list.Count == 1)
            {
                // 这里使用RemoveExtend而不是匿名函数，为了避免生成包装类，事件的Target将指向包装类的实例，
                //而内部要对Target实行弱引用，就必须保证事件的Target是实体对象本身。
                // OnDataChange内部对事件进行了拆分，弱引用Target，反射调用Method，那样性能较低，所以使用了快速方法访问器MethodInfoEx，
                Entity<TDependEntity>.Meta.OnDataChange += RemoveExtend;
            }

            return value;
        }

        /// <summary>
        /// 清理依赖于某类型的缓存
        /// </summary>
        /// <param name="dependType">依赖类型</param>
        void RemoveExtend(Type dependType)
        {
            // 停止扩展属性的情况下不生效
            if (StopExtend) return;

            if (Depends == null || Extends.Count < 1) return;
            // 找到依赖类型的扩展属性键值集合
            List<String> list = Depends[dependType];
            if (list == null || list.Count < 1) return;

            lock (Extends)
            {
                // 清理该类型的所有扩展属性
                foreach (String key in list)
                {
                    //if (Extends.ContainsKey(key))
                    {
                        //if (Database.Debug)
                        //{
                        //    Object value = Extends[key];
                        //    Database.WriteLog("RemoveExtend({0}, {1}, {2})", key, this, value != null ? value.ToString() : "null");
                        //}
                        Extends.Remove(key);
                    }
                }
                list.Clear();
            }
        }

        /// <summary>
        /// 设置扩展属性
        /// </summary>
        /// <typeparam name="TDependEntity"></typeparam>
        /// <param name="key"></param>
        /// <param name="value"></param>
        protected virtual void SetExtend<TDependEntity>(String key, Object value) where TDependEntity : Entity<TDependEntity>, new()
        {
            // 针对每个类型，仅注册一个事件
            Type type = typeof(TDependEntity);
            List<String> list = null;
            if (!Depends.TryGetValue(type, out list))
            {
                list = new List<String>();
                Depends.Add(type, list);
            }

            lock (Extends)
            {
                Extends[key] = value;
                if (!list.Contains(key)) list.Add(key);

                // 停止扩展属性的情况下不生效
                if (!StopExtend && list.Count == 1)
                {
                    Entity<TDependEntity>.Meta.OnDataChange += RemoveExtend;
                }
            }
        }
        #endregion

        #region 辅助方法
        /// <summary>
        /// 取得一个值的Sql值。
        /// 当这个值是字符串类型时，会在该值前后加单引号；
        /// </summary>
        /// <param name="obj">对象</param>
        /// <param name="field">字段特性</param>
        /// <returns>Sql值的字符串形式</returns>
        String IEntityOperate.SqlDataFormat(Object obj, String field) { return SqlDataFormatInternal(obj, field); }

        /// <summary>
        /// 根据属性列表和值列表，构造查询条件。
        /// 例如构造多主键限制查询条件。
        /// </summary>
        /// <param name="names">属性列表</param>
        /// <param name="values">值列表</param>
        /// <param name="action">联合方式</param>
        /// <returns>条件子串</returns>
        String IEntityOperate.MakeCondition(String[] names, Object[] values, String action)
        {
            return MakeConditionInternal(names, values, action);
        }

        /// <summary>
        /// 构造条件
        /// </summary>
        /// <param name="name">名称</param>
        /// <param name="value">值</param>
        /// <param name="action">大于小于等符号</param>
        /// <returns></returns>
        String IEntityOperate.MakeCondition(String name, Object value, String action)
        {
            return MakeConditionInternal(name, value, action);
        }

        internal abstract String SqlDataFormatInternal(Object obj, String field);
        internal abstract String MakeConditionInternal(String[] names, Object[] values, String action);
        internal abstract String MakeConditionInternal(String name, Object value, String action);

        /// <summary>
        /// 把一个FindAll返回的集合转为实体接口列表集合
        /// </summary>
        /// <param name="collection"></param>
        /// <returns></returns>
        EntityList<IEntity> ToList(ICollection collection)
        {
            if (collection == null || collection.Count < 1) return null;

            EntityList<IEntity> list = new EntityList<IEntity>();
            foreach (IEntity item in collection)
            {
                list.Add(item);
            }

            return list;
        }

        /// <summary>
        /// 所有绑定到数据表的属性
        /// </summary>
        List<FieldItem> IEntityOperate.Fields { get { return FieldsInternal; } }

        /// <summary>
        /// 字段名列表
        /// </summary>
        List<String> IEntityOperate.FieldNames { get { return FieldNamesInternal; } }

        internal abstract List<FieldItem> FieldsInternal { get; }
        internal abstract List<String> FieldNamesInternal { get; }
        #endregion
    }
}