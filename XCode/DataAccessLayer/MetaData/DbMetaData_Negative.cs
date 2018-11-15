using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NewLife.Reflection;
using NewLife.Security;

namespace XCode.DataAccessLayer
{
    /* 反向工程层次结构：
     *  SetTables
     *      OnSetTables
     *          CheckDatabase
     *          CheckAllTables
     *              GetTables
     *              CheckTable
     *                  CreateTable
     *                      DDLSchema.CreateTable
     *                      DDLSchema.AddTableDescription
     *                      DDLSchema.AddColumnDescription
     *                      DDLSchema.CreateIndex
     *                  CheckColumnsChange
     *                      DDLSchema.AddColumn
     *                      DDLSchema.AddColumnDescription
     *                      DDLSchema.DropColumn
     *                      IsColumnChanged
     *                          DDLSchema.AlterColumn
     *                      IsColumnDefaultChanged
     *                          ChangeColmnDefault
     *                              DDLSchema.DropDefault
     *                              DDLSchema.AddDefault
     *                      DropColumnDescription
     *                      AddColumnDescription
     *                  =>SQLite.CheckColumnsChange
     *                      ReBuildTable
     *                          CreateTableSQL
     *                  CheckTableDescriptionAndIndex
     *                      DropTableDescription
     *                      AddTableDescription
     *                      DDLSchema.DropIndex
     *                      DDLSchema.CreateIndex
     */

    /* CreateTableSQL层次结构：
     *  CreateTableSQL
     *      FieldClause
     *          GetFieldType
     *              FindDataType
     *              GetFormatParam
     *                  GetFormatParamItem
     *          GetFieldConstraints
     *          GetFieldDefault
     *              CheckAndGetDefaultDateTimeNow
     */

    partial class DbMetaData
    {
        #region 属性
        private String ConnName => Database.ConnName;
        #endregion

        #region 反向工程
        /// <summary>设置表模型，检查数据表是否匹配表模型，反向工程</summary>
        /// <param name="mode">设置</param>
        /// <param name="tables"></param>
        public void SetTables(Migration mode, params IDataTable[] tables)
        {
            if (mode == Migration.Off) return;

            OnSetTables(tables, mode);
        }

        protected virtual void OnSetTables(IDataTable[] tables, Migration mode)
        {
            var dbExist = CheckDatabase(mode);

            CheckAllTables(tables, mode, dbExist);
        }

        Boolean? hasCheckedDatabase;
        private Boolean CheckDatabase(Migration mode)
        {
            if (hasCheckedDatabase != null) return hasCheckedDatabase.Value;

            //数据库检查
            var dbExist = false;
            try
            {
                dbExist = (Boolean)SetSchema(DDLSchema.DatabaseExist, null);
            }
            catch
            {
                // 如果异常，默认认为数据库存在
                dbExist = true;
            }

            if (!dbExist)
            {
                if (mode > Migration.ReadOnly)
                {
                    WriteLog("创建数据库：{0}", ConnName);
                    SetSchema(DDLSchema.CreateDatabase, null, null);

                    dbExist = true;
                }
                else
                {
                    var sql = GetSchemaSQL(DDLSchema.CreateDatabase, null, null);
                    if (String.IsNullOrEmpty(sql))
                        WriteLog("请为连接{0}创建数据库！", ConnName);
                    else
                        WriteLog("请为连接{0}创建数据库，使用以下语句：{1}", ConnName, Environment.NewLine + sql);
                }
            }

            hasCheckedDatabase = dbExist;
            return dbExist;
        }

