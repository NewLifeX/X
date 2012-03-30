using System;
using System.Collections.Generic;
using System.Text;
using System.Data.Common;
using XCode.Exceptions;

namespace XCode.DataAccessLayer
{
    /// <summary>
    /// 分布式数据库。同时操作多个数据库，多读多写，读写分析。
    /// 支持：
    /// 1，一主多从写入（主库同步写入从库异步写入）；
    /// 2，多主多从写入（主库同步写入从库异步写入）；
    /// 3，按权重分布式读取；
    /// </summary>
    /// <remarks>
    /// 1，通过连接字符串配置读写服务器组，并加上权重，如“WriteServer='connA*1,connC*0' ReadServer='connB*8,connD'”；
    /// 2，对于写服务器，权重大于0表示作为主服务器，操作返回值取主服务器操作总和，等于0表示作为从服务器，采用异步方式写入，不设置权重表示0，全部不设置权重表示1；
    /// 3，对于读服务器，默认根据权重进行随机分配，不设置表示1；
    /// 4，对于读服务器，可优先考虑最近使用的数据库
    /// </remarks>
    class Distributed : DbBase
    {
        #region 属性
        /// <summary>返回数据库类型。</summary>
        public override DatabaseType DbType
        {
            get { return DatabaseType.Distributed; }
        }

        /// <summary>工厂</summary>
        public override DbProviderFactory Factory
        {
            get { throw new NotSupportedException(); }
        }
        #endregion

        #region 方法
        /// <summary>创建数据库会话</summary>
        /// <returns></returns>
        protected override IDbSession OnCreateSession()
        {
            return new DistributedDbSession();
        }

        /// <summary>创建元数据对象</summary>
        /// <returns></returns>
        protected override IMetaData OnCreateMetaData()
        {
            return new DistributedDbMetaData();
        }
        #endregion

        #region 读写服务器集合
        private Server[] _WriteServers;
        /// <summary>写入服务器集合</summary>
        public Server[] WriteServers
        {
            get { return _WriteServers; }
            set { _WriteServers = value; }
        }

        private Server[] _ReadServers;
        /// <summary>读取服务器集合</summary>
        public Server[] ReadServers
        {
            get { return _ReadServers; }
            set { _ReadServers = value; }
        }

        const String ExceptionMessage1 = "缺少写入服务器配置WriteServer！";
        const String ExceptionMessage2 = "缺少读取服务器配置ReadServer！";

        void LoadConfig(String connStr)
        {
            if (String.IsNullOrEmpty(connStr)) return;

            DbConnectionStringBuilder builder = new DbConnectionStringBuilder();
            builder.ConnectionString = connStr;

            if (builder.ContainsKey("WriteServer")) throw new XDbException(this, ExceptionMessage1);
            if (builder.ContainsKey("ReadServer")) throw new XDbException(this, ExceptionMessage2);

            String ws = (String)builder["WriteServer"];
            String rs = (String)builder["ReadServer"];
            if (String.IsNullOrEmpty(ws)) throw new XDbException(this, ExceptionMessage1);
            if (String.IsNullOrEmpty(rs)) throw new XDbException(this, ExceptionMessage2);

            #region 加载写入服务器
            String[] ss = ws.Split(new Char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            if (ss == null || ss.Length < 1) throw new XDbException(this, ExceptionMessage1);

            List<Server> list = new List<Server>();
            foreach (String item in ss)
            {
                String name = item.Trim();
                if (String.IsNullOrEmpty(name)) continue;

                Int32 p = name.IndexOf("*");
                if (p > 0)
                    list.Add(new Server(name.Substring(0, p), Int32.Parse(name.Substring(p + 1))));
                else
                    list.Add(new Server(name, 0));
            }
            // 按权重降序
            list.Sort(delegate(Server item1, Server item2) { return -1 * item1.Weight.CompareTo(item2.Weight); });
            WriteServers = list.ToArray();
            #endregion

            #region 加载读取服务器
            ss = rs.Split(new Char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            if (ss == null || ss.Length < 1) throw new XDbException(this, ExceptionMessage2);

            list.Clear();
            foreach (String item in ss)
            {
                String name = item.Trim();
                if (String.IsNullOrEmpty(name)) continue;

                Int32 p = name.IndexOf("*");
                if (p > 0)
                    list.Add(new Server(name.Substring(0, p), Int32.Parse(name.Substring(p + 1))));
                else
                    list.Add(new Server(name, 1));
            }
            // 按权重降序
            list.Sort(delegate(Server item1, Server item2) { return -1 * item1.Weight.CompareTo(item2.Weight); });
            ReadServers = list.ToArray();
            #endregion
        }

        #region 服务器配置
        public class Server
        {
            private String _ConnName;
            /// <summary>连接名</summary>
            public String ConnName
            {
                get { return _ConnName; }
                set { _ConnName = value; }
            }

            private Int32 _Weight;
            /// <summary>权重</summary>
            public Int32 Weight
            {
                get { return _Weight; }
                set { _Weight = value; }
            }

            private IDatabase _Db;
            /// <summary>数据库对象</summary>
            public IDatabase Db
            {
                get
                {
                    if (_Db == null)
                    {
                        _Db = DAL.Create(ConnName).Db;
                    }
                    return _Db;
                }
                //set { _Db = value; }
            }

            public Server(String connname, Int32 weight)
            {
                ConnName = connname;
                Weight = weight;
            }
        }
        #endregion
        #endregion

        #region 获取数据库操作接口
        private Int32 _Inited = 0;
        /// <summary>初始化</summary>
        void Init()
        {
            if (_Inited > 0) return;
            _Inited++;

            LoadConfig(ConnectionString);
        }

        Random _Rnd;
        /// <summary>随机数产生器</summary>
        /// <returns></returns>
        Random GetRnd()
        {
            if (_Rnd == null) _Rnd = new Random((Int32)DateTime.Now.Ticks);
            return _Rnd;
        }

        /// <summary>获取一个用于读取的数据库对象</summary>
        /// <returns></returns>
        public IDatabase GetReadDb()
        {
            Init();

            // 计算权重总和
            Int32 weight = 0;
            foreach (Server item in ReadServers)
            {
                weight += item.Weight;
            }

            // 随机抽取
            Int32 index = GetRnd().Next(0, weight);
            foreach (Server item in ReadServers)
            {
                if (index < item.Weight) return item.Db;

                index -= item.Weight;
            }

            throw new XCodeException("设计错误，不应该到达这里！");
        }
        #endregion
    }

    class DistributedDbSession : DbSession
    {

    }

    class DistributedDbMetaData : DbMetaData
    {

    }
}