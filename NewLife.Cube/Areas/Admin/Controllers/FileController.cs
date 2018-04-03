using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using XCode.Membership;

namespace NewLife.Cube.Admin.Controllers
{
    /// <summary>文件管理</summary>
    [DisplayName("文件")]
    [EntityAuthorize(PermissionFlags.Detail)]
    public class FileController : ControllerBaseX
    {
        /// <summary>菜单顺序。扫描是会反射读取</summary>
        protected static Int32 MenuOrder { get; set; }

        static FileController()
        {
            MenuOrder = 28;
        }

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

        private FileItem GetItme(String r)
        {
            var inf = GetFile(r) as FileSystemInfo ?? GetDirectory(r);
            if (inf == null) return null;

            var fi = new FileItem
            {
                Name = inf.Name,
                FullName = GetFullName(inf.FullName),
                Raw = inf.FullName,
                Directory = inf is DirectoryInfo,
                LastWrite = inf.LastWriteTime
            };

            if (inf is FileInfo)
            {
                var f = inf as FileInfo;
                if (f.Length < 1024)
                    fi.Size = "{0:n0}".F(f.Length);
                else if (f.Length < 1024 * 1024)
                    fi.Size = "{0:n2}K".F((Double)f.Length / 1024);
                else if (f.Length < 1024 * 1024 * 1024)
                    fi.Size = "{0:n2}M".F((Double)f.Length / 1024 / 1024);
                else if (f.Length < 1024L * 1024 * 1024 * 1024)
                    fi.Size = "{0:n2}G".F((Double)f.Length / 1024 / 1024 / 1024);
            }

            return fi;
        }

        private String GetFullName(String r)
        {
            return r.TrimStart(Root).TrimStart(Root.TrimEnd(Path.DirectorySeparatorChar + ""));
        }
        #endregion