        private void CheckAllTables(IDataTable[] tables, Migration mode, Boolean dbExit)
        {
            // 数据库表进入字典
            var dic = new Dictionary<String, IDataTable>(StringComparer.OrdinalIgnoreCase);
            if (dbExit)
            {
                var dbtables = OnGetTables(tables.Select(t => t.TableName).ToArray());
                if (dbtables != null && dbtables.Count > 0)
                {
                    foreach (var item in dbtables)
                    {
                        dic.Add(item.TableName, item);
                    }
                }
            }

            foreach (var item in tables)
            {
                try
                {
                    // 判断指定表是否存在于数据库中，以决定是创建表还是修改表
                    if (dic.TryGetValue(item.TableName, out var dbtable))
                        CheckTable(item, dbtable, mode);
                    else
                        CheckTable(item, null, mode);
                }
                catch (Exception ex)
                {
                    WriteLog(ex.ToString());
                }
            }
        }

        protected virtual void CheckTable(IDataTable entitytable, IDataTable dbtable, Migration mode)
        {
            var onlySql = mode <= Migration.ReadOnly;
            if (dbtable == null)
            {
                // 没有字段的表不创建
                if (entitytable.Columns.Count < 1) return;

                WriteLog("创建表：{0}({1})", entitytable.TableName, entitytable.Description);

                var sb = new StringBuilder();
                // 建表，如果不是onlySql，执行时DAL会输出SQL日志
                CreateTable(sb, entitytable, onlySql);

                // 仅获取语句
                if (onlySql) WriteLog("只检查不对数据库进行操作,请手工创建表：" + entitytable.TableName + Environment.NewLine + sb.ToString());
            }
            else
            {
                var noDelete = mode < Migration.Full;
                var sql = CheckColumnsChange(entitytable, dbtable, onlySql, noDelete);
                if (!String.IsNullOrEmpty(sql)) sql += ";";
                sql += CheckTableDescriptionAndIndex(entitytable, dbtable, mode);
                if (!sql.IsNullOrEmpty()) WriteLog("只检查不对数据库进行操作,请手工使用以下语句修改表：" + Environment.NewLine + sql);
            }
        }

        /// <summary>检查字段改变。某些数据库（如SQLite）没有添删改字段的DDL语法，可重载该方法，使用重建表方法ReBuildTable</summary>
        /// <param name="entitytable"></param>
        /// <param name="dbtable"></param>
        /// <param name="onlySql"></param>
        /// <param name="noDelete"></param>
        /// <returns></returns>
        protected virtual String CheckColumnsChange(IDataTable entitytable, IDataTable dbtable, Boolean onlySql, Boolean noDelete)
        {
            //var onlySql = mode <= Migration.ReadOnly;
            //var noDelete = mode < Migration.Full;

            var sb = new StringBuilder();
            var etdic = entitytable.Columns.ToDictionary(e => e.ColumnName.ToLower(), e => e, StringComparer.OrdinalIgnoreCase);
            var dbdic = dbtable.Columns.ToDictionary(e => e.ColumnName.ToLower(), e => e, StringComparer.OrdinalIgnoreCase);

            #region 新增列
            foreach (var item in entitytable.Columns)
            {
                if (!dbdic.ContainsKey(item.ColumnName.ToLower()))
                {
                    // 非空字段需要重建表
                    if (!item.Nullable)
                    {
                        //var sql = ReBuildTable(entitytable, dbtable);
                        //if (noDelete)
                        //{
                        //    WriteLog("数据表新增非空字段[{0}]，需要重建表，请手工执行：\r\n{1}", item.Name, sql);
                        //    return sql;
                        //}

                        //Database.CreateSession().Execute(sql);
                        //return String.Empty;

                        // 非空字段作为可空字段新增，避开重建表
                        item.Nullable = true;
                    }

                    PerformSchema(sb, onlySql, DDLSchema.AddColumn, item);
                    if (!item.Description.IsNullOrEmpty()) PerformSchema(sb, onlySql, DDLSchema.AddColumnDescription, item);
                }
            }
            #endregion

            #region 删除列
            var sbDelete = new StringBuilder();
            for (var i = dbtable.Columns.Count - 1; i >= 0; i--)
            {
                var item = dbtable.Columns[i];
                if (!etdic.ContainsKey(item.ColumnName.ToLower()))
                {
                    if (!String.IsNullOrEmpty(item.Description)) PerformSchema(sb, onlySql || noDelete, DDLSchema.DropColumnDescription, item);
                    PerformSchema(sbDelete, onlySql || noDelete, DDLSchema.DropColumn, item);
                }
            }
            if (sbDelete.Length > 0)
            {
                if (noDelete)
                {
                    // 不许删除列，显示日志
                    WriteLog("数据表中发现有多余字段，请手工执行以下语句删除：" + Environment.NewLine + sbDelete);
                }
                else
                {
                    if (sb.Length > 0) sb.AppendLine(";");
                    sb.Append(sbDelete.ToString());
                }
            }
            #endregion

            #region 修改列
            // 开发时的实体数据库
            var entityDb = DbFactory.Create(entitytable.DbType);

            foreach (var item in entitytable.Columns)
            {
                if (!dbdic.TryGetValue(item.ColumnName, out var dbf)) continue;

                if (IsColumnTypeChanged(item, dbf))
                {
                    WriteLog("字段{0}.{1}类型需要由数据库的{2}改变为实体的{3}", entitytable.Name, item.Name, dbf.DataType, item.DataType);
                    PerformSchema(sb, noDelete, DDLSchema.AlterColumn, item, dbf);
                }
                if (IsColumnChanged(item, dbf, entityDb)) PerformSchema(sb, noDelete, DDLSchema.AlterColumn, item, dbf);

                if (item.Description + "" != dbf.Description + "")
                {
                    // 先删除旧注释
                    //if (dbf.Description != null) PerformSchema(sb, noDelete, DDLSchema.DropColumnDescription, dbf);

                    // 加上新注释
                    if (!item.Description.IsNullOrEmpty()) PerformSchema(sb, onlySql, DDLSchema.AddColumnDescription, item);
                }
            }
            #endregion

            return sb.ToString();
        }

