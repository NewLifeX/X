using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using XCode.Exceptions;

namespace XCode.DataAccessLayer
{
    /// <summary>
    /// SqlServer数据库
    /// </summary>
    internal class SqlServerSession : DbSession<SqlServerSession>
    {
        #region 属性
        /// <summary>
        /// 是否SQL2005
        /// </summary>
        public Boolean IsSQL2005 { get { return (Database as SqlServer).IsSQL2005; } }

        /// <summary>
        /// 0级类型
        /// </summary>
        public String level0type { get { return IsSQL2005 ? "SCHEMA" : "USER"; } }
        #endregion

        #region 查询
        /// <summary>
        /// 快速查询单表记录数，稍有偏差
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public override int QueryCountFast(string tableName)
        {
            String sql = String.Format("select rows from sysindexes where id = object_id('{0}') and indid in (0,1)", tableName);

            QueryTimes++;
            DbCommand cmd = PrepareCommand();
            cmd.CommandText = sql;
            if (Debug) WriteLog(cmd.CommandText);
            try
            {
                Int32 rs = Convert.ToInt32(cmd.ExecuteScalar());
                //AutoClose();
                return rs;
            }
            catch (DbException ex)
            {
                throw OnException(ex, cmd.CommandText);
            }
            finally
            {
                AutoClose();
            }
        }
        #endregion

        #region 构架
        /// <summary>
        /// 取得所有表构架
        /// </summary>
        /// <returns></returns>
        public override List<XTable> GetTables()
        {
            try
            {
                //一次性把所有的表说明查出来
                DataSet ds = Query(DescriptionSql);
                DataTable DescriptionTable = ds == null || ds.Tables == null || ds.Tables.Count < 1 ? null : ds.Tables[0];

                DataTable dt = GetSchema("Tables", null);
                if (dt == null || dt.Rows == null || dt.Rows.Count < 1) return null;

                AllFields = Query(SchemaSql).Tables[0];

                // 列出用户表
                DataRow[] rows = dt.Select(String.Format("{0}='BASE TABLE' Or {0}='VIEW'", "TABLE_TYPE"));
                List<XTable> list = GetTables(rows);
                if (list == null || list.Count < 1) return list;

                // 修正备注
                foreach (XTable item in list)
                {
                    DataRow[] drs = DescriptionTable == null ? null : DescriptionTable.Select("n='" + item.Name + "'");
                    item.Description = drs == null || drs.Length < 1 ? "" : drs[0][1].ToString();
                }

                return list;
            }
            catch (DbException ex)
            {
                throw new XDbSessionException(this, "取得所有表构架出错！", ex);
            }

            //List<XTable> list = null;
            //try
            //{
            //    DataTable dt = GetSchema("Tables", null);

            //    //一次性把所有的表说明查出来
            //    DataSet ds = Query(DescriptionSql);
            //    DataTable DescriptionTable = ds == null || ds.Tables == null || ds.Tables.Count < 1 ? null : ds.Tables[0];

            //    list = new List<XTable>();
            //    if (dt != null && dt.Rows != null && dt.Rows.Count > 0)
            //    {
            //        AllFields = Query(SchemaSql).Tables[0];

            //        foreach (DataRow drTable in dt.Rows)
            //        {
            //            if (drTable["TABLE_NAME"].ToString() != "dtproperties" &&
            //                drTable["TABLE_NAME"].ToString() != "sysconstraints" &&
            //                drTable["TABLE_NAME"].ToString() != "syssegments" &&
            //               (drTable["TABLE_TYPE"].ToString() == "BASE TABLE" || drTable["TABLE_TYPE"].ToString() == "VIEW"))
            //            {
            //                XTable xt = new XTable();
            //                xt.ID = list.Count + 1;
            //                xt.Name = drTable["TABLE_NAME"].ToString();

            //                DataRow[] drs = DescriptionTable == null ? null : DescriptionTable.Select("n='" + xt.Name + "'");
            //                xt.Description = drs == null || drs.Length < 1 ? "" : drs[0][1].ToString();

            //                xt.IsView = drTable["TABLE_TYPE"].ToString() == "VIEW";
            //                xt.DbType = DbType;
            //                xt.Fields = GetFields(xt);

            //                list.Add(xt);
            //            }
            //        }
            //    }
            //}
            //catch (DbException ex)
            //{
            //    throw new XDbException(this, "取得所有表构架出错！", ex);
            //}

            //if (list == null || list.Count < 1) return null;

            //return list;
        }

        private DataTable AllFields = null;

        ///// <summary>
        ///// 取得指定表的所有列构架
        ///// </summary>
        ///// <param name="table"></param>
        ///// <returns></returns>
        //protected override List<XField> GetFields(XTable table)
        //{
        //    if (AllFields == null) return base.GetFields(table);

        //    DataRow[] rows = AllFields.Select("表名='" + table.Name + "'", null);
        //    if (rows == null || rows.Length < 1) return base.GetFields(table);

        //    List<XField> list = new List<XField>();
        //    //DataColumnCollection columns = GetColumns(xt.Name);
        //    foreach (DataRow dr in rows)
        //    {
        //        XField field = table.CreateField();
        //        field.ID = Int32.Parse(dr["字段序号"].ToString());
        //        field.Name = dr["字段名"].ToString();
        //        field.RawType = dr["类型"].ToString();
        //        //xf.DataType = FieldTypeToClassType(dr["类型"].ToString());
        //        //field.DataType = FieldTypeToClassType(field);
        //        field.Identity = Boolean.Parse(dr["标识"].ToString());

        //        //if (columns != null && columns.Contains(xf.Name))
        //        //{
        //        //    DataColumn dc = columns[xf.Name];
        //        //    xf.DataType = dc.DataType;
        //        //}

