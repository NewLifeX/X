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
    public class DbController
    {
        /// <summary>查询数据</summary>
        /// <param name="pk"></param>
        /// <returns></returns>
        [Api(nameof(Query))]
        public Packet Query(Packet pk)
        {
            if (!Decode(pk, out var db, out var sql, out var ps)) return null;

            var dal = DAL.Create(db);
            var dps = ps == null ? null : dal.Db.CreateParameters(ps);

            var rs = dal.Query(sql, dr =>
            {
                var ds = new DbSet();
                ds.Read(dr);

                return ds;
            }, dps);

            return rs?.ToPacket();
        }

        /// <summary>查询数据表总记录数</summary>
        /// <param name="pk"></param>
        /// <returns></returns>
        [Api(nameof(QueryCount))]
        public Int64 QueryCount(Packet pk)
        {
            if (!Decode(pk, out var db, out var table, out _)) return -1;

            var dal = DAL.Create(db);

            return dal.Session.QueryCountFast(table);
        }

        /// <summary>执行语句</summary>
        /// <param name="pk"></param>
        /// <returns></returns>
        [Api(nameof(Execute))]
        public Int64 Execute(Packet pk)
        {
            if (!Decode(pk, out var db, out var sql, out var ps)) return -1;

            var dal = DAL.Create(db);
            var dps = ps == null ? null : dal.Db.CreateParameters(ps);

            var rs = 0L;
            if (sql.StartsWithIgnoreCase("@Insert"))
                rs = dal.InsertAndGetIdentity(sql.Substring(1), CommandType.Text, dps);
            else
                rs = dal.Execute(sql, CommandType.Text, dps);

            return rs;
        }

        #region 辅助
        private Boolean Decode(Packet pk, out String db, out String sql, out IDictionary<String, Object> ps)
        {
            db = null;
            sql = null;
            ps = null;

            if (pk == null || pk.Total == 0) return false;

            var ms = pk.GetStream();
            var bn = new Binary { EncodeInt = true, Stream = ms };
            db = bn.Read<String>();
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
        #endregion
    }
}