        /// <summary>检查表说明和索引</summary>
        /// <param name="entitytable"></param>
        /// <param name="dbtable"></param>
        /// <param name="mode"></param>
        /// <returns></returns>
        protected virtual String CheckTableDescriptionAndIndex(IDataTable entitytable, IDataTable dbtable, Migration mode)
        {
            var onlySql = mode <= Migration.ReadOnly;
            var noDelete = mode < Migration.Full;

            var sb = new StringBuilder();

            #region 表说明
            if (entitytable.Description + "" != dbtable.Description + "")
            {
                //// 先删除旧注释
                //if (!String.IsNullOrEmpty(dbtable.Description)) PerformSchema(sb, onlySql, DDLSchema.DropTableDescription, dbtable);

                // 加上新注释
                if (!String.IsNullOrEmpty(entitytable.Description)) PerformSchema(sb, onlySql, DDLSchema.AddTableDescription, entitytable);
            }
            #endregion

            #region 删除索引
            var dbdis = dbtable.Indexes;
            if (dbdis != null)
            {
                foreach (var item in dbdis.ToArray())
                {
                    // 计算的索引不需要删除
                    //if (item.Computed) continue;

                    // 主键的索引不能删
                    if (item.PrimaryKey) continue;

                    var di = ModelHelper.GetIndex(entitytable, item.Columns);
                    if (di != null && di.Unique == item.Unique) continue;

                    PerformSchema(sb, noDelete, DDLSchema.DropIndex, item);
                    dbdis.Remove(item);
                }
            }
            #endregion

            #region 新增索引
            var edis = entitytable.Indexes;
            if (edis != null)
            {
                foreach (var item in edis.ToArray())
                {
                    if (item.PrimaryKey) continue;

                    var di = ModelHelper.GetIndex(dbtable, item.Columns);
                    // 计算出来的索引，也表示没有，需要创建
                    if (di != null && di.Unique == item.Unique) continue;
                    //// 如果这个索引的唯一字段是主键，则无需建立索引
                    //if (item.Columns.Length == 1 && entitytable.GetColumn(item.Columns[0]).PrimaryKey) continue;
                    // 如果索引全部就是主键，无需创建索引
                    if (entitytable.GetColumns(item.Columns).All(e => e.PrimaryKey)) continue;

                    PerformSchema(sb, onlySql, DDLSchema.CreateIndex, item);

                    if (di == null)
                        edis.Add(item.Clone(dbtable));
                    //else
                    //    di.Computed = false;
                }
            }
            #endregion

            if (!onlySql) return null;

            return sb.ToString();
        }

