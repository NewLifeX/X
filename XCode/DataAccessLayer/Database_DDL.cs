using System;
using System.Collections.Generic;
using System.Text;

namespace XCode.DataAccessLayer
{
    partial class Database
    {
        #region 数据定义
        /// <summary>
        /// 获取数据定义语句
        /// </summary>
        /// <param name="schema">数据定义模式</param>
        /// <param name="values">其它信息</param>
        /// <returns></returns>
        public virtual String GetSchemaSQL(DDLSchema schema, params Object[] values)
        {
            switch (schema)
            {
                case DDLSchema.CreateDatabase:
                    return CreateDatabaseSQL((String)values[0], (String)values[1]);
                case DDLSchema.DropDatabase:
                    return DropDatabaseSQL((String)values[0]);
                case DDLSchema.DatabaseExist:
                    return DatabaseExistSQL(values == null || values.Length < 1 ? null : (String)values[0]);
                case DDLSchema.CreateTable:
                    return CreateTableSQL((XTable)values[0]);
                case DDLSchema.DropTable:
                    return DropTableSQL((String)values[0]);
                case DDLSchema.TableExist:
                    return TableExistSQL((String)values[0]);
                case DDLSchema.AddTableDescription:
                    return AddTableDescriptionSQL((String)values[0], (String)values[1]);
                case DDLSchema.DropTableDescription:
                    return DropTableDescriptionSQL((String)values[0]);
                case DDLSchema.AddColumn:
                    return AddColumnSQL((String)values[0], (XField)values[1]);
                case DDLSchema.AlterColumn:
                    return AlterColumnSQL((String)values[0], (XField)values[1]);
                case DDLSchema.DropColumn:
                    return DropColumnSQL((String)values[0], (String)values[1]);
                case DDLSchema.AddColumnDescription:
                    return AddColumnDescriptionSQL((String)values[0], (String)values[1], (String)values[2]);
                case DDLSchema.DropColumnDescription:
                    return DropColumnDescriptionSQL((String)values[0], (String)values[1]);
                case DDLSchema.AddDefault:
                    return AddDefaultSQL((String)values[0], (XField)values[1]);
                case DDLSchema.DropDefault:
                    return DropDefaultSQL((String)values[0], (String)values[1]);
                default:
                    break;
            }

            throw new NotSupportedException("不支持该操作！");
        }