        //        field.PrimaryKey = Boolean.Parse(dr["主键"].ToString());

        //        field.Length = Int32.Parse(dr["长度"].ToString());
        //        field.NumOfByte = Int32.Parse(dr["占用字节数"].ToString());
        //        field.Digit = Int32.Parse(dr["小数位数"].ToString());

        //        field.Nullable = Boolean.Parse(dr["允许空"].ToString());
        //        field.Default = dr["默认值"].ToString();
        //        field.Description = dr["字段说明"].ToString();

        //        //处理默认值
        //        while (!String.IsNullOrEmpty(field.Default) && field.Default[0] == '(' && field.Default[field.Default.Length - 1] == ')')
        //        {
        //            field.Default = field.Default.Substring(1, field.Default.Length - 2);
        //        }
        //        if (!String.IsNullOrEmpty(field.Default)) field.Default = field.Default.Trim(new Char[] { '"', '\'' });

        //        list.Add(field);
        //    }

        //    return list;
        //}

        protected override void FixField(XField field, DataRow dr)
        {
            base.FixField(field, dr);

            DataRow[] rows = AllFields.Select("表名='" + field.Table.Name + "' And 字段名='" + field.Name + "'", null);
            if (rows != null && rows.Length > 0)
            {
                DataRow dr2 = rows[0];

                field.Identity = GetDataRowValue<Boolean>(dr2, "标识");
                field.PrimaryKey = GetDataRowValue<Boolean>(dr2, "主键");
                field.NumOfByte = GetDataRowValue<Int32>(dr2, "占用字节数");
                field.Description = GetDataRowValue<String>(dr2, "字段说明");
            }
        }

        /// <summary>
        /// 已重载。主键构架
        /// </summary>
        protected override DataTable PrimaryKeys
        {
            get
            {
                if (_PrimaryKeys == null)
                {
                    DataTable pks = GetSchema("IndexColumns", new String[] { null, null, null });
                    if (pks == null) return null;

                    //// 取得所有索引
                    //DataTable dt = GetSchema("Indexes", new String[] { null, null, null });
                    //// 取得所有拥有索引的表名
                    //Dictionary<String, Int32> dic = new Dictionary<string, int>();
                    //foreach (DataRow item in dt.Rows)
                    //{
                    //    String name = GetDataRowValue<String>(item, "table_name");
                    //}

                    //DataRow[] drs = dt.Select("type_desc='NONCLUSTERED'");
                    //if (drs != null && drs.Length > 0)
                    //{

                    //}

                    _PrimaryKeys = pks;
                }
                return _PrimaryKeys;
            }
        }

        #region 字段类型到数据类型对照表
        //public override Type FieldTypeToClassType(String type)
        //{
        //    switch (type)
        //    {
        //        case "text":
        //        case "uniqueidentifier":
        //        case "ntext":
        //        case "varchar":
        //        case "char":
        //        case "timestamp":
        //        case "nvarchar":
        //        case "nchar":
        //            return typeof(String);
        //        case "bit":
        //            return typeof(Boolean);
        //        case "tinyint":
        //        case "smallint":
        //            return typeof(Int16);
        //        case "int":
        //        case "numeric":
        //            return typeof(Int32);
        //        case "bigint":
        //            return typeof(Int64);
        //        case "decimal":
        //        case "money":
        //        case "smallmoney":
        //            return typeof(Decimal);
        //        case "smallldatetime":
        //        case "datetime":
        //            return typeof(DateTime);
        //        case "real":
        //        case "float":
        //            return typeof(Double);
        //        case "image":
        //        case "sql_variant":
        //        case "varbinary":
        //        case "binary":
        //        case "systemname":
        //            return typeof(Byte[]);
        //        default:
        //            return typeof(String);
        //    }
        //    //if (type.Equals("Int32", StringComparison.OrdinalIgnoreCase)) return "Int32";
        //    //if (type.Equals("varchar", StringComparison.OrdinalIgnoreCase)) return "String";
        //    //if (type.Equals("text", StringComparison.OrdinalIgnoreCase)) return "String";
        //    //if (type.Equals("double", StringComparison.OrdinalIgnoreCase)) return "Double";
        //    //if (type.Equals("datetime", StringComparison.OrdinalIgnoreCase)) return "DateTime";
        //    //if (type.Equals("Int32", StringComparison.OrdinalIgnoreCase)) return "Int32";
        //    //if (type.Equals("Int32", StringComparison.OrdinalIgnoreCase)) return "Int32";
        //    //throw new Exception("Error");
        //}
        #endregion

