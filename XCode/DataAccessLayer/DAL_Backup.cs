﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NewLife.Collections;
using NewLife.Data;
using NewLife.Model;
using NewLife.Reflection;
using NewLife.Serialization;

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
        public Int32 Backup(String table, Stream stream, Action<Int32, DbTable> progress = null)
        {
            var writeFile = new WriteFileActor
            {
                Stream = stream,

                // 最多同时堆积数页
                BoundedCapacity = 4,
            };
            //writeFile.Start();

            var sb = new SelectBuilder
            {
                Table = Db.FormatTableName(table)
            };

            // 总行数
            writeFile.Total = SelectCount(sb);
            WriteLog("备份[{0}/{1}]开始，共[{2:n0}]行", table, ConnName, writeFile.Total);

            var row = 0;
            var pageSize = 10_000;
            var total = 0;
            var sw = Stopwatch.StartNew();
            while (true)
            {
                // 分页
                var sb2 = PageSplit(sb, row, pageSize);

                // 查询数据
                var dt = Session.Query(sb2.ToString(), null);
                if (dt == null) break;

                var count = dt.Rows.Count;
                WriteLog("备份[{0}/{1}]数据 {2:n0} + {3:n0}", table, ConnName, row, count);

                // 进度报告
                progress?.Invoke(row, dt);

                // 消费数据
                writeFile.Tell(dt);

                // 下一页
                total += count;
                if (count < pageSize) break;
                row += pageSize;
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
        /// <param name="table"></param>
        /// <param name="file"></param>
        /// <returns></returns>
        public Int32 Backup(String table, String file = null)
        {
            if (file.IsNullOrEmpty()) file = table + ".table";

            var file2 = file.GetFullPath();
            file2.EnsureDirectory(true);

            WriteLog("备份[{0}/{1}]到文件 {2}", table, ConnName, file2);

            using (var fs = new FileStream(file2, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite))
            {
                var rs = 0;
                if (file.EndsWithIgnoreCase(".gz"))
                {
#if NET4
                    using (var gs = new GZipStream(fs, CompressionMode.Compress, true))
#else
                    using (var gs = new GZipStream(fs, CompressionLevel.Optimal, true))
#endif
                    {
                        rs = Backup(table, gs);
                    }
                }
                else
                {
                    rs = Backup(table, fs);
                }

                // 截断文件
                fs.SetLength(fs.Position);

                return rs;
            }
        }

        /// <summary>备份一批表到指定目录</summary>
        /// <param name="tables"></param>
        /// <param name="dir"></param>
        /// <param name="backupSchema">备份架构</param>
        /// <returns></returns>
        public IDictionary<String, Int32> BackupAll(String[] tables, String dir, Boolean backupSchema = true)
        {
            var dic = new Dictionary<String, Int32>();

            IList<IDataTable> tbls = null;
            if (tables == null)
            {
                tbls = Tables;
                tables = tbls.Select(e => e.TableName).ToArray();
            }
            if (tables != null && tables.Length > 0)
            {
                // 备份架构
                if (backupSchema)
                {
                    if (tbls == null) tbls = Tables;
                    var bs = tables.Select(e => tbls.FirstOrDefault(t => e.EqualIgnoreCase(t.Name, t.TableName))).Where(e => e != null).ToArray();

                    var xml = Export(bs);
                    dir.EnsureDirectory(false);
                    File.WriteAllText(dir.CombinePath(ConnName + ".xml"), xml);
                }

                foreach (var item in tables)
                {
                    dic[item] = Backup(item, dir.CombinePath(item + ".table"));
                }
            }

            return dic;
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

            protected override void Receive(ActorContext context)
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
                if (rs == null || rs.Count == 0) return;

                // 写入文件
                dt.WriteData(bn);
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
            var pageSize = 10_000;
            var total = 0;
            while (true)
            {
                // 读取数据
                var count = dt.ReadData(bn, Math.Min(dt.Total - row, pageSize));

                var rs = dt.Rows;
                if (rs == null || rs.Count == 0) break;

                WriteLog("恢复[{0}/{1}]数据 {2:n0} + {3:n0}", table.Name, ConnName, row, rs.Count);

                // 进度报告
                progress?.Invoke(row, dt);

                // 批量写入数据库。克隆对象，避免被修改
                writeDb.Tell(dt.Clone());

                // 下一页
                total += count;
                if (count < pageSize) break;
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
            if (file.IsNullOrEmpty() || !File.Exists(file)) return 0;

            if (table == null)
            {
                var name = Path.GetFileNameWithoutExtension(file);
                table = Tables.FirstOrDefault(e => name.EqualIgnoreCase(e.Name, e.TableName));
            }
            else
                SetTables(table);
            if (table == null) return 0;

            var file2 = file.GetFullPath();
            file2.EnsureDirectory(true);

            WriteLog("恢复[{2}]到[{0}/{1}]", table, ConnName, file);

            var compressed = file.EndsWithIgnoreCase(".gz");
            return file2.AsFile().OpenRead(compressed, s => Restore(s, table));
            //using (var fs = new FileStream(file2, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            //{
            //    if (file.EndsWithIgnoreCase(".gz"))
            //    {
            //        using (var gs = new GZipStream(fs, CompressionMode.Decompress, true))
            //        {
            //            return Restore(gs, table);
            //        }
            //    }
            //    else
            //    {
            //        return Restore(fs, table);
            //    }
            //}
        }

        /// <summary>从指定目录恢复一批数据到目标库</summary>
        /// <param name="dir"></param>
        /// <param name="tables"></param>
        /// <returns></returns>
        public IDictionary<String, Int64> RestoreAll(String dir, IDataTable[] tables = null)
        {
            var dic = new Dictionary<String, Int64>();
            if (dir.IsNullOrEmpty() || !Directory.Exists(dir)) return dic;

            if (tables == null)
            {
                var schm = dir.AsDirectory().GetAllFiles("*.xml").FirstOrDefault();
                var tbls = schm != null ? ImportFrom(schm.FullName) : Tables;
                var ts = new List<IDataTable>();
                foreach (var item in dir.AsDirectory().GetFiles("*.table"))
                {
                    var name = Path.GetFileNameWithoutExtension(item.Name);
                    var tb = tbls.FirstOrDefault(e => name.EqualIgnoreCase(e.Name, e.TableName));
                    if (tb != null) ts.Add(tb);
                }
                tables = ts.ToArray();
            }
            if (tables != null && tables.Length > 0)
            {
                foreach (var item in tables)
                {
                    dic[item.Name] = Restore(dir.CombinePath(item.Name + ".table"), item);
                }
            }

            return dic;
        }

        class WriteDbActor : Actor
        {
            public DAL Dal { get; set; }
            public IDataTable Table { get; set; }

            private String _TableName;
            private IDataColumn[] _Columns;

            protected override void Receive(ActorContext context)
            {
                if (!(context.Message is DbTable dt)) return;

                // 匹配要写入的列
                if (_TableName == null)
                {
                    _TableName = Dal.Db.FormatTableName(Table.TableName);
                    _Columns = Table.GetColumns(dt.Columns);
                }

                // 批量插入
                var ds = new List<IIndexAccessor>();
                foreach (var item in dt)
                {
                    ds.Add(item);
                }
                Dal.Session.Insert(_TableName, _Columns, ds);
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

            var sw = Stopwatch.StartNew();

            // 表结构
            if (syncSchema) dal.SetTables(table);

            var sb = new SelectBuilder
            {
                Table = Db.FormatTableName(table.TableName)
            };

            var row = 0;
            var pageSize = 10_000;
            var total = 0;
            while (true)
            {
                // 分页
                var sb2 = PageSplit(sb, row, pageSize);

                // 查询数据
                var dt = Session.Query(sb2.ToString(), null);
                if (dt == null) break;

                var count = dt.Rows.Count;
                WriteLog("同步[{0}/{1}]数据 {2:n0} + {3:n0}", table.Name, ConnName, row, count);

                // 进度报告
                progress?.Invoke(row, dt);

                // 消费数据
                writeDb.Tell(dt);

                // 下一页
                total += count;
                if (count < pageSize) break;
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
            var dic = new Dictionary<String, Int32>();

            if (tables == null) tables = Tables.ToArray();
            if (tables != null && tables.Length > 0)
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