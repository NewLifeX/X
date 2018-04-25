using System;

namespace NewLife.Exceptions
{
    /// <summary>编码异常</summary>
    public class EncoderException : CodecException
    {
        /// <summary>实例化</summary>
        /// <param name="message"></param>
        public EncoderException(String message) : base(message) { }

        /// <summary>实例化</summary>
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        public EncoderException(String message, Exception innerException) : base(message, innerException) { }
    }
}