        #region 取得字段信息的SQL模版
        private String _SchemaSql = "";
        /// <summary>
        /// 构架SQL
        /// </summary>
        public virtual String SchemaSql
        {
            get
            {
                if (String.IsNullOrEmpty(_SchemaSql))
                {
                    StringBuilder sb = new StringBuilder();
                    sb.Append("SELECT ");
                    sb.Append("表名=d.name,");
                    sb.Append("字段序号=a.colorder,");
                    sb.Append("字段名=a.name,");
                    sb.Append("标识=case when COLUMNPROPERTY( a.id,a.name,'IsIdentity')=1 then Convert(Bit,1) else Convert(Bit,0) end,");
                    sb.Append("主键=case when exists(SELECT 1 FROM sysobjects where xtype='PK' and name in (");
                    sb.Append("SELECT name FROM sysindexes WHERE id = a.id AND indid in(");
                    sb.Append("SELECT indid FROM sysindexkeys WHERE id = a.id AND colid=a.colid");
                    sb.Append("))) then Convert(Bit,1) else Convert(Bit,0) end,");
                    sb.Append("类型=b.name,");
                    sb.Append("占用字节数=a.length,");
                    sb.Append("长度=COLUMNPROPERTY(a.id,a.name,'PRECISION'),");
                    sb.Append("小数位数=isnull(COLUMNPROPERTY(a.id,a.name,'Scale'),0),");
                    sb.Append("允许空=case when a.isnullable=1 then Convert(Bit,1)else Convert(Bit,0) end,");
                    sb.Append("默认值=isnull(e.text,''),");
                    sb.Append("字段说明=isnull(g.[value],'')");
                    sb.Append("FROM syscolumns a ");
                    sb.Append("left join systypes b on a.xtype=b.xusertype ");
                    sb.Append("inner join sysobjects d on a.id=d.id  and d.xtype='U' ");
                    sb.Append("left join syscomments e on a.cdefault=e.id ");
                    if (IsSQL2005)
                    {
                        //sb.Append("SELECT ");
                        //sb.Append("表名=d.name,");
                        //sb.Append("字段序号=a.colorder,");
                        //sb.Append("字段名=a.name,");
                        //sb.Append("标识=case when COLUMNPROPERTY( a.id,a.name,'IsIdentity')=1 then Convert(Bit,1) else Convert(Bit,0) end,");
                        //sb.Append("主键=case when exists(SELECT 1 FROM sysobjects where xtype='PK' and name in (");
                        //sb.Append("SELECT name FROM sysindexes WHERE id = a.id AND indid in(");
                        //sb.Append("SELECT indid FROM sysindexkeys WHERE id = a.id AND colid=a.colid");
                        //sb.Append("))) then Convert(Bit,1) else Convert(Bit,0) end,");
                        //sb.Append("类型=b.name,");
                        //sb.Append("占用字节数=a.length,");
                        //sb.Append("长度=COLUMNPROPERTY(a.id,a.name,'PRECISION'),");
                        //sb.Append("小数位数=isnull(COLUMNPROPERTY(a.id,a.name,'Scale'),0),");
                        //sb.Append("允许空=case when a.isnullable=1 then Convert(Bit,1)else Convert(Bit,0) end,");
                        //sb.Append("默认值=isnull(e.text,''),");
                        //sb.Append("字段说明=isnull(g.[value],'')");
                        //sb.Append("FROM syscolumns a ");
                        //sb.Append("left join systypes b on a.xtype=b.xusertype ");
                        //sb.Append("inner join sysobjects d on a.id=d.id  and d.xtype='U' ");
                        //sb.Append("left join syscomments e on a.cdefault=e.id ");
                        sb.Append("left join sys.extended_properties g on a.id=g.major_id and a.colid=g.minor_id and g.name = 'MS_Description'  ");
                        //sb.Append("order by a.id,a.colorder");
                    }
                    else
                    {
                        //sb.Append("SELECT ");
                        //sb.Append("表名=d.name,");
                        //sb.Append("字段序号=a.colorder,");
                        //sb.Append("字段名=a.name,");
                        //sb.Append("标识=case when COLUMNPROPERTY( a.id,a.name,'IsIdentity')=1 then Convert(Bit,1) else Convert(Bit,0) end,");
                        //sb.Append("主键=case when exists(SELECT 1 FROM sysobjects where xtype='PK' and name in (");
                        //sb.Append("SELECT name FROM sysindexes WHERE id = a.id AND indid in(");
                        //sb.Append("SELECT indid FROM sysindexkeys WHERE id = a.id AND colid=a.colid");
                        //sb.Append("))) then Convert(Bit,1) else Convert(Bit,0) end,");
                        //sb.Append("类型=b.name,");
                        //sb.Append("占用字节数=a.length,");
                        //sb.Append("长度=COLUMNPROPERTY(a.id,a.name,'PRECISION'),");
                        //sb.Append("小数位数=isnull(COLUMNPROPERTY(a.id,a.name,'Scale'),0),");
                        //sb.Append("允许空=case when a.isnullable=1 then Convert(Bit,1)else Convert(Bit,0) end,");
                        //sb.Append("默认值=isnull(e.text,''),");
                        //sb.Append("字段说明=isnull(g.[value],'')");
                        //sb.Append("FROM syscolumns a ");
                        //sb.Append("left join systypes b on a.xtype=b.xusertype ");
                        //sb.Append("inner join sysobjects d on a.id=d.id  and d.xtype='U' ");
                        //sb.Append("left join syscomments e on a.cdefault=e.id ");
                        sb.Append("left join sysproperties g on a.id=g.id and a.colid=g.smallid  ");
                        //sb.Append("order by a.id,a.colorder");
                    }
                    sb.Append("order by a.id,a.colorder");
                    _SchemaSql = sb.ToString();
                }
                return _SchemaSql;
            }
        }

        private readonly String _DescriptionSql2000 = "select b.name n, a.value v from sysproperties a inner join sysobjects b on a.id=b.id where a.smallid=0";
        private readonly String _DescriptionSql2005 = "select b.name n, a.value v from sys.extended_properties a inner join sysobjects b on a.major_id=b.id and a.minor_id=0 and a.name = 'MS_Description'";
        /// <summary>
        /// 取表说明SQL
        /// </summary>
        public virtual String DescriptionSql { get { return IsSQL2005 ? _DescriptionSql2005 : _DescriptionSql2000; } }
        #endregion

        #region 数据定义
        //public override string GetSchemaSQL(DDLSchema schema, params object[] values)
        //{
        //    if (schema == DDLSchema.DropDatabase) return DropDatabaseSQL((String)values[0]);

        //    return base.GetSchemaSQL(schema, values);
        //}

