using System.Text;
using System.Web;
using NewLife.IO;

namespace System.IO
{
    /// <summary>路径操作帮助</summary>
    public static class PathHelper
    {
        #region 属性
        private static String _BaseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        /// <summary>基础目录。GetFullPath依赖于此，默认为当前应用程序域基础目录</summary>
        public static String BaseDirectory { get { return _BaseDirectory; } set { _BaseDirectory = value; } }
        #endregion

        #region 路径操作辅助
        /// <summary>获取文件或目录的全路径，过滤相对目录</summary>
        /// <remarks>不确保目录后面一定有分隔符，是否有分隔符由原始路径末尾决定</remarks>
        /// <param name="path">文件或目录</param>
        /// <returns></returns>
        public static String GetFullPath(this String path)
        {
            if (String.IsNullOrEmpty(path)) return path;

            if (!Path.IsPathRooted(path))
            {
                path = path.TrimStart('~');
                path = path.Replace("/", "\\").TrimStart('\\');

                path = Path.Combine(BaseDirectory, path);
            }

            return Path.GetFullPath(path);
        }

        /// <summary>确保目录存在，若不存在则创建</summary>
        /// <remarks>
        /// 斜杠结尾的路径一定是目录，无视第二参数；
        /// 默认是文件，这样子只需要确保上一层目录存在即可，否则如果把文件当成了目录，目录的创建会导致文件无法创建。
        /// </remarks>
        /// <param name="path">文件路径或目录路径，斜杠结尾的路径一定是目录，无视第二参数</param>
        /// <param name="isfile">该路径是否是否文件路径。文件路径需要取目录部分</param>
        /// <returns></returns>
        public static String EnsureDirectory(this String path, Boolean isfile = true)
        {
            if (String.IsNullOrEmpty(path)) return path;

            path = path.GetFullPath();
            if (File.Exists(path) || Directory.Exists(path)) return path;

            var dir = path;
            // 斜杠结尾的路径一定是目录，无视第二参数
            if (dir[dir.Length - 1] == Path.PathSeparator)
                dir = Path.GetDirectoryName(path);
            else if (isfile)
                dir = Path.GetDirectoryName(path);

            /*!!! 基础类库的用法应该有明确的用途，而不是通过某些小伎俩去让人猜测 !!!*/

            //// 如果有圆点说明可能是文件
            //var p1 = dir.LastIndexOf('.');
            //if (p1 >= 0)
            //{
            //    // 要么没有斜杠，要么圆点必须在最后一个斜杠后面
            //    var p2 = dir.LastIndexOf('\\');
            //    if (p2 < 0 || p2 < p1) dir = Path.GetDirectoryName(path);
            //}

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

        #region 文件扩展
        /// <summary>文件路径作为文件信息</summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public static FileInfo AsFile(this String file)
        {
            return new FileInfo(file);
        }

        /// <summary>从文件中读取数据</summary>
        /// <param name="file"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public static Byte[] ReadBytes(this FileInfo file, Int32 offset = 0, Int32 count = 0)
        {
            using (var fs = file.OpenRead())
            {
                fs.Position = offset;

                if (count <= 0) count = (Int32)(fs.Length - offset);

                return fs.ReadBytes(count);
            }
        }

        /// <summary>把数据写入文件指定位置</summary>
        /// <param name="file"></param>
        /// <param name="data"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        public static FileInfo WriteBytes(this FileInfo file, Byte[] data, Int32 offset = 0)
        {
            using (var fs = file.OpenWrite())
            {
                fs.Position = offset;

                fs.Write(data, offset, data.Length);
            }

            return file;
        }

        /// <summary>读取所有文本，自动检测编码</summary>
        /// <remarks>性能较File.ReadAllText略慢，可通过提前检测BOM编码来优化</remarks>
        /// <param name="file"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        public static String ReadText(this FileInfo file, Encoding encoding = null)
        {
            using (var fs = file.OpenRead())
            {
                if (encoding == null) encoding = fs.Detect(Encoding.Default);
                using (var reader = new StreamReader(fs, encoding))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        //public static Stream WriteText(this FileInfo file)
        #endregion
    }
}