        /// <summary>
        /// 设置数据定义模式
        /// </summary>
        /// <param name="schema">数据定义模式</param>
        /// <param name="values">其它信息</param>
        /// <returns></returns>
        public virtual Object SetSchema(DDLSchema schema, params Object[] values)
        {
            String sql = GetSchemaSQL(schema, values);
            if (String.IsNullOrEmpty(sql)) return null;

            if (schema == DDLSchema.TableExist || schema == DDLSchema.DatabaseExist)
            {
                return QueryCount(sql) > 0;
            }
            else
            {
                String[] ss = sql.Split(new String[] { ";" + Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
                if (ss == null || ss.Length < 1)
                    return Execute(sql);
                else
                {
                    foreach (String item in ss)
                    {
                        Execute(item);
                    }
                    return 0;
                }
            }
        }

        /// <summary>
        /// 字段片段
        /// </summary>
        /// <param name="field"></param>
        /// <param name="onlyDefine">仅仅定义。定义操作才允许设置自增和使用默认值</param>
        /// <returns></returns>
        public virtual String FieldClause(XField field, Boolean onlyDefine)
        {
            StringBuilder sb = new StringBuilder();

            //字段名
            //sb.AppendFormat("[{0}] ", field.Name);
            sb.AppendFormat("{0} ", FormatKeyWord(field.Name));

            String typeName = null;
            // 如果还是原来的数据库类型，则直接使用
            if (DbType == field.Table.DbType) typeName = field.RawType;

            #region 类型
            TypeCode tc = Type.GetTypeCode(field.DataType);
            //switch (tc)
            //{
            //    case TypeCode.Boolean:
            //        sb.Append("bit");
            //        break;
            //    case TypeCode.Byte:
            //        sb.Append("byte");
            //        break;
            //    case TypeCode.Char:
            //        sb.Append("bit");
            //        break;
            //    case TypeCode.DBNull:
            //        break;
            //    case TypeCode.DateTime:
            //        sb.Append("datetime");
            //        break;
            //    case TypeCode.Decimal:
            //        sb.AppendFormat("NUMERIC({0},{1})", field.Length, field.Digit);
            //        break;
            //    case TypeCode.Double:
            //        sb.Append("double");
            //        break;
            //    case TypeCode.Empty:
            //        break;
            //    case TypeCode.Int16:
            //    case TypeCode.UInt16:
            //        if (onlyDefine && field.Identity)
            //            sb.Append("AUTOINCREMENT(1,1)");
            //        else
            //            sb.Append("short");
            //        break;
            //    case TypeCode.Int32:
            //    case TypeCode.Int64:
            //    case TypeCode.UInt32:
            //    case TypeCode.UInt64:
            //        if (onlyDefine && field.Identity)
            //            sb.Append("AUTOINCREMENT(1,1)");
            //        else
            //            sb.Append("Long");
            //        break;
            //    case TypeCode.Object:
            //        if (field.DataType == typeof(Byte[]))
            //        {
            //            sb.Append("Binary");
            //            break;
            //        }
            //        break;
            //    case TypeCode.SByte:
            //        sb.Append("byte");
            //        break;
            //    case TypeCode.Single:
            //        sb.Append("real");
            //        break;
            //    case TypeCode.String:
            //        Int32 len = field.Length;
            //        if (len < 1) len = 50;
            //        if (len > 255)
            //            sb.Append("Memo ");
            //        else
            //            sb.AppendFormat("Text({0}) ", len);
            //        break;
            //    default:
            //        break;
            //}
            #endregion

            if (field.PrimaryKey)
            {
                sb.Append(" Primary Key");
            }
            else
            {
                //是否为空
                //if (!field.Nullable) sb.Append(" NOT NULL");
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
                    sb.AppendFormat(" DEFAULT '{0}'", field.Default);
                else if (tc == TypeCode.DateTime)
                {
                    String d = field.Default;
                    //if (String.Equals(d, "getdate()", StringComparison.OrdinalIgnoreCase)) d = "now()";
                    if (String.Equals(d, "getdate()", StringComparison.OrdinalIgnoreCase)) d =Meta.DateTimeNow;
                    sb.AppendFormat(" DEFAULT {0}", d);
                }
                else
                    sb.AppendFormat(" DEFAULT {0}", field.Default);
            }
            //else if (onlyDefine && !field.PrimaryKey && !field.Nullable)
            //{
            //    //该字段不允许空，而又没有默认值时，设置默认值
            //    if (!includeDefault || String.IsNullOrEmpty(field.Default))
            //    {
            //        if (tc == TypeCode.String)
            //            sb.AppendFormat(" DEFAULT ('{0}')", "");
            //        else if (tc == TypeCode.DateTime)
            //        {
            //            String d = SqlDateTime.MinValue.Value.ToString("yyyy-MM-dd HH:mm:ss");
            //            sb.AppendFormat(" DEFAULT {0}", d);
            //        }
            //        else
            //            sb.AppendFormat(" DEFAULT {0}", "''");
            //    }
            //}

            return sb.ToString();
        }
        #endregion

        #region 数据定义语句
        public virtual String CreateDatabaseSQL(String dbname, String file)
        {
            return null;
        }

        public virtual String DropDatabaseSQL(String dbname)
        {
            return String.Format("Drop Database {0}", FormatKeyWord(dbname));
        }

        public virtual String DatabaseExistSQL(String dbname)
        {
            throw new NotSupportedException("该功能未实现！");
        }

        public virtual String CreateTableSQL(XTable table)
        {
            List<XField> Fields = new List<XField>(table.Fields);
            Fields.Sort(delegate(XField item1, XField item2) { return item1.ID.CompareTo(item2.ID); });

            StringBuilder sb = new StringBuilder();

            sb.AppendFormat("CREATE TABLE {0}(", FormatKeyWord(table.Name));
            //List<String> keys = new List<string>();
            for (Int32 i = 0; i < Fields.Count; i++)
            {
                sb.AppendLine();
                sb.Append("\t");
                sb.Append(FieldClause(Fields[i], true));
                if (i < Fields.Count - 1) sb.Append(",");

                //if (Fields[i].PrimaryKey) keys.Add(Fields[i].Name);
            }
            sb.AppendLine();
            sb.Append(")");

            ////默认值
            //foreach (XField item in Fields)
            //{
            //    if (!String.IsNullOrEmpty(item.Default))
            //    {
            //        sb.AppendLine(";");
            //        sb.Append(AlterColumnSQL(table.Name, item));
            //    }
            //}

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

            return sb.ToString();
        }

        public virtual String DropTableSQL(String tablename)
        {
            return String.Format("Drop Table {0}", FormatKeyWord(tablename));
        }

        public virtual String TableExistSQL(String tablename)
        {
            throw new NotSupportedException("该功能未实现！");
        }

        public virtual String AddTableDescriptionSQL(String tablename, String description)
        {
            return null;
        }

        public virtual String DropTableDescriptionSQL(String tablename)
        {
            return null;
        }

        public virtual String AddColumnSQL(String tablename, XField field)
        {
            return String.Format("Alter Table {0} Add {1}", FormatKeyWord(tablename), FieldClause(field, true));
        }

        public virtual String AlterColumnSQL(String tablename, XField field)
        {
            return String.Format("Alter Table {0} Alter Column {1}", FormatKeyWord(tablename), FieldClause(field, false));
        }

        public virtual String DropColumnSQL(String tablename, String columnname)
        {
            return String.Format("Alter Table {0} Drop Column {1}", FormatKeyWord(tablename), columnname);
        }

        public virtual String AddColumnDescriptionSQL(String tablename, String columnname, String description)
        {
            return null;
        }

        public virtual String DropColumnDescriptionSQL(String tablename, String columnname)
        {
            return null;
        }

        public virtual String AddDefaultSQL(String tablename, XField field)
        {
            return null;
        }

        public virtual String DropDefaultSQL(String tablename, String columnname)
        {
            return null;
        }
        #endregion
    }
}