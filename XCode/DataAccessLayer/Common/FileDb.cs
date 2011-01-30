using System;
using System.Data.Common;
using System.Data.OleDb;
using System.IO;
using XCode.Exceptions;
using System.Data;

namespace XCode.DataAccessLayer
{
    /// <summary>
    /// 文件型数据库
    /// </summary>
    abstract class FileDbBase : DbBase
    {
        #region 属性
        /// <summary>链接字符串</summary>
        public override string ConnectionString
        {
            get
            {
                return base.ConnectionString;
            }
            set
            {
                try
                {
                    OleDbConnectionStringBuilder csb = new OleDbConnectionStringBuilder(value);
                    // 不是绝对路径
                    if (!String.IsNullOrEmpty(csb.DataSource) && csb.DataSource.Length > 1 && csb.DataSource.Substring(1, 1) != ":")
                    {
                        String mdbPath = csb.DataSource;
                        if (mdbPath.StartsWith("~/") || mdbPath.StartsWith("~\\"))
                        {
                            mdbPath = mdbPath.Replace("/", "\\").Replace("~\\", AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\') + "\\");
                        }
                        else if (mdbPath.StartsWith("./") || mdbPath.StartsWith(".\\"))
                        {
                            mdbPath = mdbPath.Replace("/", "\\").Replace(".\\", AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\') + "\\");
                        }
                        else
                        {
                            mdbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, mdbPath.Replace("/", "\\"));
                        }
                        csb.DataSource = mdbPath;
                        FileName = mdbPath;
                        value = csb.ConnectionString;
                    }
                }
                catch (DbException ex)
                {
                    throw new XDbException(this, "分析OLEDB连接字符串时出错", ex);
                }
                base.ConnectionString = value;
            }
        }

        private String _FileName;
        /// <summary>文件</summary>
        public String FileName
        {
            get { return _FileName; }
            private set { _FileName = value; }
        }
        #endregion
    }

    /// <summary>
    /// 文件型数据库会话
    /// </summary>
    abstract class FileDbSession : DbSession
    {
        #region 属性
        /// <summary>文件</summary>
        public String FileName
        {
            get { return (Database as Access).FileName; }
        }
        #endregion

        #region 方法
        /// <summary>
        /// 已重载。打开数据库连接前创建数据库
        /// </summary>
        public override void Open()
        {
            CreateDatabase();

            base.Open();
        }

        protected void CreateDatabase()
        {
            if (!File.Exists(FileName)) Database.CreateMetaData().SetSchema(DDLSchema.CreateDatabase, null);
        }
        #endregion
    }

    /// <summary>
    /// 文件型数据库元数据
    /// </summary>
    abstract class FileDbMetaData : DbMetaData
    {
        #region 属性
        /// <summary>文件</summary>
        public String FileName
        {
            get { return (Database as Access).FileName; }
        }
        #endregion

        #region 数据定义
        /// <summary>
        /// 设置数据定义模式
        /// </summary>
        /// <param name="schema"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        public override object SetSchema(DDLSchema schema, object[] values)
        {
            //Object obj = null;
            switch (schema)
            {
                case DDLSchema.CreateDatabase:
                    CreateDatabase();
                    return null;
                case DDLSchema.DropDatabase:
                    DropDatabase();
                    return null;
                case DDLSchema.DatabaseExist:
                    return File.Exists(FileName);
                //case DDLSchema.CreateTable:
                //    obj = base.SetSchema(DDLSchema.CreateTable, values);
                //    XTable table = values[0] as XTable;
                //    if (!String.IsNullOrEmpty(table.Description)) AddTableDescription(table.Name, table.Description);
                //    foreach (XField item in table.Fields)
                //    {
                //        if (!String.IsNullOrEmpty(item.Description)) AddColumnDescription(table.Name, item.Name, item.Description);
                //    }
                //    return obj;
                //case DDLSchema.DropTable:
                //    break;
                //case DDLSchema.TableExist:
                //    DataTable dt = GetSchema("Tables", new String[] { null, null, (String)values[0], "TABLE" });
                //    if (dt == null || dt.Rows == null || dt.Rows.Count < 1) return false;
                //    return true;
                default:
                    break;
            }
            return base.SetSchema(schema, values);
        }

        /// <summary>
        /// 创建数据库
        /// </summary>
        protected virtual void CreateDatabase()
        {
            // 提前创建目录
            String dir = Path.GetDirectoryName(FileName);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            if (!File.Exists(FileName)) File.Create(FileName);
        }

        protected void DropDatabase()
        {
            //首先关闭数据库
            DbBase db = Database as DbBase;
            if (db != null)
                db.ReleaseSession();
            else
                Database.CreateSession().Dispose();

            OleDbConnection.ReleaseObjectPool();
            GC.Collect();

            if (File.Exists(FileName)) File.Delete(FileName);
        }
        #endregion
    }
}