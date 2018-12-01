using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using NewLife.Collections;
using NewLife.Data;
using NewLife.Reflection;
using NewLife.Serialization;

namespace XCode.DataAccessLayer
{
    partial class DAL
    {
        #region 属性
        [ThreadStatic]
        private static Int32 _QueryTimes;
        /// <summary>查询次数</summary>
        public static Int32 QueryTimes => _QueryTimes;

        [ThreadStatic]
        private static Int32 _ExecuteTimes;
        /// <summary>执行次数</summary>
        public static Int32 ExecuteTimes => _ExecuteTimes;
        #endregion

        #region 数据操作方法
        /// <summary>根据条件把普通查询SQL格式化为分页SQL。</summary>
        /// <param name="builder">查询生成器</param>
        /// <param name="startRowIndex">开始行，0表示第一行</param>
        /// <param name="maximumRows">最大返回行数，0表示所有行</param>
        /// <returns>分页SQL</returns>
        public SelectBuilder PageSplit(SelectBuilder builder, Int64 startRowIndex, Int64 maximumRows)
        {
            if (startRowIndex <= 0 && maximumRows <= 0) return builder;

            // 2016年7月2日 HUIYUE 取消分页SQL缓存，此部分缓存提升性能不多，但有可能会造成分页数据不准确，感觉得不偿失
            return Db.PageSplit(builder, startRowIndex, maximumRows);
        }

        /// <summary>执行SQL查询，返回记录集</summary>
        /// <param name="sql">SQL语句</param>
        /// <returns></returns>
        public DataSet Select(String sql)
        {
            return QueryByCache(sql, "", "", (s, k2, k3) => Session.Query(s), nameof(Select));
        }

        /// <summary>执行SQL查询，返回记录集</summary>
        /// <param name="builder">SQL语句</param>
        /// <param name="startRowIndex">开始行，0表示第一行</param>
        /// <param name="maximumRows">最大返回行数，0表示所有行</param>
        /// <returns></returns>
        public DataSet Select(SelectBuilder builder, Int64 startRowIndex, Int64 maximumRows)
        {
            return QueryByCache(builder, startRowIndex, maximumRows, (sb, start, max) =>
            {
                sb = PageSplit(sb, start, max);
                return Session.Query(sb.ToString(), CommandType.Text, sb.Parameters.ToArray());
            }, nameof(Select));
        }

        /// <summary>执行SQL查询，返回记录集</summary>
        /// <param name="builder">SQL语句</param>
        /// <param name="startRowIndex">开始行，0表示第一行</param>
        /// <param name="maximumRows">最大返回行数，0表示所有行</param>
        /// <returns></returns>
        public DbTable Query(SelectBuilder builder, Int64 startRowIndex, Int64 maximumRows)
        {
            return QueryByCache(builder, startRowIndex, maximumRows, (sb, start, max) =>
            {
                sb = PageSplit(sb, start, max);
                return Session.Query(sb.ToString(), sb.Parameters.ToArray());
            }, nameof(Query));
        }

        /// <summary>执行SQL查询，返回记录集</summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="ps">命令参数</param>
        /// <returns></returns>
        public DbTable Query(String sql, IDictionary<String, Object> ps = null)
        {
            return QueryByCache(sql, ps, "", (s, p, k3) => Session.Query(s, Db.CreateParameters(p)), nameof(Query));
        }

        /// <summary>执行SQL查询，返回总记录数</summary>
        /// <param name="sb">查询生成器</param>
        /// <returns></returns>
        public Int32 SelectCount(SelectBuilder sb)
        {
            return (Int32)QueryByCache(sb, "", "", (s, k2, k3) => Session.QueryCount(s), nameof(SelectCount));
        }

        /// <summary>执行SQL查询，返回总记录数</summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="type">命令类型，默认SQL文本</param>
        /// <param name="ps">命令参数</param>
        /// <returns></returns>
        public Int32 SelectCount(String sql, CommandType type, params IDataParameter[] ps)
        {
            return (Int32)QueryByCache(sql, type, ps, (s, t, p) => Session.QueryCount(s, t, p), nameof(SelectCount));
        }

        /// <summary>执行SQL语句，返回受影响的行数</summary>
        /// <param name="sql">SQL语句</param>
        /// <returns></returns>
        public Int32 Execute(String sql)
        {
            return ExecuteByCache(sql, "", "", (s, t, p) => Session.Execute(s));
        }

        /// <summary>执行插入语句并返回新增行的自动编号</summary>
        /// <param name="sql"></param>
        /// <returns>新增行的自动编号</returns>
        public Int64 InsertAndGetIdentity(String sql)
        {
            return ExecuteByCache(sql, "", "", (s, t, p) => Session.InsertAndGetIdentity(s));
        }