        /// <summary>检查字段是否有改变，除了默认值和备注以外</summary>
        /// <param name="entityColumn"></param>
        /// <param name="dbColumn"></param>
        /// <param name="entityDb"></param>
        /// <returns></returns>
        protected virtual Boolean IsColumnChanged(IDataColumn entityColumn, IDataColumn dbColumn, IDatabase entityDb)
        {
            // 自增、主键、非空等，不再认为是字段修改，减轻反向工程复杂度
            //if (entityColumn.Identity != dbColumn.Identity) return true;
            //if (entityColumn.PrimaryKey != dbColumn.PrimaryKey) return true;
            //if (entityColumn.Nullable != dbColumn.Nullable && !entityColumn.Identity && !entityColumn.PrimaryKey) return true;

            // 是否已改变
            var isChanged = false;

            //仅针对字符串类型比较长度
            if (!isChanged && entityColumn.DataType == typeof(String) && entityColumn.Length != dbColumn.Length)
            {
                isChanged = true;

                //如果是大文本类型，长度可能不等
                if ((entityColumn.Length > Database.LongTextLength || entityColumn.Length <= 0) &&
                    (entityDb != null && dbColumn.Length > entityDb.LongTextLength || dbColumn.Length <= 0)) isChanged = false;
            }

            return isChanged;
        }

        protected virtual Boolean IsColumnTypeChanged(IDataColumn entityColumn, IDataColumn dbColumn)
        {
            var type = entityColumn.DataType;
            if (type.IsEnum) type = typeof(Int32);
            if (type == dbColumn.DataType) return false;

            //// 整型不做改变
            //if (type.IsInt() && dbColumn.DataType.IsInt()) return false;

            // 类型不匹配，不一定就是有改变，还要查找类型对照表是否有匹配的，只要存在任意一个匹配，就说明是合法的
            foreach (var item in FieldTypeMaps)
            {
                //if (entityColumn.DataType == item.Key && dbColumn.DataType == item.Value) return false;
                // 把不常用的类型映射到常用类型，比如数据库SByte映射到实体类Byte，UInt32映射到Int32，而不需要重新修改数据库
                if (dbColumn.DataType == item.Key && type == item.Value) return false;
            }

            return true;
        }

