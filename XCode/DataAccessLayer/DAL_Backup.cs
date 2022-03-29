using System;
using System.Collections.Generic;
using System.IO;
using NewLife.Log;

namespace XCode.DataAccessLayer
{
    public partial class DAL
    {
        #region 备份
        /// <summary>备份单表数据</summary>
        /// <remarks>
        /// 最大支持21亿行
        /// </remarks>
        /// <param name="table">数据表</param>
        /// <param name="stream">目标数据流</param>
        /// <returns></returns>
        public Int32 Backup(IDataTable table, Stream stream)
        {
            var dpk = new DbPackage { Dal = this, Tracer = Tracer ?? GlobalTracer, Log = XTrace.Log };
            return dpk.Backup(table, stream);
        }

        /// <summary>备份单表数据到文件</summary>
        /// <param name="table">数据表</param>
        /// <param name="file">文件。.gz后缀时采用压缩</param>
        /// <returns></returns>
        public Int32 Backup(IDataTable table, String file = null)
        {
            var dpk = new DbPackage { Dal = this, Tracer = Tracer ?? GlobalTracer, Log = XTrace.Log };
            return dpk.Backup(table, file);
        }

        /// <summary>备份一批表到指定压缩文件</summary>
        /// <param name="tables">数据表集合</param>
        /// <param name="file">zip压缩文件</param>
        /// <param name="backupSchema">备份架构</param>
        /// <param name="ignoreError">忽略错误，继续恢复下一张表</param>
        /// <returns></returns>
        public Int32 BackupAll(IList<IDataTable> tables, String file, Boolean backupSchema = true, Boolean ignoreError = true)
        {
            var dpk = new DbPackage { Dal = this, IgnoreError = ignoreError, Tracer = Tracer ?? GlobalTracer, Log = XTrace.Log };
            return dpk.BackupAll(tables, file, backupSchema);
        }
        #endregion

        #region 恢复
        /// <summary>从数据流恢复数据</summary>
        /// <param name="stream">数据流</param>
        /// <param name="table">数据表</param>
        /// <returns></returns>
        public Int32 Restore(Stream stream, IDataTable table)
        {
            var dpk = new DbPackage { Dal = this, Tracer = Tracer ?? GlobalTracer, Log = XTrace.Log };
            return dpk.Restore(stream, table);
        }

        /// <summary>从文件恢复数据</summary>
        /// <param name="file">zip压缩文件</param>
        /// <param name="table">数据表</param>
        /// <param name="setSchema">是否设置数据表模型，自动建表</param>
        /// <returns></returns>
        public Int64 Restore(String file, IDataTable table, Boolean setSchema = true)
        {
            var dpk = new DbPackage { Dal = this, Tracer = Tracer ?? GlobalTracer, Log = XTrace.Log };
            return dpk.Restore(file, table, setSchema);
        }

        /// <summary>从指定压缩文件恢复一批数据到目标库</summary>
        /// <param name="file">zip压缩文件</param>
        /// <param name="tables">数据表。为空时从压缩包读取xml模型文件</param>
        /// <param name="setSchema">是否设置数据表模型，自动建表</param>
        /// <param name="ignoreError">忽略错误，继续下一张表</param>
        /// <returns></returns>
        public IDataTable[] RestoreAll(String file, IDataTable[] tables = null, Boolean setSchema = true, Boolean ignoreError = true)
        {
            var dpk = new DbPackage { Dal = this, IgnoreError = ignoreError, Tracer = Tracer ?? GlobalTracer, Log = XTrace.Log };
            return dpk.RestoreAll(file, tables, setSchema);
        }
        #endregion

        #region 同步
        /// <summary>同步单表数据</summary>
        /// <remarks>
        /// 把数据同一张表同步到另一个库
        /// </remarks>
        /// <param name="table">数据表</param>
        /// <param name="connName">目标连接名</param>
        /// <param name="syncSchema">同步架构</param>
        /// <returns></returns>
        public Int32 Sync(IDataTable table, String connName, Boolean syncSchema = true)
        {
            var dpk = new DbPackage { Dal = this, Tracer = Tracer ?? GlobalTracer, Log = XTrace.Log };
            return dpk.Sync(table, connName, syncSchema);
        }

        /// <summary>备份一批表到另一个库</summary>
        /// <param name="tables">表名集合</param>
        /// <param name="connName">目标连接名</param>
        /// <param name="syncSchema">同步架构</param>
        /// <param name="ignoreError">忽略错误，继续下一张表</param>
        /// <returns></returns>
        public IDictionary<String, Int32> SyncAll(IDataTable[] tables, String connName, Boolean syncSchema = true, Boolean ignoreError = true)
        {
            var dpk = new DbPackage { Dal = this, IgnoreError = ignoreError, Tracer = Tracer ?? GlobalTracer, Log = XTrace.Log };
            return dpk.SyncAll(tables, connName, syncSchema);
        }
        #endregion
    }
}