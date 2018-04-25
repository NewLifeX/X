using System;

namespace NewLife.Exceptions
{
    /// <summary>编解码异常</summary>
    public class CodecException : Exception
    {
        /// <summary>实例化</summary>
        public CodecException() { }

        /// <summary>实例化</summary>
        /// <param name="message"></param>
        public CodecException(String message) : base(message) { }

        /// <summary>实例化</summary>
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        public CodecException(String message, Exception innerException) : base(message, innerException) { }
    }
}