        protected virtual String ReBuildTable(IDataTable entitytable, IDataTable dbtable)
        {
            // 通过重建表的方式修改字段
            var tableName = dbtable.TableName;
            var tempTableName = "Temp_" + tableName + "_" + Rand.Next(1000, 10000);
            tableName = FormatName(tableName);
            tempTableName = FormatName(tempTableName);

            // 每个分号后面故意加上空格，是为了让DbMetaData执行SQL时，不要按照分号加换行来拆分这个SQL语句
            var sb = new StringBuilder();
            sb.AppendLine("BEGIN TRANSACTION; ");
            sb.Append(RenameTable(tableName, tempTableName));
            sb.AppendLine("; ");
            sb.Append(CreateTableSQL(entitytable));
            sb.AppendLine("; ");

            // 如果指定了新列和旧列，则构建两个集合
            if (entitytable.Columns != null && entitytable.Columns.Count > 0 && dbtable.Columns != null && dbtable.Columns.Count > 0)
            {
                var sbName = new StringBuilder();
                var sbValue = new StringBuilder();
                foreach (var item in entitytable.Columns)
                {
                    var name = item.ColumnName;
                    var fname = FormatName(name);
                    var type = item.DataType;
                    var field = dbtable.GetColumn(item.ColumnName);
                    if (field == null)
                    {
                        // 如果新增了不允许空的列，则处理一下默认值
                        if (!item.Nullable)
                        {
                            if (type == typeof(String))
                            {
                                if (sbName.Length > 0) sbName.Append(", ");
                                if (sbValue.Length > 0) sbValue.Append(", ");
                                sbName.Append(fname);
                                sbValue.Append("''");
                            }
                            else if (type == typeof(Int16) || type == typeof(Int32) || type == typeof(Int64) ||
                                type == typeof(Single) || type == typeof(Double) || type == typeof(Decimal))
                            {
                                if (sbName.Length > 0) sbName.Append(", ");
                                if (sbValue.Length > 0) sbValue.Append(", ");
                                sbName.Append(fname);
                                sbValue.Append("0");
                            }
                            else if (type == typeof(DateTime))
                            {
                                if (sbName.Length > 0) sbName.Append(", ");
                                if (sbValue.Length > 0) sbValue.Append(", ");
                                sbName.Append(fname);
                                sbValue.Append(Database.FormatDateTime(DateTime.MinValue));
                            }
                            else if (type == typeof(Boolean))
                            {
                                if (sbName.Length > 0) sbName.Append(", ");
                                if (sbValue.Length > 0) sbValue.Append(", ");
                                sbName.Append(fname);
                                sbValue.Append(Database.FormatValue(item, false));
                            }
                        }
                    }
                    else
                    {
                        if (sbName.Length > 0) sbName.Append(", ");
                        if (sbValue.Length > 0) sbValue.Append(", ");
                        sbName.Append(fname);
                        // 处理一下非空默认值
                        if (field.Nullable && !item.Nullable)
                        {
                            if (type == typeof(String))
                                sbValue.Append("ifnull({0}, \'\')".F(fname));
                            else if (type == typeof(Int16) || type == typeof(Int32) || type == typeof(Int64) ||
                               type == typeof(Single) || type == typeof(Double) || type == typeof(Decimal))
                                sbValue.Append("ifnull({0}, 0)".F(fname));
                            else if (type == typeof(DateTime))
                                sbValue.Append("ifnull({0}, {1})".F(fname, Database.FormatDateTime(DateTime.MinValue)));
                        }
                        else
                        {
                            //sbValue.Append(fname);

                            // 处理字符串不允许空，ntext不支持+""
                            if (type == typeof(String) && !item.Nullable && item.Length > 0 && item.Length < Database.LongTextLength)
                                sbValue.Append(Database.StringConcat(fname, "\'\'"));
                            else
                                sbValue.Append(fname);
                        }
                    }
                }
                sb.AppendFormat("Insert Into {0}({2}) Select {3} From {1}", tableName, tempTableName, sbName.ToString(), sbValue.ToString());
            }
            else
            {
                sb.AppendFormat("Insert Into {0} Select * From {1}", tableName, tempTableName);
            }
            sb.AppendLine("; ");
            sb.AppendFormat("Drop Table {0}", tempTableName);
            sb.AppendLine("; ");
            sb.Append("COMMIT;");

            return sb.ToString();
        }

        protected virtual String RenameTable(String tableName, String tempTableName) => String.Format("Alter Table {0} Rename To {1}", tableName, tempTableName);

