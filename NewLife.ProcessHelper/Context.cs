using System;
using System.Collections.Generic;
using System.Text;

namespace NewLife.ProcessHelper
{
    class Context
    {
        #region 属性
        private Int32 _PID;
        /// <summary>进程标识</summary>
        public Int32 PID
        {
            get { return _PID; }
            set { _PID = value; }
        }

        private String _FileName;
        /// <summary>文件名</summary>
        public String FileName
        {
            get { return _FileName; }
            set { _FileName = value; }
        }

        private String _Args;
        /// <summary>参数</summary>
        public String Args
        {
            get { return _Args; }
            set { _Args = value; }
        }
        #endregion
    }
}