        public override object SetSchema(DDLSchema schema, params object[] values)
        {
            Object obj = null;
            String dbname = String.Empty;
            String databaseName = String.Empty;
            switch (schema)
            {
                case DDLSchema.DatabaseExist:
                    databaseName = values == null || values.Length < 1 ? null : (String)values[0];
                    if (String.IsNullOrEmpty(databaseName)) databaseName = DatabaseName;
                    values = new Object[] { databaseName };

                    dbname = DatabaseName;

                    //如果指定了数据库名，并且不是master，则切换到master
                    if (!String.IsNullOrEmpty(dbname) && !String.Equals(dbname, "master", StringComparison.OrdinalIgnoreCase))
                    {
                        DatabaseName = "master";
                        obj = QueryCount(GetSchemaSQL(schema, values)) > 0;
                        DatabaseName = dbname;
                        return obj;
                    }
                    else
                    {
                        return QueryCount(GetSchemaSQL(schema, values)) > 0;
                    }
                case DDLSchema.TableExist:
                    return QueryCount(GetSchemaSQL(schema, values)) > 0;
                case DDLSchema.CreateDatabase:
                    databaseName = values == null || values.Length < 1 ? null : (String)values[0];
                    if (String.IsNullOrEmpty(databaseName)) databaseName = DatabaseName;
                    values = new Object[] { databaseName, values == null || values.Length < 2 ? null : values[1] };

                    dbname = DatabaseName;
                    DatabaseName = "master";
                    obj = base.SetSchema(schema, values);
                    DatabaseName = dbname;
                    return obj;
                case DDLSchema.DropDatabase:
                    databaseName = values == null || values.Length < 1 ? null : (String)values[0];
                    if (String.IsNullOrEmpty(databaseName)) databaseName = DatabaseName;
                    values = new Object[] { databaseName, values == null || values.Length < 2 ? null : values[1] };

                    dbname = DatabaseName;
                    DatabaseName = "master";
                    //obj = base.SetSchema(schema, values);
                    //if (Execute(String.Format("Drop Database [{0}]", dbname)) < 1)
                    //{
                    //    Execute(DropDatabaseSQL(databaseName));
                    //}
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine("use master");
                    sb.AppendLine(";");
                    sb.AppendLine("declare   @spid   varchar(20),@dbname   varchar(20)");
                    sb.AppendLine("declare   #spid   cursor   for");
                    sb.AppendFormat("select   spid=cast(spid   as   varchar(20))   from   master..sysprocesses   where   dbid=db_id('{0}')", dbname);
                    sb.AppendLine();
                    sb.AppendLine("open   #spid");
                    sb.AppendLine("fetch   next   from   #spid   into   @spid");
                    sb.AppendLine("while   @@fetch_status=0");
                    sb.AppendLine("begin");
                    sb.AppendLine("exec('kill   '+@spid)");
                    sb.AppendLine("fetch   next   from   #spid   into   @spid");
                    sb.AppendLine("end");
                    sb.AppendLine("close   #spid");
                    sb.AppendLine("deallocate   #spid");

                    Int32 count = 0;
                    try { count = Execute(sb.ToString()); }
                    catch { }
                    obj = Execute(String.Format("Drop Database {0}", FormatKeyWord(dbname))) > 0;
                    //sb.AppendFormat("Drop Database [{0}]", dbname);

                    DatabaseName = dbname;
                    return obj;
                default:
                    break;
            }
            return base.SetSchema(schema, values);
        }

        /// <summary>
        /// 字段片段
        /// </summary>
        /// <param name="field"></param>
        /// <param name="onlyDefine">仅仅定义。定义操作才允许设置自增和使用默认值</param>
        /// <returns></returns>
        public override String FieldClause(XField field, Boolean onlyDefine)
        {
            StringBuilder sb = new StringBuilder();

            //字段名
            //sb.AppendFormat("[{0}] ", field.Name);
            sb.AppendFormat("{0} ", FormatKeyWord(field.Name));

            //类型
            TypeCode tc = Type.GetTypeCode(field.DataType);
            switch (tc)
            {
                case TypeCode.Boolean:
                    sb.Append("[bit]");
                    break;
                case TypeCode.Byte:
                    sb.Append("[byte]");
                    break;
                case TypeCode.Char:
                    sb.Append("[char]");
                    break;
                case TypeCode.DBNull:
                    break;
                case TypeCode.DateTime:
                    sb.Append("[datetime]");
                    break;
                case TypeCode.Decimal:
                    sb.Append("[money]");
                    if (onlyDefine && field.Identity) sb.Append(" IDENTITY(1,1)");
                    break;
                case TypeCode.Double:
                    sb.Append("[float]");
                    break;
                case TypeCode.Empty:
                    break;
                case TypeCode.Int16:
                case TypeCode.UInt16:
                    sb.Append("[smallint]");
                    if (onlyDefine && field.Identity) sb.Append(" IDENTITY(1,1)");
                    break;
                case TypeCode.Int32:
                case TypeCode.UInt32:
                    sb.Append("[int]");
                    if (onlyDefine && field.Identity) sb.Append(" IDENTITY(1,1)");
                    break;
                case TypeCode.Int64:
                case TypeCode.UInt64:
                    sb.Append("[bigint]");
                    if (onlyDefine && field.Identity) sb.Append(" IDENTITY(1,1)");
                    break;
                case TypeCode.Object:
                    break;
                case TypeCode.SByte:
                    sb.Append("[byte]");
                    break;
                case TypeCode.Single:
                    sb.Append("[float]");
                    break;
                case TypeCode.String:
                    Int32 len = field.Length;
                    if (len < 1) len = 50;
                    if (len > 4000)
                        sb.Append("[ntext]");
                    else
                        sb.AppendFormat("[nvarchar]({0})", len);
                    break;
                default:
                    break;
            }

            //是否为空
            if (!field.PrimaryKey && !field.Identity)
            {
                if (field.Nullable)
                    sb.Append(" NULL");
                else
                {
                    sb.Append(" NOT NULL");
                }
            }

            //默认值
            if (onlyDefine && !String.IsNullOrEmpty(field.Default))
            {
                if (tc == TypeCode.String)
                    sb.AppendFormat(" DEFAULT ('{0}')", field.Default);
                else if (tc == TypeCode.DateTime)
                {
                    String d = field.Default;
                    //if (String.Equals(d, "now()", StringComparison.OrdinalIgnoreCase)) d = "getdate()";
                    if (String.Equals(d, "now()", StringComparison.OrdinalIgnoreCase)) d = Database.DateTimeNow;
                    sb.AppendFormat(" DEFAULT {0}", d);
                }
                else
                    sb.AppendFormat(" DEFAULT {0}", field.Default);
            }
            //else if (!onlyDefine && !field.PrimaryKey && !field.Nullable)
            //{
            //    //在定义语句中，该字段不允许空，而又没有默认值时，设置默认值
            //    if (!includeDefault || String.IsNullOrEmpty(field.Default))
            //    {
            //        if (tc == TypeCode.String)
            //            sb.AppendFormat(" DEFAULT ('{0}')", "");
            //        else if (tc == TypeCode.DateTime)
            //        {
            //            String d = SqlDateTime.MinValue.Value.ToString("yyyy-MM-dd HH:mm:ss");
            //            //d = "1900-01-01";
            //            sb.AppendFormat(" DEFAULT '{0}'", d);
            //        }
            //        else
            //            sb.AppendFormat(" DEFAULT {0}", "''");
            //    }
            //}

            return sb.ToString();
        }