        /// <summary>
        /// 获取架构语句，该执行的已经执行。
        /// 如果取不到语句，则输出日志信息；
        /// 如果不是纯语句，则执行；
        /// </summary>
        /// <param name="sb"></param>
        /// <param name="onlySql"></param>
        /// <param name="schema"></param>
        /// <param name="values"></param>
        protected Boolean PerformSchema(StringBuilder sb, Boolean onlySql, DDLSchema schema, params Object[] values)
        {
            var sql = GetSchemaSQL(schema, values);
            if (!String.IsNullOrEmpty(sql))
            {
                if (sb.Length > 0) sb.AppendLine(";");
                sb.Append(sql);
            }
            else if (sql == null)
            {
                // 只有null才表示通过非SQL的方式处理，而String.Empty表示已经通过别的SQL处理，这里不用输出日志

                // 没办法形成SQL，输出日志信息
                var s = new StringBuilder();
                if (values != null && values.Length > 0)
                {
                    foreach (var item in values)
                    {
                        if (s.Length > 0) s.Append(" ");
                        s.Append(item);
                    }
                }

                IDataColumn dc = null;
                IDataTable dt = null;
                if (values != null && values.Length > 0)
                {
                    dc = values[0] as IDataColumn;
                    dt = values[0] as IDataTable;
                }

                switch (schema)
                {
                    case DDLSchema.AddTableDescription:
                        WriteLog("{0}({1},{2})", schema, dt.TableName, dt.Description);
                        break;
                    case DDLSchema.DropTableDescription:
                        WriteLog("{0}({1})", schema, dt);
                        break;
                    case DDLSchema.AddColumn:
                        WriteLog("{0}({1})", schema, dc);
                        break;
                    //case DDLSchema.AlterColumn:
                    //    break;
                    case DDLSchema.DropColumn:
                        WriteLog("{0}({1})", schema, dc.ColumnName);
                        break;
                    case DDLSchema.AddColumnDescription:
                        WriteLog("{0}({1},{2})", schema, dc.ColumnName, dc.Description);
                        break;
                    case DDLSchema.DropColumnDescription:
                        WriteLog("{0}({1})", schema, dc.ColumnName);
                        break;
                    default:
                        WriteLog("修改表：{0} {1}", schema.ToString(), s.ToString());
                        break;
                }
                //WriteLog("修改表：{0} {1}", schema.ToString(), s.ToString());
            }

            if (!onlySql)
            {
                try
                {
                    SetSchema(schema, values);
                }
                catch (Exception ex)
                {
                    WriteLog("修改表{0}失败！{1}", schema.ToString(), ex.Message);
                    return false;
                }
            }

            return true;
        }

        protected virtual void CreateTable(StringBuilder sb, IDataTable table, Boolean onlySql)
        {
            // 创建表失败后，不再处理注释和索引
            if (!PerformSchema(sb, onlySql, DDLSchema.CreateTable, table)) return;

            // 加上表注释
            if (!String.IsNullOrEmpty(table.Description)) PerformSchema(sb, onlySql, DDLSchema.AddTableDescription, table);

            // 加上字段注释
            foreach (var item in table.Columns)
            {
                if (!String.IsNullOrEmpty(item.Description)) PerformSchema(sb, onlySql, DDLSchema.AddColumnDescription, item);
            }

            // 加上索引
            if (table.Indexes != null)
            {
                foreach (var item in table.Indexes)
                {
                    if (item.PrimaryKey) continue;
                    // 如果索引全部就是主键，无需创建索引
                    if (table.GetColumns(item.Columns).All(e => e.PrimaryKey)) continue;

                    PerformSchema(sb, onlySql, DDLSchema.CreateIndex, item);
                }
            }
        }
        #endregion

