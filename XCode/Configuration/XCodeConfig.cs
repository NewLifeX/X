using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data.Common;
using System.Reflection;
using System.Text;
using NewLife.Collections;
using XCode.DataAccessLayer;
using XCode.Exceptions;
using NewLife.Configuration;

namespace XCode.Configuration
{
    /// <summary>
    /// 实体类配置管理类
    /// </summary>
    internal class XCodeConfig
    {
        private static DictionaryCache<Type, TableMapAttribute[]> _AllTableMaps = new DictionaryCache<Type, TableMapAttribute[]>();
        /// <summary>
        /// 所有多表映射
        /// </summary>
        /// <param name="type">实体类型</param>
        /// <returns>所有多表映射列表</returns>
        static TableMapAttribute[] AllTableMaps(Type type)
        {
            //if (_AllTableMaps.ContainsKey(t)) return _AllTableMaps[t];
            //lock (_AllTableMaps)
            //{
            //    if (_AllTableMaps.ContainsKey(t)) return _AllTableMaps[t];
            return _AllTableMaps.GetItem(type, delegate(Type key)
            {
                List<TableMapAttribute> maps = new List<TableMapAttribute>();
                PropertyInfo[] pis = key.GetProperties();
                foreach (PropertyInfo pi in pis)
                {
                    TableMapAttribute table = TableMapAttribute.GetCustomAttribute(pi);
                    maps.Add(table);
                }
                //_AllTableMaps.Add(key, maps.ToArray());
                return maps.ToArray();
            });
        }

        /// <summary>
        /// 查找指定类型的映射
        /// </summary>
        /// <param name="type"></param>
        /// <param name="jointypes"></param>
        /// <returns></returns>
        public static TableMapAttribute[] TableMaps(Type type, Type[] jointypes)
        {
            //取得所有映射关系
            List<Type> joinlist = new List<Type>(jointypes);
            //根据传入的实体类型列表来决定处理哪些多表关联
            List<TableMapAttribute> maps = new List<TableMapAttribute>();
            foreach (TableMapAttribute item in AllTableMaps(type))
            {
                Type t = joinlist.Find(delegate(Type elm) { return elm == item.MapEntity; });
                if (t != null)
                {
                    maps.Add(item);
                    joinlist.Remove(t);
                }
            }
            return maps.ToArray();
        }

        private static DictionaryCache<Type, BindTableAttribute> _Tables = new DictionaryCache<Type, BindTableAttribute>();
        /// <summary>
        /// 取得指定类的数据表。
        /// 静态缓存。
        /// </summary>
        /// <param name="type">实体类型</param>
        /// <returns>实体类绑定的数据表</returns>
        public static BindTableAttribute Table(Type type)
        {
            //if (_Tables.ContainsKey(t)) return _Tables[t];
            //lock (_Tables)
            //{
            //    if (_Tables.ContainsKey(t)) return _Tables[t];

            //    BindTableAttribute table = BindTableAttribute.GetCustomAttribute(t);

            //    _Tables.Add(t, table);

            //    ////检查数据实体授权
            //    //if (XLicense.License.EntityCount != _Tables.Count)
            //    //    XLicense.License.EntityCount = _Tables.Count;

            //    return table;
            //}

            return _Tables.GetItem(type, delegate(Type key) { return BindTableAttribute.GetCustomAttribute(key); });
        }

        private static DictionaryCache<Type, XTable> _XTables = new DictionaryCache<Type, XTable>();
        /// <summary>
        /// 获取类型对应的XTable
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static XTable GetTable(Type type)
        {
            return _XTables.GetItem(type, delegate(Type key)
            {
                BindTableAttribute bt = Table(key);
                XTable table = new XTable();
                table.Name = bt.Name;
                table.DbType = bt.DbType;
                table.Description = bt.Description;

                table.Fields = new List<XField>();
                foreach (FieldItem fi in FieldItem.Fields(key))
                {
                    XField f = table.CreateField();
                    fi.Fill(f);

                    table.Fields.Add(f);
                }

                return table;
            });
        }

        /// <summary>
        /// 获取类型指定名称的字段
        /// </summary>
        /// <param name="type"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static XField GetField(Type type, String name)
        {
            XTable table = GetTable(type);
            if (table == null || table.Fields == null) return null;

            foreach (XField item in table.Fields)
            {
                if (item.Name == name) return item;
            }
            return null;
        }

