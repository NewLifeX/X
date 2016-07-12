using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using NewLife.Security;
using XCode.DataAccessLayer;
using XCode.Membership;

namespace NewLife.Cube.Admin.Controllers
{
    /// <summary>数据库管理</summary>
    [DisplayName("数据库管理")]
    [EntityAuthorize(PermissionFlags.Detail)]
    public class DbController : ControllerBaseX
    {
        /// <summary>数据库列表</summary>
        /// <returns></returns>
        public ActionResult Index()
        {
            var list = new List<DbItem>();

            // 读取配置文件
            var css = new HashSet<String>(StringComparer.OrdinalIgnoreCase);
            foreach (ConnectionStringSettings set in ConfigurationManager.ConnectionStrings)
            {
                if (!css.Contains(set.Name)) css.Add(set.Name);
            }

            foreach (var item in DAL.ConnStrs)
            {
                var di = new DbItem();
                di.Name = item.Key;
                di.ConnStr = item.Value.ConnectionString;

                //var type = DbFactory.GetProviderType(item.Value.ConnectionString, item.Value.ProviderName);
                //if (type != null)
                //{
                //    var db = Activator.CreateInstance(type) as IDatabase;
                //    if (db != null)
                //    {
                //        di.Type = db.DbType;
                //        if (!di.ConnStr.IsNullOrEmpty()) di.Version = db.ServerVersion;
                //    }
                //}
                var dal = DAL.Create(item.Key);
                di.Type = dal.DbType;
                try
                {
                    di.Version = dal.Db.ServerVersion;
                }
                catch { }

                if (!css.Contains(di.Name)) di.Dynamic = true;

                di.Backups = Rand.Next(5);

                list.Add(di);
            }

            return View(list);
        }

    }
}
