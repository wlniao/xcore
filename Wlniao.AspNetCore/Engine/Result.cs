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
        /// 返回的消息内容
        /// </summary>
        public string Message { get; set; } = string.Empty;
        
        /// <summary>
        /// 返回状态码（200表示成功）
        /// </summary>
        public int StatusCode { get; set; } = 0;

        /// <summary>
        /// 是否存在错误情况
        /// </summary>
        /// <returns></returns>
        [System.Text.Json.Serialization.JsonIgnore]
        public bool Success => StatusCode == 200;

        /// <summary>
        /// 是否存在错误情况
        /// </summary>
        /// <returns></returns>
        [System.Text.Json.Serialization.JsonIgnore]
        public bool HasError => StatusCode != 200;
        
        /// <summary>
        /// 设置返回内容
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public Result<T> SetData(T data)
        {
            this.Data = data;
            return this;
        }
        
        /// <summary>
        /// 设置返回消息
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public Result<T> SetMessage(string message)
        {
            this.Message = message;
            return this;
        }
        /// <summary>
        /// 设置返回消息
        /// </summary>
        /// <param name="message"></param>
        /// <param name="statusCode"></param>
        /// <returns></returns>
        public Result<T> SetMessage(string message, int statusCode)
        {
            this.Message = message;
            this.StatusCode = statusCode;
            return this;
        }
        
        /// <summary>
        /// 设置返回状态
        /// </summary>
        /// <param name="statusCode"></param>
        /// <returns></returns>
        public Result<T> SetStatusCode(int statusCode)
        {
            this.StatusCode = statusCode;
            return this;
        }

        /// <summary>
        /// 返回成功结果
        /// </summary>
        /// <param name="data"></param>
        /// <param name="statusCode"></param>
        /// <returns></returns>
        public static Result<T> OutSuccess(T data, int statusCode = 200)
        {
            return new Result<T> { Data = data, Message = "success", StatusCode = statusCode };
        }
        
        /// <summary>
        /// 返回成功结果
        /// </summary>
        /// <param name="message"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public static Result<T> OutSuccess(string message, T data)
        {
            return new Result<T> { Data = data, Message = message, StatusCode = 200 };
        }
        
        /// <summary>
        /// 返回错误消息
        /// </summary>
        /// <param name="message"></param>
        /// <param name="statusCode"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public static Result<T> OutMessage(string message, int statusCode, T data = default(T)!)
        {
            return new Result<T> { Data = data, Message = message, StatusCode = statusCode };
        }
    }
}