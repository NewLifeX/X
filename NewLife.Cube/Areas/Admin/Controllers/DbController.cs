using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.IO;
using System.Web.Mvc;
using NewLife.Web;
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
        [EntityAuthorize(PermissionFlags.Detail)]
        public ActionResult Index()
        {
            var list = new List<DbItem>();
            var dir = XCode.Setting.Current.BackupPath.AsDirectory();

            // 读取配置文件
            foreach (var item in DAL.ConnStrs)
            {
                var di = new DbItem
                {
                    Name = item.Key,
                    ConnStr = item.Value
                };

                var dal = DAL.Create(item.Key);
                di.Type = dal.DbType;
                try
                {
                    di.Version = dal.Db.ServerVersion;
                }
                catch { }

                if (dir.Exists) di.Backups = dir.GetFiles("{0}_*".F(dal.ConnName), SearchOption.TopDirectoryOnly).Length;

                list.Add(di);
            }

            return View("Index", list);
        }

        /// <summary>持久化连接</summary>
        /// <param name="name"></param>
        /// <returns></returns>
        [EntityAuthorize(PermissionFlags.Update)]
        public ActionResult SetStatic(String name)
        {
            // 读取配置文件
            var css = ConfigurationManager.ConnectionStrings;
            var conns = new HashSet<String>(StringComparer.OrdinalIgnoreCase);
            foreach (ConnectionStringSettings set in css)
            {
                if (!conns.Contains(set.Name)) conns.Add(set.Name);
            }

            var msg = "";
            if (!DAL.ConnStrs.ContainsKey(name))
                msg = "找不到连接{0}".F(name);
            else if (conns.Contains(name))
                msg = "连接 {0} 已经存在于配置文件".F(name);
            else
            {
                try
                {
                    var dal = DAL.Create(name);
                    var set = new ConnectionStringSettings(name, dal.ConnStr);
                    //css.Add(set);

                    var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                    config.ConnectionStrings.ConnectionStrings.Add(set);
                    config.Save();

                    msg = "持久化连接 {0} 成功".F(name);
                }
                catch (Exception ex)
                {
                    msg = "持久化连接 {0} 失败 {1}".F(name, ex.Message);
                }
            }

            Js.Alert(msg);

            return Index();
        }

        /// <summary>备份数据库</summary>
        /// <param name="name"></param>
        /// <returns></returns>
        [EntityAuthorize(PermissionFlags.Insert)]
        public ActionResult Backup(String name)
        {
            var dal = DAL.Create(name);
            var bak = dal.Db.CreateMetaData().SetSchema(DDLSchema.BackupDatabase, dal.ConnName, null);

            return Index();
        }

        /// <summary>下载数据库备份</summary>
        /// <param name="name"></param>
        /// <returns></returns>
        [EntityAuthorize(PermissionFlags.Detail)]
        public ActionResult Download(String name)
        {


            return Index();
        }
    }
}
