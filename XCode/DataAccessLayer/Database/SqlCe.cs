using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Data.SqlTypes;
using System.IO;
using System.Reflection;
using System.Text;

namespace XCode.DataAccessLayer
{
    /// <summary>SqlCe数据库。由 @Goon(12600112) 测试并完善正向反向工程</summary>
    class SqlCe : FileDbBase
    {
        #region 属性
        /// <summary>返回数据库类型。外部DAL数据库类请使用Other</summary>
        public override DatabaseType DbType
        {
            get { return DatabaseType.SqlCe; }
        }

        private static DbProviderFactory _dbProviderFactory;
        /// <summary>SqlCe提供者工厂</summary>
        static DbProviderFactory dbProviderFactory
        {
            get
            {
                if (_dbProviderFactory == null)
                {
                    lock (typeof(SqlCe))
                    {
                        if (_dbProviderFactory == null) _dbProviderFactory = GetProviderFactory("System.Data.SqlServerCe.dll", "System.Data.SqlServerCe.SqlCeProviderFactory");
                    }
                }

                return _dbProviderFactory;
            }
        }

        /// <summary>工厂</summary>
        public override DbProviderFactory Factory
        {
            get { return dbProviderFactory; }
        }

        /// <summary>SqlCe版本,默认3.5</summary>
        public SQLCEVersion SqlCeVer
        {
            get
            {
                if (FileName == null) return SQLCEVersion.SQLCE35;

                try
                {
                    return SqlCeHelper.DetermineVersion(FileName);
                }
                catch
                {
                    return SQLCEVersion.SQLCE35;
                }
            }
        }
        #endregion

        #region 方法
        /// <summary>创建数据库会话</summary>
        /// <returns></returns>
        protected override IDbSession OnCreateSession()
        {
            return new SqlCeSession();
        }

        /// <summary>创建元数据对象</summary>
        /// <returns></returns>
        protected override IMetaData OnCreateMetaData()
        {
            return new SqlCeMetaData();
        }
        #endregion

        #region 数据库特性
        /// <summary>当前时间函数</summary>
        public override String DateTimeNow { get { return "getdate()"; } }

        /// <summary>最小时间</summary>
        public override DateTime DateTimeMin { get { return SqlDateTime.MinValue.Value; } }

        /// <summary>格式化时间为SQL字符串</summary>
        /// <param name="dateTime">时间值</param>
        /// <returns></returns>
        public override String FormatDateTime(DateTime dateTime)
        {
            return String.Format("'{0:yyyy-MM-dd HH:mm:ss}'", dateTime);
        }

        /// <summary>格式化关键字</summary>
        /// <param name="keyWord">关键字</param>
        /// <returns></returns>
        public override String FormatKeyWord(String keyWord)
        {
            //if (String.IsNullOrEmpty(keyWord)) throw new ArgumentNullException("keyWord");
            if (String.IsNullOrEmpty(keyWord)) return keyWord;

            if (keyWord.StartsWith("[") && keyWord.EndsWith("]")) return keyWord;

            return String.Format("[{0}]", keyWord);
        }
        #endregion

        #region 分页
        public override SelectBuilder PageSplit(SelectBuilder builder, int startRowIndex, int maximumRows)
        {
            return MSPageSplit.PageSplit(builder, startRowIndex, maximumRows, false, b => CreateSession().QueryCount(b));
        }
        #endregion
    }

    /// <summary>SqlCe会话</summary>
    class SqlCeSession : FileDbSession
    {
        #region 方法
        protected override void CreateDatabase()
        {
            //不能用脚本创建
            return;

        }
        #endregion

        /// <summary>执行插入语句并返回新增行的自动编号</summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="type">命令类型，默认SQL文本</param>
        /// <param name="ps">命令参数</param>
        /// <returns>新增行的自动编号</returns>
        public override Int64 InsertAndGetIdentity(string sql, CommandType type = CommandType.Text, params DbParameter[] ps)
        {
            Boolean b = IsAutoClose;
            // 禁用自动关闭，保证两次在同一会话
            IsAutoClose = false;

            BeginTransaction();
            try
            {
                Int64 rs = Execute(sql, type, ps);
                if (rs > 0) rs = ExecuteScalar<Int64>("Select @@Identity");
                Commit();
                return rs;
            }
            catch { Rollback(); throw; }
            finally
            {
                IsAutoClose = b;
                AutoClose();
            }
        }

