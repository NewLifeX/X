using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Text;
using System.Xml.Serialization;
using NewLife.Collections;
using NewLife.IO;
using NewLife.Reflection;

namespace XCode
{
    /// <summary>
    /// 数据实体基类的基类
    /// </summary>
    [Serializable]
    public abstract partial class EntityBase : BinaryAccessor, IEntity, ICloneable
    {
        #region 创建实体
        /// <summary>
        /// 首次连接数据库时初始化数据，仅用于实体类重载，用户不应该调用该方法
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        internal protected virtual void InitData() { }
        #endregion

        #region 填充数据
        /// <summary>
        /// 从一个数据行对象加载数据。不加载关联对象。
        /// </summary>
        /// <param name="dr">数据行</param>
        public abstract void LoadData(DataRow dr);
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

        #region 获取/设置 字段值
        /// <summary>
        /// 设置字段值，该方法影响脏数据。
        /// </summary>
        /// <param name="name">字段名</param>
        /// <param name="value">值</param>
        /// <returns>返回是否成功设置了数据</returns>
        public Boolean SetItem(String name, Object value)
        {
            Boolean b = OnPropertyChanging(name, value);
            if (b)
            {
                // OnPropertyChanging中根据新旧值是否相同来影响脏数据
                // SetItem作为必定影响脏数据的代替者
                this[name] = value;
                Dirtys[name] = true;
            }
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
        #endregion

        #region 导入导出Json
        /// <summary>
        /// 导出Json
        /// </summary>
        /// <returns></returns>
        public virtual String ToJson()
        {
            Json json = new Json();
            return json.Serialize(this);
        }
        #endregion

        #region 克隆
        /// <summary>
        /// 创建当前对象的克隆对象，仅拷贝基本字段
        /// </summary>
        /// <returns></returns>
        public abstract Object Clone();

        /// <summary>
        /// 复制来自指定实体的成员，可以是不同类型的实体，只复制共有的基本字段，影响脏数据
        /// </summary>
        /// <param name="entity">来源实体对象</param>
        /// <param name="setDirty">是否设置脏数据</param>
        /// <returns>实际复制成员数</returns>
        public virtual Int32 CopyFrom(IEntity entity, Boolean setDirty)
        {
            IEntity src = this;
            IList<String> names1 = EntityFactory.CreateOperate(src.GetType()).FieldNames;
            if (names1 == null || names1.Count < 1) return 0;
            IList<String> names2 = EntityFactory.CreateOperate(entity.GetType()).FieldNames;
            if (names2 == null || names2.Count < 1) return 0;

            Int32 n = 0;
            foreach (String item in names1)
            {
                if (names2.Contains(item))
                {
                    if (setDirty)
                        src.SetItem(item, entity[item]);
                    else
                        src[item] = entity[item];

                    n++;
                }
            }
            return n;
        }
        #endregion

        #region 脏数据
        [NonSerialized]
        private DirtyCollection _Dirtys;
        /// <summary>脏属性。存储哪些属性的数据被修改过了。</summary>
        [XmlIgnore]
        internal protected DirtyCollection Dirtys
        {
            get
            {
                if (_Dirtys == null) _Dirtys = new DirtyCollection();
                return _Dirtys;
            }
            //set { _Dirtys = value; }
        }

        /// <summary>脏属性。存储哪些属性的数据被修改过了。</summary>
        IDictionary<String, Boolean> IEntity.Dirtys { get { return Dirtys; } }

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
        //[Obsolete("改为使用OnPropertyChanging")]
        protected virtual Boolean OnPropertyChange(String fieldName, Object newValue)
        {
            if (_PropertyChanging != null) _PropertyChanging(this, new PropertyChangingEventArgs(fieldName));
            // 如果数据没有改变，不应该影响脏数据
            //Dirtys[fieldName] = true;
            if (!Object.Equals(this[fieldName], newValue)) Dirtys[fieldName] = true;
            return true;
        }
        #endregion

        #region 扩展属性
        [NonSerialized]
        private DictionaryCache<String, Object> _Extends;
        /// <summary>扩展属性</summary>
        [XmlIgnore]
        [Browsable(false)]
        public DictionaryCache<String, Object> Extends
        {
            get { return _Extends ?? (_Extends = new DictionaryCache<String, Object>()); }
        }

        /// <summary>扩展属性</summary>
        IDictionary<String, Object> IEntity.Extends { get { return Extends; } }

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

        /// <summary>
        /// 改为线程静态，避免线程间干扰。注意初始化赋值对线程静态无效，只有第一个生效
        /// </summary>
        [ThreadStatic]
        private static Boolean? _StopExtend = false;
        /// <summary>
        /// 是否停止扩展属性，停止扩展属性后，可以避免扩展属性自动触发获取数据的功能
        /// </summary>
        public static Boolean StopExtend
        {
            get
            {
                // 注意初始化赋值对线程静态无效，只有第一个生效
                if (_StopExtend == null) _StopExtend = false;
                return _StopExtend.Value;
            }
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
        protected virtual TResult GetExtend<TDependEntity, TResult>(String key, Func<String, Object> func)
            where TDependEntity : Entity<TDependEntity>, new()
        {
            return GetExtend<TDependEntity, TResult>(key, func, true);
        }

        /// <summary>
        /// 获取扩展属性，获取数据时向指定的依赖实体类注册数据更改事件
        /// </summary>
        /// <typeparam name="TDependEntity">依赖实体类，该实体类数据更改时清空所有依赖于实体类的扩展属性</typeparam>
        /// <typeparam name="TResult">返回类型</typeparam>
        /// <param name="key">键值</param>
        /// <param name="func">回调</param>
        /// <param name="cacheDefault">是否缓存默认值，可选参数，默认缓存</param>
        /// <returns></returns>
        protected virtual TResult GetExtend<TDependEntity, TResult>(String key, Func<String, Object> func, Boolean cacheDefault)
            where TDependEntity : Entity<TDependEntity>, new()
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
            return (TResult)Extends.GetItem<Func<String, Object>, List<String>>(key, func, list, new Func<String, Func<String, Object>, List<String>, Object>(GetExtend<TDependEntity>), cacheDefault);
        }

        Object GetExtend<TDependEntity>(String key, Func<String, Object> func, List<String> list) where TDependEntity : Entity<TDependEntity>, new()
        {
            //if (Database.Debug) Database.WriteLog("GetExtend({0}, {1})", key, this);

            //Func<String, Object> func = args[0] as Func<String, Object>;
            //List<String> list = args[1] as List<String>;

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
            //List<String> list = Depends[dependType];
            List<String> list = null;
            if (!Depends.TryGetValue(dependType, out list) || list == null || list.Count < 1) return;

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
    }
}