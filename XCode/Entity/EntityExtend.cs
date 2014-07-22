using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using NewLife.Collections;
using NewLife.Reflection;

namespace XCode
{
    /// <summary>实体扩展</summary>
    public class EntityExtend : DictionaryCache<String, Object>, IDictionary<String, Object>
    {
        [NonSerialized]
        private Dictionary<Type, List<String>> _depends;
        /// <summary>类型依赖</summary>
        [XmlIgnore]
        private Dictionary<Type, List<String>> Depends { get { return _depends ?? (_depends = new Dictionary<Type, List<String>>()); } }

        /// <summary>获取扩展属性，获取数据时向指定的依赖实体类注册数据更改事件</summary>
        /// <typeparam name="TDependEntity">依赖实体类，该实体类数据更改时清空所有依赖于实体类的扩展属性</typeparam>
        /// <typeparam name="TResult">返回类型</typeparam>
        /// <param name="key">键值</param>
        /// <param name="func">回调</param>
        /// <returns></returns>
        public virtual TResult GetExtend<TDependEntity, TResult>(String key, Func<String, Object> func)
            where TDependEntity : Entity<TDependEntity>, new()
        {
            return GetExtend<TDependEntity, TResult>(key, func, true);
        }

        /// <summary>获取扩展属性，获取数据时向指定的依赖实体类注册数据更改事件</summary>
        /// <typeparam name="TDependEntity">依赖实体类，该实体类数据更改时清空所有依赖于实体类的扩展属性</typeparam>
        /// <typeparam name="TResult">返回类型</typeparam>
        /// <param name="key">键值</param>
        /// <param name="func">回调</param>
        /// <param name="cacheDefault">是否缓存默认值，可选参数，默认缓存</param>
        /// <returns></returns>
        public virtual TResult GetExtend<TDependEntity, TResult>(String key, Func<String, Object> func, Boolean cacheDefault)
            where TDependEntity : Entity<TDependEntity>, new()
        {
            Object value = null;
            if (TryGetValue(key, out value)) return (TResult)value;

            // 针对每个类型，仅注册一个事件
            Type type = typeof(TDependEntity);
            List<String> list = null;
            var dps = Depends;
            if (!dps.TryGetValue(type, out list))
            {
                lock (dps)
                {
                    if (!dps.TryGetValue(type, out list))
                    {
                        list = new List<String>();
                        dps.Add(type, list);
                    }
                }
            }

            CacheDefault = cacheDefault;

            // 这里使用了成员方法GetExtend<TDependEntity>而不是匿名函数，为了避免生成包装类，且每次调用前实例化包装类带来较大开销
            return (TResult)GetItem<Func<String, Object>, List<String>>(key, func, list, GetExtend<TDependEntity>);
        }

        Object GetExtend<TDependEntity>(String key, Func<String, Object> func, List<String> list) where TDependEntity : Entity<TDependEntity>, new()
        {
            Object value = null;
            if (func != null) value = func(key);
            if (!list.Contains(key)) list.Add(key);
            if (list.Count == 1)
            {
                // 这里使用RemoveExtend而不是匿名函数，为了避免生成包装类，事件的Target将指向包装类的实例，
                // 而内部要对Target实行弱引用，就必须保证事件的Target是实体对象本身。
                // OnDataChange内部对事件进行了拆分，弱引用Target，反射调用Method，那样性能较低，所以使用了快速方法访问器MethodInfoEx，
                Entity<TDependEntity>.Meta.Session.OnDataChange += RemoveExtend;
            }

            return value;
        }

        /// <summary>清理依赖于某类型的缓存</summary>
        /// <param name="dependType">依赖类型</param>
        void RemoveExtend(Type dependType)
        {
            if (Depends == null || Count < 1) return;
            // 找到依赖类型的扩展属性键值集合
            List<String> list = null;
            if (!Depends.TryGetValue(dependType, out list) || list == null || list.Count < 1) return;

            lock (this)
            {
                // 清理该类型的所有扩展属性
                foreach (var key in list)
                {
                    if (ContainsKey(key)) Remove(key);
                }
                list.Clear();
            }
        }

        /// <summary>设置扩展属性</summary>
        /// <typeparam name="TDependEntity"></typeparam>
        /// <param name="key"></param>
        /// <param name="value">数值</param>
        public virtual void SetExtend<TDependEntity>(String key, Object value) where TDependEntity : Entity<TDependEntity>, new()
        {
            // 针对每个类型，仅注册一个事件
            Type type = typeof(TDependEntity);
            List<String> list = null;
            if (!Depends.TryGetValue(type, out list))
            {
                lock (Depends)
                {
                    if (!Depends.TryGetValue(type, out list))
                    {
                        list = new List<String>();
                        Depends.Add(type, list);
                    }
                }
            }

            lock (this)
            {
                this[key] = value;
                if (!list.Contains(key)) list.Add(key);

                if (list.Count == 1)
                {
                    Entity<TDependEntity>.Meta.Session.OnDataChange += RemoveExtend;
                }
            }
        }
    }
}