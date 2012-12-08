using System;

namespace NewLife.Net.FTP
{
    /// <summary>FTP项</summary>
    public class FTPItem
    {
        #region 属性
        private String _Name;
        /// <summary>名称</summary>
        public String Name { get { return _Name; } set { _Name = value; } }

        private Int64 _Size;
        /// <summary>大小</summary>
        public Int64 Size { get { return _Size; } set { _Size = value; } }

        private DateTime _Modified;
        /// <summary>修改时间</summary>
        public DateTime Modified { get { return _Modified; } set { _Modified = value; } }

        private Boolean _IsDirectory;
        /// <summary>是否目录</summary>
        public Boolean IsDirectory { get { return _IsDirectory; } set { _IsDirectory = value; } }
        #endregion
    }
}