using System;

namespace Wlniao.Runtime
{
    /// <summary>
    /// XCore内部异常
    /// </summary>
    public class MessageException : Exception
    {
        /// <summary>
        /// 状态码
        /// </summary>
        public int StatusCode { get; }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="statusCode"></param>
        public MessageException(string message, int statusCode) : base(message)
        {
            StatusCode = statusCode;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="statusCode"></param>
        /// <param name="innerException"></param>
        public MessageException(string message, int statusCode, Exception innerException) : base(message, innerException)
        {
            StatusCode = statusCode;
        }
    }
}