        /// <summary>返回数据源的架构信息</summary>
        /// <param name="collectionName">指定要返回的架构的名称。</param>
        /// <param name="restrictionValues">为请求的架构指定一组限制值。</param>
        /// <returns></returns>
        public override DataTable GetSchema(string collectionName, string[] restrictionValues)
        {
            //sqlce3.5 不支持GetSchema
            if (String.Equals(collectionName, DbMetaDataCollectionNames.MetaDataCollections, StringComparison.OrdinalIgnoreCase))
                return null;
            else
                return base.GetSchema(collectionName, restrictionValues);
        }
    }

    /// <summary>SqlCe元数据</summary>
    class SqlCeMetaData : FileDbMetaData
    {
        #region 构架
        protected override List<IDataTable> OnGetTables(ICollection<String> names)
        {
            #region 查表、字段信息、索引信息、主键信息
            var session = Database.CreateSession();

            //表信息
            DataTable dt = null;
            dt = session.Query(_AllTableNameSql).Tables[0];

            _columns = session.Query(_AllColumnSql).Tables[0];
            _indexes = session.Query(_AllIndexSql).Tables[0];

            //数据类型DBType --〉DotNetType转换
            DataTypes = CreateSqlCeDataType(session.Query(_DataTypeSql).Tables[0]);
            #endregion

            if (dt == null || dt.Rows == null || dt.Rows.Count < 1) return null;

            // 默认列出所有字段
            var rows = dt.Select("TABLE_TYPE='table'");
            rows = OnGetTables(names, rows);
            if (rows == null || rows.Length < 1) return null;

            return GetTables(rows);
        }

