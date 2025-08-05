namespace Wlniao.Engine
{
    /// <summary>
    /// 接口返回实例
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Result<T>
    {
        /// <summary>
        /// 返回的数据
        /// </summary>
        public T? Data { get; set; }
        
        /// <summary>
        /// 返回状态码（200表示成功）
        /// </summary>
        public int Code { get; set; } = 0;
        
        /// <summary>
        /// 返回的消息内容
        /// </summary>
        public string Message { get; set; } = string.Empty;
    
        /// <summary>
        /// 是否存在错误情况
        /// </summary>
        /// <returns></returns>
        public bool Success => Code == 200;

        /// <summary>
        /// 是否存在错误情况
        /// </summary>
        /// <returns></returns>
        public bool HasError => Code != 200;
    }
}