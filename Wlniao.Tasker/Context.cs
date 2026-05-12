namespace Wlniao.Tasker
{
    /// <summary>
    /// 任务消息
    /// </summary>
    public class Context
    {
        /// <summary>
        /// 任务编号
        /// </summary>
        public string? key { get; set; }
        /// <summary>
        /// 任务主题
        /// </summary>
        public string? topic { get; set; }
        /// <summary>
        /// 指定应用
        /// </summary>
        public string? appid { get; set; }
        /// <summary>
        /// 指定客户端
        /// </summary>
        public string? clientid { get; set; }
    }
}
