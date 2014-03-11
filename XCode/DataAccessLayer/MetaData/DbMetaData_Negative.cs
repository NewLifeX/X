using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

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
        private String ConnName { get { return Database.ConnName; } }
        #endregion

        #region 反向工程
        /// <summary>设置表模型，检查数据表是否匹配表模型，反向工程</summary>
        /// <param name="tables"></param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("请改用多参数版本！")]
        public void SetTables(params IDataTable[] tables)
        {
            var set = new NegativeSetting();
            set.CheckOnly = DAL.NegativeCheckOnly;
            set.NoDelete = DAL.NegativeNoDelete;
            OnSetTables(tables, set);
        }

        /// <summary>设置表模型，检查数据表是否匹配表模型，反向工程</summary>
        /// <param name="setting">设置</param>
        /// <param name="tables"></param>
        public void SetTables(NegativeSetting setting, params IDataTable[] tables)
        {
            OnSetTables(tables, setting);
        }

        protected virtual void OnSetTables(IDataTable[] tables, NegativeSetting setting)
        {
            CheckDatabase(setting);

            CheckAllTables(tables, setting);
        }

        Boolean hasCheckedDatabase;
        private void CheckDatabase(NegativeSetting setting)
        {
            if (hasCheckedDatabase) return;
            hasCheckedDatabase = true;

            //数据库检查
            Boolean dbExist = true;
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
                if (!setting.CheckOnly)
                {
                    WriteLog("创建数据库：{0}", ConnName);
                    SetSchema(DDLSchema.CreateDatabase, null, null);
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
        }

        private void CheckAllTables(IDataTable[] tables, NegativeSetting setting)
        {
            // 数据库表进入字典
            var dic = new Dictionary<String, IDataTable>(StringComparer.OrdinalIgnoreCase);
            var dbtables = OnGetTables(new HashSet<String>(tables.Select(t => t.TableName), StringComparer.OrdinalIgnoreCase));
            if (dbtables != null && dbtables.Count > 0)
            {
                foreach (var item in dbtables)
                {
                    dic.Add(item.TableName, item);
                }
            }

            foreach (var item in tables)
            {
                try
                {
                    // 判断指定表是否存在于数据库中，以决定是创建表还是修改表
                    IDataTable dbtable = null;
                    if (dic.TryGetValue(item.TableName, out dbtable))
                        CheckTable(item, dbtable, setting);
                    else
                        CheckTable(item, null, setting);
                }
                catch (Exception ex)
                {
                    WriteLog(ex.ToString());
                }
            }
        }

        protected virtual void CheckTable(IDataTable entitytable, IDataTable dbtable, NegativeSetting setting)
        {
            //Boolean onlySql = DAL.NegativeCheckOnly;

            if (dbtable == null)
            {
                // 没有字段的表不创建
                if (entitytable.Columns.Count < 1) return;

                #region 创建表
                WriteLog("创建表：{0}({1})", entitytable.TableName, entitytable.Description);

                var sb = new StringBuilder();
                // 建表，如果不是onlySql，执行时DAL会输出SQL日志
                CreateTable(sb, entitytable, setting.CheckOnly);

                // 仅获取语句
                //if (onlySql) WriteLog("XCode.Negative.Enable没有设置为True，请手工创建表：" + entitytable.Name + Environment.NewLine + sb.ToString());
                if (setting.CheckOnly) WriteLog("只检查不对数据库进行操作,请手工创建表：" + entitytable.TableName + Environment.NewLine + sb.ToString());
                #endregion
            }
            else
            {
                #region 修改表
                String sql = CheckColumnsChange(entitytable, dbtable, setting);
                if (!String.IsNullOrEmpty(sql)) sql += ";";
                sql += CheckTableDescriptionAndIndex(entitytable, dbtable, setting.CheckOnly);
                if (!String.IsNullOrEmpty(sql) && setting.CheckOnly)
                {
                    WriteLog("只检查不对数据库进行操作,请手工使用以下语句修改表：" + Environment.NewLine + sql);
                }
                #endregion
            }
        }

        /// <summary>检查字段改变。某些数据库（如SQLite）没有添删改字段的DDL语法，可重载该方法，使用重建表方法ReBuildTable</summary>
        /// <param name="entitytable"></param>
        /// <param name="dbtable"></param>
        /// <param name="setting"></param>
        /// <returns></returns>
        protected virtual String CheckColumnsChange(IDataTable entitytable, IDataTable dbtable, NegativeSetting setting)
        {
            #region 准备工作
            var onlySql = setting.CheckOnly;
            var sql = String.Empty;
            var sb = new StringBuilder();
            var entitydic = new Dictionary<String, IDataColumn>(StringComparer.OrdinalIgnoreCase);
            if (entitytable.Columns != null)
            {
                foreach (var item in entitytable.Columns)
                {
                    entitydic.Add(item.ColumnName.ToLower(), item);
                }
            }
            var dbdic = new Dictionary<String, IDataColumn>(StringComparer.OrdinalIgnoreCase);
            if (dbtable.Columns != null)
            {
                foreach (var item in dbtable.Columns)
                {
                    dbdic.Add(item.ColumnName.ToLower(), item);
                }
            }
            #endregion

            #region 新增列
            foreach (IDataColumn item in entitytable.Columns)
            {
                if (!dbdic.ContainsKey(item.ColumnName.ToLower()))
                {
                    //AddColumn(sb, item, onlySql);
                    PerformSchema(sb, onlySql, DDLSchema.AddColumn, item);
                    if (!String.IsNullOrEmpty(item.Description)) PerformSchema(sb, onlySql, DDLSchema.AddColumnDescription, item);

                    //! 以下已经不需要了，目前只有SQLite会采用重建表的方式添加删除字段。如果这里提前添加了字段，重建表的时候，会导致失败。

                    //// 这里必须给dbtable加加上当前列，否则下面如果刚好有删除列的话，会导致增加列成功，然后删除列重建表的时候没有新加的列
                    //dbtable.Columns.Add(item.Clone(dbtable));
                }
            }
            #endregion

            #region 删除列
            var sbDelete = new StringBuilder();
            for (int i = dbtable.Columns.Count - 1; i >= 0; i--)
            {
                var item = dbtable.Columns[i];
                if (!entitydic.ContainsKey(item.ColumnName.ToLower()))
                {
                    if (!String.IsNullOrEmpty(item.Description)) PerformSchema(sb, onlySql, DDLSchema.DropColumnDescription, item);
                    PerformSchema(sbDelete, setting.NoDelete, DDLSchema.DropColumn, item);
                }
            }
            if (sbDelete.Length > 0)
            {
                if (setting.NoDelete)
                {
                    //不许删除列，显示日志
                    WriteLog("数据表中发现有多余字段，请手工执行以下语句删除：" + Environment.NewLine + sbDelete.ToString());
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
                IDataColumn dbf = null;
                if (!dbdic.TryGetValue(item.ColumnName, out dbf)) continue;

                if (IsColumnTypeChanged(item, dbf))
                {
                    WriteLog("字段{0}.{1}类型需要由{2}改变为{3}", entitytable.Name, item.Name, dbf.DataType, item.DataType);
                    PerformSchema(sb, onlySql, DDLSchema.AlterColumn, item, dbf);
                }
                if (IsColumnChanged(item, dbf, entityDb)) PerformSchema(sb, onlySql, DDLSchema.AlterColumn, item, dbf);
                if (IsColumnDefaultChanged(item, dbf, entityDb)) ChangeColmnDefault(sb, onlySql, item, dbf, entityDb);

                if (item.Description + "" != dbf.Description + "")
                {
                    // 先删除旧注释
                    //if (!String.IsNullOrEmpty(dbf.Description)) DropColumnDescription(sb, dbf, onlySql);
                    //if (!String.IsNullOrEmpty(dbf.Description)) PerformSchema(sb, onlySql, DDLSchema.DropColumnDescription, dbf);
                    if (dbf.Description != null) PerformSchema(sb, onlySql, DDLSchema.DropColumnDescription, dbf);

                    // 加上新注释
                    if (!String.IsNullOrEmpty(item.Description)) PerformSchema(sb, onlySql, DDLSchema.AddColumnDescription, item);
                }
            }
            #endregion

            return sb.ToString();
        }

        /// <summary>检查表说明和索引</summary>
        /// <param name="entitytable"></param>
        /// <param name="dbtable"></param>
        /// <param name="onlySql"></param>
        /// <returns></returns>
        protected virtual String CheckTableDescriptionAndIndex(IDataTable entitytable, IDataTable dbtable, Boolean onlySql)
        {
            var sb = new StringBuilder();

            #region 表说明
            if (entitytable.Description + "" != dbtable.Description + "")
            {
                // 先删除旧注释
                //if (!String.IsNullOrEmpty(dbtable.Description)) DropTableDescription(sb, dbtable, onlySql);
                if (!String.IsNullOrEmpty(dbtable.Description)) PerformSchema(sb, onlySql, DDLSchema.DropTableDescription, dbtable);

                // 加上新注释
                //if (!String.IsNullOrEmpty(entitytable.Description)) AddTableDescription(sb, entitytable, onlySql);
                if (!String.IsNullOrEmpty(entitytable.Description)) PerformSchema(sb, onlySql, DDLSchema.AddTableDescription, entitytable);
            }
            #endregion

            #region 删除索引
            if (dbtable.Indexes != null)
            {
                for (int i = dbtable.Indexes.Count - 1; i >= 0; i--)
                {
                    var item = dbtable.Indexes[i];
                    // 计算的索引不需要删除
                    if (item.Computed) continue;

                    // 主键的索引不能删
                    if (item.PrimaryKey) continue;

                    var di = ModelHelper.GetIndex(entitytable, item.Columns);
                    if (di != null && di.Unique == item.Unique) continue;

                    PerformSchema(sb, onlySql, DDLSchema.DropIndex, item);
                    dbtable.Indexes.RemoveAt(i);
                }
            }
            #endregion

            #region 新增索引
            if (entitytable.Indexes != null)
            {
                foreach (var item in entitytable.Indexes)
                {
                    if (item.PrimaryKey) continue;

                    var di = ModelHelper.GetIndex(dbtable, item.Columns);
                    // 计算出来的索引，也表示没有，需要创建
                    if (di != null && di.Unique == item.Unique && !di.Computed) continue;
                    //// 如果这个索引的唯一字段是主键，则无需建立索引
                    //if (item.Columns.Length == 1 && entitytable.GetColumn(item.Columns[0]).PrimaryKey) continue;
                    // 如果索引全部就是主键，无需创建索引
                    if (entitytable.GetColumns(item.Columns).All(e => e.PrimaryKey)) continue;

                    PerformSchema(sb, onlySql, DDLSchema.CreateIndex, item);

                    if (di == null)
                        dbtable.Indexes.Add(item.Clone(dbtable));
                    else
                        di.Computed = false;
                }
            }
            #endregion

            return sb.ToString();
        }

        /// <summary>检查字段是否有改变，除了默认值和备注以外</summary>
        /// <param name="entityColumn"></param>
        /// <param name="dbColumn"></param>
        /// <param name="entityDb"></param>
        /// <returns></returns>
        protected virtual Boolean IsColumnChanged(IDataColumn entityColumn, IDataColumn dbColumn, IDatabase entityDb)
        {
            if (entityColumn.Identity != dbColumn.Identity) return true;
            if (entityColumn.PrimaryKey != dbColumn.PrimaryKey) return true;
            if (entityColumn.Nullable != dbColumn.Nullable && !entityColumn.Identity && !entityColumn.PrimaryKey) return true;

            // 是否已改变
            Boolean isChanged = false;

            ////比较类型/允许空/主键
            //if (entityColumn.DataType != dbColumn.DataType ||
            //    entityColumn.Identity != dbColumn.Identity ||
            //    entityColumn.PrimaryKey != dbColumn.PrimaryKey ||
            //    entityColumn.Nullable != dbColumn.Nullable && !entityColumn.Identity && !entityColumn.PrimaryKey)
            //{
            //    isChanged = true;
            //}

            //仅针对字符串类型比较长度
            if (!isChanged && Type.GetTypeCode(entityColumn.DataType) == TypeCode.String && entityColumn.Length != dbColumn.Length)
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
            if (entityColumn.DataType == dbColumn.DataType) return false;

            // 类型不匹配，不一定就是有改变，还要查找类型对照表是否有匹配的，只要存在任意一个匹配，就说明是合法的
            foreach (var item in FieldTypeMaps)
            {
                if (entityColumn.DataType == item.Key && dbColumn.DataType == item.Value) return false;
            }

            return true;
        }

        /// <summary>检查字段默认值是否有改变</summary>
        /// <param name="entityColumn"></param>
        /// <param name="dbColumn"></param>
        /// <param name="entityDb"></param>
        /// <returns></returns>
        protected virtual Boolean IsColumnDefaultChanged(IDataColumn entityColumn, IDataColumn dbColumn, IDatabase entityDb)
        {
            // 是否已改变
            Boolean isChanged = false;

            //比较默认值
            isChanged = !(entityColumn.Default + "").EqualIgnoreCase(dbColumn.Default + "");

            if (isChanged && !String.IsNullOrEmpty(entityColumn.Default) && !String.IsNullOrEmpty(dbColumn.Default))
            {
                var tc = Type.GetTypeCode(entityColumn.DataType);
                // 特殊处理时间
                if (tc == TypeCode.DateTime)
                {
                    // 如果当前默认值是开发数据库的时间默认值，则判断当前数据库的时间默认值
                    if (entityDb.DateTimeNow == entityColumn.Default && Database.DateTimeNow == dbColumn.Default) isChanged = false;
                }
                // 特殊处理Guid
                else if (tc == TypeCode.String)
                {
                    if (entityDb.NewGuid == entityColumn.Default && Database.NewGuid == dbColumn.Default) isChanged = false;
                }
                // 如果字段类型是Guid，不需要设置默认值，则也说明是Guid字段
                else if (entityColumn.DataType == typeof(Guid))
                {
                    if ((entityDb.NewGuid == entityColumn.Default || String.IsNullOrEmpty(entityColumn.Default)) && Database.NewGuid == dbColumn.Default) isChanged = false;
                }
            }

            return isChanged;
        }

        /// <summary>改变字段默认值。这里仅仅默认处理了时间日期，如果需要兼容多数据库，子类需要重载</summary>
        /// <param name="sb"></param>
        /// <param name="onlySql"></param>
        /// <param name="entityColumn"></param>
        /// <param name="dbColumn"></param>
        /// <param name="entityDb"></param>
        protected virtual void ChangeColmnDefault(StringBuilder sb, Boolean onlySql, IDataColumn entityColumn, IDataColumn dbColumn, IDatabase entityDb)
        {
            // 如果数据库存在默认值，则删除
            if (!String.IsNullOrEmpty(dbColumn.Default))
                PerformSchema(sb, onlySql, DDLSchema.DropDefault, dbColumn);

            // 如果实体存在默认值，则增加
            if (!String.IsNullOrEmpty(entityColumn.Default))
            {
                var tc = Type.GetTypeCode(entityColumn.DataType);
                String dv = entityColumn.Default;
                // 特殊处理时间
                if (tc == TypeCode.DateTime)
                {
                    if (dv == entityDb.DateTimeNow) entityColumn.Default = Database.DateTimeNow;
                }
                // 特殊处理Guid
                else if (tc == TypeCode.String || entityColumn.DataType == typeof(Guid))
                {
                    if (dv == entityDb.NewGuid) entityColumn.Default = Database.NewGuid;
                }
                // 如果字段类型是Guid，不需要设置默认值，则也说明是Guid字段
                else if (tc == TypeCode.String)
                {
                    if (dv == entityDb.NewGuid || String.IsNullOrEmpty(dv)) entityColumn.Default = Database.NewGuid;
                }

                PerformSchema(sb, onlySql, DDLSchema.AddDefault, entityColumn);

                // 还原
                entityColumn.Default = dv;
            }
        }

        protected virtual String ReBuildTable(IDataTable entitytable, IDataTable dbtable)
        {
            // 通过重建表的方式修改字段
            String tableName = dbtable.TableName;
            String tempTableName = "Temp_" + tableName + "_" + new Random((Int32)DateTime.Now.Ticks).Next(1000, 10000);
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
                    String name = item.ColumnName;
                    var field = dbtable.GetColumn(item.ColumnName);
                    if (field == null)
                    {
                        // 如果新增了不允许空的列，则处理一下默认值
                        if (!item.Nullable)
                        {
                            if (item.DataType == typeof(String))
                            {
                                if (sbName.Length > 0) sbName.Append(", ");
                                if (sbValue.Length > 0) sbValue.Append(", ");
                                sbName.Append(FormatName(name));
                                sbValue.Append("''");
                            }
                            else if (item.DataType == typeof(Int16) || item.DataType == typeof(Int32) || item.DataType == typeof(Int64) ||
                                item.DataType == typeof(Single) || item.DataType == typeof(Double) || item.DataType == typeof(Decimal))
                            {
                                if (sbName.Length > 0) sbName.Append(", ");
                                if (sbValue.Length > 0) sbValue.Append(", ");
                                sbName.Append(FormatName(name));
                                sbValue.Append("0");
                            }
                            else if (item.DataType == typeof(DateTime))
                            {
                                if (sbName.Length > 0) sbName.Append(", ");
                                if (sbValue.Length > 0) sbValue.Append(", ");
                                sbName.Append(FormatName(name));
                                sbValue.Append(Database.FormatDateTime(Database.DateTimeMin));
                            }
                        }
                    }
                    else
                    {
                        if (sbName.Length > 0) sbName.Append(", ");
                        if (sbValue.Length > 0) sbValue.Append(", ");
                        sbName.Append(FormatName(name));
                        //sbValue.Append(FormatName(name));

                        // 处理字符串不允许空
                        if (item.DataType == typeof(String) && !item.Nullable)
                            sbValue.Append(Database.StringConcat(FormatName(name), "\'\'"));
                        else
                            sbValue.Append(FormatName(name));
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

        protected virtual String RenameTable(String tableName, String tempTableName)
        {
            return String.Format("Alter Table {0} Rename To {1}", tableName, tempTableName);
        }

        /// <summary>
        /// 获取架构语句，该执行的已经执行。
        /// 如果取不到语句，则输出日志信息；
        /// 如果不是纯语句，则执行；
        /// </summary>
        /// <param name="sb"></param>
        /// <param name="onlySql"></param>
        /// <param name="schema"></param>
        /// <param name="values"></param>
        protected void PerformSchema(StringBuilder sb, Boolean onlySql, DDLSchema schema, params Object[] values)
        {
            var sql = GetSchemaSQL(schema, values);
            if (!String.IsNullOrEmpty(sql))
            {
                if (sb.Length > 0) sb.AppendLine(";");
                sb.Append(sql);

                //if (!onlySql) XTrace.WriteLine("修改表：" + sql);
            }
            else if (sql == null)
            {
                // 只有null才表示通过非SQL的方式处理，而String.Empty表示已经通过别的SQL处理，这里不用输出日志

                // 没办法形成SQL，输出日志信息
                var s = new StringBuilder();
                if (values != null && values.Length > 0)
                {
                    foreach (Object item in values)
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
                    //case DDLSchema.CreateDatabase:
                    //    break;
                    //case DDLSchema.DropDatabase:
                    //    break;
                    //case DDLSchema.DatabaseExist:
                    //    break;
                    //case DDLSchema.CreateTable:
                    //    break;
                    //case DDLSchema.DropTable:
                    //    break;
                    //case DDLSchema.TableExist:
                    //    break;
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
                    case DDLSchema.AddDefault:
                        WriteLog("{0}({1},{2})", schema, dc.ColumnName, dc.Default);
                        break;
                    case DDLSchema.DropDefault:
                        WriteLog("{0}({1})", schema, dc.ColumnName);
                        break;
                    //case DDLSchema.CreateIndex:
                    //    break;
                    //case DDLSchema.DropIndex:
                    //    break;
                    //case DDLSchema.BackupDatabase:
                    //    break;
                    //case DDLSchema.RestoreDatabase:
                    //    break;
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
                }
            }
        }

        protected virtual void CreateTable(StringBuilder sb, IDataTable table, Boolean onlySql)
        {
            PerformSchema(sb, onlySql, DDLSchema.CreateTable, table);

            // 加上表注释
            //if (!String.IsNullOrEmpty(table.Description)) AddTableDescription(sb, table, onlySql);
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
                    if (item.PrimaryKey || item.Computed) continue;
                    //// 如果这个索引的唯一字段是主键，则无需建立索引
                    //if (item.Columns.Length == 1 && table.GetColumn(item.Columns[0]).PrimaryKey) continue;
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
                case DDLSchema.DropDatabase:
                    return DropDatabaseSQL((String)values[0]);
                case DDLSchema.DatabaseExist:
                    return DatabaseExistSQL(values == null || values.Length < 1 ? null : (String)values[0]);
                case DDLSchema.CreateTable:
                    return CreateTableSQL((IDataTable)values[0]);
                case DDLSchema.DropTable:
                    if (values[0] is IDataTable)
                        return DropTableSQL((IDataTable)values[0]);
                    else
                        return DropTableSQL(values[0].ToString());
                case DDLSchema.TableExist:
                    if (values[0] is IDataTable)
                        return TableExistSQL((IDataTable)values[0]);
                    else
                        return TableExistSQL(values[0].ToString());
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
                case DDLSchema.AddDefault:
                    return AddDefaultSQL((IDataColumn)values[0]);
                case DDLSchema.DropDefault:
                    return DropDefaultSQL((IDataColumn)values[0]);
                case DDLSchema.CreateIndex:
                    return CreateIndexSQL((IDataIndex)values[0]);
                case DDLSchema.DropIndex:
                    return DropIndexSQL((IDataIndex)values[0]);
                case DDLSchema.CompactDatabase:
                    return CompactDatabaseSQL();
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
            switch (schema)
            {
                case DDLSchema.CreateTable:
                    if (MetaDataCollections.Contains(_.Databases))
                    {

                    }
                    break;
                case DDLSchema.TableExist:
                    {
                        String name;
                        if (values[0] is IDataTable)
                            name = (values[0] as IDataTable).TableName;
                        else
                            name = values[0].ToString();

                        var dt = GetSchema(_.Tables, new String[] { null, null, name, "TABLE" });
                        if (dt == null || dt.Rows == null || dt.Rows.Count < 1) return false;
                        return true;
                    }
                case DDLSchema.BackupDatabase:
                    {
                        Backup((String)values[0], (String)values[1]);
                        return true;
                    }
                default:
                    break;
            }

            var sql = GetSchemaSQL(schema, values);
            if (String.IsNullOrEmpty(sql)) return null;

            var session = Database.CreateSession();

            if (schema == DDLSchema.TableExist || schema == DDLSchema.DatabaseExist) return session.QueryCount(sql) > 0;

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
            if (Database.DbType == field.Table.DbType && !field.Identity) typeName = field.RawType;

            if (String.IsNullOrEmpty(typeName)) typeName = GetFieldType(field);

            sb.Append(typeName);

            // 约束
            sb.Append(GetFieldConstraints(field, onlyDefine));

            //默认值
            sb.Append(GetFieldDefault(field, onlyDefine));

            return sb.ToString();
        }

        /// <summary>取得字段约束</summary>
        /// <param name="field">字段</param>
        /// <param name="onlyDefine">仅仅定义</param>
        /// <returns></returns>
        protected virtual String GetFieldConstraints(IDataColumn field, Boolean onlyDefine)
        {
            if (field.PrimaryKey && field.Table.PrimaryKeys.Length < 2)
            {
                return " Primary Key";
            }
            else
            {
                //是否为空
                //if (!field.Nullable) sb.Append(" NOT NULL");
                if (field.Nullable)
                    return " NULL";
                else
                    return " NOT NULL";
            }
        }

        /// <summary>取得字段默认值</summary>
        /// <param name="field">字段</param>
        /// <param name="onlyDefine">仅仅定义</param>
        /// <returns></returns>
        protected virtual String GetFieldDefault(IDataColumn field, Boolean onlyDefine)
        {
            if (String.IsNullOrEmpty(field.Default)) return null;

            TypeCode tc = Type.GetTypeCode(field.DataType);

            // 特殊处理时间和NewGuid默认值
            String d = field.Default;
            if (CheckAndGetDefault(field, ref d))
            {
                // 如果数据库特性没有默认值，则说明不支持
                if (String.IsNullOrEmpty(d)) return null;

                return String.Format(" Default {0}", d);
            }

            if (tc == TypeCode.String)
            {
                return String.Format(" Default {0}", Database.FormatValue(field, field.Default));
            }
            else if (tc == TypeCode.DateTime)
            {
                if (field.Default.Contains("(") || field.Default.EqualIgnoreCase(Database.DateTimeNow))
                    return String.Format(" Default {0}", d);
                else
                    return String.Format(" Default '{0}'", d);
            }
            else
                return String.Format(" Default {0}", field.Default);
        }
        #endregion

        #region 数据定义语句
        public virtual String CreateDatabaseSQL(String dbname, String file)
        {
            return String.Format("Create Database {0}", FormatName(dbname));
        }

        public virtual String DropDatabaseSQL(String dbname)
        {
            return String.Format("Drop Database {0}", FormatName(dbname));
        }

        public virtual String DatabaseExistSQL(String dbname)
        {
            //throw new NotSupportedException("该功能未实现！");
            //return String.Empty;
            return null;
        }

        public virtual String CreateTableSQL(IDataTable table)
        {
            List<IDataColumn> Fields = new List<IDataColumn>(table.Columns);
            //Fields.Sort(delegate(IDataColumn item1, IDataColumn item2) { return item1.ID.CompareTo(item2.ID); });
            Fields.OrderBy(dc => dc.ID);

            StringBuilder sb = new StringBuilder();

            sb.AppendFormat("Create Table {0}(", FormatName(table.TableName));
            for (Int32 i = 0; i < Fields.Count; i++)
            {
                sb.AppendLine();
                sb.Append("\t");
                sb.Append(FieldClause(Fields[i], true));
                if (i < Fields.Count - 1) sb.Append(",");
            }
            sb.AppendLine();
            sb.Append(")");

            return sb.ToString();
        }

        String DropTableSQL(IDataTable table) { return DropTableSQL(table.TableName); }

        public virtual String DropTableSQL(String tableName) { return String.Format("Drop Table {0}", FormatName(tableName)); }

        String TableExistSQL(IDataTable table) { return TableExistSQL(table.TableName); }

        public virtual String TableExistSQL(String tableName) { throw new NotSupportedException("该功能未实现！"); }

        public virtual String AddTableDescriptionSQL(IDataTable table) { return null; }

        public virtual String DropTableDescriptionSQL(IDataTable table) { return null; }

        public virtual String AddColumnSQL(IDataColumn field) { return String.Format("Alter Table {0} Add {1}", FormatName(field.Table.TableName), FieldClause(field, true)); }

        public virtual String AlterColumnSQL(IDataColumn field, IDataColumn oldfield) { return String.Format("Alter Table {0} Alter Column {1}", FormatName(field.Table.TableName), FieldClause(field, false)); }

        public virtual String DropColumnSQL(IDataColumn field) { return String.Format("Alter Table {0} Drop Column {1}", FormatName(field.Table.TableName), field.ColumnName); }

        public virtual String AddColumnDescriptionSQL(IDataColumn field) { return null; }

        public virtual String DropColumnDescriptionSQL(IDataColumn field) { return null; }

        public virtual String AddDefaultSQL(IDataColumn field) { return null; }

        public virtual String DropDefaultSQL(IDataColumn field) { return null; }

        public virtual String CreateIndexSQL(IDataIndex index)
        {
            StringBuilder sb = new StringBuilder();
            if (index.Unique)
                sb.Append("Create Unique Index ");
            else
                sb.Append("Create Index ");

            sb.Append(FormatName(index.Name));
            sb.AppendFormat(" On {0} (", FormatName(index.Table.TableName));
            for (int i = 0; i < index.Columns.Length; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append(FormatName(index.Columns[i]));
                //else
                //    sb.AppendFormat("{0} {1}", FormatKeyWord(index.Columns[i].Name), isAscs[i].Value ? "Asc" : "Desc");
            }
            sb.Append(")");

            return sb.ToString();
        }

        public virtual String DropIndexSQL(IDataIndex index)
        {
            return String.Format("Drop Index {0} On {1}", FormatName(index.Name), FormatName(index.Table.TableName));
        }

        public virtual String CompactDatabaseSQL() { return null; }
        #endregion

        #region 操作
        protected virtual void Backup(String dbname, String file) { throw new NotImplementedException(); }

        public virtual String CompactDatabase() { throw new NotImplementedException(); }
        #endregion
    }
}