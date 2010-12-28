using System;
using System.Collections.Generic;
using System.Text;

namespace XUrlRewrite.Helper
{
    /// <summary>
    /// 显示文件信息的工具类
    /// </summary>
    public class FileInfoDisplay
    {
        static long KBLength = 1024;
        static long MBLength = 1024 * 1024;
        static long GBLength = 1024 * 1024 * 1024;
        static UInt64 TBLength = (UInt64)1024 * (UInt64)1024 * (UInt64)1024 * (UInt64)1024;
        /// <summary>
        /// 显示指定字节大小的文件大小,会自动显示为KB MB GM TB
        /// </summary>
        /// <param name="length"></param>
        /// <returns></returns>
        public static String Length(long length)
        {
            if (length < 0)
            {
                throw new ArgumentOutOfRangeException("length", "大小小于0");
            }
            else if ((UInt64)length >= TBLength)
            {
                return String.Format("{0:########0.00} TB", (UInt64)length / TBLength);
            }
            else if (length >= GBLength)
            {
                return String.Format("{0:###0.00} GB", length / GBLength);
            }
            else if (length >= MBLength)
            {
                return String.Format("{0:###0.00} MB", length / MBLength);
            }
            else if (length >= KBLength)
            {
                return String.Format("{0:###0.00} KB", length / KBLength);
            }
            else
            {
                return String.Format("{0:###0.00} B", length);
            }
        }
    }
}