        #region 列表&删除
        /// <summary>文件管理主视图</summary>
        /// <returns></returns>
        [EntityAuthorize(PermissionFlags.Detail)]
        public ActionResult Index(String r, String sort)
        {
            var di = GetDirectory(r) ?? Root.AsDirectory();

            // 检查目录越界
            var root = Root.TrimEnd(Path.DirectorySeparatorChar);
            if (!di.FullName.StartsWithIgnoreCase(root)) di = Root.AsDirectory();

            // 计算当前路径
            var fd = di.FullName;
            if (fd.StartsWith(Root)) fd = fd.Substring(Root.Length);
            ViewBag.Current = fd;

            // 遍历所有子目录
            var fis = di.GetFileSystemInfos();
            var list = new List<FileItem>();
            foreach (var item in fis)
            {
                if (item.Attributes.Has(FileAttributes.Hidden)) continue;

                var fi = GetItme(item.FullName);

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

            // 剪切板
            ViewBag.Clip = GetClip();

            return View("Index", list);
        }

        /// <summary>删除</summary>
        /// <param name="r"></param>
        /// <returns></returns>
        [EntityAuthorize(PermissionFlags.Delete)]
        public ActionResult Delete(String r)
        {
            var p = "";

            var fi = GetFile(r);
            if (fi != null)
            {
                p = GetFullName(fi.Directory.FullName);
                WriteLog("删除", fi.FullName);
                fi.Delete();
            }
            else
            {
                var di = GetDirectory(r);
                if (di == null) throw new Exception("找不到文件或目录！");

                p = GetFullName(di.Parent.FullName);
                WriteLog("删除", di.FullName);
                di.Delete(true);
            }

            return RedirectToAction("Index", new { r = p });
        }
        #endregion

        #region 压缩与解压缩
        /// <summary>压缩文件</summary>
        /// <param name="r"></param>
        /// <returns></returns>
        [EntityAuthorize(PermissionFlags.Insert)]
        public ActionResult Compress(String r)
        {
            var p = "";

            var fi = GetFile(r);
            if (fi != null)
            {
                p = GetFullName(fi.Directory.FullName);
                var dst = "{0}_{1:yyyyMMddHHmmss}.zip".F(fi.Name, DateTime.Now);
                dst = fi.Directory.FullName.CombinePath(dst);
                WriteLog("压缩", "{0} => {1}".F(fi.FullName, dst));
                fi.Compress(dst);
            }
            else
            {
                var di = GetDirectory(r);
                if (di == null) throw new Exception("找不到文件或目录！");

                p = GetFullName(di.Parent.FullName);
                var dst = "{0}_{1:yyyyMMddHHmmss}.zip".F(di.Name, DateTime.Now);
                dst = di.Parent.FullName.CombinePath(dst);
                WriteLog("压缩", "{0} => {1}".F(di.FullName, dst));
                di.Compress(dst);
            }

            return RedirectToAction("Index", new { r = p });
        }

        /// <summary>解压缩</summary>
        /// <param name="r"></param>
        /// <returns></returns>
        [EntityAuthorize(PermissionFlags.Update)]
        public ActionResult Decompress(String r)
        {
            var fi = GetFile(r);
            if (fi == null) throw new Exception("找不到文件！");

            var p = GetFullName(fi.Directory.FullName);
            WriteLog("解压缩", fi.FullName);
            fi.Extract(fi.Directory.FullName, true);

            return RedirectToAction("Index", new { r = p });
        }
        #endregion

        #region 上传下载
        /// <summary>上传文件</summary>
        /// <param name="r"></param>
        /// <param name="file"></param>
        /// <returns></returns>
        [EntityAuthorize(PermissionFlags.Insert)]
        public ActionResult Upload(String r, HttpPostedFileBase file)
        {
            if (file != null)
            {
                var di = GetDirectory(r);
                if (di == null) throw new Exception("找不到目录！");

                var dest = di.FullName.CombinePath(file.FileName);
                WriteLog("上传", dest);
                file.SaveAs(dest);
            }

            return RedirectToAction("Index", new { r });
        }

        /// <summary>下载文件</summary>
        /// <param name="r"></param>
        /// <returns></returns>
        [EntityAuthorize(PermissionFlags.Detail)]
        public ActionResult Download(String r)
        {
            var fi = GetFile(r);
            if (fi == null) throw new Exception("找不到文件！");

            WriteLog("下载", fi.FullName);

            return File(fi.FullName, "application/octet-stream", fi.Name);
        }
        #endregion

        #region 复制粘贴
        private const String CLIPKEY = "File_Clipboard";
        private List<FileItem> GetClip()
        {
            var list = Session[CLIPKEY] as List<FileItem>;
            if (list == null) Session[CLIPKEY] = list = new List<FileItem>();

            return list;
        }

        /// <summary>复制文件到剪切板</summary>
        /// <param name="r"></param>
        /// <param name="f"></param>
        /// <returns></returns>
        [EntityAuthorize(PermissionFlags.Detail)]
        public ActionResult Copy(String r, String f)
        {
            var fi = GetItme(f);
            if (fi == null) throw new Exception("找不到文件或目录！");

            var list = GetClip();
            if (!list.Any(e => e.Raw == fi.Raw)) list.Add(fi);

            //return RedirectToAction("Index", new { r });
            return Index(r, null);
        }

        /// <summary>从剪切板移除</summary>
        /// <param name="r"></param>
        /// <param name="f"></param>
        /// <returns></returns>
        [EntityAuthorize(PermissionFlags.Detail)]
        public ActionResult CancelCopy(String r, String f)
        {
            var fi = GetItme(f);
            if (fi == null) throw new Exception("找不到文件或目录！");

            var list = GetClip();
            list.RemoveAll(e => e.Raw == fi.Raw);

            //return RedirectToAction("Index", new { r });
            return Index(r, null);
        }

        /// <summary>粘贴文件到当前目录</summary>
        /// <param name="r"></param>
        /// <returns></returns>
        [EntityAuthorize(PermissionFlags.Insert)]
        public ActionResult Paste(String r)
        {
            var di = GetDirectory(r) ?? Root.AsDirectory();
            if (di == null) throw new Exception("找不到目录！");

            var list = GetClip();
            foreach (var item in list)
            {
                var dst = di.FullName.CombinePath(item.Name);
                WriteLog("复制", "{0} => {1}".F(item.Raw, dst));
                if (item.Directory)
                    item.Raw.AsDirectory().CopyTo(dst);
                else
                    System.IO.File.Copy(item.Raw, dst, true);
            }
            list.Clear();

            return Index(r, null);
        }

        /// <summary>移动文件到当前目录</summary>
        /// <param name="r"></param>
        /// <returns></returns>
        [EntityAuthorize(PermissionFlags.Insert)]
        public ActionResult Move(String r)
        {
            var di = GetDirectory(r) ?? Root.AsDirectory();
            if (di == null) throw new Exception("找不到目录！");

            var list = GetClip();
            foreach (var item in list)
            {
                var dst = di.FullName.CombinePath(item.Name);
                WriteLog("移动", "{0} => {1}".F(item.Raw, dst));
                if (item.Directory)
                    Directory.Move(item.Raw, dst);
                else
                    System.IO.File.Move(item.Raw, dst);
            }
            list.Clear();

            return Index(r, null);
        }

        /// <summary>清空剪切板</summary>
        /// <returns></returns>
        [EntityAuthorize(PermissionFlags.Detail)]
        public ActionResult ClearClipboard(String r)
        {
            var list = GetClip();
            list.Clear();

            return Index(r, null);
        }
        #endregion

        #region 日志
        private static void WriteLog(String action, String remark)
        {
            LogProvider.Provider.WriteLog(typeof(FileController), action, remark);
        }
        #endregion
    }
}