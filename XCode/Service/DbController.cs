using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NewLife.Data;
using NewLife.Reflection;
using NewLife.Remoting;
using NewLife.Serialization;
using XCode.DataAccessLayer;

namespace XCode.Service
{
    /// <summary>数据服务</summary>
    [Api("Db")]
    public class DbController : IActionFilter
    {
        /// <summary>数据操作层</summary>
        public DAL Dal { get => ControllerContext.Current.Session["Dal"] as DAL; set => ControllerContext.Current.Session["Dal"] = value; }

        /// <summary>登录</summary>
        /// <param name="db"></param>
        /// <param name="user"></param>
        /// <param name="pass"></param>
        /// <param name="cookie"></param>
        /// <returns></returns>
        [Api(nameof(Login))]
        public LoginInfo Login(String db, String user, String pass, String cookie)
        {
            var dal = DAL.Create(db);
            Dal = dal;

            return new LoginInfo
            {
                DbType = dal.DbType,
                Version = dal.Db.ServerVersion,
            };
        }

        /// <summary>查询数据</summary>
        /// <param name="pk"></param>
        /// <returns></returns>
        [Api(nameof(Query))]
        public Packet Query(Packet pk)
        {
            if (!Decode(pk, out var sql, out var ps)) return null;

            var dal = Dal;

            var rs = dal.Query(sql, ps);

            return rs?.ToPacket();
        }

        /// <summary>查询数据表总记录数</summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        [Api(nameof(QueryCount))]
        public Int64 QueryCount(String tableName)
        {
            //if (!Decode(pk, out var table, out _)) return -1;

            return Dal.Session.QueryCountFast(tableName);
        }

        /// <summary>执行语句</summary>
        /// <param name="pk"></param>
        /// <returns></returns>
        [Api(nameof(Execute))]
        public Int64 Execute(Packet pk)
        {
            if (!Decode(pk, out var sql, out var ps)) return -1;

            var dal = Dal;
            var dps = ps == null ? null : dal.Db.CreateParameters(ps);

            var rs = 0L;
            if (sql.StartsWithIgnoreCase("@Insert"))
                rs = dal.InsertAndGetIdentity(sql.Substring(1), CommandType.Text, dps);
            else
                rs = dal.Execute(sql, CommandType.Text, dps);

            return rs;
        }

        #region 辅助
        private Boolean Decode(Packet pk, out String sql, out IDictionary<String, Object> ps)
        {
            sql = null;
            ps = null;

            if (pk == null || pk.Total == 0) return false;

            var ms = pk.GetStream();
            var bn = new Binary { EncodeInt = true, Stream = ms };
            sql = bn.Read<String>();

            // 如果还有数据，就是参数
            if (ms.Position < ms.Length)
            {
                var count = bn.Read<Int32>();
                if (count > 0)
                {
                    var dic = new Dictionary<String, Object>();
                    for (var i = 0; i < count; i++)
                    {
                        var name = bn.Read<String>();
                        var tc = (TypeCode)bn.Read<Byte>();
                        var type = tc.ToString().GetTypeEx(false);
                        var value = bn.Read(type);

                        dic[name] = value;
                    }
                    ps = dic;
                }
            }

            return true;
        }

        /// <summary>执行前</summary>
        /// <param name="filterContext"></param>
        public void OnActionExecuting(ControllerContext filterContext)
        {
            var dal = Dal;
            if (dal == null && filterContext.ActionName != "Db/Login") throw new ApiException(401, "未登录！");
        }

        /// <summary>执行后</summary>
        /// <param name="filterContext"></param>
        public void OnActionExecuted(ControllerContext filterContext) { }
        #endregion
    }
}