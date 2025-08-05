namespace Wlniao.Engine
{
    /// <summary>
    /// 多租户系统信息
    /// </summary>
    public class ConsumerInfo
    {
        /// <summary>
        /// 当前系统用户标识
        /// </summary>
        public string Id { get; set; }
        
        /// <summary>
        /// 当前系统安全密钥
        /// </summary>
        public string SecretKey { get; set;}
        
        /// <summary>
        /// 当前系统域名
        /// </summary>
        public string Domain { get; set; }
    }
}