        public override string CreateDatabaseSQL(string dbname, string file)
        {
            if (String.IsNullOrEmpty(file)) return String.Format("CREATE DATABASE {0}", FormatKeyWord(dbname));

            String logfile = String.Empty;
            if (!String.IsNullOrEmpty(file))
            {
                if ((file.Length < 2 || file[1] != Path.VolumeSeparatorChar))
                {
                    file = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, file);
                }
                file = String.Format("FILENAME = N'{0}' , ", file);
                logfile = file.Substring(0, file.Length - 3) + "ldf";
            }

            StringBuilder sb = new StringBuilder();

            sb.AppendFormat("CREATE DATABASE {0} ON  PRIMARY", FormatKeyWord(dbname));
            sb.AppendLine();
            sb.AppendFormat(@"( NAME = N'{0}_Data', {1}SIZE = 1024 , MAXSIZE = UNLIMITED, FILEGROWTH = 10%)", dbname, file);
            sb.AppendLine();
            sb.Append("LOG ON ");
            sb.AppendLine();
            sb.AppendFormat(@"( NAME = N'{0}_Log', {1}SIZE = 1024 , MAXSIZE = UNLIMITED, FILEGROWTH = 10%)", dbname, logfile);
            sb.AppendLine();

            return sb.ToString();
        }

        public override string DatabaseExistSQL(string dbname)
        {
            return String.Format("SELECT * FROM sysdatabases WHERE name = N'{0}'", dbname);
        }

        public override string CreateTableSQL(XTable table)
        {
            List<XField> Fields = new List<XField>(table.Fields);
            Fields.Sort(delegate(XField item1, XField item2) { return item1.ID.CompareTo(item2.ID); });

            StringBuilder sb = new StringBuilder();

            sb.AppendFormat("CREATE TABLE {0}(", FormatKeyWord(table.Name));
            List<String> keys = new List<string>();
            for (Int32 i = 0; i < Fields.Count; i++)
            {
                sb.AppendLine();
                sb.Append("\t");
                sb.Append(FieldClause(Fields[i], true));
                if (i < Fields.Count - 1) sb.Append(",");

                if (Fields[i].PrimaryKey) keys.Add(Fields[i].Name);
            }

            //主键
            if (keys.Count > 0)
            {
                sb.Append(",");
                sb.AppendLine();
                sb.Append("\t");
                sb.AppendFormat("CONSTRAINT [PK_{0}] PRIMARY KEY CLUSTERED", table.Name);
                sb.AppendLine();
                sb.Append("\t");
                sb.Append("(");
                for (Int32 i = 0; i < keys.Count; i++)
                {
                    sb.AppendLine();
                    sb.Append("\t\t");
                    sb.AppendFormat("{0} ASC", FormatKeyWord(keys[i]));
                    if (i < keys.Count - 1) sb.Append(",");
                }
                sb.AppendLine();
                sb.Append("\t");
                sb.Append(") ON [PRIMARY]");
            }

            sb.AppendLine();
            sb.Append(") ON [PRIMARY]");

            //注释
            if (!String.IsNullOrEmpty(table.Description))
            {
                String sql = AddTableDescriptionSQL(table.Name, table.Description);
                if (!String.IsNullOrEmpty(sql))
                {
                    sb.AppendLine(";");
                    sb.Append(sql);
                }
            }
            //字段注释
            foreach (XField item in table.Fields)
            {
                if (!String.IsNullOrEmpty(item.Description))
                {
                    sb.AppendLine(";");
                    sb.Append(AddColumnDescriptionSQL(table.Name, item.Name, item.Description));
                }
            }

            return sb.ToString();
        }

        public override string TableExistSQL(String tablename)
        {
            if (IsSQL2005)
                return String.Format("select * from sysobjects where xtype='U' and name='{0}'", tablename);
            else
                return String.Format("SELECT * FROM sysobjects WHERE id = OBJECT_ID(N'[dbo].{0}') AND OBJECTPROPERTY(id, N'IsUserTable') = 1", FormatKeyWord(tablename));
        }

