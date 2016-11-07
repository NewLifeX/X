using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Web.Mvc;
using XCode.Membership;

namespace NewLife.Cube.Admin.Controllers
{
    /// <summary>文件管理</summary>
    [DisplayName("文件管理")]
    [EntityAuthorize(PermissionFlags.Detail)]
    public class FileController : ControllerBaseX
    {
        /// <summary>文件管理主视图</summary>
        /// <returns></returns>
        public ActionResult Index(String r)
        {
            // 默认根目录
            var root = "../".GetFullPath();

            if (r.IsNullOrEmpty()) r = "./";
            var di = root.CombinePath(r).AsDirectory();

            // 检查目录越界
            if (!di.FullName.StartsWithIgnoreCase(root)) di = root.AsDirectory();

            ViewBag.Current = di.FullName;

            var fis = di.GetFileSystemInfos();
            var list = new List<FileItem>();
            if (di.FullName.EnsureEnd(Path.DirectorySeparatorChar + "") != root)
            {
                list.Add(new FileItem
                {
                    Name = "../",
                    Directory = true,
                    FullName = di.Parent.FullName.EnsureEnd(Path.DirectorySeparatorChar + "").TrimStart(root)
                });
            }
            foreach (var item in fis)
            {
                if (item.Attributes.Has(FileAttributes.Hidden)) continue;

                var fi = new FileItem();
                fi.Name = item.Name;
                fi.FullName = item.FullName.TrimStart(root);
                fi.Directory = item is DirectoryInfo;
                fi.LastWrite = item.LastWriteTime;

                if (item is FileInfo)
                {
                    var f = item as FileInfo;
                    if (f.Length < 1024)
                        fi.Size = "{0:n0}".F(f.Length);
                    else if (f.Length < 1024 * 1024)
                        fi.Size = "{0:n2}K".F((Double)f.Length / 1024);
                    else if (f.Length < 1024 * 1024 * 1024)
                        fi.Size = "{0:n2}M".F((Double)f.Length / 1024 / 1024);
                    else if (f.Length < 1024L * 1024 * 1024 * 1024)
                        fi.Size = "{0:n2}G".F((Double)f.Length / 1024 / 1024 / 1024);
                }

                list.Add(fi);
            }

            // 排序，目录优先
            list = list.OrderByDescending(e => e.Directory).ThenBy(e => e.Name).ToList();

            return View(list);
        }
    }
}