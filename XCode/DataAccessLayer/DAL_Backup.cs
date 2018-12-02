using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
            //var readDb = new ReadDbActor
            //{
            //    Dal = this,
            //    Table = table,
            //};

            var writeFile = new WriteFileActor
            {
                //ReadDb = readDb,
                Dal = this,
                Stream = stream,
                Progress = progress,
            };
            //readDb.WriteFile = writeFile;

            // 最多同时堆积数页
            writeFile.Start(4);

            //// 从0行开始处理
            //readDb.Start();
            //readDb.Add(0);

            // 原始位置和行数位置
            var pOri = stream.Position;
            var pCount = 0L;

            var sb = new SelectBuilder
            {
                Table = Db.FormatTableName(table)
            };

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

                // 消费数据
                writeFile.Add(new Tuple<Int32, DbTable>(row, dt));

                // 下一页
                total += dt.Rows.Count;
                if (dt.Rows.Count < pageSize) break;
                row += pageSize;
            }

            // 等待写入完成
            writeFile.Stop();
            writeFile.Wait();

            sw.Stop();
            var ms = sw.Elapsed.TotalMilliseconds;
            WriteLog("备份[{0}/{1}]完成，共[{2:n0}]行，耗时{3:n0}ms，速度{4:n0}tps，大小{5:n0}字节", table, ConnName, total, ms, total * 1000 / ms, stream.Position - pOri);

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
                return Backup(table, fs);
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

        //class ReadDbActor : Actor<Int32>
        //{
        //    public WriteFileActor WriteFile { get; set; }
        //    public DAL Dal { get; set; }
        //    public String Table { get; set; }
        //    public Int32 PageSize { get; set; } = 10_000;

        //    protected override void OnAct(Int32 message)
        //    {
        //        var sb = new SelectBuilder
        //        {
        //            Table = Dal.Db.FormatTableName(Table)
        //        };
        //        var row = message;
        //        var sb2 = Dal.PageSplit(sb, row, PageSize);

        //        // 查询数据
        //        var dt = Dal.Session.Query(sb2.ToString(), null);
        //        if (dt == null)
        //        {
        //            WriteFile.Stop();
        //            return;
        //        }

        //        // 通知处理
        //        WriteFile.Add(new Tuple<Int32, DbTable>(row, dt));
        //    }
        //}

        class WriteFileActor : Actor<Tuple<Int32, DbTable>>
        {
            //public ReadDbActor ReadDb { get; set; }
            public DAL Dal { get; set; }
            public String Table { get; set; }
            public Stream Stream { get; set; }
            public Binary Binary { get; set; }
            public Action<Int32, DbTable> Progress { get; set; }
            public Int64 OriginPosition { get; set; }
            public Int64 CountPosition { get; set; }
            public Int32 Total { get; set; }
            //public Stopwatch Watch { get; set; }

            public override void Start(Int32 boundedCapacity)
            {
                //Watch = Stopwatch.StartNew();

                // 二进制读写器
                Binary = new Binary
                {
                    EncodeInt = true,
                    Stream = Stream,
                };

                OriginPosition = Stream.Position;

                base.Start(boundedCapacity);
            }

            public override void Stop()
            {
                base.Stop();

                var bn = Binary;
                var stream = bn.Stream;

                var total = Total;
                if (total > 0)
                {
                    // 更新行数
                    var p = stream.Position;
                    stream.Position = CountPosition;
                    bn.Write(total.GetBytes(), 0, 4);
                    stream.Position = p;
                }

                //Watch.Stop();
                //var ms = Watch.Elapsed.TotalMilliseconds;
                //DAL.WriteLog("备份[{0}/{1}]完成，共[{2:n0}]行，耗时{3:n0}ms，速度{4:n0}tps，大小{5:n0}字节", ReadDb.Table, Dal.ConnName, total, ms, total * 1000 / ms, stream.Position - OriginPosition);
            }

            protected override void OnAct(Tuple<Int32, DbTable> message)
            {
                var row = message.Item1;
                var dt = message.Item2;
                var bn = Binary;
                //var db = ReadDb;

                // 写头部结构。没有数据时可以备份结构
                if (row == 0)
                {
                    dt.WriteHeader(bn);

                    // 数据行数，占位
                    CountPosition = bn.Stream.Position - 4;
                }

                var rs = dt.Rows;
                if (rs == null || rs.Count == 0) return;

                WriteLog("备份[{0}/{1}]数据 {2:n0} + {3:n0}", Table, Dal.ConnName, row, rs.Count);

                // 写入文件
                dt.WriteData(bn);

                Progress?.Invoke(row, dt);

                //row += db.PageSize;

                //// 再读一页
                //db.Add(row);
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
            // 二进制读写器
            var bn = new Binary
            {
                EncodeInt = true,
                Stream = stream,
            };

            // 原始位置和行数位置
            var pOri = stream.Position;

            var sw = Stopwatch.StartNew();
            var dt = new DbTable();
            dt.ReadHeader(bn);

            // 匹配要写入的列
            var tableName = Db.FormatTableName(table.TableName);
            var columns = table.GetColumns(dt.Columns);

            var row = 0;
            var pageSize = 10_000;
            var total = 0;
            while (true)
            {
                // 读取数据
                var count = dt.ReadData(bn, Math.Min(dt.Total - row, pageSize));

                var rs = dt.Rows;
                if (rs == null || rs.Count == 0) break;

                WriteLog("恢复[{0}/{1}]数据 {2:n0} + {3:n0}", table, ConnName, row, rs.Count);

                // 批量写入数据库
                var ds = new List<IIndexAccessor>();
                foreach (var item in dt)
                {
                    ds.Add(item);
                }
                Session.Insert(tableName, columns, ds);

                // 进度报告
                progress?.Invoke(row, dt);

                // 下一页
                total += count;
                if (count < pageSize) break;
                row += pageSize;
            }

            sw.Stop();
            var ms = sw.Elapsed.TotalMilliseconds;
            WriteLog("恢复[{0}/{1}]完成，共[{2:n0}]行，耗时{3:n0}ms，速度{4:n0}tps", table, ConnName, total, ms, total * 1000 / ms);

            // 返回总行数
            return total;
        }

        /// <summary>从文件恢复数据</summary>
        /// <param name="file"></param>
        /// <param name="table"></param>
        /// <returns></returns>
        public Int32 Restore(String file, IDataTable table = null)
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

            using (var fs = new FileStream(file2, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                return Restore(fs, table);
            }
        }

        /// <summary>从指定目录恢复一批数据到目标库</summary>
        /// <param name="dir"></param>
        /// <param name="tables"></param>
        /// <returns></returns>
        public IDictionary<String, Int32> RestoreAll(String dir, IDataTable[] tables = null)
        {
            var dic = new Dictionary<String, Int32>();
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
        #endregion
    }
}