        #region 数据定义
        /// <summary>获取数据定义语句</summary>
        /// <param name="schema">数据定义模式</param>
        /// <param name="values">其它信息</param>
        /// <returns></returns>
        public virtual String GetSchemaSQL(DDLSchema schema, params Object[] values)
        {
            switch (schema)
            {
                case DDLSchema.CreateDatabase:
                    return CreateDatabaseSQL((String)values[0], (String)values[1]);
                //case DDLSchema.DropDatabase:
                //    return DropDatabaseSQL((String)values[0]);
                case DDLSchema.DatabaseExist:
                    return DatabaseExistSQL(values == null || values.Length < 1 ? null : (String)values[0]);
                case DDLSchema.CreateTable:
                    return CreateTableSQL((IDataTable)values[0]);
                //case DDLSchema.DropTable:
                //    if (values[0] is IDataTable)
                //        return DropTableSQL((IDataTable)values[0]);
                //    else
                //        return DropTableSQL(values[0].ToString());
                //case DDLSchema.TableExist:
                //    if (values[0] is IDataTable)
                //        return TableExistSQL((IDataTable)values[0]);
                //    else
                //        return TableExistSQL(values[0].ToString());
                case DDLSchema.AddTableDescription:
                    return AddTableDescriptionSQL((IDataTable)values[0]);
                case DDLSchema.DropTableDescription:
                    return DropTableDescriptionSQL((IDataTable)values[0]);
                case DDLSchema.AddColumn:
                    return AddColumnSQL((IDataColumn)values[0]);
                case DDLSchema.AlterColumn:
                    return AlterColumnSQL((IDataColumn)values[0], values.Length > 1 ? (IDataColumn)values[1] : null);
                case DDLSchema.DropColumn:
                    return DropColumnSQL((IDataColumn)values[0]);
                case DDLSchema.AddColumnDescription:
                    return AddColumnDescriptionSQL((IDataColumn)values[0]);
                case DDLSchema.DropColumnDescription:
                    return DropColumnDescriptionSQL((IDataColumn)values[0]);
                case DDLSchema.CreateIndex:
                    return CreateIndexSQL((IDataIndex)values[0]);
                case DDLSchema.DropIndex:
                    return DropIndexSQL((IDataIndex)values[0]);
                //case DDLSchema.CompactDatabase:
                //    return CompactDatabaseSQL();
                default:
                    break;
            }

            throw new NotSupportedException("不支持该操作！");
        }

        /// <summary>设置数据定义模式</summary>
        /// <param name="schema">数据定义模式</param>
        /// <param name="values">其它信息</param>
        /// <returns></returns>
        public virtual Object SetSchema(DDLSchema schema, params Object[] values)
        {
            //Object obj = null;
            //switch (schema)
            //{
            //    case DDLSchema.CreateTable:
            //        //if (MetaDataCollections.Contains(_.Databases))
            //        //{

            //        //}
            //        break;
            //    case DDLSchema.TableExist:
            //        {
            //            String name;
            //            if (values[0] is IDataTable)
            //                name = (values[0] as IDataTable).TableName;
            //            else
            //                name = values[0].ToString();

            //            var dt = GetSchema(_.Tables, new String[] { null, null, name, "TABLE" });
            //            if (dt == null || dt.Rows == null || dt.Rows.Count < 1) return false;
            //            return true;
            //        }
            //    case DDLSchema.BackupDatabase:
            //        return Backup((String)values[0], (String)values[1], (Boolean)values[2]);
            //    default:
            //        break;
            //}

            var sql = GetSchemaSQL(schema, values);
            if (String.IsNullOrEmpty(sql)) return null;

            var session = Database.CreateSession();

            if (/*schema == DDLSchema.TableExist ||*/ schema == DDLSchema.DatabaseExist) return session.QueryCount(sql) > 0;

            // 分隔符是分号加换行，如果不想被拆开执行（比如有事务），可以在分号和换行之间加一个空格
            var ss = sql.Split(";" + Environment.NewLine);
            if (ss == null || ss.Length < 1) return session.Execute(sql);

            foreach (var item in ss)
            {
                session.Execute(item);
            }
            return 0;
        }