        protected override List<IDataIndex> GetIndexes(IDataTable table)
        {
            var list = base.GetIndexes(table);
            if (list != null && list.Count > 0)
            {
                // SqlCe的索引直接以索引字段的方式排布，所以需要重新组合起来
                var dic = new Dictionary<String, IDataIndex>();
                foreach (var item in list)
                {
                    IDataIndex di = null;
                    if (!dic.TryGetValue(item.Name, out di))
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

        public override string CreateTableSQL(IDataTable table)
        {
            var sql = base.CreateTableSQL(table);
            if (String.IsNullOrEmpty(sql) || table.PrimaryKeys == null || table.PrimaryKeys.Length < 2) return sql;

            // 处理多主键
            var sb = new StringBuilder();
            foreach (var item in table.PrimaryKeys)
            {
                if (sb.Length > 0) sb.Append(",");
                sb.Append(FormatName(item.Name));
            }
            sql += ";" + Environment.NewLine;
            sql += String.Format("Alter Table {0} Add Constraint PK_{1} Primary Key ({2})", FormatName(table.Name), table.Name, sb.ToString());
            return sql;
        }

        protected override string GetFieldConstraints(IDataColumn field, Boolean onlyDefine)
        {
            // 非定义时（修改字段），主键字段没有约束
            if (!onlyDefine && field.PrimaryKey) return null;

            var str = base.GetFieldConstraints(field, onlyDefine);

            // 非定义时，自增字段没有约束
            if (onlyDefine && field.Identity) str = " IDENTITY(1,1)" + str;

            return str;
        }

        #endregion

        #region 取得字段信息的SQL模版

        private DataTable CreateSqlCeDataType(DataTable src)
        {
            var drs = src.Select();
            foreach (var dr in drs)
            {
                dr["datatype"] = DBTypeToDotNetDataType(dr["typename"].ToString());
            }
            src.AcceptChanges();
            return src;
        }

        private string DBTypeToDotNetDataType(string DBType)
        {
            switch (DBType)
            {
                case "smallint": return "System.Int16";
                case "int": return "System.Int32";
                case "bigint": return "System.Int64";
                case "nvarchar":
                case "char":
                case "nchar":
                case "ntext":
                case "text":
                case "varchar": return "System.String";
                case "bit": return "System.Boolean";
                case "smalldatetime":
                case "datetime": return "System.DateTime";
                case "float": return "System.Double";
                case "decimal":
                case "money":
                case "smallmoney":
                case "numeric": return "System.Decimal";
                case "real": return "System.Single";
                case "uniqueidentifier": return "System.Guid";
                case "tinyint": return "System.Byte";
                case "image":
                case "timestamp":
                case "binary":
                case "varbinary": return "System.Byte[]";
                case "variant": return "System.Object";
                default:
                    return "";
            }
        }

        private readonly String _AllTableNameSql = "SELECT table_name,TABLE_TYPE FROM information_schema.tables WHERE TABLE_TYPE <> N'SYSTEM TABLE' ";

        private readonly String _AllColumnSql =
                "SELECT     Column_name, is_nullable, data_type, character_maximum_length, numeric_precision, autoinc_increment as AUTOINCREMENT, autoinc_seed, column_hasdefault, column_default, column_flags, numeric_scale, table_name, autoinc_next  " +
                "FROM         information_schema.columns " +
                "WHERE      SUBSTRING(COLUMN_NAME, 1,5) <> '__sys'  " +
                "ORDER BY ordinal_position ASC ";

        private readonly String _AllIndexSql =
                "SELECT     TABLE_NAME, INDEX_NAME, PRIMARY_KEY, [UNIQUE], [CLUSTERED], ORDINAL_POSITION, COLUMN_NAME, COLLATION AS SORT_ORDER " +
                "FROM         Information_Schema.Indexes " +
                "WHERE      SUBSTRING(COLUMN_NAME, 1,5) <> '__sys'   " +
                "ORDER BY TABLE_NAME, INDEX_NAME, ORDINAL_POSITION";


        private readonly String _DataTypeSql =
                "SELECT     TYPE_NAME as typename, DATA_TYPE as ProviderDbType,TYPE_NAME as datatype, COLUMN_SIZE as ColumnSize, LITERAL_PREFIX as LiteralPrefix, " +
                "           LITERAL_SUFFIX as LiteralSuffix, CREATE_PARAMS as CreateParameters, IS_NULLABLE as IsNullable, CASE_SENSITIVE as IsCaseSensitive, " +
                "           SEARCHABLE as IsSearchable, UNSIGNED_ATTRIBUTE as IsUnsigned, FIXED_PREC_SCALE, AUTO_UNIQUE_VALUE, LOCAL_TYPE_NAME,  " +
                "           MINIMUM_SCALE as MinimumScale, MAXIMUM_SCALE as MaximumScale, GUID , TYPELIB , VERSION , IS_LONG as IsLong, BEST_MATCH as IsBestMatch, IS_FIXEDLENGTH as IsFixedLength  " +
                " FROM      INFORMATION_SCHEMA.PROVIDER_TYPES ";

        #region 未使用，以后可能有用
        //private readonly String _AllPrimaryKeySql =
        //        "SELECT u.COLUMN_NAME, c.CONSTRAINT_NAME, c.TABLE_NAME " +
        //        "FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS AS c INNER JOIN " +
        //        "INFORMATION_SCHEMA.KEY_COLUMN_USAGE AS u ON c.CONSTRAINT_NAME = u.CONSTRAINT_NAME AND u.TABLE_NAME = c.TABLE_NAME " +
        //        "where c.CONSTRAINT_TYPE = 'PRIMARY KEY' ORDER BY u.TABLE_NAME, c.CONSTRAINT_NAME, u.ORDINAL_POSITION";

        //private readonly String _AllForeignKeySql =
        //        "SELECT DISTINCT KCU1.TABLE_NAME AS FK_TABLE_NAME,  KCU1.CONSTRAINT_NAME AS FK_CONSTRAINT_NAME, KCU1.COLUMN_NAME AS FK_COLUMN_NAME, " +
        //        "KCU2.TABLE_NAME AS UQ_TABLE_NAME, KCU2.CONSTRAINT_NAME AS UQ_CONSTRAINT_NAME, KCU2.COLUMN_NAME AS UQ_COLUMN_NAME, RC.UPDATE_RULE, RC.DELETE_RULE, KCU2.ORDINAL_POSITION AS UQ_ORDINAL_POSITION, KCU1.ORDINAL_POSITION AS FK_ORDINAL_POSITION " +
        //        "FROM INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS RC " +
        //        "JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE KCU1 ON KCU1.CONSTRAINT_NAME = RC.CONSTRAINT_NAME " +
        //        "JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE KCU2 ON  KCU2.CONSTRAINT_NAME =  RC.UNIQUE_CONSTRAINT_NAME AND KCU2.ORDINAL_POSITION = KCU1.ORDINAL_POSITION AND KCU2.TABLE_NAME = RC.UNIQUE_CONSTRAINT_TABLE_NAME " +
        //        "ORDER BY FK_TABLE_NAME, FK_CONSTRAINT_NAME, FK_ORDINAL_POSITION";
        #endregion

        #endregion
    }

    /// <summary>SqlCe版本</summary>
    public enum SQLCEVersion
    {
        /// <summary>Sqlce Ver2.0</summary>
        SQLCE20 = 0,

        /// <summary>Sqlce Ver3.0</summary>
        SQLCE30 = 1,

        /// <summary>Sqlce Ver3.5</summary>
        SQLCE35 = 2,

        /// <summary>Sqlce Ver4.0</summary>
        SQLCE40 = 3
    }

    /// <summary>SqlCe辅助类</summary>
    public static class SqlCeHelper
    {
        static Dictionary<int, SQLCEVersion> versionDictionary = new Dictionary<int, SQLCEVersion>
        { 
            { 0x73616261, SQLCEVersion.SQLCE20 },
            { 0x002dd714, SQLCEVersion.SQLCE30 }, 
            { 0x00357b9d, SQLCEVersion.SQLCE35 }, 
            { 0x003d0900, SQLCEVersion.SQLCE40 } 
        };

        /// <summary>检查给定SqlCe文件的版本</summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static SQLCEVersion DetermineVersion(string fileName)
        {
            int versionLONGWORD = 0;

            using (var fs = new FileStream(fileName, FileMode.Open))
            {
                fs.Seek(16, SeekOrigin.Begin);
                using (var reader = new BinaryReader(fs))
                {
                    versionLONGWORD = reader.ReadInt32();
                }
            }

            if (versionDictionary.ContainsKey(versionLONGWORD))
                return versionDictionary[versionLONGWORD];
            else
                throw new ApplicationException("不能确定该sdf的版本！");
        }

        /// <summary>检测SqlServerCe3.5是否安装</summary>
        /// <returns></returns>
        public static bool IsV35Installed()
        {
            try
            {
                Assembly.Load("System.Data.SqlServerCe, Version=3.5.1.0, Culture=neutral, PublicKeyToken=89845dcd8080cc91");
            }
            catch (FileNotFoundException)
            {
                return false;
            }
            try
            {
                var factory = DbProviderFactories.GetFactory("System.Data.SqlServerCe.3.5");
            }
            catch (ConfigurationException)
            {
                return false;
            }
            catch (ArgumentException)
            {
                return false;
            }
            return true;
        }

        /// <summary>检测SqlServerCe4是否安装</summary>
        /// <returns></returns>
        public static bool IsV40Installed()
        {
            try
            {
                Assembly.Load("System.Data.SqlServerCe, Version=4.0.0.0, Culture=neutral, PublicKeyToken=89845dcd8080cc91");
            }
            catch (FileNotFoundException)
            {
                return false;
            }
            try
            {
                var factory = DbProviderFactories.GetFactory("System.Data.SqlServerCe.4.0");
            }
            catch (ConfigurationException)
            {
                return false;
            }
            catch (ArgumentException)
            {
                return false;
            }
            return true;
        }
    }
}