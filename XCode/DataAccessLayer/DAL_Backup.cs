using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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

            var task = writeFile.Start();

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

                progress?.Invoke(row, dt);

                // 下一页
                total += dt.Rows.Count;
                if (dt.Rows.Count < pageSize) break;
                row += pageSize;
            }

            // 等待写入完成
            writeFile.Stop();
            task.Wait();

            sw.Stop();
            var ms = sw.Elapsed.TotalMilliseconds;
            WriteLog("备份[{0}/{1}]完成，共[{2:n0}]行，耗时{3:n0}ms，速度{4:n0}tps", table, ConnName, total, ms, total * 1000 / ms);

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

        class WriteFileActor : Actor
        {
            public Stream Stream { get; set; }

            private Binary _Binary;
            private Int64 _CountPosition;
            private Int32 _Total;

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

            protected override void Act()
            {
                base.Act();

                var total = _Total;
                if (total > 0)
                {
                    var bn = _Binary;
                    var stream = bn.Stream;

                    // 更新行数
                    var p = stream.Position;
                    stream.Position = _CountPosition;
                    bn.Write(total.GetBytes(), 0, 4);
                    stream.Position = p;
                }
            }

            protected override void OnAct(Object message)
            {
                var msg = message as Tuple<Int32, DbTable>;
                var row = msg.Item1;
                var dt = msg.Item2;
                var bn = _Binary;

                // 写头部结构。没有数据时可以备份结构
                if (row == 0)
                {
                    dt.WriteHeader(bn);

                    // 数据行数，占位
                    _CountPosition = bn.Stream.Position - 4;
                }

                var rs = dt.Rows;
                if (rs == null || rs.Count == 0) return;

                // 写入文件
                dt.WriteData(bn);

                _Total += rs.Count;
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