        /// <summary>
        /// 取得指定类的数据表名。
        /// 静态缓存。
        /// 特殊处理Oracle数据库，在表名前加上方案名（用户名）
        /// </summary>
        /// <param name="t">实体类型</param>
        /// <returns>实体类绑定的数据表</returns>
        public static String TableName(Type t)
        {
            BindTableAttribute table = Table(t);
            String str;
            if (table != null)
                str = table.Name;
            else
                str = t.Name;

            // 特殊处理Oracle数据库，在表名前加上方案名（用户名）
            //DAL dal = StaticDBO(t);
            DAL dal = DAL.Create(ConnName(t));
            if (dal != null && !str.Contains("."))
            {
                if (dal.DbType == DatabaseType.Oracle)
                {
                    //DbConnectionStringBuilder ocsb = dal.Db.Factory.CreateConnectionStringBuilder();
                    //ocsb.ConnectionString = dal.ConnStr;
                    // 加上用户名
                    //String UserID = (String)ocsb["User ID"];
                    String UserID = (dal.Db as Oracle).UserID;
                    if (!String.IsNullOrEmpty(UserID)) str = UserID + "." + str;
                }
            }
            return str;
        }

        private static Dictionary<Type, String> _ConnName = new Dictionary<Type, String>();
        /// <summary>
        /// 取得指定类的数据库连接名。
        /// 静态缓存。
        /// </summary>
        /// <param name="t">实体类型</param>
        /// <returns>实体类绑定的数据库连接名</returns>
        public static String ConnName(Type t)
        {
            BindTableAttribute table = Table(t);

            String connName = null;
            if (table != null) connName = table.ConnName;

            String str = FindConnMap(connName, t.Name);
            return String.IsNullOrEmpty(str) ? connName : str;
        }

        private static List<String> _ConnMaps;
        /// <summary>
        /// 连接名映射
        /// </summary>
        private static List<String> ConnMaps
        {
            get
            {
                if (_ConnMaps != null) return _ConnMaps;
                _ConnMaps = new List<String>();
                //String str = ConfigurationManager.AppSettings["XCodeConnMaps"];
                String str = Config.GetConfig<String>("XCode.ConnMaps", Config.GetConfig<String>("XCodeConnMaps"));
                if (String.IsNullOrEmpty(str)) return _ConnMaps;
                String[] ss = str.Split(',');
                foreach (String item in ss)
                {
                    if (item.Contains("#") && !item.EndsWith("#") ||
                        item.Contains("@") && !item.EndsWith("@")) _ConnMaps.Add(item.Trim());
                }
                return _ConnMaps;
            }
        }

        /// <summary>
        /// 根据连接名和类名查找连接名映射
        /// </summary>
        /// <param name="connName"></param>
        /// <param name="className"></param>
        /// <returns></returns>
        private static String FindConnMap(String connName, String className)
        {
            String name1 = connName + "#";
            String name2 = className + "@";

            foreach (String item in ConnMaps)
            {
                if (item.StartsWith(name1)) return item.Substring(name1.Length);
                if (item.StartsWith(name2)) return item.Substring(name2.Length);
            }
            return null;
        }

        private static DictionaryCache<Type, String> _SelectsEx = new DictionaryCache<Type, String>();
        /// <summary>
        /// 取得指定类对应的Select字句字符串。每个字段均带前缀。
        /// 静态缓存。
        /// </summary>
        /// <param name="type">实体类型</param>
        /// <returns>Select字句字符串</returns>
        public static String SelectsEx(Type type)
        {
            return _SelectsEx.GetItem(type, delegate(Type key)
            {
                String prefix = ColumnPrefix(key);
                String tablename = TableName(key);
                StringBuilder sbSelects = new StringBuilder();
                foreach (FieldItem fi in FieldItem.Fields(key))
                {
                    if (sbSelects.Length > 0) sbSelects.Append(", ");
                    sbSelects.AppendFormat("{0}.{1} as {2}{1}", tablename, fi.ColumnName, prefix);
                }
                return sbSelects.ToString();
            });
        }

        /// <summary>
        /// 取得字段前缀
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public static String ColumnPrefix(Type t)
        {
            return String.Format("XCode_Map_{0}_", XCodeConfig.TableName(t));
        }
    }
}