        public override string AddTableDescriptionSQL(String tablename, String description)
        {
            return String.Format("EXEC dbo.sp_addextendedproperty @name=N'MS_Description', @value=N'{1}' , @level0type=N'{2}',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'{0}'", tablename, description, level0type);
        }

        public override string DropTableDescriptionSQL(String tablename)
        {
            return String.Format("EXEC dbo.sp_dropextendedproperty @name=N'MS_Description', @level0type=N'{1}',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'{0}'", tablename, level0type);
        }

        public override string AddColumnSQL(string tablename, XField field)
        {
            String sql = String.Format("Alter TABLE {0} Add {1}", FormatKeyWord(tablename), FieldClause(field, true));
            //if (!String.IsNullOrEmpty(field.Default)) sql += ";" + AddDefaultSQL(tablename, field.Name, field.Description);
            if (!String.IsNullOrEmpty(field.Description))
            {
                //AddColumnDescriptionSQL中会调用DropColumnDescriptionSQL，这里不需要了
                //sql += ";" + Environment.NewLine + DropColumnDescriptionSQL(tablename, field.Name);
                sql += ";" + Environment.NewLine + AddColumnDescriptionSQL(tablename, field.Name, field.Description);
            }
            return sql;
        }

        public override string AlterColumnSQL(string tablename, XField field)
        {
            String sql = String.Format("Alter Table {0} Alter Column {1}", FormatKeyWord(tablename), FieldClause(field, false));
            if (!String.IsNullOrEmpty(field.Default)) sql += ";" + Environment.NewLine + AddDefaultSQL(tablename, field);
            if (!String.IsNullOrEmpty(field.Description)) sql += ";" + Environment.NewLine + AddColumnDescriptionSQL(tablename, field.Name, field.Description);
            return sql;
        }

        public override string DropColumnSQL(string tablename, string columnname)
        {
            //删除默认值
            String sql = DeleteConstraintsSQL(tablename, columnname, null);
            if (!String.IsNullOrEmpty(sql)) sql += ";" + Environment.NewLine;

            //删除主键
            String sql2 = DeleteConstraintsSQL(tablename, columnname, "PK");
            if (!String.IsNullOrEmpty(sql2)) sql += sql2 + ";" + Environment.NewLine;

            sql += base.DropColumnSQL(tablename, columnname);
            return sql;
        }

        public override string AddColumnDescriptionSQL(String tablename, String columnname, String description)
        {
            String sql = DropColumnDescriptionSQL(tablename, columnname);
            if (!String.IsNullOrEmpty(sql)) sql += ";" + Environment.NewLine;
            sql += String.Format("EXEC dbo.sp_addextendedproperty @name=N'MS_Description', @value=N'{1}' , @level0type=N'{3}',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'{0}', @level2type=N'COLUMN',@level2name=N'{2}'", tablename, description, columnname, level0type);
            return sql;
        }

        public override string DropColumnDescriptionSQL(String tablename, String columnname)
        {
            //StringBuilder sb = new StringBuilder();
            //sb.Append("IF EXISTS (");
            //sb.AppendFormat("select * from syscolumns a inner join sysproperties g on a.id=g.id and a.colid=g.smallid and g.name='MS_Description' inner join sysobjects c on a.id=c.id where a.name='{1}' and c.name='{0}'", tablename, columnname);
            //sb.AppendLine(")");
            //sb.AppendLine("BEGIN");
            //sb.AppendFormat("EXEC dbo.sp_dropextendedproperty @name=N'MS_Description', @level0type=N'USER',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'{0}', @level2type=N'COLUMN',@level2name=N'{1}'", tablename, columnname);
            //sb.AppendLine();
            //sb.Append("END");
            //return sb.ToString();

            String sql = String.Format("select * from syscolumns a inner join sysproperties g on a.id=g.id and a.colid=g.smallid and g.name='MS_Description' inner join sysobjects c on a.id=c.id where a.name='{1}' and c.name='{0}'", tablename, columnname);
            Int32 count = QueryCount(sql);
            if (count <= 0) return null;

            return String.Format("EXEC dbo.sp_dropextendedproperty @name=N'MS_Description', @level0type=N'{2}',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'{0}', @level2type=N'COLUMN',@level2name=N'{1}'", tablename, columnname, level0type);
        }

        public override string AddDefaultSQL(string tablename, XField field)
        {
            String sql = DropDefaultSQL(tablename, field.Name);
            if (!String.IsNullOrEmpty(sql)) sql += ";" + Environment.NewLine;
            if (Type.GetTypeCode(field.DataType) == TypeCode.String)
                sql += String.Format("ALTER TABLE {0} ADD CONSTRAINT DF_{0}_{1} DEFAULT N'{2}' FOR {1}", tablename, field.Name, field.Default);
            else if (Type.GetTypeCode(field.DataType) == TypeCode.DateTime)
            {
                String dv = field.Default;
                if (!String.IsNullOrEmpty(dv) && dv.Equals("now()", StringComparison.OrdinalIgnoreCase)) dv = "getdate()";
                sql += String.Format("ALTER TABLE {0} ADD CONSTRAINT DF_{0}_{1} DEFAULT {2} FOR {1}", tablename, field.Name, dv);
            }
            else
                sql += String.Format("ALTER TABLE {0} ADD CONSTRAINT DF_{0}_{1} DEFAULT {2} FOR {1}", tablename, field.Name, field.Default);
            return sql;
        }

        public override string DropDefaultSQL(string tablename, string columnname)
        {
            return DeleteConstraintsSQL(tablename, columnname, "D");
        }

