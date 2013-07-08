
namespace System.IO
{
    /// <summary>路径操作帮助</summary>
    public static class PathHelper
    {
        #region 路径操作辅助
        /// <summary>获取文件或目录的全路径，过滤相对目录</summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static String GetFullPath(this String path)
        {
            if (String.IsNullOrEmpty(path)) return path;

            if (!Path.IsPathRooted(path)) path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path);

            return Path.GetFullPath(path);
        }

        /// <summary>确保目录存在，若不存在则创建</summary>
        /// <param name="path">文件路径或目录路径（斜杠结尾）</param>
        /// <returns></returns>
        public static String EnsureDirectory(this String path)
        {
            if (String.IsNullOrEmpty(path)) return path;

            path = path.GetFullPath();
            if (File.Exists(path) || Directory.Exists(path)) return path;

            var dir = path;
            if (dir[dir.Length - 1] != Path.PathSeparator) dir = Path.GetDirectoryName(path);
            if (!String.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);

            return path;
        }

        /// <summary>合并多段路径</summary>
        /// <param name="path"></param>
        /// <param name="ps"></param>
        /// <returns></returns>
        public static String CombinePath(this String path, params String[] ps)
        {
            if (path == null || ps == null || ps.Length < 1) return path;

            //return Path.Combine(path, path2);
            foreach (var item in ps)
            {
                path = Path.Combine(path, item);
            }
            return path;
        }
        #endregion
    }
}