        /// <summary>字段片段</summary>
        /// <param name="field">字段</param>
        /// <param name="onlyDefine">仅仅定义。定义操作才允许设置自增和使用默认值</param>
        /// <returns></returns>
        public virtual String FieldClause(IDataColumn field, Boolean onlyDefine)
        {
            var sb = new StringBuilder();

            //字段名
            sb.AppendFormat("{0} ", FormatName(field.ColumnName));

            String typeName = null;
            // 如果还是原来的数据库类型，则直接使用
            //if (Database.DbType == field.Table.DbType) typeName = field.RawType;
            // 每种数据库的自增差异太大，理应由各自处理，而不采用原始值
            if (Database.Type == field.Table.DbType && !field.Identity) typeName = field.RawType;

            if (String.IsNullOrEmpty(typeName)) typeName = GetFieldType(field);

            sb.Append(typeName);

            // 约束
            sb.Append(GetFieldConstraints(field, onlyDefine));

            return sb.ToString();
        }

        /// <summary>取得字段约束</summary>
        /// <param name="field">字段</param>
        /// <param name="onlyDefine">仅仅定义</param>
        /// <returns></returns>
        protected virtual String GetFieldConstraints(IDataColumn field, Boolean onlyDefine)
        {
            if (field.PrimaryKey && field.Table.PrimaryKeys.Length < 2) return " Primary Key";

            // 是否为空
            return field.Nullable ? " NULL" : " NOT NULL";
        }
        #endregion

        #region 数据定义语句
        public virtual String CreateDatabaseSQL(String dbname, String file) => $"Create Database {FormatName(dbname)}";

        public virtual String DropDatabaseSQL(String dbname) => $"Drop Database {FormatName(dbname)}";

        public virtual String DatabaseExistSQL(String dbname) => null;

        public virtual String CreateTableSQL(IDataTable table)
        {
            var fs = new List<IDataColumn>(table.Columns);

            var sb = new StringBuilder();

            sb.AppendFormat("Create Table {0}(", FormatName(table.TableName));
            for (var i = 0; i < fs.Count; i++)
            {
                sb.AppendLine();
                sb.Append("\t");
                sb.Append(FieldClause(fs[i], true));
                if (i < fs.Count - 1) sb.Append(",");
            }
            sb.AppendLine();
            sb.Append(")");

            return sb.ToString();
        }

        public virtual String DropTableSQL(IDataTable table) => $"Drop Table {FormatName(table.TableName)}";

        public virtual String TableExistSQL(IDataTable table) => throw new NotSupportedException("该功能未实现！");

        public virtual String AddTableDescriptionSQL(IDataTable table) => null;

        public virtual String DropTableDescriptionSQL(IDataTable table) => null;

        public virtual String AddColumnSQL(IDataColumn field) => $"Alter Table {FormatName(field.Table.TableName)} Add {FieldClause(field, true)}";

        public virtual String AlterColumnSQL(IDataColumn field, IDataColumn oldfield) => $"Alter Table {FormatName(field.Table.TableName)} Alter Column {FieldClause(field, false)}";

        public virtual String DropColumnSQL(IDataColumn field) => $"Alter Table {FormatName(field.Table.TableName)} Drop Column {field.ColumnName}";

        public virtual String AddColumnDescriptionSQL(IDataColumn field) => null;

        public virtual String DropColumnDescriptionSQL(IDataColumn field) => null;

        public virtual String CreateIndexSQL(IDataIndex index)
        {
            var sb = new StringBuilder();
            if (index.Unique)
                sb.Append("Create Unique Index ");
            else
                sb.Append("Create Index ");

            sb.Append(FormatName(index.Name));
            sb.AppendFormat(" On {0} (", FormatName(index.Table.TableName));
            for (var i = 0; i < index.Columns.Length; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append(FormatName(index.Columns[i]));
            }
            sb.Append(")");

            return sb.ToString();
        }

        public virtual String DropIndexSQL(IDataIndex index) => $"Drop Index {FormatName(index.Name)} On {FormatName(index.Table.TableName)}";

        //public virtual String CompactDatabaseSQL() => null;
        #endregion

        #region 操作
        public virtual String Backup(String dbname, String bakfile, Boolean compressed) => throw new NotImplementedException();

        public virtual Int32 CompactDatabase() => throw new NotImplementedException();
        #endregion
    }
}