        /// <summary>执行SQL查询，返回记录集</summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="type">命令类型，默认SQL文本</param>
        /// <param name="ps">命令参数</param>
        /// <returns></returns>
        public DataSet Select(String sql, CommandType type, params IDataParameter[] ps)
        {
            return QueryByCache(sql, type, ps, (s, t, p) => Session.Query(s, t, p), nameof(Select));
        }

        /// <summary>执行SQL语句，返回受影响的行数</summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="type">命令类型，默认SQL文本</param>
        /// <param name="ps">命令参数</param>
        /// <returns></returns>
        public Int32 Execute(String sql, CommandType type, params IDataParameter[] ps)
        {
            return ExecuteByCache(sql, type, ps, (s, t, p) => Session.Execute(s, t, p));
        }

        /// <summary>执行插入语句并返回新增行的自动编号</summary>
        /// <param name="sql"></param>
        /// <param name="type">命令类型，默认SQL文本</param>
        /// <param name="ps">命令参数</param>
        /// <returns>新增行的自动编号</returns>
        public Int64 InsertAndGetIdentity(String sql, CommandType type, params IDataParameter[] ps)
        {
            return ExecuteByCache(sql, type, ps, (s, t, p) => Session.InsertAndGetIdentity(s, t, p));
        }

        /// <summary>执行SQL查询，返回记录集</summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="type">命令类型，默认SQL文本</param>
        /// <param name="ps">命令参数</param>
        /// <returns></returns>
        public DataSet Select(String sql, CommandType type, IDictionary<String, Object> ps)
        {
            return QueryByCache(sql, type, ps, (s, t, p) => Session.Query(s, t, Db.CreateParameters(p)), nameof(Select));
        }

        /// <summary>执行SQL语句，返回受影响的行数</summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="type">命令类型，默认SQL文本</param>
        /// <param name="ps">命令参数</param>
        /// <returns></returns>
        public Int32 Execute(String sql, CommandType type, IDictionary<String, Object> ps)
        {
            return ExecuteByCache(sql, type, ps, (s, t, p) => Session.Execute(s, t, Db.CreateParameters(p)));
        }

        /// <summary>执行SQL语句，返回结果中的第一行第一列</summary>
        /// <typeparam name="T">返回类型</typeparam>
        /// <param name="sql">SQL语句</param>
        /// <param name="type">命令类型，默认SQL文本</param>
        /// <param name="ps">命令参数</param>
        /// <returns></returns>
        public T ExecuteScalar<T>(String sql, CommandType type, IDictionary<String, Object> ps)
        {
            return ExecuteByCache(sql, type, ps, (s, t, p) => Session.ExecuteScalar<T>(s, t, Db.CreateParameters(p)));
        }
        #endregion

        #region 事务
        /// <summary>开始事务</summary>
        /// <remarks>
        /// Read Uncommitted: 允许读取脏数据，一个事务能看到另一个事务还没有提交的数据。（不会阻止其它操作）
        /// Read Committed: 确保事务读取的数据都必须是已经提交的数据。它限制了读取中间的，没有提交的，脏的数据。
        /// 但是它不能确保当事务重新去读取的时候，读的数据跟上次读的数据是一样的，也就是说当事务第一次读取完数据后，
        /// 该数据是可能被其他事务修改的，当它再去读取的时候，数据可能是不一样的。（数据隐藏，不阻止）
        /// Repeatable Read: 是一个更高级别的隔离级别，如果事务再去读取同样的数据，先前的数据是没有被修改过的。（阻止其它修改）
        /// Serializable: 它做出了最有力的保证，除了每次读取的数据是一样的，它还确保每次读取没有新的数据。（阻止其它添删改）
        /// </remarks>
        /// <param name="level">事务隔离等级</param>
        /// <returns>剩下的事务计数</returns>
        public Int32 BeginTransaction(IsolationLevel level = IsolationLevel.ReadCommitted)
        {
            CheckDatabase();

            return Session.BeginTransaction(level);
        }

        /// <summary>提交事务</summary>
        /// <returns>剩下的事务计数</returns>
        public Int32 Commit() => Session.Commit();

        /// <summary>回滚事务，忽略异常</summary>
        /// <returns>剩下的事务计数</returns>
        public Int32 Rollback() => Session.Rollback();
        #endregion

        #region 缓存
        /// <summary>缓存存储</summary>
        public DictionaryCache<String, Object> Store { get; set; }

        /// <summary>数据层缓存。默认10秒</summary>
        public Int32 Expire { get; set; }

