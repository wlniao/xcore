namespace Wlniao.Tasker
{
    /// <summary>
    /// 延时任务实体
    /// </summary>
    public class DelayJob
    {
        /// <summary>
        /// 任务标识
        /// </summary>
        public string? key { get; set; }
        /// <summary>
        /// 任务主题
        /// </summary>
        public string? topics { get; set; }
        /// <summary>
        /// 任务的执行时间点，如当前unixtime后的10秒/30秒/1分钟/3分钟/5分钟/10分钟/30分钟
        /// </summary>
        public System.Collections.Generic.List<long>? times { get; set; }
    }
}