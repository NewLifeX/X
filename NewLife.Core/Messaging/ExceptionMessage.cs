using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace NewLife.Messaging
{
    /// <summary>
    /// 异常消息
    /// </summary>
    public class ExceptionMessage : Message
    {
        /// <summary>
        /// 消息编号
        /// </summary>
        public override int ID
        {
            get { return 0xFF; }
        }

        private String _Error;
        /// <summary>异常</summary>
        public String Error
        {
            get { return _Error; }
            set { _Error = value; }
        }

        /// <summary>
        /// 初始化
        /// </summary>
        public ExceptionMessage() { }

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="ex"></param>
        public ExceptionMessage(Exception ex)
        {
            Error = ex.Message;
        }
    }
}