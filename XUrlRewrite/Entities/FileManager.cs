using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using System.IO;
using System.Web;

namespace XUrlRewrite.Entities
{
    /// <summary>
    /// 模板文件管理业务类
    /// </summary>
    [DataObject]
    public class FileManager
    {
        /// <summary>
        /// 查找指定应用在模板目录下,指定子路径的所有模板文件
        /// </summary>
        /// <param name="app"></param>
        /// <param name="path">如果为空白或者null则表示模板目录根路径</param>
        /// <returns></returns>
        [DataObjectMethod(DataObjectMethodType.Select)]
        public static List<FileSystemInfo> FindAllFiles(HttpApplication app, String path)
        {
            String[] files = Directory.GetFileSystemEntries(GetTemplatePath(app, path));
            List<FileSystemInfo> ret = new List<FileSystemInfo>();
            foreach (String p in files)
            {
                if (Directory.Exists(p))
                {
                    ret.Add(new DirectoryInfo(p));
                }
                else if (File.Exists(p))
                {
                    ret.Add(new FileInfo(p));
                }
            }
            return ret;
        }
        /// <summary>
        /// TODO
        /// </summary>
        /// <param name="app"></param>
        /// <param name="files"></param>
        /// <param name="path"></param>
        /// <param name="removeSource"></param>
        public static void CopyFiles(HttpApplication app, List<FileSystemInfo> files, String path, Boolean removeSource)
        {

        }
        /// <summary>
        /// TODO
        /// </summary>
        /// <param name="app"></param>
        /// <param name="files"></param>
        public static void DeleteFiles(HttpApplication app, List<FileSystemInfo> files)
        {

        }

        internal static String GetTemplatePath(HttpApplication app, String path)
        {
            String dir = ConfigWrap.FindTemplateConfig(app).Directory;
            if (path == null) path = "";
            String basePath = dir.Substring(0, 2) == "~/" ? app.Server.MapPath(dir) : dir;
            if (path.Length > 0)
            {
                if (path[0] == '/')
                {
                    path = path.Substring(1);
                }
                path.Replace('/', '\\');
            }
            return Path.Combine(basePath, path);
        }
    }
}
