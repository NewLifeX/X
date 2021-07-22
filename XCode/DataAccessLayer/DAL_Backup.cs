﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NewLife;
using NewLife.Data;
using NewLife.Model;
using NewLife.Reflection;
using NewLife.Serialization;
using XCode.Transform;

namespace XCode.DataAccessLayer
{
    partial class DAL
    {
        #region 备份
        /// <summary>备份单表数据</summary>
        /// <remarks>
        /// 最大支持21亿行
        /// </remarks>
        /// <param name="table">数据表</param>
        /// <param name="stream">目标数据流</param>
        /// <param name="progress">进度回调，参数为已处理行数和当前页表</param>
        /// <returns></returns>
        public Int32 Backup(IDataTable table, Stream stream, Action<Int64, DbTable> progress = null)
        {
            var writeFile = new WriteFileActor
            {
                Stream = stream,

                // 最多同时堆积数
                BoundedCapacity = 4,
            };

            // 自增
            var id = table.Columns.FirstOrDefault(e => e.Identity);
            if (id == null)
            {
                var pks = table.PrimaryKeys;
                if (pks != null && pks.Length == 1 && pks[0].DataType.IsInt()) id = pks[0];
            }
            var tableName = Db.FormatName(table);
            var sb = new SelectBuilder { Table = tableName };

            // 总行数
            writeFile.Total = SelectCount(sb);
            WriteLog("备份[{0}/{1}]开始，共[{2:n0}]行", table, ConnName, writeFile.Total);

            IExtracter<DbTable> extracer = new PagingExtracter(this, tableName);
            if (id != null)
                extracer = new IdExtracter(this, tableName, id.ColumnName);

            var sw = Stopwatch.StartNew();
            var total = 0;
            foreach (var dt in extracer.Fetch())
            {
                var count = dt.Rows.Count;
                WriteLog("备份[{0}/{1}]数据 {2:n0} + {3:n0}", table, ConnName, extracer.Row, count);
                if (count == 0) break;

                // 进度报告
                progress?.Invoke(extracer.Row, dt);

                // 消费数据
                writeFile.Tell(dt);

                total += count;
            }

            // 通知写入完成
            writeFile.Stop(-1);

            sw.Stop();
            var ms = sw.Elapsed.TotalMilliseconds;
            WriteLog("备份[{0}/{1}]完成，共[{2:n0}]行，耗时{3:n0}ms，速度{4:n0}tps", table, ConnName, total, ms, total * 1000L / ms);

            // 返回总行数
            return total;
        }

