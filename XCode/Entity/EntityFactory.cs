using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NewLife;
using NewLife.Log;
using NewLife.Reflection;
using XCode.Configuration;
using XCode.DataAccessLayer;

namespace XCode
{
    /// <summary>实体工厂</summary>
    public static class EntityFactory
    {
        #region 创建实体工厂
        private static readonly ConcurrentDictionary<Type, IEntityFactory> _factories = new();
        /// <summary>实体工厂集合</summary>
        public static IDictionary<Type, IEntityFactory> Entities => _factories;

        /// <summary>创建实体操作接口</summary>
        /// <param name="type">类型</param>
        /// <returns></returns>
        [Obsolete("=>CreateFactory", true)]
        public static IEntityFactory CreateOperate(Type type) => CreateFactory(type);

        /// <summary>创建实体操作接口</summary>
        /// <remarks>
        /// 因为只用来做实体操作，所以只需要一个实例即可。
        /// 调用平均耗时3.95ns，57.39%在EnsureInit
        /// </remarks>
        /// <param name="type">类型</param>
        /// <returns></returns>
        public static IEntityFactory CreateFactory(Type type)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));

            if (_factories.TryGetValue(type, out var factory)) return factory;

            // 有可能有子类直接继承实体类，这里需要找到继承泛型实体类的那一层
            while (!type.BaseType.IsGenericType) type = type.BaseType;

            if (_factories.TryGetValue(type, out factory)) return factory;

            //// 确保实体类已被初始化，实际上，因为实体类静态构造函数中会注册IEntityFactory，所以下面的委托按理应该再也不会被执行了
            //// 先实例化，在锁里面添加到列表但不实例化，避免实体类的实例化过程中访问CreateFactory导致死锁产生
            //type.CreateInstance();

            //if (!_factories.TryGetValue(type, out factory)) throw new XCodeException("无法创建[{0}]的实体工厂！", type.FullName);

            // 读取特性中指定的自定义工程，若未指定，则使用默认工厂
            var att = type.GetCustomAttribute<EntityFactoryAttribute>();
            var factoryType = att?.Type;
            if (factoryType == null) factoryType = typeof(Entity<>).MakeGenericType(type).GetNestedType("DefaultEntityFactory").MakeGenericType(type);

            factory = factoryType?.CreateInstance() as IEntityFactory;
            if (factory == null) throw new XCodeException("无法创建[{0}]的实体工厂！", type.FullName);

            _factories.TryAdd(type, factory);

            return factory;
        }

        /// <summary>根据类型创建实体工厂</summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static IEntityFactory AsFactory(this Type type) => CreateFactory(type);

        /// <summary>使用指定的实体对象创建实体操作接口，主要用于Entity内部调用，避免反射带来的损耗</summary>
        /// <param name="type">类型</param>
        /// <param name="factory"></param>
        /// <returns></returns>
        public static IEntityFactory Register(Type type, IEntityFactory factory)
        {
            _factories[type] = factory ?? throw new ArgumentNullException(nameof(factory));

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
                // 实体类的基类必须是泛型，避免多级继承导致误判
                if (!item.BaseType.IsGenericType) continue;

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
                    var msg = $"设计错误！发现表{table.TableName}同时被两个实体类（{type.FullName}和{item.FullName}）使用！";
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

        #region 现代化用法
        /// <summary>初始化所有数据库连接的实体类和数据表</summary>
        public static void InitAll()
        {
            DAL.WriteLog("初始化所有数据库连接的实体类和数据表");

            // 加载所有实体类
            var types = typeof(IEntity).GetAllSubclasses().Where(e => e.BaseType.IsGenericType).ToList();
            var connNames = new List<String>();
            foreach (var item in types)
            {
                var ti = TableItem.Create(item);
                if (ti == null || connNames.Contains(ti.ConnName) || ti.ModelCheckMode != ModelCheckModes.CheckAllTablesWhenInit) continue;
                connNames.Add(ti.ConnName);

                Init(ti.ConnName, types);
            }
        }

        /// <summary>初始化指定连接，执行反向工程检查，初始化字段</summary>
        /// <param name="connName">连接名</param>
        public static void Init(String connName) => Init(connName, null);

        private static void Init(String connName, IList<Type> types)
        {
            // 加载所有实体类
            if (types == null) types = typeof(IEntity).GetAllSubclasses().Where(e => e.BaseType.IsGenericType).ToList();

            // 初始化工厂
            var facts = new List<IEntityFactory>();
            foreach (var type in types)
            {
                var ti = TableItem.Create(type);
                if (ti != null && ti.ConnName == connName && ti.ModelCheckMode == ModelCheckModes.CheckAllTablesWhenInit)
                {
                    var fact = CreateFactory(type);
                    facts.Add(fact);
                }
            }

            var dal = DAL.Create(connName);
            DAL.WriteLog("初始化数据库：{0}/{1} 实体类：{2}", connName, dal.DbType, facts.Join(",", e => e.EntityType.Name));

            // 反向工程检查
            if (dal.Db.Migration > Migration.Off)
            {
                //var tables = facts.Select(e => e.Table.DataTable).ToArray();
                var tables = new List<IDataTable>();
                foreach (var item in facts)
                {
                    // 克隆一份，防止修改
                    var table = item.Table.DataTable;
                    table = table.Clone() as IDataTable;

                    if (table != null && table.TableName != item.TableName)
                    {
                        // 表名去掉前缀
                        var name = item.TableName;
                        if (name.Contains(".")) name = name.Substring(".");

                        table.TableName = name;
                    }
                    tables.Add(table);
                    dal.HasCheckTables.Add(table.TableName);
                }
                dal.SetTables(tables.ToArray());
            }

            // 实体类初始化数据
            foreach (var item in facts)
            {
                //if (item.Default is EntityBase entity)
                //{
                //    entity.InitData();
                //}
                item.Session.InitData();
            }
        }
        #endregion
    }
}