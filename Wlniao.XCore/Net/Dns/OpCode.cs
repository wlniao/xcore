namespace Wlniao.Net.Dns
{
    /// <summary>
    /// 
    /// </summary>
    internal enum OpCode
    {
        /// <summary>
        /// 标准查询
        /// </summary>
        QUERY = 0,

        /// <summary>
        /// 反向查询
        /// </summary>
        IQUERY = 1,

        /// <summary>
        /// 服务器状态查询
        /// </summary>
        STATUS = 2,
    }
}
