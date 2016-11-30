using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using XCode.Membership;

namespace NewLife.Cube.Admin.Controllers
{
    /// <summary>文件管理</summary>
    [DisplayName("文件管理")]
    [EntityAuthorize(PermissionFlags.Detail)]
    public class FileController : ControllerBaseX
    {
        #region 基础
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

        private String GetFullName(String r)
        {
            return r.TrimStart(Root).TrimStart(Root.TrimEnd(Path.DirectorySeparatorChar + ""));
        }
        #endregion

        /// <summary>文件管理主视图</summary>
        /// <returns></returns>
        public ActionResult Index(String r, String sort)
        {
            var di = GetDirectory(r) ?? Root.AsDirectory();

            // 检查目录越界
            var root = Root.TrimEnd(Path.DirectorySeparatorChar);
            if (!di.FullName.StartsWithIgnoreCase(root)) di = Root.AsDirectory();

            var fd = di.FullName;
            if (fd.StartsWith(Root)) fd = fd.Substring(Root.Length);
            ViewBag.Current = fd;

            var fis = di.GetFileSystemInfos();
            var list = new List<FileItem>();
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
            switch (sort)
            {
                case "size":
                    list = list.OrderByDescending(e => e.Size).ThenBy(e => e.Name).ToList();
                    break;
                case "lastwrite":
                    list = list.OrderByDescending(e => e.LastWrite).ThenBy(e => e.Name).ToList();
                    break;
                case "name":
                default:
                    list = list.OrderByDescending(e => e.Directory).ThenBy(e => e.Name).ToList();
                    break;
            }
            // 在开头插入上一级目录
            if (!di.FullName.EqualIgnoreCase(Root, root))
            {
                list.Insert(0, new FileItem
                {
                    Name = "../",
                    Directory = true,
                    FullName = GetFullName(di.Parent.FullName)
                });
            }

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
                di.Delete(true);
            }

            return RedirectToAction("Index", new { r = p });
        }

        #region 压缩与解压缩
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
            var fi = GetFile(r);
            if (fi == null) throw new Exception("找不到文件！");

            var p = GetFullName(fi.Directory.FullName);
            fi.Extract(fi.Directory.FullName, true);

            return RedirectToAction("Index", new { r = p });
        }
        #endregion

        #region 上传下载
        /// <summary>上传文件</summary>
        /// <param name="r"></param>
        /// <param name="file"></param>
        /// <returns></returns>
        public ActionResult Upload(String r, HttpPostedFileBase file)
        {
            if (file != null)
            {
                var di = GetDirectory(r);
                if (di == null) throw new Exception("找不到目录！");

                var dest = di.FullName.CombinePath(file.FileName);
                file.SaveAs(dest);
            }

            return RedirectToAction("Index", new { r });
        }

        /// <summary>下载文件</summary>
        /// <param name="r"></param>
        /// <returns></returns>
        public ActionResult Download(String r)
        {
            var fi = GetFile(r);
            if (fi == null) throw new Exception("找不到文件！");

            return File(fi.FullName, "application/octet-stream", fi.Name);
        }
        #endregion

        #region 复制粘贴
        private const String CLIPKEY = "File_Clipboard";
        private ICollection<String> GetClip()
        {
            var list = Session[CLIPKEY] as ICollection<String>;
            if (list == null) Session[CLIPKEY] = list = new HashSet<String>(StringComparer.OrdinalIgnoreCase);

            return list;
        }

        /// <summary>复制文件到剪切板</summary>
        /// <param name="r"></param>
        /// <returns></returns>
        public ActionResult Copy(String r)
        {
            var list = GetClip();
            if (!list.Contains(r)) list.Add(r);

            return RedirectToAction("Index", new { r });
        }

        /// <summary>粘贴文件到当前目录</summary>
        /// <param name="r"></param>
        /// <returns></returns>
        public ActionResult Paste(String r)
        {
            return RedirectToAction("Index", new { r });
        }

        /// <summary>清空剪切板</summary>
        /// <returns></returns>
        public ActionResult ClearClipboard(String r)
        {
            return RedirectToAction("Index", new { r });
        }
        #endregion
    }
}