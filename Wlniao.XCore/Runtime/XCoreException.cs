using System;

namespace Wlniao.Runtime
{
    /// <summary>
    /// 
    /// </summary>
    public class XCoreException : Exception
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        public XCoreException(string message) : base(message)
        {

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        public XCoreException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}