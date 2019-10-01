﻿#if __WIN__
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.OleDb;
using System.IO;
using System.Linq;
using System.Reflection;

namespace XCode.DataAccessLayer
{
    class Access : FileDbBase
    {
        #region 属性
        /// <summary>返回数据库类型。外部DAL数据库类请使用Other</summary>
        public override DatabaseType Type => DatabaseType.Access;

        private static DbProviderFactory _dbProviderFactory;
        /// <summary>工厂</summary>
        public override DbProviderFactory Factory
        {
            get
            {
                if (_dbProviderFactory == null) _dbProviderFactory = OleDbFactory.Instance;

                return _dbProviderFactory;
            }
        }

        protected override String DefaultConnectionString
        {
            get
            {
                var builder = Factory.CreateConnectionStringBuilder();
                if (builder != null)
                {
                    var name = Path.GetTempFileName();
                    //FileSource.ReleaseFile(Assembly.GetExecutingAssembly(), "Database.mdb", name, true);

                    builder[_.DataSource] = name;
                    builder["Provider"] = "Microsoft.Jet.OLEDB.4.0";
                    return builder.ToString();
                }

                return base.DefaultConnectionString;
            }
        }
        #endregion

        #region 方法
        /// <summary>创建数据库会话</summary>
        /// <returns></returns>
        protected override IDbSession OnCreateSession() => new AccessSession(this);

        /// <summary>创建元数据对象</summary>
        /// <returns></returns>
        protected override IMetaData OnCreateMetaData() => new AccessMetaData();

        public override Boolean Support(String providerName)
        {
            providerName = providerName.ToLower();
            if (providerName.Contains("microsoft.jet.oledb")) return true;
            if (providerName.Contains("access")) return true;
            if (providerName.Contains("oledb")) return true;

            return false;
        }

        protected override void OnSetConnectionString(ConnectionStringBuilder builder)
        {
            base.OnSetConnectionString(builder);

            // 特别处理一下Excel
            var fn = DatabaseName;
            if (!fn.IsNullOrEmpty())
            {
                var ext = Path.GetExtension(fn);
                if (ext.EqualIgnoreCase(".xls")) builder.TryAdd("Extended Properties", "Excel 8.0");
            }
        }
        #endregion

        #region 数据库特性
        /// <summary>长文本长度</summary>
        public override Int32 LongTextLength => 255;

        public override String FormatName(String name)
        {
            if (!String.IsNullOrEmpty(name) && name.Contains("$"))
                return FormatKeyWord(name);
            else
                return base.FormatName(name);
        }

        /// <summary>格式化时间为SQL字符串</summary>
        /// <param name="dateTime">时间值</param>
        /// <returns></returns>
        public override String FormatDateTime(DateTime dateTime) => "#" + dateTime.ToFullString() + "#";

        /// <summary>格式化关键字</summary>
        /// <param name="keyWord">关键字</param>
        /// <returns></returns>
        public override String FormatKeyWord(String keyWord)
        {
            if (String.IsNullOrEmpty(keyWord)) return keyWord;

            if (keyWord.StartsWith("[") && keyWord.EndsWith("]")) return keyWord;

            return $"[{keyWord}]";
        }

        /// <summary>格式化数据为SQL数据</summary>
        /// <param name="field">字段</param>
        /// <param name="value">数值</param>
        /// <returns></returns>
        public override String FormatValue(IDataColumn field, Object value)
        {
            if (field != null && field.DataType == typeof(Boolean) || value != null && value.GetType() == typeof(Boolean))
            {
                if (value == null) return field.Nullable ? "null" : "";

                return value.ToString();
            }

            return base.FormatValue(field, value);
        }
        #endregion

        #region 分页
        public override SelectBuilder PageSplit(SelectBuilder builder, Int64 startRowIndex, Int64 maximumRows) => MSPageSplit.PageSplit(builder, startRowIndex, maximumRows, false, b => CreateSession().QueryCount(b));
        #endregion

        #region 平台检查
        /// <summary>是否支持</summary>
        public static void CheckSupport()
        {
            var module = typeof(Object).Module;
            module.GetPEKind(out var _, out var machine);

            if (machine != ImageFileMachine.I386) throw new NotSupportedException("64位平台不支持OLEDB驱动！");
        }
        #endregion
    }

    /// <summary>Access数据库</summary>
    internal class AccessSession : FileDbSession
    {
        #region 构造函数
        public AccessSession(IDatabase db) : base(db) => Access.CheckSupport();
        #endregion

        #region 方法
        #endregion

        #region 基本方法 查询/执行
        /// <summary>执行插入语句并返回新增行的自动编号</summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="type">命令类型，默认SQL文本</param>
        /// <param name="ps">命令参数</param>
        /// <returns>新增行的自动编号</returns>
        public override Int64 InsertAndGetIdentity(String sql, CommandType type = CommandType.Text, params IDataParameter[] ps)
        {
            BeginTransaction(IsolationLevel.Serializable);
            try
            {
                Int64 rs = Execute(sql, type, ps);
                if (rs > 0) rs = ExecuteScalar<Int64>("Select @@Identity");
                Commit();
                return rs;
            }
            catch { Rollback(true); throw; }
        }
        #endregion
    }