        /// <summary>
        /// 删除约束脚本。
        /// </summary>
        /// <param name="tablename"></param>
        /// <param name="columnname"></param>
        /// <param name="type">约束类型，默认值是D，如果未指定，则删除所有约束</param>
        /// <returns></returns>
        protected virtual String DeleteConstraintsSQL(String tablename, String columnname, String type)
        {
            String sql = null;
            if (IsSQL2005)
                sql = String.Format("select b.name from sys.tables a inner join sys.default_constraints b on a.object_id=b.parent_object_id inner join sys.columns c on a.object_id=c.object_id and b.parent_column_id=c.column_id where a.name='{0}' and c.name='{1}'", tablename, columnname);
            else
                sql = String.Format("select b.name from syscolumns a inner join sysobjects b on a.cdefault=b.id inner join sysobjects c on a.id=c.id where a.name='{1}' and c.name='{0}'", tablename, columnname);
            if (!String.IsNullOrEmpty(type)) sql += String.Format(" and b.xtype='{0}'", type);
            if (type == "PK") sql = String.Format("select c.name from sysobjects a inner join syscolumns b on a.id=b.id  inner join sysobjects c on c.parent_obj=a.id where a.name='{0}' and b.name='{1}' and c.xtype='PK'", tablename, columnname);
            DataSet ds = Query(sql);
            if (ds == null || ds.Tables == null || ds.Tables[0].Rows.Count < 1) return null;

            StringBuilder sb = new StringBuilder();
            foreach (DataRow dr in ds.Tables[0].Rows)
            {
                String name = dr[0].ToString();
                if (sb.Length > 0) sb.AppendLine(";");
                sb.AppendFormat("ALTER TABLE {0} DROP CONSTRAINT {1}", FormatKeyWord(tablename), name);
            }
            return sb.ToString();
        }

