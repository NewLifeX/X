using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.IO;
using System.Data.Common;

namespace XCode.DataAccessLayer
{
    /// <summary>
    /// 文件型数据库
    /// </summary>
    abstract class FileDbBase : DbBase
    {
        #region 属性
        protected override string DefaultConnectionString
        {
            get
            {
                DbConnectionStringBuilder builder = Factory.CreateConnectionStringBuilder();
                if (builder != null)
                {
                    builder["Data Source"] = Path.GetTempFileName();
                    return builder.ToString();
                }

                return base.DefaultConnectionString;
            }
        }

        protected override void OnSetConnectionString(XDbConnectionStringBuilder builder)
        {
            base.OnSetConnectionString(builder);

            String file;
            //if (!builder.TryGetValue("Data Source", out file)) return;
            // 允许空，当作内存数据库处理
            builder.TryGetValue("Data Source", out file);
            file = ResoleFile(file);
            builder["Data Source"] = file;
            FileName = file;
        }

        protected virtual String ResoleFile(String file)
        {
            if (String.IsNullOrEmpty(file)) return file;

            if (file.StartsWith("~/") || file.StartsWith("~\\"))
            {
                file = file.Replace("/", "\\").Replace("~\\", AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\') + "\\");
            }
            else if (file.StartsWith("./") || file.StartsWith(".\\"))
            {
                file = file.Replace("/", "\\").Replace(".\\", AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\') + "\\");
            }
            else if (!Path.IsPathRooted(file))
            {
                file = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, file.Replace("/", "\\"));
                // 过滤掉不必要的符号
                file = new FileInfo(file).FullName;
            }

            return file;
        }

        private String _FileName;
        /// <summary>文件</summary>
        public String FileName
        {
            get { return _FileName; }
            set { _FileName = value; }
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
            get { return Database is FileDbBase ? (Database as FileDbBase).FileName : null; }
        }
        #endregion

        #region 方法
        private static List<String> hasChecked = new List<string>();

        /// <summary>
        /// 已重载。打开数据库连接前创建数据库
        /// </summary>
        public override void Open()
        {
            if (!String.IsNullOrEmpty(FileName))
            {
                if (!hasChecked.Contains(FileName))
                {
                    hasChecked.Add(FileName);
                    CreateDatabase();
                }
            }

            base.Open();
        }

        protected virtual void CreateDatabase()
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
            get { return (Database as FileDbBase).FileName; }
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
            if (String.IsNullOrEmpty(FileName)) return;

            // 提前创建目录
            String dir = Path.GetDirectoryName(FileName);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            if (!File.Exists(FileName)) File.Create(FileName).Dispose();
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