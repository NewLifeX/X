using System;
using System.Collections;
using System.Collections.Generic;
using NewLife.Collections;

namespace XCode
{
    /// <summary>实体处理模块</summary>
    public interface IEntityModule
    {
        /// <summary>为指定实体类初始化模块，返回是否支持</summary>
        /// <param name="entityType"></param>
        /// <returns></returns>
        Boolean Init(Type entityType);

        /// <summary>创建实体对象</summary>
        /// <param name="entity"></param>
        /// <param name="forEdit"></param>
        void Create(IEntity entity, Boolean forEdit);

        /// <summary>验证实体对象</summary>
        /// <param name="entity"></param>
        /// <param name="isNew"></param>
        /// <returns></returns>
        Boolean Valid(IEntity entity, Boolean isNew);

        /// <summary>删除实体对象</summary>
        /// <param name="entity"></param>
        Boolean Delete(IEntity entity);
    }

    /// <summary>实体模块集合</summary>
    public class EntityModules : IEnumerable<IEntityModule>
    {
        #region 全局静态
        /// <summary></summary>
        public static EntityModules Global { get; } = new EntityModules(null);
        #endregion

        #region 属性
        /// <summary>实体类型</summary>
        public Type EntityType { get; set; }

        /// <summary>模块集合</summary>
        public List<IEntityModule> Modules { get; set; } = new List<IEntityModule>();
        #endregion

        #region 构造
        /// <summary>实例化实体模块集合</summary>
        /// <param name="entityType"></param>
        public EntityModules(Type entityType)
        {
            EntityType = entityType;
        }
        #endregion

        #region 方法
        /// <summary>添加实体模块</summary>
        /// <param name="module"></param>
        /// <returns></returns>
        public virtual Boolean Add(IEntityModule module)
        {
            // 未指定实体类型表示全局模块，不需要初始化
            if (EntityType != null && !module.Init(EntityType)) return false;

            Modules.Add(module);

            return true;
        }

        /// <summary>添加实体模块</summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public virtual IEntityModule Add<T>() where T : IEntityModule, new()
        {
            var module = new T();
            if (Add(module)) return null;

            return module;
        }

        /// <summary>创建实体时执行模块</summary>
        /// <param name="entity"></param>
        /// <param name="forEdit"></param>
        public void Create(IEntity entity, Boolean forEdit)
        {
            foreach (var item in Modules)
            {
                item.Create(entity, forEdit);
            }

            if (this != Global) Global.Create(entity, forEdit);
        }

        /// <summary>添加更新实体时验证</summary>
        /// <param name="entity"></param>
        /// <param name="isNew"></param>
        /// <returns></returns>
        public Boolean Valid(IEntity entity, Boolean isNew)
        {
            foreach (var item in Modules)
            {
                if (!item.Valid(entity, isNew)) return false;
            }

            if (this != Global) Global.Valid(entity, isNew);

            return true;
        }

        /// <summary>删除实体对象</summary>
        /// <param name="entity"></param>
        public Boolean Delete(IEntity entity)
        {
            foreach (var item in Modules)
            {
                if (!item.Delete(entity)) return false;
            }

            if (this != Global) Global.Delete(entity);

            return true;
        }
        #endregion

        #region IEnumerable<IEntityModule> 成员
        IEnumerator<IEntityModule> IEnumerable<IEntityModule>.GetEnumerator() { return Modules.GetEnumerator(); }
        #endregion

        #region IEnumerable 成员
        IEnumerator IEnumerable.GetEnumerator() { return Modules.GetEnumerator(); }
        #endregion
    }

    /// <summary>实体模块基类</summary>
    public abstract class EntityModule : IEntityModule
    {
        #region IEntityModule 成员
        private Dictionary<Type, Boolean> _Inited = new Dictionary<Type, Boolean>();
        /// <summary>为指定实体类初始化模块，返回是否支持</summary>
        /// <param name="entityType"></param>
        /// <returns></returns>
        public Boolean Init(Type entityType)
        {
            var dic = _Inited;
            if (dic.TryGetValue(entityType, out var b)) return b;
            lock (dic)
            {
                if (dic.TryGetValue(entityType, out b)) return b;

                return dic[entityType] = OnInit(entityType);
            }
        }

        /// <summary>为指定实体类初始化模块，返回是否支持</summary>
        /// <param name="entityType"></param>
        /// <returns></returns>
        protected virtual Boolean OnInit(Type entityType) { return true; }

        /// <summary>创建实体对象</summary>
        /// <param name="entity"></param>
        /// <param name="forEdit"></param>
        public void Create(IEntity entity, Boolean forEdit) { if (Init(entity?.GetType())) OnCreate(entity, forEdit); }

        /// <summary>创建实体对象</summary>
        /// <param name="entity"></param>
        /// <param name="forEdit"></param>
        protected virtual void OnCreate(IEntity entity, Boolean forEdit) { }

        /// <summary>验证实体对象</summary>
        /// <param name="entity"></param>
        /// <param name="isNew"></param>
        /// <returns></returns>
        public Boolean Valid(IEntity entity, Boolean isNew)
        {
            if (!Init(entity?.GetType())) return true;

            return OnValid(entity, isNew);
        }

        /// <summary>验证实体对象</summary>
        /// <param name="entity"></param>
        /// <param name="isNew"></param>
        /// <returns></returns>
        protected virtual Boolean OnValid(IEntity entity, Boolean isNew) { return true; }

        /// <summary>删除实体对象</summary>
        /// <param name="entity"></param>
        public Boolean Delete(IEntity entity)
        {
            if (!Init(entity?.GetType())) return true;

            return OnDelete(entity);
        }

        /// <summary>删除实体对象</summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        protected virtual Boolean OnDelete(IEntity entity) { return true; }
        #endregion

        #region 辅助
        /// <summary>设置脏数据项。如果某个键存在并且数据没有脏，则设置</summary>
        /// <param name="fieldNames"></param>
        /// <param name="entity"></param>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns>返回是否成功设置了数据</returns>
        protected virtual Boolean SetNoDirtyItem(ICollection<String> fieldNames, IEntity entity, String name, Object value)
        {
            if (fieldNames.Contains(name) && !entity.Dirtys[name]) return entity.SetItem(name, value);

            return false;
        }

        private static DictionaryCache<Type, ICollection<String>> _fieldNames = new DictionaryCache<Type, ICollection<String>>();
        /// <summary>获取实体类的字段名。带缓存</summary>
        /// <param name="entityType"></param>
        /// <returns></returns>
        protected static ICollection<String> GetFieldNames(Type entityType)
        {
            return _fieldNames.GetItem(entityType, t =>
            {
                var fact = EntityFactory.CreateOperate(t);
                //return fact == null ? null : fact.FieldNames;
                if (fact == null) return null;

                return new HashSet<String>(fact.FieldNames);
            });
        }
        #endregion
    }
}