using System;

namespace NewLife.Remoting
{
    /// <summary>远程调用异常</summary>
    public class ApiException : Exception
    {
        /// <summary>代码</summary>
        public Int32 Code { get; set; }

        ///// <summary>异常消息。已重载，加上错误码前缀</summary>
        //public override String Message => $"[{Code}]{base.Message}";

        /// <summary>实例化远程调用异常</summary>
        /// <param name="code"></param>
        /// <param name="message"></param>
        public ApiException(Int32 code, String message) : base(message) => Code = code;

        /// <summary>实例化远程调用异常</summary>
        /// <param name="code"></param>
        /// <param name="ex"></param>
        public ApiException(Int32 code, Exception ex) : base(ex.Message, ex) => Code = code;
    }
}