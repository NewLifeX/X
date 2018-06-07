using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NewLife.Data;
using NewLife.Reflection;
using NewLife.Remoting;
using NewLife.Serialization;

namespace XCode.Service
{
    /// <summary>数据客户端</summary>
    public class DbClient : ApiClient
    {
        /// <summary>异步查询</summary>
        /// <param name="db">数据库</param>
        /// <param name="sql">语句</param>
        /// <param name="ps">参数集合</param>
        /// <returns></returns>
        public async Task<DbSet> QueryAsync(String db, String sql, IDictionary<String, Object> ps)
        {
            var arg = Encode(db, sql, ps);

            var rs = await InvokeAsync<Packet>("Db/Query", arg);
            //if (rs == null || rs.Total == 0) return null;

            var ds = new DbSet();
            ds.Read(rs);

            return ds;
        }

        /// <summary>异步查数据表总记录数</summary>
        /// <remarks>借助索引快速查询，但略有偏差</remarks>
        /// <param name="db">数据库</param>
        /// <param name="table">数据表</param>
        /// <param name="ps">参数集合</param>
        /// <returns></returns>
        public async Task<Int64> QueryCountAsync(String db, String table, IDictionary<String, Object> ps)
        {
            var arg = Encode(db, table, ps);

            return await InvokeAsync<Int64>("Db/QueryCount", arg);
        }

        /// <summary>异步执行</summary>
        /// <param name="db">数据库</param>
        /// <param name="sql">语句</param>
        /// <param name="ps">参数集合</param>
        /// <returns></returns>
        public async Task<Int64> ExecuteAsync(String db, String sql, IDictionary<String, Object> ps)
        {
            var arg = Encode(db, sql, ps);

            return await InvokeAsync<Int64>("Db/Execute", arg);
        }

        #region 辅助
        private Packet Encode(String db, String sql, IDictionary<String, Object> ps)
        {
            // 头部预留8字节，方便加协议头
            var bn = new Binary { EncodeInt = true };
            bn.Stream.Seek(8, SeekOrigin.Current);

            bn.Write(db);
            bn.Write(sql);

            if (ps != null && ps.Count > 0)
            {
                bn.Write(ps.Count);
                foreach (var item in ps)
                {
                    bn.Write(item.Key);

                    var tc = item.Value.GetType().GetTypeCode();
                    if (tc == TypeCode.Object) throw new NotSupportedException($"数据参数不支持类型{item.Value.GetType().FullName}");

                    bn.Write((Byte)tc);
                    bn.Write(item.Value);
                }
            }

            var ms = bn.Stream;
            ms.Position = 8;

            return new Packet(ms);
        }
        #endregion
    }
}