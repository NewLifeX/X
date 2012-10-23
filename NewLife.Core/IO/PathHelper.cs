using System;
using System.Collections.Generic;
using System.Text;

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
        #endregion
    }
}