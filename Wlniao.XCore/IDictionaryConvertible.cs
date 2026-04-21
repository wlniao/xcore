using System.Collections.Generic;

namespace Wlniao
{
    /// <summary>
    /// Dictionary转换类型
    /// </summary>
    public interface IDictionaryConvertible
    {
        /// <summary>
        /// 对象转换为Dictionary
        /// </summary>
        /// <returns></returns>
        Dictionary<string, object> ToDictionary();
    }
}