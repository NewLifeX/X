using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using XCode.DataAccessLayer;

namespace NewLife.Cube.Admin
{
    /// <summary>文件项</summary>
    public class FileItem
    {
        /// <summary>连接名</summary>
        public String Name { get; set; }

        /// <summary>全路径</summary>
        public String FullName { get; set; }

        /// <summary>是否目录</summary>
        public Boolean Directory { get; set; }

        /// <summary>大小字符串</summary>
        public String Size { get; set; }

        /// <summary>最后写入时间</summary>
        public DateTime LastWrite { get; set; }
    }
}