    /// <summary>Access元数据</summary>
    class AccessMetaData : FileDbMetaData
    {
        #region 构架
        protected override List<IDataTable> OnGetTables(String[] names)
        {
            var dt = GetSchema(_.Tables, null);
            if (dt?.Rows == null || dt.Rows.Count < 1) return null;

            // 默认列出所有字段
            var rows = dt.Select(String.Format("{0}='Table' Or {0}='View'", "TABLE_TYPE"));

            return GetTables(rows, names);
        }

        /// <summary>获取索引</summary>
        /// <param name="table"></param>
        /// <param name="indexes">索引</param>
        /// <param name="indexColumns">索引列</param>
        /// <returns></returns>
        protected override List<IDataIndex> GetIndexes(IDataTable table, DataTable indexes, DataTable indexColumns)
        {
            var list = base.GetIndexes(table, indexes, indexColumns);
            if (list != null && list.Count > 0)
            {
                // Access的索引直接以索引字段的方式排布，所以需要重新组合起来
                var dic = new Dictionary<String, IDataIndex>();
                foreach (var item in list)
                {
                    if (!dic.TryGetValue(item.Name, out var di))
                    {
                        dic.Add(item.Name, item);
                    }
                    else
                    {
                        var ss = new List<String>(di.Columns);
                        if (item.Columns != null && item.Columns.Length > 0 && !ss.Contains(item.Columns[0]))
                        {
                            ss.Add(item.Columns[0]);
                            di.Columns = ss.ToArray();
                        }
                    }
                }
                list.Clear();
                foreach (var item in dic.Values)
                {
                    list.Add(item);
                }
            }
            return list;
        }

        protected override String GetFieldConstraints(IDataColumn field, Boolean onlyDefine)
        {
            var str = base.GetFieldConstraints(field, onlyDefine);

            if (field.Identity) str = " AUTOINCREMENT(1,1)" + str;

            return str;
        }
        #endregion

        #region 反向工程创建表
        public override String CreateTableSQL(IDataTable table)
        {
            var sql = base.CreateTableSQL(table);

            var pks = table.PrimaryKeys;
            if (String.IsNullOrEmpty(sql) || pks.Length < 2) return sql;

            // 处理多主键
            var names = pks.Select(e => e.ColumnName).ToArray();
            var di = ModelHelper.GetIndex(table, names);
            if (di == null)
            {
                di = table.CreateIndex();
                di.PrimaryKey = true;
                di.Unique = true;
                di.Columns = names;
            }
            // Access里面的主键索引名必须叫这个
            di.Name = "PrimaryKey";

            sql += ";" + Environment.NewLine;
            sql += CreateIndexSQL(di);
            return sql;
        }

        public override String CreateIndexSQL(IDataIndex index)
        {
            var sql = base.CreateIndexSQL(index);
            if (String.IsNullOrEmpty(sql) || !index.PrimaryKey) return sql;

            return sql + " WITH PRIMARY";
        }
        #endregion

        #region 数据类型
        protected override String GetFieldType(IDataColumn field)
        {
            var typeName = base.GetFieldType(field);

            //if (typeName.StartsWith("VarChar")) return typeName.Replace("VarChar", "Text");
            if (field.Identity) return null;

            return typeName;
        }

        /// <summary>数据类型映射</summary>
        private static Dictionary<Type, String[]> _DataTypes = new Dictionary<Type, String[]>
        {
            { typeof(Byte[]), new String[] { "binary", "varbinary", "blob", "image", "general", "oleobject" } },
            { typeof(Guid), new String[] { "uniqueidentifier", "guid" } },
            { typeof(Boolean), new String[] { "bit", "yesno", "logical", "bool", "boolean" } },
            { typeof(Byte), new String[] { "tinyint" } },
            { typeof(Int16), new String[] { "smallint" } },
            { typeof(Int32), new String[] { "int" } },
            { typeof(Int64), new String[] { "integer", "counter", "autoincrement", "identity", "long", "bigint" } },
            { typeof(Single), new String[] { "single" } },
            { typeof(Double), new String[] { "real", "float", "double" } },
            { typeof(Decimal), new String[] { "money", "decimal", "currency", "numeric" } },
            { typeof(DateTime), new String[] { "datetime", "smalldate", "timestamp", "date", "time" } },
            { typeof(String), new String[] { "nvarchar({0})", "ntext", "varchar({0})", "memo({0})", "longtext({0})", "note({0})", "text({0})", "string({0})", "char({0})", "char({0})" } }
        };
        #endregion
    }
}
#endif