using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NewLife.Data;
using NewLife.Log;
using NewLife.Net;
using NewLife.Reflection;
using NewLife.Remoting;
using NewLife.Security;
using NewLife.Serialization;

namespace XCode.Service
{
    /// <summary>数据客户端</summary>
    public class DbClient : ApiClient
    {
        #region 属性
        /// <summary>数据库连接</summary>
        public String Db { get; set; }

        /// <summary>用户名</summary>
        public String UserName { get; set; }

        /// <summary>密码</summary>
        public String Password { get; set; }

        /// <summary>最后一次登录成功后的消息</summary>
        public LoginInfo Info { get; private set; }
        #endregion

        #region 方法
        /// <summary>实例化</summary>
        public DbClient()
        {
#if DEBUG
            Log = XTrace.Log;
            EncoderLog = XTrace.Log;
#endif
        }

        /// <summary>实例化</summary>
        /// <param name="uri"></param>
        public DbClient(String uri)
        {
            if (!uri.IsNullOrEmpty())
            {
                var u = new Uri(uri);

                Servers = new[] { "{2}://{0}:{1}".F(u.Host, u.Port, u.Scheme) };

                Db = u.PathAndQuery.Split("/").FirstOrDefault();
                var us = u.UserInfo.Split(":");
                UserName = us.Length > 0 ? us[0] : null;
                Password = us.Length > 1 ? us[1] : null;
            }
        }
        #endregion

        #region 登录
        /// <summary>连接后自动登录</summary>
        /// <param name="client">客户端</param>
        protected override async Task<Object> OnLoginAsync(ISocketClient client)
        {
            var cookie = Rand.NextString(16);
            var pass2 = cookie.GetBytes().RC4(Password.GetBytes()).ToBase64();

            var rs = await InvokeWithClientAsync<LoginInfo>(client, "Db/Login", new { Db, UserName, pass = pass2, cookie });
            if (Setting.Current.Debug) XTrace.WriteLine("登录{0}成功！{1}", Servers.FirstOrDefault(), rs.ToJson());

            return Info = rs;
        }
        #endregion

        #region 核心方法
        ///// <summary>异步连接</summary>
        ///// <returns></returns>
        //public async Task<LoginInfo> LoginAsync() => await LoginAsync(Db, UserName, Password);

        ///// <summary>异步登录</summary>
        ///// <param name="db">要访问的数据库</param>
        ///// <param name="user">用户名</param>
        ///// <param name="pass">密码</param>
        ///// <returns></returns>
        //protected async Task<LoginInfo> LoginAsync(String db, String user, String pass)
        //{
        //    var cookie = Rand.NextString(16);
        //    var pass2 = cookie.GetBytes().RC4(pass.GetBytes()).ToBase64();

        //    var rs = await InvokeAsync<LoginInfo>("Db/Login", new { db, user, pass = pass2, cookie });
        //    if (Setting.Current.Debug) XTrace.WriteLine("登录{0}成功！{1}", Servers.FirstOrDefault(), rs.ToJson());

        //    return rs;
        //}

        /// <summary>异步查询</summary>
        /// <param name="sql">语句</param>
        /// <param name="ps">参数集合</param>
        /// <returns></returns>
        public async Task<DbTable> QueryAsync(String sql, IDictionary<String, Object> ps = null)
        {
            var arg = Encode(sql, ps);

            var rs = await InvokeAsync<Packet>("Db/Query", arg);
            //if (rs == null || rs.Total == 0) return null;

            var ds = new DbTable();
            ds.Read(rs);

            return ds;
        }

        /// <summary>异步查数据表总记录数</summary>
        /// <remarks>借助索引快速查询，但略有偏差</remarks>
        /// <param name="tableName">数据表</param>
        /// <returns></returns>
        public async Task<Int64> QueryCountAsync(String tableName)
        {
            //var arg = Encode(tableName, null);

            return await InvokeAsync<Int64>("Db/QueryCount", new { tableName });
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
        #endregion

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