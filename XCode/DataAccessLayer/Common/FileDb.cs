using System;
using System.Collections.Generic;
using System.IO;

namespace XCode.DataAccessLayer
{
    /// <summary>文件型数据库</summary>
    abstract class FileDbBase : DbBase
    {
        #region 属性
        protected override String DefaultConnectionString
        {
            get
            {
                var builder = Factory.CreateConnectionStringBuilder();
                if (builder != null)
                {
                    builder[_.DataSource] = Path.GetTempFileName();
                    return builder.ToString();
                }

                return base.DefaultConnectionString;
            }
        }

        protected override void OnSetConnectionString(ConnectionStringBuilder builder)
        {
            base.OnSetConnectionString(builder);

            //if (!builder.TryGetValue(_.DataSource, out file)) return;
            // 允许空，当作内存数据库处理
            //builder.TryGetValue(_.DataSource, out var file);
            var file = builder[_.DataSource];
            file = OnResolveFile(file);
            builder[_.DataSource] = file;
            DatabaseName = file;
        }

        protected virtual String OnResolveFile(String file) => ResolveFile(file);

        ///// <summary>文件</summary>
        //public String FileName { get; set; }
        #endregion
    }

    /// <summary>文件型数据库会话</summary>
    abstract class FileDbSession : DbSession
    {
        #region 属性
        /// <summary>文件</summary>
        public String FileName => (Database as FileDbBase)?.DatabaseName;
        #endregion

        #region 构造函数
        protected FileDbSession(IDatabase db) : base(db)
        {
            if (!String.IsNullOrEmpty(FileName))
            {
                if (!hasChecked.Contains(FileName))
                {
                    hasChecked.Add(FileName);
                    CreateDatabase();
                }
            }
        }
        #endregion

        #region 方法
        private static List<String> hasChecked = new List<String>();

        ///// <summary>已重载。打开数据库连接前创建数据库</summary>
        //public override void Open()
        //{
        //    if (!String.IsNullOrEmpty(FileName))
        //    {
        //        if (!hasChecked.Contains(FileName))
        //        {
        //            hasChecked.Add(FileName);
        //            CreateDatabase();
        //        }
        //    }

        //    base.Open();
        //}

        protected virtual void CreateDatabase()
        {
            if (!File.Exists(FileName)) Database.CreateMetaData().SetSchema(DDLSchema.CreateDatabase, null);
        }
        #endregion

        #region 高级
        /// <summary>清空数据表，标识归零</summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public override Int32 Truncate(String tableName)
        {
            var sql = "Delete From {0}".F(Database.FormatName(tableName));
            return Execute(sql);
        }
        #endregion
    }

    /// <summary>文件型数据库元数据</summary>
    abstract class FileDbMetaData : DbMetaData
    {
        #region 属性
        /// <summary>文件</summary>
        public String FileName => (Database as FileDbBase).DatabaseName;
        #endregion

        #region 数据定义
        /// <summary>设置数据定义模式</summary>
        /// <param name="schema"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        public override Object SetSchema(DDLSchema schema, Object[] values)
        {
            //Object obj = null;
            switch (schema)
            {
                case DDLSchema.CreateDatabase:
                    CreateDatabase();
                    return null;
                //case DDLSchema.DropDatabase:
                //    DropDatabase();
                //    return null;
                case DDLSchema.DatabaseExist:
                    return File.Exists(FileName);
                default:
                    break;
            }
            return base.SetSchema(schema, values);
        }

        /// <summary>创建数据库</summary>
        protected virtual void CreateDatabase()
        {
            if (String.IsNullOrEmpty(FileName)) return;

            // 提前创建目录
            var dir = Path.GetDirectoryName(FileName);
            if (!String.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);

            if (!File.Exists(FileName))
            {
                DAL.WriteLog("创建数据库：{0}", FileName);

                File.Create(FileName).Dispose();
            }
        }

        protected virtual void DropDatabase()
        {
            //首先关闭数据库
            if (Database is DbBase db)
                db.ReleaseSession();
            else
                Database.CreateSession().Dispose();

            //OleDbConnection.ReleaseObjectPool();
            GC.Collect();

            if (File.Exists(FileName)) File.Delete(FileName);
        }
        #endregion
    }
}