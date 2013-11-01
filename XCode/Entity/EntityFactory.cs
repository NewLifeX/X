using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NewLife.Collections;
using NewLife.Log;
using NewLife.Reflection;
using XCode.Configuration;
using XCode.DataAccessLayer;
using XCode.Exceptions;

namespace XCode
{
    /// <summary>实体工厂</summary>
    public static class EntityFactory
    {
        #region 创建实体
        /// <summary>创建指定类型的实例</summary>
        /// <param name="typeName"></param>
        /// <returns></returns>
        public static IEntity Create(String typeName) { return Create(GetType(typeName)); }

        /// <summary>创建指定类型的实例</summary>
        /// <param name="type">类型</param>
        /// <returns></returns>
        public static IEntity Create(Type type)
        {
            if (type == null || type.IsInterface || type.IsAbstract) return null;

            return CreateOperate(type).Create();
        }
        #endregion

        #region 创建实体操作接口
        /// <summary>创建实体操作接口</summary>
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
        /// <summary>创建实体操作接口</summary>
        /// <remarks>因为只用来做实体操作，所以只需要一个实例即可</remarks>
        /// <param name="type">类型</param>
        /// <returns></returns>
        public static IEntityOperate CreateOperate(Type type)
        {
            if (type == null) throw new ArgumentNullException("type");

            // 确保实体类已被初始化，实际上，因为实体类静态构造函数中会注册IEntityOperate，所以下面的委托按理应该再也不会被执行了
            EnsureInit(type);

            return op_cache.GetItem(type, delegate(Type key)
            {
                Type optype = null;
                if (typeof(IEntityOperate).IsAssignableFrom(key))
                {
                    optype = key;
                }
                else
                {
                    //Type t = GetEntityOperateType();
                    //if (t != null) key = t.MakeGenericType(key);
                    optype = GetEntityOperateType(key);
                }
                if (optype == null || !typeof(IEntityOperate).IsAssignableFrom(optype))
                    throw new XCodeException("无法创建{0}的实体操作接口！", key);

                var op = optype.CreateInstance() as IEntityOperate;
                if (op == null) throw new XCodeException("无法创建{0}的实体操作接口！", key);

                // 如果源实体类型实现了IEntity接口，则以它的对象为操作者的默认值
                // 因为可能存在非泛型继承，比如Admin=>Administrator=>Administrator<Administrator>
                if (typeof(IEntity).IsAssignableFrom(key)) op.Default = key.CreateInstance() as IEntity;

                return op;
            });
        }

        static Type GetEntityOperateType(Type type)
        {
            //return type.GetNestedType("EntityOperate", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
            // 所有内嵌类
            Type[] ts = type.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
            if (ts != null && ts.Length > 0)
            {
                foreach (Type item in ts)
                {
                    // 实现了IEntityOperate接口的内嵌类
                    if (typeof(IEntityOperate).IsAssignableFrom(item))
                    {
                        Type optype = item;
                        // 此时这个内嵌类只是泛型声明而已
                        if (optype.IsGenericType && optype.IsGenericTypeDefinition)
                        {
                            // 从声明类中找到真正的实体类型，组建泛型内嵌类
                            if (type.IsGenericType && !type.IsGenericTypeDefinition)
                            {
                                optype = optype.MakeGenericType(type.GetGenericArguments());
                            }
                        }
                        return optype;
                    }
                }
            }
            if (type.BaseType != typeof(Object)) return GetEntityOperateType(type.BaseType);

            return null;
        }

        /// <summary>使用指定的实体对象创建实体操作接口，主要用于Entity内部调用，避免反射带来的损耗</summary>
        /// <param name="type">类型</param>
        /// <param name="entity"></param>
        /// <returns></returns>
        internal static IEntityOperate Register(Type type, IEntityOperate entity)
        {
            if (entity == null) return CreateOperate(type);

            // 重新使用判断，减少锁争夺
            var oc = op_cache;
            if (oc.ContainsKey(type)) return oc[type];
            lock (op_cache)
            // op_cache曾经是两次非常严重的死锁的核心所在
            // 事实上，不管怎么样处理，只要这里还锁定op_cache，那么实体类静态构造函数和CreateOperate方法，就有可能导致死锁产生
            //lock ("op_cache" + type.FullName)
            {
                if (oc.ContainsKey(type)) return oc[type];

                oc[type] = entity;

                return entity;
            }
        }
        #endregion

        #region 加载插件
        /// <summary>列出所有实体类</summary>
        /// <returns></returns>
        public static List<Type> LoadEntities()
        {
            return typeof(IEntity).GetAllSubclasses().ToList();
        }

        /// <summary>获取指定连接名下的所有实体类</summary>
        /// <param name="connName"></param>
        /// <returns></returns>
        public static IEnumerable<Type> LoadEntities(String connName)
        {
            return typeof(IEntity).GetAllSubclasses().Where(t => TableItem.Create(t).ConnName == connName);
        }

        /// <summary>获取指定连接名下的所有实体类</summary>
        /// <param name="connName"></param>
        /// <param name="isLoadAssembly">是否从未加载程序集中获取类型。使用仅反射的方法检查目标类型，如果存在，则进行常规加载</param>
        /// <returns></returns>
        public static IEnumerable<Type> LoadEntities(String connName, Boolean isLoadAssembly)
        {
            return typeof(IEntity).GetAllSubclasses(isLoadAssembly).Where(t => TableItem.Create(t).ConnName == connName);
        }

