using System;
namespace Wlniao
{
    /// <summary>
    /// 
    /// </summary>
    public class WlniaoConsole : Wlniao.Runtime.Console
    {
        /// <summary>
        /// 
        /// </summary>
        public WlniaoConsole() : base() { }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="timeout"></param>
        public WlniaoConsole(DateTime timeout) : base(timeout) { }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="TotalCount"></param>
        public WlniaoConsole(double TotalCount) : base(TotalCount) { }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="TotalCount"></param>
        public WlniaoConsole(int TotalCount) : base(TotalCount) { }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="TotalCount"></param>
        public WlniaoConsole(long TotalCount) : base(TotalCount) { }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="TotalCount"></param>
        public WlniaoConsole(float TotalCount) : base(TotalCount) { }
    }
}