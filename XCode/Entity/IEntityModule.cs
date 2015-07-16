using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NewLife.Reflection;

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
    }

    /// <summary>实体模块集合</summary>
    class EntityModules : ICollection<IEntityModule>
    {
        #region 属性
        private Type _EntityType;
        /// <summary>实体类型</summary>
        public Type EntityType { get { return _EntityType; } set { _EntityType = value; } }

        private List<IEntityModule> _Modules = new List<IEntityModule>();
        /// <summary>模块集合</summary>
        public List<IEntityModule> Modules { get { return _Modules; } set { _Modules = value; } }
        #endregion

        #region 构造
        public EntityModules(Type entityType)
        {
            EntityType = entityType;

            // 扫描添加
            foreach (var item in AssemblyX.FindAllPlugins(typeof(IEntityModule), true))
            {
                var module = item.CreateInstance() as IEntityModule;
                Add(module);
            }
        }
        #endregion

        #region 方法
        public virtual Boolean Add(IEntityModule module)
        {
            if (!module.Init(EntityType)) return false;

            Modules.Add(module);

            return true;
        }

        public void Create(IEntity entity, Boolean forEdit)
        {
            foreach (var item in Modules)
            {
                item.Create(entity, forEdit);
            }
        }

        public Boolean Valid(IEntity entity, Boolean isNew)
        {
            foreach (var item in Modules)
            {
                if (!item.Valid(entity, isNew)) return false;
            }

            return true;
        }
        #endregion

        #region ICollection<IEntityModule> 成员
        void ICollection<IEntityModule>.Add(IEntityModule item) { Add(item); }

        void ICollection<IEntityModule>.Clear() { Modules.Clear(); }

        bool ICollection<IEntityModule>.Contains(IEntityModule item) { return Modules.Contains(item); }

        void ICollection<IEntityModule>.CopyTo(IEntityModule[] array, int arrayIndex) { Modules.CopyTo(array, arrayIndex); }

        int ICollection<IEntityModule>.Count { get { return Modules.Count; } }

        bool ICollection<IEntityModule>.IsReadOnly { get { return (Modules as ICollection<IEntityModule>).IsReadOnly; } }

        bool ICollection<IEntityModule>.Remove(IEntityModule item) { return Modules.Remove(item); }
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
        /// <summary>为指定实体类初始化模块，返回是否支持</summary>
        /// <param name="entityType"></param>
        /// <returns></returns>
        public virtual Boolean Init(Type entityType) { return true; }

        /// <summary>创建实体对象</summary>
        /// <param name="entity"></param>
        /// <param name="forEdit"></param>
        public virtual void Create(IEntity entity, Boolean forEdit) { }

        /// <summary>验证实体对象</summary>
        /// <param name="entity"></param>
        /// <param name="isNew"></param>
        /// <returns></returns>
        public virtual Boolean Valid(IEntity entity, bool isNew) { return true; }
        #endregion
    }
}