        public override String DropDatabaseSQL(String dbname)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("use master");
            sb.AppendLine(";");
            sb.AppendLine("declare   @spid   varchar(20),@dbname   varchar(20)");
            sb.AppendLine("declare   #spid   cursor   for");
            sb.AppendFormat("select   spid=cast(spid   as   varchar(20))   from   master..sysprocesses   where   dbid=db_id('{0}')", dbname);
            sb.AppendLine();
            sb.AppendLine("open   #spid");
            sb.AppendLine("fetch   next   from   #spid   into   @spid");
            sb.AppendLine("while   @@fetch_status=0");
            sb.AppendLine("begin");
            sb.AppendLine("exec('kill   '+@spid)");
            sb.AppendLine("fetch   next   from   #spid   into   @spid");
            sb.AppendLine("end");
            sb.AppendLine("close   #spid");
            sb.AppendLine("deallocate   #spid");
            sb.AppendLine(";");
            sb.AppendFormat("Drop Database {0}", FormatKeyWord(dbname));
            return sb.ToString();
        }
        #endregion
        #endregion
    }

    class SqlServer : DbBase<SqlServer, SqlServerSession>
    {
        #region 属性
        /// <summary>
        /// 返回数据库类型。外部DAL数据库类请使用Other
        /// </summary>
        public override DatabaseType DbType
        {
            get { return DatabaseType.SqlServer; }
        }

        /// <summary>工厂</summary>
        public override DbProviderFactory Factory
        {
            get { return SqlClientFactory.Instance; }
        }

        private Boolean? _IsSQL2005;
        /// <summary>是否SQL2005及以上</summary>
        public Boolean IsSQL2005
        {
            get
            {
                if (_IsSQL2005 == null)
                {
                    //切换到master库
                    DbSession session = CreateSession() as DbSession;
                    String dbname = session.DatabaseName;
                    //如果指定了数据库名，并且不是master，则切换到master
                    if (!String.IsNullOrEmpty(dbname) && !String.Equals(dbname, "master", StringComparison.OrdinalIgnoreCase))
                    {
                        session.DatabaseName = "master";
                    }

                    //取数据库版本
                    if (!session.Opened) session.Open();
                    String ver = session.Conn.ServerVersion;
                    session.AutoClose();

                    _IsSQL2005 = !ver.StartsWith("08");

                    if (!String.IsNullOrEmpty(dbname) && !String.Equals(dbname, "master", StringComparison.OrdinalIgnoreCase))
                    {
                        session.DatabaseName = dbname;
                    }
                }
                return _IsSQL2005.Value;
            }
            set { _IsSQL2005 = value; }
        }
        #endregion

        #region 分页
        /// <summary>
        /// 构造分页SQL
        /// </summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="startRowIndex">开始行，0开始</param>
        /// <param name="maximumRows">最大返回行数</param>
        /// <param name="keyColumn">唯一键。用于not in分页</param>
        /// <returns>分页SQL</returns>
        public override String PageSplit(string sql, Int32 startRowIndex, Int32 maximumRows, string keyColumn)
        {
            // 从第一行开始，不需要分页
            if (startRowIndex <= 0 && maximumRows < 1) return sql;

            // 指定了起始行，并且是SQL2005及以上版本，使用RowNumber算法
            if (startRowIndex > 0 && IsSQL2005) return PageSplitRowNumber(sql, startRowIndex, maximumRows, keyColumn);

            // 如果没有Order By，直接调用基类方法
            // 先用字符串判断，命中率高，这样可以提高处理效率
            if (!sql.Contains(" Order "))
            {
                if (!sql.ToLower().Contains(" order ")) return base.PageSplit(sql, startRowIndex, maximumRows, keyColumn);
            }
            //// 使用正则进行严格判断。必须包含Order By，并且它右边没有右括号)，表明有order by，且不是子查询的，才需要特殊处理
            //MatchCollection ms = Regex.Matches(sql, @"\border\s*by\b([^)]+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            //if (ms == null || ms.Count < 1 || ms[0].Index < 1)
            String sql2 = sql;
            String orderBy = CheckOrderClause(ref sql2);
            if (String.IsNullOrEmpty(orderBy))
            {
                return base.PageSplit(sql, startRowIndex, maximumRows, keyColumn);
            }
            // 已确定该sql最外层含有order by，再检查最外层是否有top。因为没有top的order by是不允许作为子查询的
            if (Regex.IsMatch(sql, @"^[^(]+\btop\b", RegexOptions.Compiled | RegexOptions.IgnoreCase))
            {
                return base.PageSplit(sql, startRowIndex, maximumRows, keyColumn);
            }
            //String orderBy = sql.Substring(ms[0].Index);

            // 从第一行开始，不需要分页
            if (startRowIndex <= 0)
            {
                if (maximumRows < 1)
                    return sql;
                else
                    return String.Format("Select Top {0} * From {1} {2}", maximumRows, CheckSimpleSQL(sql2), orderBy);
                //return String.Format("Select Top {0} * From {1} {2}", maximumRows, CheckSimpleSQL(sql.Substring(0, ms[0].Index)), orderBy);
            }

            #region Max/Min分页
            // 如果要使用max/min分页法，首先keyColumn必须有asc或者desc
            if (keyColumn.ToLower().EndsWith(" desc") || keyColumn.ToLower().EndsWith(" asc") || keyColumn.ToLower().EndsWith(" unknown"))
            {
                String str = PageSplitMaxMin(sql, startRowIndex, maximumRows, keyColumn);
                if (!String.IsNullOrEmpty(str)) return str;
                keyColumn = keyColumn.Substring(0, keyColumn.IndexOf(" "));
            }
            #endregion

            sql = CheckSimpleSQL(sql2);

            if (String.IsNullOrEmpty(keyColumn)) throw new ArgumentNullException("keyColumn", "这里用的not in分页算法要求指定主键列！");

            if (maximumRows < 1)
                sql = String.Format("Select * From {1} Where {2} Not In(Select Top {0} {2} From {1} {3}) {3}", startRowIndex, sql, keyColumn, orderBy);
            else
                sql = String.Format("Select Top {0} * From {1} Where {2} Not In(Select Top {3} {2} From {1} {4}) {4}", maximumRows, sql, keyColumn, startRowIndex, orderBy);
            return sql;
        }

        /// <summary>
        /// 已重写。获取分页
        /// </summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="startRowIndex">开始行，0开始</param>
        /// <param name="maximumRows">最大返回行数</param>
        /// <param name="keyColumn">主键列。用于not in分页</param>
        /// <returns></returns>
        public String PageSplitRowNumber(String sql, Int32 startRowIndex, Int32 maximumRows, String keyColumn)
        {
            // 从第一行开始，不需要分页
            if (startRowIndex <= 0)
            {
                if (maximumRows < 1)
                    return sql;
                else
                    return base.PageSplit(sql, startRowIndex, maximumRows, keyColumn);
            }

            String orderBy = String.Empty;
            if (sql.ToLower().Contains(" order "))
            {
                // 使用正则进行严格判断。必须包含Order By，并且它右边没有右括号)，表明有order by，且不是子查询的，才需要特殊处理
                //MatchCollection ms = Regex.Matches(sql, @"\border\s*by\b([^)]+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
                //if (ms != null && ms.Count > 0 && ms[0].Index > 0)
                String sql2 = sql;
                String orderBy2 = CheckOrderClause(ref sql2);
                if (String.IsNullOrEmpty(orderBy))
                {
                    // 已确定该sql最外层含有order by，再检查最外层是否有top。因为没有top的order by是不允许作为子查询的
                    if (!Regex.IsMatch(sql, @"^[^(]+\btop\b", RegexOptions.Compiled | RegexOptions.IgnoreCase))
                    {
                        //orderBy = sql.Substring(ms[0].Index).Trim();
                        //sql = sql.Substring(0, ms[0].Index).Trim();
                        orderBy = orderBy2.Trim();
                        sql = sql2.Trim();
                    }
                }
            }

            if (String.IsNullOrEmpty(orderBy)) orderBy = "Order By " + keyColumn;
            sql = CheckSimpleSQL(sql);

            //row_number()从1开始
            if (maximumRows < 1)
                sql = String.Format("Select * From (Select row_number() over({2}) as row_number, * From {1}) XCode_Temp_b Where row_Number>={0}", startRowIndex + 1, sql, orderBy);
            else
                sql = String.Format("Select * From (Select row_number() over({3}) as row_number, * From {1}) XCode_Temp_b Where row_Number Between {0} And {2}", startRowIndex + 1, sql, startRowIndex + maximumRows, orderBy);

            return sql;
        }
        #endregion

        #region 数据库特性
        /// <summary>
        /// 当前时间函数
        /// </summary>
        public override String DateTimeNow { get { return "getdate()"; } }

        /// <summary>
        /// 最小时间
        /// </summary>
        public override DateTime DateTimeMin { get { return SqlDateTime.MinValue.Value; } }

        /// <summary>
        /// 格式化时间为SQL字符串
        /// </summary>
        /// <param name="dateTime">时间值</param>
        /// <returns></returns>
        public override String FormatDateTime(DateTime dateTime)
        {
            return String.Format("'{0:yyyy-MM-dd HH:mm:ss}'", dateTime);
        }

        /// <summary>
        /// 格式化关键字
        /// </summary>
        /// <param name="keyWord">关键字</param>
        /// <returns></returns>
        public override String FormatKeyWord(String keyWord)
        {
            if (String.IsNullOrEmpty(keyWord)) throw new ArgumentNullException("keyWord");

            if (keyWord.StartsWith("[") && keyWord.EndsWith("]")) return keyWord;

            return String.Format("[{0}]", keyWord);
        }
        #endregion
    }
}