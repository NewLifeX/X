using System;

namespace NewLife.Exceptions
{
    /// <summary>解码异常</summary>
    public class DecoderException : CodecException
    {
        /// <summary>实例化</summary>
        /// <param name="message"></param>
        public DecoderException(String message) : base(message) { }

        /// <summary>实例化</summary>
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        public DecoderException(String message, Exception innerException) : base(message, innerException) { }
    }
}