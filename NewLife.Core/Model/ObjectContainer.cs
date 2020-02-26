using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using NewLife.Reflection;

namespace NewLife.Model
{
    /// <summary>对象容器，仅依赖查找，不支持注入</summary>
    public class ObjectContainer : IObjectContainer
    {
        #region 静态
        /// <summary>当前容器</summary>
        public static IObjectContainer Current { get; set; } = new ObjectContainer();
        #endregion

        #region 属性
        private readonly IList<IObject> _list = new List<IObject>();

        /// <summary>注册项个数</summary>
        public Int32 Count => _list.Count;

        Boolean ICollection<IObject>.IsReadOnly => false;

        /// <summary>索引访问</summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public IObject this[Int32 index] { get => _list[index]; set => _list[index] = value; }
        #endregion

        #region 方法
        /// <summary>添加</summary>
        /// <param name="item"></param>
        public void Add(IObject item)
        {
            for (var i = 0; i < _list.Count; i++)
            {
                // 覆盖重复项
                if (_list[i].ServiceType == item.ServiceType)
                {
                    _list[i] = item;
                    return;
                }
            }

            _list.Add(item);
        }

        /// <summary>插入</summary>
        /// <param name="index"></param>
        /// <param name="item"></param>
        public void Insert(Int32 index, IObject item) => _list.Insert(index, item);

        /// <summary>删除</summary>
        /// <param name="item"></param>
        public Boolean Remove(IObject item) => _list.Remove(item);

        /// <summary>删除</summary>
        /// <param name="index"></param>
        public void RemoveAt(Int32 index) => _list.RemoveAt(index);

        /// <summary>清空</summary>
        public void Clear() => _list.Clear();

        /// <summary>查找位置</summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public Int32 IndexOf(IObject item) => _list.IndexOf(item);

        /// <summary>包含</summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public Boolean Contains(IObject item) => _list.Contains(item);

        /// <summary>拷贝</summary>
        /// <param name="array"></param>
        /// <param name="arrayIndex"></param>
        public void CopyTo(IObject[] array, Int32 arrayIndex) => _list.CopyTo(array, arrayIndex);

        /// <summary>枚举</summary>
        /// <returns></returns>
        public IEnumerator<IObject> GetEnumerator() => _list.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        #endregion

        #region 注册
        /// <summary>注册</summary>
        /// <param name="serviceType">接口类型</param>
        /// <param name="implementationType">实现类型</param>
        /// <param name="instance">实例</param>
        /// <returns></returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual IObjectContainer Register(Type serviceType, Type implementationType, Object instance)
        {
            if (serviceType == null) throw new ArgumentNullException(nameof(serviceType));

            var item = new ObjectMap
            {
                ServiceType = serviceType,
                ImplementationType = implementationType,
                Instance = instance,
            };
            Add(item);

            return this;
        }

        /// <summary>注册</summary>
        /// <param name="from">接口类型</param>
        /// <param name="to">实现类型</param>
        /// <param name="instance">实例</param>
        /// <param name="id">标识</param>
        /// <param name="priority">优先级</param>
        /// <returns></returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual IObjectContainer Register(Type from, Type to, Object instance, Object id = null, Int32 priority = 0)
        => Register(from, to, instance);
        #endregion

        #region 解析
        /// <summary>解析类型的实例</summary>
        /// <param name="serviceType">接口类型</param>
        /// <returns></returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Object Resolve(Type serviceType)
        {
            if (serviceType == null) throw new ArgumentNullException(nameof(serviceType));

            var item = _list.FirstOrDefault(e => e.ServiceType == serviceType);
            //var item = _list.LastOrDefault(e => e.ServiceType == serviceType);
            if (item == null) return null;

            var type = item.ImplementationType ?? item.ServiceType;
            switch (item.Lifttime)
            {
                case ObjectLifetime.Singleton:
                    if (item is ObjectMap map)
                    {
                        if (map.Instance == null) map.Instance = type.CreateInstance();

                        return map.Instance;
                    }
                    return type.CreateInstance();

                case ObjectLifetime.Scoped:
                case ObjectLifetime.Transient:
                default:
                    return type.CreateInstance();
            }
        }

        /// <summary>解析类型指定名称的实例</summary>
        /// <param name="from">接口类型</param>
        /// <param name="id">标识</param>
        /// <returns></returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Object Resolve(Type from, Object id) => Resolve(from);

        /// <summary>解析类型指定名称的实例</summary>
        /// <param name="from">接口类型</param>
        /// <param name="id">标识</param>
        /// <returns></returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Object ResolveInstance(Type from, Object id = null) => Resolve(from);
        #endregion

        #region 辅助
        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override String ToString() => String.Format("{0}[Count={1}]", GetType().Name, Count);
        #endregion
    }

    class ObjectMap : IObject
    {
        #region 属性
        /// <summary>服务类型</summary>
        public Type ServiceType { get; set; }

        /// <summary>实现类型</summary>
        public Type ImplementationType { get; set; }

        /// <summary>生命周期</summary>
        public ObjectLifetime Lifttime { get; set; }

        /// <summary>实例</summary>
        public Object Instance { get; set; }

        /// <summary>对象工厂</summary>
        public Func<IServiceProvider, Object> Factory { get; set; }
        #endregion

        #region 方法
        public override String ToString() => String.Format("[{0},{1}]", ServiceType?.Name, ImplementationType?.Name);
        #endregion
    }

    class ServiceProvider : IServiceProvider
    {
        private readonly IObjectContainer _container;

        public ServiceProvider(IObjectContainer container) => _container = container;

        public Object GetService(Type serviceType) => _container.Resolve(serviceType);
    }
}