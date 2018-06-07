using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NewLife.Data;
using NewLife.Reflection;
using NewLife.Remoting;
using NewLife.Security;
using NewLife.Serialization;

namespace XCode.Service
{
    /// <summary>数据客户端</summary>
    public class DbClient : ApiClient
    {
        /// <summary>异步登录</summary>
        /// <param name="db">要访问的数据库</param>
        /// <param name="user">用户名</param>
        /// <param name="pass">密码</param>
        /// <returns></returns>
        public async Task<IDictionary<String,Object>> LoginAsync(String db, String user, String pass)
        {
            var cookie = Rand.NextString(16);
            var pass2 = cookie.GetBytes().RC4(pass.GetBytes()).ToBase64();

            return await InvokeAsync<IDictionary<String, Object>>("Db/Login", new { db, user, pass = pass2, cookie });
        }

        /// <summary>异步查询</summary>
        /// <param name="sql">语句</param>
        /// <param name="ps">参数集合</param>
        /// <returns></returns>
        public async Task<DbSet> QueryAsync(String sql, IDictionary<String, Object> ps = null)
        {
            var arg = Encode(sql, ps);

            var rs = await InvokeAsync<Packet>("Db/Query", arg);
            //if (rs == null || rs.Total == 0) return null;

            var ds = new DbSet();
            ds.Read(rs);

            return ds;
        }

        /// <summary>异步查数据表总记录数</summary>
        /// <remarks>借助索引快速查询，但略有偏差</remarks>
        /// <param name="table">数据表</param>
        /// <param name="ps">参数集合</param>
        /// <returns></returns>
        public async Task<Int64> QueryCountAsync(String table, IDictionary<String, Object> ps = null)
        {
            var arg = Encode(table, ps);

            return await InvokeAsync<Int64>("Db/QueryCount", arg);
        }

        /// <summary>异步执行</summary>
        /// <param name="sql">语句</param>
        /// <param name="ps">参数集合</param>
        /// <returns></returns>
        public async Task<Int64> ExecuteAsync(String sql, IDictionary<String, Object> ps = null)
        {
            var arg = Encode(sql, ps);

            return await InvokeAsync<Int64>("Db/Execute", arg);
        }

        #region 辅助
        private Packet Encode(String sql, IDictionary<String, Object> ps)
        {
            // 头部预留8字节，方便加协议头
            var bn = new Binary { EncodeInt = true };
            bn.Stream.Seek(8, SeekOrigin.Current);

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