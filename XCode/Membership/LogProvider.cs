using System;
using System.Linq;
using System.Text;
using System.Web;
using NewLife.Collections;
using NewLife.Log;
using NewLife.Model;
using NewLife.Reflection;

namespace XCode.Membership
{
    /// <summary>日志提供者。提供业务日志输出到数据库的功能</summary>
    public abstract class LogProvider
    {
        #region 基本功能
        /// <summary>写日志</summary>
        /// <param name="category">类型</param>
        /// <param name="action">操作</param>
        /// <param name="remark">备注</param>
        /// <param name="userid">用户</param>
        /// <param name="name">名称</param>
        /// <param name="ip">地址</param>
        public abstract void WriteLog(String category, String action, String remark, Int32 userid = 0, String name = null, String ip = null);

        /// <summary>写日志</summary>
        /// <param name="type">类型</param>
        /// <param name="action">操作</param>
        /// <param name="remark">备注</param>
        /// <param name="userid">用户</param>
        /// <param name="name">名称</param>
        /// <param name="ip">地址</param>
        public virtual void WriteLog(Type type, String action, String remark, Int32 userid = 0, String name = null, String ip = null)
        {
            var cat = "";
            if (type.As<IEntity>())
            {
                var fact = EntityFactory.CreateOperate(type);
                if (fact != null) cat = fact.Table.DataTable.DisplayName;
            }
            if (cat.IsNullOrEmpty()) cat = type.GetDisplayName() ?? type.GetDescription() ?? type.Name;
            WriteLog(cat, action, remark, userid, name, ip);
        }

        /// <summary>输出实体对象日志</summary>
        /// <param name="action"></param>
        /// <param name="entity"></param>
        public void WriteLog(String action, IEntity entity)
        {
            if (!Enable) return;

            var fact = EntityFactory.CreateOperate(entity.GetType());

            // 构造字段数据的字符串表示形式
            var sb = Pool.StringBuilder.Get();
            foreach (var fi in fact.Fields)
            {
                if (action == "修改" && !fi.PrimaryKey && !entity.IsDirty(fi.Name)) continue;
                var v = entity[fi.Name];
                // 空字符串不写日志
                if (action == "添加" || action == "删除")
                {
                    if (v + "" == "") continue;
                    if (v is Boolean && (Boolean)v == false) continue;
                    if (v is Int32 && (Int32)v == 0) continue;
                    if (v is DateTime && (DateTime)v == DateTime.MinValue) continue;
                }

                // 日志里面不要出现密码
                if (fi.Name.EqualIgnoreCase("pass", "password")) v = null;

                sb.Separate(",").Append("{0}={1}".F(fi.Name, v));
            }

            var userid = 0;
            var name = "";
            if (entity is IManageUser user)
            {
                userid = user.ID;
                name = user + "";
            }

            WriteLog(entity.GetType(), action, sb.Put(true), userid, name);
        }

        /// <summary>是否使用日志</summary>
        public Boolean Enable { get; set; } = true;
        #endregion

        #region 日志转换
        /// <summary>转为标准日志接口</summary>
        /// <param name="category">日志分类</param>
        /// <returns></returns>
        public NewLife.Log.ILog AsLog(String category) => new DbLog { Provider = this, Category = category };

        class DbLog : Logger
        {
            public LogProvider Provider { get; set; }
            public String Category { get; set; }

            protected override void OnWrite(LogLevel level, String format, params Object[] args)
            {
                var msg = format.F(args);
                var act = "";
                var p = msg.IndexOf(' ');
                if (p > 0)
                {
                    act = msg.Substring(0, p).Trim();
                    msg = msg.Substring(p + 1).Trim();
                }

                // 从参数里提取用户对象
                var user = args.FirstOrDefault(e => e is IManageUser) as IManageUser;

                Provider.WriteLog(Category, act, msg, user?.ID ?? 0, user + "");
            }
        }
        #endregion

        #region 静态属性
        static LogProvider() => ObjectContainer.Current.AutoRegister<LogProvider, DefaultLogProvider>();

        private static LogProvider _Provider;
        /// <summary>当前成员提供者</summary>
        public static LogProvider Provider
        {
            get
            {
                if (_Provider == null) _Provider = ObjectContainer.Current.Resolve<LogProvider>();
                return _Provider;
            }
            set { _Provider = value; }
        }
        #endregion
    }

    /// <summary>泛型日志提供者，使用泛型日志实体基类作为派生</summary>
    /// <typeparam name="TLog"></typeparam>
    public class LogProvider<TLog> : LogProvider where TLog : Log<TLog>, new()
    {
        #region 提供者
        /// <summary>当前用户提供者</summary>
        public IManageProvider Provider2 { get; set; }
        #endregion

        /// <summary>写日志</summary>
        /// <param name="category">类型</param>
        /// <param name="action">操作</param>
        /// <param name="remark">备注</param>
        /// <param name="userid">用户</param>
        /// <param name="name">名称</param>
        /// <param name="ip">地址</param>
        public override void WriteLog(String category, String action, String remark, Int32 userid = 0, String name = null, String ip = null)
        {
            if (!Enable) return;
            var factory = EntityFactory.CreateOperate(typeof(TLog));
            var log = factory.Create() as ILog;
            log.Category = category ?? throw new ArgumentNullException(nameof(category));
            log.Action = action;

            // 加上关联编号
            if (remark.StartsWithIgnoreCase("ID="))
            {
                var fi = factory.Table.Identity;
                if (fi != null) log.LinkID = remark.Substring("ID=", ",").ToInt();
            }

            if (userid > 0) log.CreateUserID = userid;
            if (!name.IsNullOrEmpty()) log.UserName = name;
            if (!ip.IsNullOrEmpty()) log.CreateIP = ip;

            // 获取当前登录信息
            if (log.CreateUserID == 0 || name.IsNullOrEmpty())
            {
                // 当前登录用户
                var prv = Provider2 ?? ManageProvider.Provider;
#if !__CORE__
                var user = prv?.Current ?? HttpContext.Current?.User?.Identity as IManageUser;
#else
                var user = prv?.Current;
#endif
                if (user != null)
                {
                    if (log.CreateUserID == 0) log.CreateUserID = user.ID;
                    if (log.UserName.IsNullOrEmpty()) log.UserName = user + "";
                }
            }

            log.Remark = remark;
            log.SaveAsync();
        }
    }

    /// <summary>默认日志提供者，使用实体类<seealso cref="Log"/></summary>
    class DefaultLogProvider : LogProvider<Log> { }
}