        /// <summary>备份单表数据到文件</summary>
        /// <param name="table">数据表</param>
        /// <param name="file">文件。.gz后缀时采用压缩</param>
        /// <returns></returns>
        public Int32 Backup(IDataTable table, String file = null)
        {
            if (file.IsNullOrEmpty()) file = table + ".table";

            var file2 = file.GetFullPath();
            file2.EnsureDirectory(true);

            WriteLog("备份[{0}/{1}]到文件 {2}", table, ConnName, file2);

            using var fs = new FileStream(file2, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
            var rs = 0;
            if (file.EndsWithIgnoreCase(".gz"))
            {
#if NET4
                using var gs = new GZipStream(fs, CompressionMode.Compress, true);
#else
                using var gs = new GZipStream(fs, CompressionLevel.Optimal, true);
#endif
                rs = Backup(table, gs);
            }
            else
            {
                rs = Backup(table, fs);
            }

            // 截断文件
            fs.SetLength(fs.Position);

            return rs;
        }

        /// <summary>备份一批表到指定压缩文件</summary>
        /// <param name="tables">数据表集合</param>
        /// <param name="file">zip压缩文件</param>
        /// <param name="backupSchema">备份架构</param>
        /// <returns></returns>
        public Int32 BackupAll(IList<IDataTable> tables, String file, Boolean backupSchema = true)
        {
            if (tables == null) throw new ArgumentNullException(nameof(tables));

            // 过滤不存在的表
            var ts = Tables.Select(e => e.TableName).ToArray();
            tables = tables.Where(e => e.TableName.EqualIgnoreCase(ts)).ToList();

            //if (tables == null) tables = Tables;
            if (tables.Count > 0)
            {
#if !NET4
                var file2 = file.GetFullPath();
                file2.EnsureDirectory(true);

                WriteLog("备份[{0}]到文件 {1}。{2}", ConnName, file2, tables.Join(",", e => e.Name));

                using var fs = new FileStream(file2, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
                using var zip = new ZipArchive(fs, ZipArchiveMode.Create, true, Encoding.UTF8);

                // 备份架构
                if (backupSchema)
                {
                    var xml = Export(tables);
                    var entry = zip.CreateEntry(ConnName + ".xml");
                    using var ms = entry.Open();
                    ms.Write(xml.GetBytes());
                }

                foreach (var item in tables)
                {
                    var entry = zip.CreateEntry(item.Name + ".table");
                    using var ms = entry.Open();
                    Backup(item, ms);
                }
#endif
            }

            return tables.Count;
        }

        class WriteFileActor : Actor
        {
            public Stream Stream { get; set; }
            public Int32 Total { get; set; }

            private Binary _Binary;
            private Boolean _writeHeader;

            public override Task Start()
            {
                // 二进制读写器
                _Binary = new Binary
                {
                    EncodeInt = true,
                    Stream = Stream,
                };

                return base.Start();
            }

            protected override Task ReceiveAsync(ActorContext context)
            {
                var dt = context.Message as DbTable;
                var bn = _Binary;

                // 写头部结构。没有数据时可以备份结构
                if (!_writeHeader)
                {
                    dt.Total = Total;
                    dt.WriteHeader(bn);

                    // 输出日志
                    var cs = dt.Columns;
                    var ts = dt.Types;
                    WriteLog("字段[{0}]：{1}", cs.Length, cs.Join());
                    WriteLog("类型[{0}]：{1}", ts.Length, ts.Join(",", e => e.Name));

                    _writeHeader = true;
                }

                var rs = dt.Rows;
                if (rs == null || rs.Count == 0) return null;

                // 写入文件
                dt.WriteData(bn);
                Stream.Flush();

                return null;
            }
        }
        #endregion

        #region 恢复
        /// <summary>从数据流恢复数据</summary>
        /// <param name="stream">数据流</param>
        /// <param name="table">数据表</param>
        /// <param name="progress">进度回调，参数为已处理行数和当前页表</param>
        /// <returns></returns>
        public Int32 Restore(Stream stream, IDataTable table, Action<Int32, DbTable> progress = null)
        {
            var writeDb = new WriteDbActor
            {
                Table = table,
                Dal = this,

                // 最多同时堆积数页
                BoundedCapacity = 4,
            };
            //writeDb.Start();

            var sw = Stopwatch.StartNew();

            // 二进制读写器
            var bn = new Binary
            {
                EncodeInt = true,
                Stream = stream,
            };

            var dt = new DbTable();
            dt.ReadHeader(bn);
            WriteLog("恢复[{0}/{1}]开始，共[{2:n0}]行", table.Name, ConnName, dt.Total);

            // 输出日志
            var cs = dt.Columns;
            var ts = dt.Types;
            WriteLog("字段[{0}]：{1}", cs.Length, cs.Join());
            WriteLog("类型[{0}]：{1}", ts.Length, ts.Join(",", e => e.Name));

            var row = 0;
            var pageSize = (Db as DbBase).BatchSize;
            var total = 0;
            while (true)
            {
                // 读取数据
                dt.ReadData(bn, Math.Min(dt.Total - row, pageSize));

                var rs = dt.Rows;
                if (rs == null || rs.Count == 0) break;

                WriteLog("恢复[{0}/{1}]数据 {2:n0} + {3:n0}", table.Name, ConnName, row, rs.Count);

                // 进度报告
                progress?.Invoke(row, dt);

                // 批量写入数据库。克隆对象，避免被修改
                writeDb.Tell(dt.Clone());

                // 下一页
                total += rs.Count;
                if (rs.Count < pageSize) break;
                row += pageSize;
            }

            // 通知写入完成
            writeDb.Stop(-1);

            sw.Stop();
            var ms = sw.Elapsed.TotalMilliseconds;
            WriteLog("恢复[{0}/{1}]完成，共[{2:n0}]行，耗时{3:n0}ms，速度{4:n0}tps", table.Name, ConnName, total, ms, total * 1000L / ms);

            // 返回总行数
            return total;
        }

        /// <summary>从文件恢复数据</summary>
        /// <param name="file"></param>
        /// <param name="table"></param>
        /// <returns></returns>
        public Int64 Restore(String file, IDataTable table = null)
        {
            if (table == null) throw new ArgumentNullException(nameof(table));
            if (file.IsNullOrEmpty()) return 0;

            var file2 = file.GetFullPath();
            if (!File.Exists(file2)) return 0;
            file2.EnsureDirectory(true);

            WriteLog("恢复[{2}]到[{0}/{1}]", table, ConnName, file);

            SetTables(table);

            var compressed = file.EndsWithIgnoreCase(".gz");
            return file2.AsFile().OpenRead(compressed, s => Restore(s, table));
        }

        /// <summary>从指定压缩文件恢复一批数据到目标库</summary>
        /// <param name="file">zip压缩文件</param>
        /// <param name="tables"></param>
        /// <returns></returns>
        public IDataTable[] RestoreAll(String file, IDataTable[] tables = null)
        {
            if (file.IsNullOrEmpty()) throw new ArgumentNullException(nameof(file));

            var file2 = file.GetFullPath();
            if (!File.Exists(file2)) return null;

#if !NET4
            using var fs = new FileStream(file2, FileMode.Open);
            using var zip = new ZipArchive(fs, ZipArchiveMode.Read, true, Encoding.UTF8);

            // 备份架构
            if (tables == null)
            {
                var entry = zip.Entries.FirstOrDefault(e => e.Name.EndsWithIgnoreCase(".xml"));
                if (entry != null)
                {
                    using var ms = entry.Open();
                    tables = Import(ms.ToStr()).ToArray();
                }
            }

            WriteLog("恢复[{0}]从文件 {1}。{2}", ConnName, file2, tables?.Join(",", e => e.Name));

            SetTables(tables);

            foreach (var item in tables)
            {
                var entry = zip.GetEntry(item.Name + ".table");
                if (entry != null && entry.Length > 0)
                {
                    using var ms = entry.Open();
                    Restore(ms, item);
                }
            }
#endif

            return tables;
        }

        class WriteDbActor : Actor
        {
            public DAL Dal { get; set; }
            public IDataTable Table { get; set; }

            private IDataColumn[] _Columns;

            protected override Task ReceiveAsync(ActorContext context)
            {
                if (context.Message is not DbTable dt) return null;

                // 匹配要写入的列
                if (_Columns == null)
                {
                    _Columns = Table.GetColumns(dt.Columns);

                    WriteLog("数据表：{0}/{1}", Table.Name, Table);
                    WriteLog("匹配列：{0}", _Columns.Join(",", e => e.ColumnName));
                }

                // 批量插入
                Dal.Session.Insert(Table, _Columns, dt.Cast<IExtend>());

                return null;
            }
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
        /// <param name="progress">进度回调，参数为已处理行数和当前页表</param>
        /// <returns></returns>
        public Int32 Sync(IDataTable table, String connName, Boolean syncSchema = true, Action<Int32, DbTable> progress = null)
        {
            var dal = connName.IsNullOrEmpty() ? null : Create(connName);

            var writeDb = new WriteDbActor
            {
                Table = table,
                Dal = dal,

                // 最多同时堆积数页
                BoundedCapacity = 4,
            };

            // 自增
            var id = table.Columns.FirstOrDefault(e => e.Identity);
            // 主键
            if (id == null)
            {
                var pks = table.PrimaryKeys;
                if (pks != null && pks.Length == 1 && pks[0].DataType.IsInt()) id = pks[0];
            }

            var sw = Stopwatch.StartNew();

            // 表结构
            if (syncSchema) dal.SetTables(table);

            var sb = new SelectBuilder
            {
                Table = Db.FormatName(table)
            };

            var row = 0L;
            var pageSize = (Db as DbBase).BatchSize;
            var total = 0;
            while (true)
            {
                var sql = "";
                // 分割数据页，自增或分页
                if (id != null)
                {
                    sb.Where = $"{id.ColumnName}>={row}";
                    sql = PageSplit(sb, 0, pageSize);
                }
                else
                    sql = PageSplit(sb, row, pageSize);

                // 查询数据
                var dt = Session.Query(sql, null);
                if (dt == null || dt.Rows.Count == 0) break;

                var count = dt.Rows.Count;
                WriteLog("同步[{0}/{1}]数据 {2:n0} + {3:n0}", table.Name, ConnName, row, count);

                // 进度报告
                progress?.Invoke((Int32)row, dt);

                // 消费数据
                writeDb.Tell(dt);

                // 下一页
                total += count;
                //if (count < pageSize) break;

                // 自增分割时，取最后一行
                if (id != null)
                    row = dt.Get<Int64>(count - 1, id.ColumnName) + 1;
                else
                    row += pageSize;
            }

            // 通知写入完成
            writeDb.Stop(-1);

            sw.Stop();
            var ms = sw.Elapsed.TotalMilliseconds;
            WriteLog("同步[{0}/{1}]完成，共[{2:n0}]行，耗时{3:n0}ms，速度{4:n0}tps", table.Name, ConnName, total, ms, total * 1000L / ms);

            // 返回总行数
            return total;
        }

        /// <summary>备份一批表到另一个库</summary>
        /// <param name="tables">表名集合</param>
        /// <param name="connName">目标连接名</param>
        /// <param name="syncSchema">同步架构</param>
        /// <returns></returns>
        public IDictionary<String, Int32> SyncAll(IDataTable[] tables, String connName, Boolean syncSchema = true)
        {
            if (tables == null) throw new ArgumentNullException(nameof(tables));

            var dic = new Dictionary<String, Int32>();

            //if (tables == null) tables = Tables.ToArray();
            if (tables.Length > 0)
            {
                // 同步架构
                if (syncSchema)
                {
                    var dal = connName.IsNullOrEmpty() ? null : Create(connName);
                    dal.SetTables(tables);
                }

                foreach (var item in tables)
                {
                    dic[item.Name] = Sync(item, connName, false);
                }
            }

            return dic;
        }
        #endregion
    }
}