        private DictionaryCache<String, Object> GetCache()
        {
            var st = Store;
            if (st != null) return st;

            var exp = Expire;
            if (exp == 0) exp = Db.DataCache;
            if (exp == 0) exp = Setting.Current.DataCacheExpire;
            if (exp <= 0) return null;

            Expire = exp;

            lock (this)
            {
                if (Store == null)
                {
                    var p = exp / 2;
                    if (p < 30) p = 30;

                    st = Store = new DictionaryCache<String, Object> { Period = p, Expire = exp };
                }
            }

            return st;
        }

        private TResult QueryByCache<T1, T2, T3, TResult>(T1 k1, T2 k2, T3 k3, Func<T1, T2, T3, TResult> callback, String prefix = null)
        {
            CheckDatabase();

            // 读缓存
            var cache = GetCache();
            if (cache != null)
            {
                var sb = Pool.StringBuilder.Get();
                if (!prefix.IsNullOrEmpty())
                {
                    sb.Append(prefix);
                    sb.Append("#");
                }
                Append(sb, k1);
                Append(sb, k2);
                Append(sb, k3);
                var key = sb.Put(true);

                return cache.GetItem(key, k =>
                {
                    Interlocked.Increment(ref _QueryTimes);
                    return callback(k1, k2, k3);
                }).ChangeType<TResult>();
            }

            Interlocked.Increment(ref _QueryTimes);

            return callback(k1, k2, k3);
        }

        private TResult ExecuteByCache<T1, T2, T3, TResult>(T1 k1, T2 k2, T3 k3, Func<T1, T2, T3, TResult> callback)
        {
            if (Db.Readonly) throw new InvalidOperationException($"数据连接[{ConnName}]只读，禁止执行{k1}");

            CheckDatabase();

            var rs = callback(k1, k2, k3);

            var st = GetCache();
            st?.Clear();

            Interlocked.Increment(ref _ExecuteTimes);

            return rs;
        }

        private static void Append(StringBuilder sb, Object value)
        {
            if (value == null) return;

            if (value is SelectBuilder builder)
            {
                sb.Append(builder);
                foreach (var item in builder.Parameters)
                {
                    sb.Append("#");
                    sb.Append(item.ParameterName);
                    sb.Append("#");
                    sb.Append(item.Value);
                }
            }
            else if (value is IDataParameter[] ps)
            {
                foreach (var item in ps)
                {
                    sb.Append("#");
                    sb.Append(item.ParameterName);
                    sb.Append("#");
                    sb.Append(item.Value);
                }
            }
            else if (value is IDictionary<String, Object> dic)
            {
                foreach (var item in dic)
                {
                    sb.Append("#");
                    sb.Append(item.Key);
                    sb.Append("#");
                    sb.Append(item.Value);
                }
            }
            else
            {
                sb.Append("#");
                sb.Append(value);
            }
        }
        #endregion

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
            // 二进制读写器
            var bn = new Binary
            {
                EncodeInt = true,
                Stream = stream,
            };

            // 原始位置和行数位置
            var pOri = stream.Position;
            var pCount = 0L;

            var sb = new SelectBuilder
            {
                Table = Db.FormatTableName(table)
            };

            var row = 0;
            var pageSize = 5000;
            var total = 0;
            var sw = Stopwatch.StartNew();
            while (true)
            {
                // 分页
                var sb2 = PageSplit(sb, row, pageSize);

                // 查询数据
                var dt = Session.Query(sb2.ToString(), null);
                if (dt == null) break;

                // 写头部结构。没有数据时可以备份结构
                if (row == 0)
                {
                    dt.WriteHeader(bn);

                    // 数据行数，占位
                    pCount = stream.Position - 4;
                }

                var rs = dt.Rows;
                if (rs == null || rs.Count == 0) break;

                WriteLog("备份[{0}/{1}]数据 {2:n0} + {3:n0}", table, ConnName, row, rs.Count);

                // 写入数据
                dt.WriteData(bn);

                // 进度报告
                progress?.Invoke(row, dt);

                // 下一页
                total += rs.Count;
                if (rs.Count < pageSize) break;
                row += pageSize;
            }

            if (total > 0)
            {
                // 更新行数
                var p = stream.Position;
                stream.Position = pCount;
                bn.Write(total.GetBytes(), 0, 4);
                stream.Position = p;
            }

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
        public IDictionary<String, Int32> BackupAll(String[] tables, String dir, Boolean backupSchema = false)
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
                    File.WriteAllText(dir.CombinePath(ConnName + ".xml"), xml);
                }

                foreach (var item in tables)
                {
                    dic[item] = Backup(item, dir.CombinePath(item + ".table"));
                }
            }

            return dic;
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
            var pageSize = 5000;
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
                //tables = Tables.ToArray();
                var tbls = Tables;
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
                    dic[item.Name] = Restore(dir.CombinePath(item + ".table"), item);
                }
            }

            return dic;
        }
        #endregion
    }
}