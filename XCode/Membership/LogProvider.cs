using System;
using System.Linq;
using NewLife;
using NewLife.Collections;
using NewLife.Log;
using NewLife.Model;
using NewLife.Reflection;

namespace XCode.Membership
{
    /// <summary>日志提供者。提供业务日志输出到数据库的功能</summary>
    public class LogProvider
    {
        #region 基本功能
        /// <summary>写日志</summary>
        /// <param name="category">类型</param>
        /// <param name="action">操作</param>
        /// <param name="remark">备注</param>
        /// <param name="userid">用户</param>
        /// <param name="name">名称</param>
        /// <param name="ip">地址</param>
        [Obsolete]
        public virtual void WriteLog(String category, String action, String remark, Int32 userid = 0, String name = null, String ip = null) => WriteLog(category, action, true, remark, userid, name, ip);

        /// <summary>写日志</summary>
        /// <param name="type">类型</param>
        /// <param name="action">操作</param>
        /// <param name="remark">备注</param>
        /// <param name="userid">用户</param>
        /// <param name="name">名称</param>
        /// <param name="ip">地址</param>
        [Obsolete]
        public virtual void WriteLog(Type type, String action, String remark, Int32 userid = 0, String name = null, String ip = null) => WriteLog(type, action, true, remark, userid, name, ip);

        /// <summary>创建日志，未写入</summary>
        /// <param name="category">类型</param>
        /// <param name="action">操作</param>
        /// <param name="success">成功</param>
        /// <param name="remark">备注</param>
        /// <param name="userid">用户</param>
        /// <param name="name">名称</param>
        /// <param name="ip">地址</param>
        public virtual Log CreateLog(String category, String action, Boolean success, String remark, Int32 userid = 0, String name = null, String ip = null)
        {
            if (category.IsNullOrEmpty()) throw new ArgumentNullException(nameof(category));

            var factory = EntityFactory.CreateOperate(typeof(Log));
            var log = factory.Create() as Log;
            log.Category = category;
            log.Action = action;
            log.Success = success;

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
                //var user = prv?.Current ?? HttpContext.Current?.User?.Identity as IManageUser;
                var user = prv?.Current;
                if (user != null)
                {
                    if (log.CreateUserID == 0) log.CreateUserID = user.ID;
                    if (log.UserName.IsNullOrEmpty()) log.UserName = user + "";
                }
            }
            if (log.CreateIP.IsNullOrEmpty()) log.CreateIP = ManageProvider.UserHost;

            log.Remark = remark;
            log.CreateTime = DateTime.Now;

            return log;
        }

        /// <summary>创建日志，未写入</summary>
        /// <param name="type">实体类型</param>
        /// <param name="action">操作</param>
        /// <param name="success">成功</param>
        /// <param name="remark">备注</param>
        /// <param name="userid">用户</param>
        /// <param name="name">名称</param>
        /// <param name="ip">地址</param>
        /// <param name="linkid">关联编号</param>
        public virtual Log CreateLog(Type type, String action, Boolean success, String remark, Int32 userid = 0, String name = null, String ip = null, Int32 linkid = 0)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));

            var cat = "";
            if (type.As<IEntity>())
            {
                var fact = EntityFactory.CreateOperate(type);
                if (fact != null) cat = fact.Table.DataTable.DisplayName;
            }
            if (cat.IsNullOrEmpty()) cat = type.GetDisplayName() ?? type.GetDescription() ?? type.Name;

            var log = CreateLog(cat, action, success, remark, userid, name, ip);
            if (linkid > 0) log.LinkID = linkid;

            return log;
        }

        /// <summary>写日志</summary>
        /// <param name="category">类型</param>
        /// <param name="action">操作</param>
        /// <param name="success">成功</param>
        /// <param name="remark">备注</param>
        /// <param name="userid">用户</param>
        /// <param name="name">名称</param>
        /// <param name="ip">地址</param>
        public virtual void WriteLog(String category, String action, Boolean success, String remark, Int32 userid = 0, String name = null, String ip = null)
        {
            if (!Enable) return;

            var log = CreateLog(category, action, success, remark, userid, name, ip);

            log.SaveAsync();
        }

        /// <summary>写日志</summary>
        /// <param name="type">类型</param>
        /// <param name="action">操作</param>
        /// <param name="success">成功</param>
        /// <param name="remark">备注</param>
        /// <param name="userid">用户</param>
        /// <param name="name">名称</param>
        /// <param name="ip">地址</param>
        public virtual void WriteLog(Type type, String action, Boolean success, String remark, Int32 userid = 0, String name = null, String ip = null)
        {
            if (!Enable) return;

            var log = CreateLog(type, action, success, remark, userid, name, ip);

            log.SaveAsync();
        }

        /// <summary>输出实体对象日志</summary>
        /// <param name="action">操作</param>
        /// <param name="entity">实体</param>
        /// <param name="error">错误信息</param>
        public void WriteLog(String action, IEntity entity, String error = null)
        {
            if (!Enable) return;

            var type = entity.GetType();
            var fact = EntityFactory.CreateOperate(type);

            // 构造字段数据的字符串表示形式
            var sb = Pool.StringBuilder.Get();
            if (error.IsNullOrEmpty()) sb.Append(error);
            foreach (var fi in fact.Fields)
            {
                if ((action == "修改" || action == "Update") && !fi.PrimaryKey && !entity.IsDirty(fi.Name)) continue;

                var v = entity[fi.Name];
                // 空字符串不写日志
                if (action is "添加" or "删除" or "Insert" or "Delete")
                {
                    if (v + "" == "") continue;
                    if (v is Boolean b && !b) continue;
                    if (v is Int32 vi && vi == 0) continue;
                    if (v is DateTime dt && dt == DateTime.MinValue) continue;
                }

                // 日志里面不要出现密码
                if (fi.Name.EqualIgnoreCase("pass", "password")) v = null;

                sb.Separate(",").AppendFormat("{0}={1}", fi.Name, v);
            }

            // 对象链接
            var linkId = 0;
            var uk = fact.Unique;
            if (uk != null && uk.IsIdentity) linkId = entity[uk.Name].ToInt();

            var userid = 0;
            var name = "";
            if (entity is IManageUser user)
            {
                userid = user.ID;
                name = user + "";
            }

            //WriteLog(entity.GetType(), action, error.IsNullOrEmpty(), sb.Put(true), userid, name);
            var category = fact.Table.DataTable.DisplayName;
            if (category.IsNullOrEmpty()) category = type.GetDisplayName() ?? type.GetDescription() ?? type.Name;

            var log = CreateLog(category, action, error.IsNullOrEmpty(), sb.Put(true), userid, name);
            log.LinkID = linkId;

            log.SaveAsync();
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
                var msg = String.Format(format, args);
                var act = "";
                var p = msg.IndexOf(' ');
                if (p > 0)
                {
                    act = msg.Substring(0, p).Trim();
                    msg = msg.Substring(p + 1).Trim();
                }

                // 从参数里提取用户对象
                var user = args.FirstOrDefault(e => e is IManageUser) as IManageUser;

                Provider.WriteLog(Category, act, true, msg, user?.ID ?? 0, user + "");
            }
        }
        #endregion

        #region 静态属性
        /// <summary>当前成员提供者</summary>
        public static LogProvider Provider { get; set; } = new LogProvider();

        /// <summary>当前用户提供者</summary>
        public IManageProvider Provider2 { get; set; }
        #endregion
    }
}