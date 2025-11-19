using System;
namespace Wlniao.Engine
{
    
    /// <summary>
    /// 
    /// </summary>
    public class EngineException : Exception
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
        public EngineException(string message, int statusCode) : base(message)
        {
            StatusCode = statusCode;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        public EngineException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}