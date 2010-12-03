using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Web;
using XCode.Configuration;
using NewLife.Log;

namespace XCode.DataAccessLayer
{
    /// <summary>
    /// 数据架构
    /// </summary>
    public class DatabaseSchema
    {
        #region 属性
        private DAL _Database;
        /// <summary>数据库</summary>
        public DAL Database
        {
            get { return _Database; }
            private set { _Database = value; }
        }

        private List<Type> _Entities;
        /// <summary>实体集合</summary>
        public List<Type> Entities
        {
            get
            {
                if (_Entities == null)
                {
                    _Entities = new List<Type>();

                    IList<Type> list = EntityFactory.AllEntities;
                    if (list != null && list.Count > 0)
                    {
                        foreach (Type item in list)
                        {
                            //BindTableAttribute bt = Config.Table(item);
                            //if (bt == null || bt.ConnName != Database.ConnName) continue;
                            String connName = Config.ConnName(item);
                            if (connName != Database.ConnName) continue;

                            _Entities.Add(item);
                        }
                    }
                }
                return _Entities;
            }
        }

        private List<XTable> _EntityTables;
        /// <summary>实体表集合</summary>
        public List<XTable> EntityTables
        {
            get
            {
                if (_EntityTables == null)
                {
                    List<XTable> tables = new List<XTable>();
                    foreach (Type item in Entities)
                    {
                        XTable table = Create(item, null);

                        tables.Add(table);
                    }
                    _EntityTables = tables;
                }
                return _EntityTables;
            }
        }

        private Dictionary<String, XTable> _DBTables;
        /// <summary>数据库表集合</summary>
        public Dictionary<String, XTable> DBTables
        {
            get
            {
                if (_DBTables != null) return _DBTables;
                lock (this)
                {
                    if (_DBTables != null) return _DBTables;

                    List<XTable> list = Database.DB.GetTables();

                    _DBTables = new Dictionary<String, XTable>();
                    if (list != null && list.Count > 0)
                    {
                        foreach (XTable item in list)
                        {
                            _DBTables.Add(item.Name, item);
                        }
                    }
                }
                return _DBTables;
            }
        }
        #endregion

        #region 构造函数
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="database"></param>
        private DatabaseSchema(DAL database)
        {
            Database = database;
        }

        private static Dictionary<DAL, DatabaseSchema> _objcache = new Dictionary<DAL, DatabaseSchema>();
        /// <summary>
        /// 创建对象
        /// </summary>
        /// <param name="database"></param>
        /// <returns></returns>
        public static DatabaseSchema Create(DAL database)
        {
            if (_objcache.ContainsKey(database)) return _objcache[database];
            lock (_objcache)
            {
                if (_objcache.ContainsKey(database)) return _objcache[database];

                DatabaseSchema ds = new DatabaseSchema(database);
                _objcache.Add(database, ds);

                return ds;
            }
        }

        private static Dictionary<String, DateTime> _cache = new Dictionary<String, DateTime>();

        /// <summary>
        /// 创建
        /// </summary>
        /// <param name="database"></param>
        public static void Check(DAL database)
        {
            ////每10分钟检查一次
            //if (_cache.ContainsKey(database.ConnName) && _cache[database.ConnName].AddMinutes(10) < DateTime.Now) return;
            if (_cache.ContainsKey(database.ConnName)) return;
            DatabaseSchema ds = null;
            lock (_cache)
            {
                //if (_cache.ContainsKey(database.ConnName) && _cache[database.ConnName].AddMinutes(10) < DateTime.Now) return;
                if (_cache.ContainsKey(database.ConnName)) return;

                ds = Create(database);
                //ds = new DatabaseSchema(database);
                //ds.Database = database;

                if (_cache.ContainsKey(database.ConnName))
                    _cache[database.ConnName] = DateTime.Now;
                else
                    _cache.Add(database.ConnName, DateTime.Now);
            }

            if (Enable != null && Enable.Value)
                ds.Check();
            else
                ds.BeginCheck();
        }
        #endregion

        #region 业务
        /// <summary>
        /// 开始检查
        /// </summary>
        public void BeginCheck()
        {
            if (Enable == null) return;

            if (Exclude.Count > 0)
            {
                //检查是否被排除的链接
                if (Exclude.Exists(delegate(String item) { return String.Equals(item, Database.ConnName); })) return;
            }

            ThreadPool.QueueUserWorkItem(new WaitCallback(CheckWrap));
        }

