using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NewLife.Log;
using NewLife.Reflection;
using XCode.Configuration;
using XCode.DataAccessLayer;

namespace XCode
{
    /// <summary>实体工厂</summary>
    public static class EntityFactory
    {
        #region 创建实体操作接口
        private static readonly ConcurrentDictionary<Type, IEntityFactory> _factories = new ConcurrentDictionary<Type, IEntityFactory>();
        /// <summary>创建实体操作接口</summary>
        /// <remarks>
        /// 因为只用来做实体操作，所以只需要一个实例即可。
        /// 调用平均耗时3.95ns，57.39%在EnsureInit
        /// </remarks>
        /// <param name="type">类型</param>
        /// <returns></returns>
        public static IEntityFactory CreateOperate(Type type)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));

            // 有可能有子类直接继承实体类，这里需要找到继承泛型实体类的那一层
            while (!type.BaseType.IsGenericType) type = type.BaseType;

            if (_factories.TryGetValue(type, out var factory)) return factory;

            // 确保实体类已被初始化，实际上，因为实体类静态构造函数中会注册IEntityOperate，所以下面的委托按理应该再也不会被执行了
            // 先实例化，在锁里面添加到列表但不实例化，避免实体类的实例化过程中访问CreateOperate导致死锁产生
            type.CreateInstance();

            if (!_factories.TryGetValue(type, out factory)) throw new XCodeException("无法创建[{0}]的实体操作接口！", type.FullName);

            return factory;
        }

        /// <summary>根据类型创建实体工厂</summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static IEntityFactory AsFactory(this Type type) => CreateOperate(type);

        /// <summary>使用指定的实体对象创建实体操作接口，主要用于Entity内部调用，避免反射带来的损耗</summary>
        /// <param name="type">类型</param>
        /// <param name="factory"></param>
        /// <returns></returns>
        public static IEntityFactory Register(Type type, IEntityFactory factory)
        {
            if (factory == null) throw new ArgumentNullException(nameof(factory));

            //return _factories.AddOrUpdate(type, factory, (t, f) => f);
            _factories[type] = factory;

            return factory;
        }
        #endregion

        #region 加载插件
        ///// <summary>列出所有实体类</summary>
        ///// <returns></returns>
        //public static List<Type> LoadEntities()
        //{
        //    return typeof(IEntity).GetAllSubclasses().ToList();
        //}

        /// <summary>获取指定连接名下的所有实体类</summary>
        /// <param name="connName"></param>
        /// <returns></returns>
        public static IEnumerable<Type> LoadEntities(String connName)
        {
            foreach (var item in typeof(IEntity).GetAllSubclasses())
            {
                var ti = TableItem.Create(item);
                if (ti == null)
                    XTrace.WriteLine("实体类[{0}]无法创建TableItem", item.FullName);
                else if (ti.ConnName == connName)
                    yield return item;
            }
        }

        /// <summary>获取指定连接名下的初始化时检查的所有实体数据表，用于反向工程检查表架构</summary>
        /// <param name="connName"></param>
        /// <param name="checkMode"></param>
        /// <returns></returns>
        public static List<IDataTable> GetTables(String connName, Boolean checkMode)
        {
            var tables = new List<IDataTable>();
            // 记录每个表名对应的实体类
            var dic = new Dictionary<String, Type>(StringComparer.OrdinalIgnoreCase);
            var list = new List<String>();
            var list2 = new List<String>();
            foreach (var item in LoadEntities(connName))
            {
                list.Add(item.Name);

                // 过滤掉第一次使用才加载的
                if (checkMode)
                {
                    var att = item.GetCustomAttribute<ModelCheckModeAttribute>(true);
                    if (att != null && att.Mode != ModelCheckModes.CheckAllTablesWhenInit) continue;
                }
                list2.Add(item.Name);

                var table = TableItem.Create(item).DataTable;

                // 判断表名是否已存在
                if (dic.TryGetValue(table.TableName, out var type))
                {
                    // 两个都不是，报错吧！
                    var msg = String.Format("设计错误！发现表{0}同时被两个实体类（{1}和{2}）使用！", table.TableName, type.FullName, item.FullName);
                    XTrace.WriteLine(msg);
                    throw new XCodeException(msg);
                }
                else
                {
                    dic.Add(table.TableName, item);
                }

                tables.Add(table);
            }

            if (DAL.Debug)
            {
                DAL.WriteLog("[{0}]的所有实体类（{1}个）：{2}", connName, list.Count, String.Join(",", list.ToArray()));
                DAL.WriteLog("[{0}]需要检查架构的实体类（{1}个）：{2}", connName, list2.Count, String.Join(",", list2.ToArray()));
            }

            return tables;
        }
        #endregion
    }
}