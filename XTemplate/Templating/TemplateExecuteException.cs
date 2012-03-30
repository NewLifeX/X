using System;
using System.Collections.Generic;
using System.Text;
using NewLife.Exceptions;
using System.Runtime.Serialization;

namespace XTemplate.Templating
{
    /// <summary>模版执行错误异常</summary>
    public class TemplateExecutionException : XException
    {
        #region 构造
        /// <summary>初始化</summary>>
        public TemplateExecutionException() { }

        /// <summary>初始化</summary>>
        /// <param name="message"></param>
        public TemplateExecutionException(String message) : base(message) { }

        /// <summary>初始化</summary>>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public TemplateExecutionException(String format, params Object[] args) : base(format, args) { }

        /// <summary>初始化</summary>>
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        public TemplateExecutionException(String message, Exception innerException) : base(message, innerException) { }

        /// <summary>初始化</summary>>
        /// <param name="innerException"></param>
        public TemplateExecutionException(Exception innerException) : base((innerException != null ? innerException.Message : null), innerException) { }

        /// <summary>初始化</summary>>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected TemplateExecutionException(SerializationInfo info, StreamingContext context) : base(info, context) { }
        #endregion
    }
}