using System;
using System.Collections.Generic;
using System.Text;
using System.Data.OleDb;
using System.IO;
using System.Data.Common;
using XCode.Exceptions;

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
            base.Open();
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

        #region 创建数据库
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
        #endregion

    }
}