        /// <summary>获取指定连接名下的所有实体数据表</summary>
        /// <param name="connName"></param>
        /// <returns></returns>
        public static List<IDataTable> GetTables(String connName)
        {
            var tables = new List<IDataTable>();
            // 记录每个表名对应的实体类
            var dic = new Dictionary<String, Type>(StringComparer.OrdinalIgnoreCase);
            var list = new List<String>();
            var list2 = new List<String>();
            foreach (Type item in LoadEntities(connName))
            {
                list.Add(item.Name);

                // 过滤掉第一次使用才加载的
                var att = item.GetCustomAttribute<ModelCheckModeAttribute>(true);
                if (att != null && att.Mode != ModelCheckModes.CheckAllTablesWhenInit) continue;
                list2.Add(item.Name);

                var table = TableItem.Create(item).DataTable;

                // 判断表名是否已存在
                Type type = null;
                if (dic.TryGetValue(table.TableName, out type))
                {
                    // 两个实体类，只能要一个

                    // 当前实体类是，跳过
                    if (IsCommonEntity(item))
                        continue;
                    // 前面那个是，排除
                    else if (IsCommonEntity(type))
                    {
                        dic[table.TableName] = item;
                        // 删除原始实体类
                        tables.RemoveAll((tb) => tb.TableName == table.TableName);
                    }
                    // 两个都不是，报错吧！
                    else
                    {
                        String msg = String.Format("设计错误！发现表{0}同时被两个实体类（{1}和{2}）使用！", table.TableName, type.FullName, item.FullName);
                        XTrace.WriteLine(msg);
                        throw new XCodeException(msg);
                    }
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

        /// <summary>是否普通实体类</summary>
        /// <param name="type">类型</param>
        /// <returns></returns>
        private static Boolean IsCommonEntity(Type type)
        {
            // 通用实体类全部都是
            //if (type.FullName.Contains("NewLife.CommonEntity")) return true;
            if (type.Namespace == "NewLife.CommonEntity") return true;

            // 实体类和基类名字相同的也是
            String name = type.BaseType.Name;
            Int32 p = name.IndexOf('`');
            if (p > 0 && type.Name == name.Substring(0, p)) return true;

            return false;
        }

        static DictionaryCache<String, Type> typeCache = new DictionaryCache<String, Type>();
        internal static Type GetType(String typeName)
        {
            return typeCache.GetItem(typeName, GetTypeInternal);
        }

        private static Type GetTypeInternal(String typeName)
        {
            var type = Reflect.GetTypeEx(typeName, true);
            if (type != null) return type;

            var entities = LoadEntities();
            if (entities == null || entities.Count <= 0) return null;

            var p = typeName.LastIndexOf(".");
            if (p >= typeName.Length - 1) return null;

            // 记录命名空间，命名空间必须精确匹配
            var ns = "";

            // 先处理带有命名空间的
            if (p > 0)
            {
                foreach (var item in entities)
                {
                    if (item.FullName == typeName) return item;

                    // 同时按照不区分大小写查找，遍历完成后如果还没有找到，就返回不区分大小写查找的结果
                    if (type == null && typeName.EqualIgnoreCase(item.FullName)) type = item;
                }
                if (type != null) return type;

                // 去掉前面的命名空间，采用表名匹配
                ns = typeName.Substring(0, p);
                typeName = typeName.Substring(p + 1);
            }

            foreach (var item in entities)
            {
                // 命名空间必须匹配，允许不区分大小写
                if (!String.IsNullOrEmpty(ns) && !ns.EqualIgnoreCase(item.Namespace)) continue;

                if (item.Name == typeName) return item;

                // 同时按照不区分大小写查找，遍历完成后如果还没有找到，就返回不区分大小写查找的结果
                if (type == null)
                {
                    if (typeName.EqualIgnoreCase(item.Name))
                        type = item;
                    else
                    {
                        // 有可能用于查找的是表名，而表名曾经被格式化（大小写、去前缀等）
                        var ti = TableItem.Create(item);
                        if (ti != null && ti.DataTable != null && typeName.EqualIgnoreCase(ti.TableName)) type = item;
                    }
                }
            }

            return type;
        }
        #endregion

        #region 确保实体类已初始化
        static List<Type> _hasInited = new List<Type>();
        /// <summary>确保实体类已经执行完静态构造函数，因为那里实在是太容易导致死锁了</summary>
        /// <param name="type">类型</param>
        internal static void EnsureInit(Type type)
        {
            if (_hasInited.Contains(type)) return;
            lock (_hasInited)
            // 如果这里锁定_hasInited，还是有可能死锁，因为可能实体类A的静态构造函数中可能导致调用另一个实体类的EnsureInit
            // 其实我们这里加锁的目的，本来就是为了避免重复添加同一个type而已
            //lock ("_hasInited" + type.FullName)
            {
                if (_hasInited.Contains(type)) return;

                type.CreateInstance();
                _hasInited.Add(type);
            }
        }
        #endregion

        #region 调试输出
        private static void WriteLog(String msg) { if (DAL.Debug) DAL.WriteLog(msg); }

        private static void WriteLog(String format, params Object[] args) { if (DAL.Debug) DAL.WriteLog(format, args); }
        #endregion
    }
}