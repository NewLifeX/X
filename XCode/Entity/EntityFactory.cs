using System;
using System.Collections.Generic;
using System.Reflection;
using NewLife.Collections;
using NewLife.Reflection;
using XCode.DataAccessLayer;

namespace XCode
{
    /// <summary>
    /// 实体工厂
    /// </summary>
    public static class EntityFactory
    {
        #region 创建实体
        /// <summary>
        /// 创建指定类型的实例
        /// </summary>
        /// <param name="typeName"></param>
        /// <returns></returns>
        public static IEntity Create(String typeName)
        {
            Type type = GetType(typeName);
            if (type == null) return null;

            return Create(type);
        }

        /// <summary>
        /// 创建指定类型的实例
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static IEntity Create(Type type)
        {
            if (type == null || type.IsInterface || type.IsAbstract) return null;

            //return Activator.CreateInstance(type) as IEntity;
            return TypeX.CreateInstance(type) as IEntity;
        }
        #endregion

        #region 创建实体操作接口
        /// <summary>
        /// 创建实体操作接口
        /// </summary>
        /// <remarks>因为只用来做实体操作，所以只需要一个实例即可</remarks>
        /// <param name="typeName"></param>
        /// <returns></returns>
        public static IEntityOperate CreateOperate(String typeName)
        {
            Type type = GetType(typeName);
            if (type == null)
            {
                WriteLog("创建实体操作接口时无法找到{0}类！", typeName);
                return null;
            }

            return CreateOperate(type);
        }

        private static DictionaryCache<Type, IEntityOperate> op_cache = new DictionaryCache<Type, IEntityOperate>();
        /// <summary>
        /// 创建实体操作接口
        /// </summary>
        /// <remarks>因为只用来做实体操作，所以只需要一个实例即可</remarks>
        /// <param name="type"></param>
        /// <returns></returns>
        public static IEntityOperate CreateOperate(Type type)
        {
            if (type == null) throw new ArgumentNullException("type");

            return op_cache.GetItem(type, delegate(Type key)
            {
                if (!typeof(IEntityOperate).IsAssignableFrom(key))
                {
                    Type t = GetEntityOperateType();
                    if (t != null) key = t.MakeGenericType(key);
                }
                if (key == null || !typeof(IEntityOperate).IsAssignableFrom(key))
                    throw new Exception(String.Format("无法创建{0}的实体操作接口！", key));

                IEntityOperate op = TypeX.CreateInstance(key) as IEntityOperate;
                if (op == null) throw new Exception(String.Format("无法创建{0}的实体操作接口！", key));

                return op;
            });
        }

        static Type GetEntityOperateType()
        {
            return typeof(Entity<>.EntityOperate);
            //return typeof(Entity<>).GetNestedType("EntityOperate", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
        }

        /// <summary>
        /// 使用指定的实体对象创建实体操作接口，主要用于Entity内部调用，避免反射带来的损耗
        /// </summary>
        /// <param name="type"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        internal static IEntityOperate Register(Type type, IEntityOperate entity)
        {
            if (entity == null) return CreateOperate(type);

            // 重新使用判断，减少锁争夺
            if (op_cache.ContainsKey(type)) return op_cache[type];
            lock (op_cache)
            {
                if (op_cache.ContainsKey(type)) return op_cache[type];

                //if (op_cache.ContainsKey(type))
                op_cache[type] = entity;
                //else
                //    op_cache.Add(type, entity);

                return entity;
            }
        }
        #endregion

        #region 加载插件
        /// <summary>
        /// 列出所有实体类
        /// </summary>
        /// <returns></returns>
        public static List<Type> LoadEntities()
        {
            return AssemblyX.FindAllPlugins(typeof(IEntity));
        }

        static DictionaryCache<String, Type> typeCache = new DictionaryCache<String, Type>();
        private static Type GetType(String typeName)
        {
            return typeCache.GetItem(typeName, GetTypeInternal);
        }

        private static Type GetTypeInternal(String typeName)
        {
            Type type = TypeX.GetType(typeName, true);
            if (type == null)
            {
                List<Type> entities = LoadEntities();
                if (entities != null && entities.Count > 0)
                {
                    if (!typeName.Contains("."))
                        type = entities.Find(delegate(Type item) { return item.Name == typeName; });
                    else
                        type = entities.Find(delegate(Type item) { return item.FullName == typeName; });
                }
            }
            return type;
        }
        #endregion

        #region 调试输出
        private static void WriteLog(String msg)
        {
            if (DAL.Debug) DAL.WriteLog(msg);
        }

        private static void WriteLog(String format, params Object[] args)
        {
            if (DAL.Debug) DAL.WriteLog(format, args);
        }
        #endregion
    }
}