        private void CheckWrap(Object state)
        {
            try
            {
                Check();
            }
            catch (Exception ex)
            {
                XTrace.WriteLine(ex.ToString());
            }
        }

        /// <summary>
        /// 检查
        /// </summary>
        public void Check()
        {
            if (Enable == null) return;

            WriteLog("开始检查数据架构：" + Database.ConnName);

            //数据库检查
            Boolean dbExist = (Boolean)Database.DB.SetSchema(DDLSchema.DatabaseExist, null);

            if (!dbExist)
            {
                XTrace.WriteLine("创建数据库：{0}", Database.ConnName);
                Database.DB.SetSchema(DDLSchema.CreateDatabase, null, null);
            }

            if (Entities == null || Entities.Count < 1)
            {
                WriteLog(Database.ConnName + "没有找到实体类。");
                return;
            }

            WriteLog(Database.ConnName + "实体个数：" + Entities.Count);

            if (EntityTables == null || EntityTables.Count < 1) return;

            lock (EntityTables)
            {
                foreach (XTable item in EntityTables)
                {
                    CheckTable(item);
                }
            }
        }

        /// <summary>
        /// 检查实体表
        /// </summary>
        /// <param name="table"></param>
        public void CheckTable(XTable table)
        {
            if (Exclude.Count > 0)
            {
                //检查是否被排除的表
                if (Exclude.Exists(delegate(String elm)
                {
                    return String.Equals(elm, table.Name, StringComparison.OrdinalIgnoreCase);
                }))
                    return;
            }

            Dictionary<String, XTable> dic = DBTables;

            try
            {
                //if (dic.ContainsKey(item.Name))
                //    CheckTable(item, dic[item.Name]);
                //else
                //    CheckTable(item, null);

                Boolean b = false;
                foreach (String elm in dic.Keys)
                {
                    if (String.Equals(elm, table.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        CheckTable(table, dic[elm]);
                        b = true;
                        break;
                    }
                }
                if (!b) CheckTable(table, null);
            }
            catch (Exception ex)
            {
                XTrace.WriteLine(ex.ToString());
            }
        }

        private void CheckTable(XTable entitytable, XTable dbtable)
        {
#if !DEBUG
            try
#endif
            {
                String sql = String.Empty;
                //Boolean b = (Boolean)Database.DB.SetSchema(DDLSchema.TableExist, new Object[] { entitytable.Name });
                if (dbtable == null)
                {
                    #region 创建表
                    if (Enable != null && Enable.Value)
                    {
                        XTrace.WriteLine("创建表：" + entitytable.Name);
                        //建表
                        Database.DB.SetSchema(DDLSchema.CreateTable, new Object[] { entitytable });
                        ////表说明
                        //try
                        //{
                        //    Database.DB.SetSchema(DDLSchema.AddTableDescription, new Object[] { entitytable.Name, entitytable.Description });
                        //}
                        //catch { }
                        ////字段说明
                        //try
                        //{
                        //    foreach (XField elm in entitytable.Fields)
                        //    {
                        //        Database.DB.SetSchema(DDLSchema.AddColumnDescription, new Object[] { entitytable.Name, elm.Name, elm.Description });
                        //    }
                        //}
                        //catch { }
                    }
                    else
                    {
                        StringBuilder sb = new StringBuilder();
                        sb.AppendLine(Database.DB.GetSchemaSQL(DDLSchema.CreateTable, new Object[] { entitytable }) + ";");
                        //try
                        //{
                        //    sql = Database.DB.GetSchemaSQL(DDLSchema.AddTableDescription, new Object[] { entitytable.Name, entitytable.Description });
                        //    if (!String.IsNullOrEmpty(sql)) sb.AppendLine(sql + ";");
                        //    foreach (XField elm in entitytable.Fields)
                        //    {
                        //        sql = Database.DB.GetSchemaSQL(DDLSchema.AddColumnDescription, new Object[] { entitytable.Name, elm.Name, elm.Description });
                        //        if (!String.IsNullOrEmpty(sql)) sb.AppendLine(sql + ";");
                        //    }
                        //}
                        //catch { }

                        sql = sb.ToString();
                        XTrace.WriteLine("DatabaseSchema_Enable没有设置为True，请手工创建表：" + entitytable.Name + Environment.NewLine + sql);
                    }
                    #endregion
                }
                else
                {
                    #region 修改表
                    if (Enable != null && Enable.Value)
                    {
                        AlterTable(entitytable, dbtable, false);
                    }
                    else
                    {
                        sql = AlterTable(entitytable, dbtable, true);
                        if (!String.IsNullOrEmpty(sql))
                        {
                            if (Enable != null && Enable.Value)
                            {
                                XTrace.WriteLine("修改表：" + Environment.NewLine + sql);
                                //拆分成多条执行
                                String[] sqls = sql.Split(new String[] { ";" + Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
                                foreach (String item in sqls)
                                {
                                    try
                                    {
                                        Database.Execute(item, "");
                                    }
                                    catch { }
                                }
                            }
                            else
                                XTrace.WriteLine("DatabaseSchema_Enable没有设置为True，请手工使用以下语句修改表：" + Environment.NewLine + sql);
                        }
                    }
                    #endregion
                }
            }
#if !DEBUG
            catch (Exception ex)
            {
                XTrace.WriteLine("检查构架信息错误！" + Environment.NewLine + ex.ToString());
                throw;
            }
#endif
        }

        private String AlterTable(XTable entitytable, XTable dbtable, Boolean onlySql)
        {
            String sql = String.Empty;
            StringBuilder sb = new StringBuilder();
            Dictionary<String, XField> entitydic = new Dictionary<String, XField>();
            if (entitytable.Fields != null && entitytable.Fields.Count > 0)
            {
                foreach (XField item in entitytable.Fields)
                {
                    entitydic.Add(item.Name.ToLower(), item);
                }
            }
            Dictionary<String, XField> dbdic = new Dictionary<String, XField>();
            if (dbtable.Fields != null && dbtable.Fields.Count > 0)
            {
                foreach (XField item in dbtable.Fields)
                {
                    dbdic.Add(item.Name.ToLower(), item);
                }
            }

            #region 新增列
            foreach (XField item in entitytable.Fields)
            {
                if (dbdic.ContainsKey(item.Name.ToLower())) continue;

                GetSchemaSQL(sb, DDLSchema.AddColumn, new Object[] { entitytable.Name, item }, onlySql);

                //if (!String.IsNullOrEmpty(item.Default)) GetSchemaSQL(sb, DDLSchema.AddDefault, new Object[] { entitytable.Name, item.Name, item.Default });
                //if (!String.IsNullOrEmpty(item.Description)) GetSchemaSQL(sb, DDLSchema.AddColumnDescription, new Object[] { entitytable.Name, item.Name, item.Description });
            }
            #endregion

            #region 删除列
            StringBuilder sb2 = new StringBuilder();
            Dictionary<String, FieldItem> names = new Dictionary<String, FieldItem>();
            foreach (XField item in dbtable.Fields)
            {
                if (entitydic.ContainsKey(item.Name.ToLower())) continue;

                //if (!String.IsNullOrEmpty(item.Default)) GetSchemaSQL(sb2, DDLSchema.DropDefault, new Object[] { entitytable.Name, item.Name });
                //if (!String.IsNullOrEmpty(item.Description)) GetSchemaSQL(sb2, DDLSchema.DropColumnDescription, new Object[] { entitytable.Name, item.Name });

                GetSchemaSQL(sb2, DDLSchema.DropColumn, new Object[] { entitytable.Name, item.Name }, onlySql);
            }
            if (sb2.Length > 0)
            {
                if (NoDelete)
                {
                    //不许删除列，显示日志
                    XTrace.WriteLine("数据表中发现有多余字段，DatabaseSchema_NoDelete被设置为True，请手工执行以下语句删除：" + Environment.NewLine + sb2.ToString());
                }
                else
                {
                    if (sb.Length > 0) sb.AppendLine(";");
                    sb.Append(sb2.ToString());
                }
            }
            #endregion

            #region 修改列
            foreach (XField item in entitytable.Fields)
            {
                if (!dbdic.ContainsKey(item.Name.ToLower())) continue;
                XField dbf = dbdic[item.Name.ToLower()];

                Boolean b = false;

                //比较类型/允许空/主键
                if (item.DataType != dbf.DataType ||
                    item.Identity != dbf.Identity ||
                    item.PrimaryKey != dbf.PrimaryKey ||
                    item.Nullable != dbf.Nullable && !item.Identity && !item.PrimaryKey)
                {
                    b = true;
                }

                //仅针对字符串类型比较长度
                if (!b && Type.GetTypeCode(item.DataType) == TypeCode.String && item.Length != dbf.Length)
                {
                    b = true;

                    //如果是大文本类型，长度可能不等
                    if (Database.DbType == DatabaseType.Access && item.Length > 255 && dbf.Length > 255) b = false;
                    if (Database.DbType == DatabaseType.SqlServer && item.Length > 4000 && dbf.Length > 4000) b = false;
                    if (Database.DbType == DatabaseType.SqlServer2005 && item.Length > 4000 && dbf.Length > 4000) b = false;
                }

                if (b)
                {
                    GetSchemaSQL(sb, DDLSchema.AlterColumn, new Object[] { entitytable.Name, item }, onlySql);
                }

                //比较默认值
                b = String.Equals(item.Default, dbf.Default, StringComparison.OrdinalIgnoreCase);

                //特殊处理时间
                if (!b && Type.GetTypeCode(item.DataType) == TypeCode.DateTime && !String.IsNullOrEmpty(item.Default) && !String.IsNullOrEmpty(dbf.Default))
                {
                    //Access数据库，实体默认值是getdate()，数据库默认值是now()，有效
                    if (Database.DbType == DatabaseType.Access && item.Default.Equals(sq.DateTimeNow, StringComparison.OrdinalIgnoreCase) && dbf.Default.Equals(ac.DateTimeNow, StringComparison.OrdinalIgnoreCase))
                        b = true;
                    //SqlServer数据库，实体默认值是now()，数据库默认值是getdate()，有效
                    else if ((Database.DbType == DatabaseType.SqlServer || Database.DbType == DatabaseType.SqlServer2005) && item.Default.Equals(ac.DateTimeNow, StringComparison.OrdinalIgnoreCase) && dbf.Default.Equals(sq.DateTimeNow, StringComparison.OrdinalIgnoreCase))
                        b = true;
                }

                if (!b)
                {
                    if (!String.IsNullOrEmpty(dbf.Default))
                    {
                        //XTrace.WriteLine("请手工删除{0}表{1}字段的默认值！", dbtable.Name, dbf.Name);
                        GetSchemaSQL(sb, DDLSchema.DropDefault, new Object[] { entitytable.Name, dbf.Name }, onlySql);
                    }
                    if (!String.IsNullOrEmpty(item.Default))
                    {
                        //XTrace.WriteLine("请手工添加{0}表{1}字段的默认值（{2}）！", entitytable.Name, item.Name, item.Default);
                        GetSchemaSQL(sb, DDLSchema.AddDefault, new Object[] { entitytable.Name, item }, onlySql);
                    }
                }

                if (item.Description != dbf.Description)
                {
                    if (!String.IsNullOrEmpty(dbf.Description)) GetSchemaSQL(sb, DDLSchema.DropColumnDescription, new Object[] { entitytable.Name, dbf.Name }, onlySql);
                    if (!String.IsNullOrEmpty(item.Description)) GetSchemaSQL(sb, DDLSchema.AddColumnDescription, new Object[] { entitytable.Name, item.Name, item.Description }, onlySql);
                }
            }
            #endregion

            #region 表说明
            if (entitytable.Description != dbtable.Description)
            {
                if (String.IsNullOrEmpty(entitytable.Description))
                    GetSchemaSQL(sb, DDLSchema.DropTableDescription, new Object[] { entitytable.Name }, onlySql);
                else
                    GetSchemaSQL(sb, DDLSchema.AddTableDescription, new Object[] { entitytable.Name, entitytable.Description }, onlySql);
            }
            #endregion

            return sb.ToString();
        }

        /// <summary>
        /// 创建指定实体类型对应于指定表名的表结构
        /// </summary>
        /// <param name="type"></param>
        /// <param name="tablename"></param>
        /// <returns></returns>
        public static XTable Create(Type type, String tablename)
        {
            XTable table = new XTable();
            BindTableAttribute bt = Config.Table(type);

            if (String.IsNullOrEmpty(tablename)) tablename = bt.Name;
            table.Name = tablename;

            table.Description = bt.Description;

            List<XField> fields = new List<XField>();
            List<FieldItem> fis = new List<FieldItem>(Config.Fields(type));
            foreach (FieldItem fi in fis)
            {
                XField f = table.CreateField();
                f.ID = fi.Column.Order;
                f.Name = fi.ColumnName;
                f.DataType = fi.Property.PropertyType;
                f.Description = fi.Column.Description;
                f.Length = fi.DataObjectField.Length;
                f.Identity = fi.DataObjectField.IsIdentity;
                f.PrimaryKey = fi.DataObjectField.PrimaryKey;
                f.Nullable = fi.DataObjectField.IsNullable;
                f.Default = fi.Column.DefaultValue;

                while (!String.IsNullOrEmpty(f.Default) && f.Default[0] == '(' && f.Default[f.Default.Length - 1] == ')')
                {
                    f.Default = f.Default.Substring(1, f.Default.Length - 2);
                }
                if (!String.IsNullOrEmpty(f.Default)) f.Default = f.Default.Trim(new Char[] { '"', '\'' });

                fields.Add(f);
            }

            table.Fields = fields;

            return table;
        }

        private static Access ac = new Access();
        private static SqlServer sq = new SqlServer();

        private void GetSchemaSQL(StringBuilder sb, DDLSchema schema, Object[] values, Boolean onlySql)
        {
            String sql = Database.DB.GetSchemaSQL(schema, values);
            if (!String.IsNullOrEmpty(sql))
            {
                if (sb.Length > 0) sb.AppendLine(";");
                sb.Append(sql);

                if (!onlySql) XTrace.WriteLine("修改表：" + sql);
            }
            else if (!onlySql)
            {
                StringBuilder s = new StringBuilder();
                if (values != null && values.Length > 0)
                {
                    foreach (Object item in values)
                    {
                        if (s.Length > 0) s.Append(" ");
                        s.Append(item);
                    }
                }
                XTrace.WriteLine("修改表：{0} {1}", schema.ToString(), s.ToString());
            }

            if (!onlySql)
            {
                try
                {
                    Database.DB.SetSchema(schema, values);
                }
                catch (Exception ex)
                {
                    XTrace.WriteLine("修改表{0}失败！{1}", schema.ToString(), ex.Message);
                }
            }
        }
        #endregion

        #region 设置
        private static Boolean? _Enable;
        /// <summary>
        /// 是否启用数据架构
        /// </summary>
        public static Boolean? Enable
        {
            get
            {
                if (_Enable != null) return _Enable.Value;

                String str = ConfigurationManager.AppSettings["DatabaseSchema_Enable"];
                if (String.IsNullOrEmpty(str)) return null;
                if (str == "1" || str.Equals(Boolean.TrueString, StringComparison.OrdinalIgnoreCase))
                    _Enable = true;
                else if (str == "0" || str.Equals(Boolean.FalseString, StringComparison.OrdinalIgnoreCase))
                    _Enable = false;
                else
                    _Enable = Convert.ToBoolean(str);
                return _Enable.Value;
            }
            set { _Enable = value; }
        }

        private static Boolean? _NoDelete;
        /// <summary>
        /// 是否启用不删除字段
        /// </summary>
        public static Boolean NoDelete
        {
            get
            {
                if (_NoDelete != null) return _Enable.Value;

                String str = ConfigurationManager.AppSettings["DatabaseSchema_NoDelete"];
                if (String.IsNullOrEmpty(str)) return false;
                if (str == "1" || str.Equals(Boolean.TrueString, StringComparison.OrdinalIgnoreCase)) return true;
                if (str == "0" || str.Equals(Boolean.FalseString, StringComparison.OrdinalIgnoreCase)) return false;
                _NoDelete = Convert.ToBoolean(str);
                return _NoDelete.Value;
            }
            set { _NoDelete = value; }
        }

        private static List<String> _Exclude;
        /// <summary>
        /// 要排除的链接名
        /// </summary>
        public static List<String> Exclude
        {
            get
            {
                if (_Exclude != null) return _Exclude;

                String str = ConfigurationManager.AppSettings["DatabaseSchema_Exclude"];
                if (String.IsNullOrEmpty(str))
                    _Exclude = new List<String>();
                else
                    _Exclude = new List<String>(str.Split(new Char[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries));

                return _Exclude;
            }
        }
        #endregion

        #region 调试输出
        private static void WriteLog(String msg)
        {
            if (XCode.DataAccessLayer.Database.Debug) XCode.DataAccessLayer.Database.WriteLog(msg);
        }
        #endregion
    }
}
