using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
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
        private String Root { get { return "../".GetFullPath(); } }

        private FileInfo GetFile(String r)
        {
            if (r.IsNullOrEmpty()) return null;

            // 默认根目录
            var fi = Root.CombinePath(r).AsFile();
            if (!fi.Exists) return null;

            return fi;
        }

        private DirectoryInfo GetDirectory(String r)
        {
            if (r.IsNullOrEmpty()) return null;

            // 默认根目录
            var di = Root.CombinePath(r).AsDirectory();
            if (!di.Exists) return null;

            return di;
        }

        //private FileSystemInfo Get(String r)
        //{
        //    var fi = GetFile(r);
        //    if (fi != null)
        //        fi.Delete();
        //    else
        //    {
        //        var di = GetDirectory(r);
        //        if (di == null) throw new Exception("找不到文件或目录！");
        //        di.Delete(true);
        //    }
        //}

        /// <summary>文件管理主视图</summary>
        /// <returns></returns>
        public ActionResult Index(String r)
        {
            var di = GetDirectory(r) ?? Root.AsDirectory();

            // 检查目录越界
            var root = Root.TrimEnd(Path.DirectorySeparatorChar);
            if (!di.FullName.StartsWithIgnoreCase(root)) di = Root.AsDirectory();

            ViewBag.Current = di.FullName;

            var fis = di.GetFileSystemInfos();
            var list = new List<FileItem>();
            if (!di.FullName.EqualIgnoreCase(Root, root))
            {
                list.Add(new FileItem
                {
                    Name = "../",
                    Directory = true,
                    FullName = GetFullName(di.Parent.FullName)
                });
            }
            foreach (var item in fis)
            {
                if (item.Attributes.Has(FileAttributes.Hidden)) continue;

                var fi = new FileItem();
                fi.Name = item.Name;
                fi.FullName = GetFullName(item.FullName);
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

        /// <summary>删除</summary>
        /// <param name="r"></param>
        /// <returns></returns>
        public ActionResult Delete(String r)
        {
            var p = "";

            var fi = GetFile(r);
            if (fi != null)
            {
                p = GetFullName(fi.Directory.FullName);
                fi.Delete();
            }
            else
            {
                var di = GetDirectory(r);
                if (di == null) throw new Exception("找不到文件或目录！");
                p = GetFullName(di.Parent.FullName);
                di.Delete();
            }

            return RedirectToAction("Index", new { r = p });
        }

        /// <summary>压缩文件</summary>
        /// <param name="r"></param>
        /// <returns></returns>
        public ActionResult Compress(String r)
        {
            var p = "";

            var fi = GetFile(r);
            if (fi != null)
            {
                p = GetFullName(fi.Directory.FullName);
                var dst = "{0}_{1:yyyyMMddHHmmss}.zip".F(fi.Name, DateTime.Now);
                dst = fi.Directory.FullName.CombinePath(dst);
                fi.Compress(dst);
            }
            else
            {
                var di = GetDirectory(r);
                if (di == null) throw new Exception("找不到文件或目录！");
                p = GetFullName(di.Parent.FullName);
                var dst = "{0}_{1:yyyyMMddHHmmss}.zip".F(di.Name, DateTime.Now);
                dst = di.Parent.FullName.CombinePath(dst);
                di.Compress(dst);
            }

            return RedirectToAction("Index", new { r = p });
        }

        /// <summary>解压缩</summary>
        /// <param name="r"></param>
        /// <returns></returns>
        public ActionResult Decompress(String r)
        {
            var p = "";

            var fi = GetFile(r);
            if (fi == null) throw new Exception("找不到文件或目录！");

            p = GetFullName(fi.Directory.FullName);
            fi.Extract(fi.Directory.FullName);

            return RedirectToAction("Index", new { r = p });
        }

        private String GetFullName(String r)
        {
            return r.TrimStart(Root).TrimStart(Root.TrimEnd(Path.DirectorySeparatorChar + ""));
        }
    }
}