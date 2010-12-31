using System;
using System.Data;
using System.Text;
using System.Text.RegularExpressions;

namespace XCode.DataAccessLayer
{
    /// <summary>
    /// Sql2005数据库
    /// </summary>
    internal class SqlServer2005Session : SqlServerSession
    {
        #region 构架
        #region 取得字段信息的SQL模版
        private String _SchemaSql = "";
        /// <summary>
        /// 构架SQL
        /// </summary>
        public override String SchemaSql
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
                    sb.Append("left join sys.extended_properties g on a.id=g.major_id and a.colid=g.minor_id and g.name = 'MS_Description'  ");
                    //sb.Append("where d.name='{0}' ");
                    sb.Append("order by a.id,a.colorder");
                    _SchemaSql = sb.ToString();
                }
                return _SchemaSql;
            }
        }

        private String _DescriptionSql = "select b.name n, a.value v from sys.extended_properties a inner join sysobjects b on a.major_id=b.id and a.minor_id=0 and a.name = 'MS_Description'";
        /// <summary>
        /// 取表说明SQL
        /// </summary>
        public override String DescriptionSql { get { return _DescriptionSql; } }
        #endregion

        #region 数据定义
        public override string DatabaseExistSQL(string dbname)
        {
            return String.Format("SELECT * FROM sys.databases WHERE name = N'{0}'", dbname);
        }

        //public override string CreateTableSQL(XTable table)
        //{
        //    List<XField> Fields = new List<XField>(table.Fields);
        //    Fields.Sort(delegate(XField item1, XField item2) { return item1.ID.CompareTo(item2.ID); });

        //    StringBuilder sb = new StringBuilder();

        //    sb.AppendFormat("CREATE TABLE [dbo].[{0}](", table.Name);
        //    List<String> keys = new List<string>();
        //    for (Int32 i = 0; i < Fields.Count; i++)
        //    {
        //        sb.AppendLine();
        //        sb.Append("\t");
        //        sb.Append(FieldClause(Fields[i], true));
        //        if (i < Fields.Count - 1) sb.Append(",");

        //        if (Fields[i].PrimaryKey) keys.Add(Fields[i].Name);
        //    }

        //    //主键
        //    if (keys.Count > 0)
        //    {
        //        sb.Append(",");
        //        sb.AppendLine();
        //        sb.Append("\t");
        //        sb.AppendFormat("CONSTRAINT [PK_{0}] PRIMARY KEY CLUSTERED", table.Name);
        //        sb.AppendLine();
        //        sb.Append("\t");
        //        sb.Append("(");
        //        for (Int32 i = 0; i < keys.Count; i++)
        //        {
        //            sb.AppendLine();
        //            sb.Append("\t\t");
        //            sb.AppendFormat("[{0}] ASC", keys[i]);
        //            if (i < keys.Count - 1) sb.Append(",");
        //        }
        //        sb.AppendLine();
        //        sb.Append("\t");
        //        sb.Append(")WITH (PAD_INDEX  = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]");
        //    }

        //    sb.AppendLine();
        //    sb.Append(") ON [PRIMARY]");

        //    //注释
        //    if (!String.IsNullOrEmpty(table.Description))
        //    {
        //        String sql = AddTableDescriptionSQL(table.Name, table.Description);
        //        if (!String.IsNullOrEmpty(sql))
        //        {
        //            sb.AppendLine(";");
        //            sb.Append(sql);
        //        }
        //    }
        //    //字段注释
        //    foreach (XField item in table.Fields)
        //    {
        //        if (!String.IsNullOrEmpty(item.Description))
        //        {
        //            sb.AppendLine(";");
        //            sb.Append(AddColumnDescriptionSQL(table.Name, item.Name, item.Description));
        //        }
        //    }

        //    return sb.ToString();
        //}

        public override string TableExistSQL(String tablename)
        {
            return String.Format("select * from sysobjects where xtype='U' and name='{0}'", tablename);
        }

        public override string AddTableDescriptionSQL(String tablename, String description)
        {
            return String.Format("EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'{1}' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'{0}'", tablename, description);
        }

        public override string DropTableDescriptionSQL(String tablename)
        {
            return String.Format("EXEC sys.sp_dropextendedproperty @name=N'MS_Description', @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'{0}'", tablename);
        }

        //public override string AddColumn(String tablename, XField field)
        //{
        //    return String.Format("ALTER TABLE [{0}] ADD {1}", tablename, FieldClause(field, true));
        //}

        //public override string AlterColumn(String tablename, XField field)
        //{
        //    return String.Format("ALTER TABLE [{0}] ALTER {1}", tablename, FieldClause(field, true));
        //}

        //public override string DropColumn(String tablename, String columnname)
        //{
        //    return String.Format("ALTER TABLE [{0}] DROP COLUMN {1}", tablename, columnname);
        //}

        public override string AddColumnDescriptionSQL(String tablename, String columnname, String description)
        {
            String sql = DropColumnDescriptionSQL(tablename, columnname);
            if (!String.IsNullOrEmpty(sql)) sql += ";" + Environment.NewLine;
            sql += String.Format("EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'{1}' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'{0}', @level2type=N'COLUMN',@level2name=N'{2}'", tablename, description, columnname);
            return sql;
        }

        public override string DropColumnDescriptionSQL(String tablename, String columnname)
        {
            //StringBuilder sb = new StringBuilder();
            //sb.Append("IF EXISTS (");
            //sb.AppendFormat("select * from syscolumns a inner join sys.extended_properties g on a.id=g.major_id and a.colid=g.minor_id and g.name = 'MS_Description' inner join sysobjects c on a.id=c.id where a.name='{1}' and c.name='{0}'", tablename, columnname);
            //sb.AppendLine(")");
            //sb.AppendLine("BEGIN");
            //sb.AppendFormat("EXEC sys.sp_dropextendedproperty @name=N'MS_Description', @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'{0}', @level2type=N'COLUMN',@level2name=N'{1}'", tablename, columnname);
            //sb.AppendLine();
            //sb.Append("END");
            //return sb.ToString();

            String sql = String.Format("select * from syscolumns a inner join sys.extended_properties g on a.id=g.major_id and a.colid=g.minor_id and g.name = 'MS_Description' inner join sysobjects c on a.id=c.id where a.name='{1}' and c.name='{0}'", tablename, columnname);
            Int32 count = QueryCount(sql);
            if (count <= 0) return null;

            return String.Format("EXEC sys.sp_dropextendedproperty @name=N'MS_Description', @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'{0}', @level2type=N'COLUMN',@level2name=N'{1}'", tablename, columnname);
        }

        /// <summary>
        /// 删除约束脚本。
        /// </summary>
        /// <param name="tablename"></param>
        /// <param name="columnname"></param>
        /// <param name="type">约束类型，默认值是D，如果未指定，则删除所有约束</param>
        /// <returns></returns>
        protected override String DeleteConstraintsSQL(String tablename, String columnname, String type)
        {
            String sql = String.Format("select b.name from sys.tables a inner join sys.default_constraints b on a.object_id=b.parent_object_id inner join sys.columns c on a.object_id=c.object_id and b.parent_column_id=c.column_id where a.name='{0}' and c.name='{1}'", tablename, columnname);
            if (!String.IsNullOrEmpty(type)) sql += String.Format(" and b.type='{0}'", type);
            if (type == "PK") sql = String.Format("select c.name from sysobjects a inner join syscolumns b on a.id=b.id  inner join sysobjects c on c.parent_obj=a.id where a.name='{0}' and b.name='{1}' and c.xtype='PK'", tablename, columnname);
            DataSet ds = Query(sql);
            if (ds == null || ds.Tables == null || ds.Tables[0].Rows.Count < 1) return null;

            StringBuilder sb = new StringBuilder();
            foreach (DataRow dr in ds.Tables[0].Rows)
            {
                String name = dr[0].ToString();
                if (sb.Length > 0) sb.AppendLine(";");
                sb.AppendFormat("ALTER TABLE {0} DROP CONSTRAINT {1}", tablename, name);
            }
            return sb.ToString();
        }
        #endregion
        #endregion
    }

    class SqlServer2005 : SqlServer
    {
        #region 属性
        /// <summary>
        /// 返回数据库类型。外部DAL数据库类请使用Other
        /// </summary>
        public override DatabaseType DbType
        {
            get { return DatabaseType.SqlServer2005; }
        }
        #endregion

        #region 分页
        /// <summary>
        /// 已重写。获取分页
        /// </summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="startRowIndex">开始行，0开始</param>
        /// <param name="maximumRows">最大返回行数</param>
        /// <param name="keyColumn">主键列。用于not in分页</param>
        /// <returns></returns>
        public override String PageSplit(String sql, Int32 startRowIndex, Int32 maximumRows, String keyColumn)
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
                MatchCollection ms = Regex.Matches(sql, @"\border\s*by\b([^)]+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
                if (ms != null && ms.Count > 0 && ms[0].Index > 0)
                {
                    // 已确定该sql最外层含有order by，再检查最外层是否有top。因为没有top的order by是不允许作为子查询的
                    if (!Regex.IsMatch(sql, @"^[^(]+\btop\b", RegexOptions.Compiled | RegexOptions.IgnoreCase))
                    {
                        orderBy = sql.Substring(ms[0].Index).Trim();
                        sql = sql.Substring(0, ms[0].Index).Trim();
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
    }
}