using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wlniao
{
    internal class RuntimeInfo
    {
        private static String _WebHost = null;
        /// <summary>
        /// 当前程序Web服务地址(通过WLN_HOST进行设置)
        /// </summary>
        public static string WebHost
        {
            get
            {
                if (_WebHost == null)
                {
                    _WebHost = Environment.GetEnvironmentVariable("WLN_HOST");
                    if (string.IsNullOrEmpty(_WebHost))
                    {
                        _WebHost = "";
                    }
                }
                return _WebHost;
            }
        }
        /// <summary>
        /// 当前程序版本号
        /// </summary>
        public static string ProgramVersion
        {
            get
            {
                var attributes = System.Reflection.Assembly.GetEntryAssembly().GetCustomAttributes(typeof(System.Reflection.AssemblyInformationalVersionAttribute), true);
                if (attributes != null && attributes.Length > 0)
                {
                    return ((System.Reflection.AssemblyInformationalVersionAttribute)attributes[0]).InformationalVersion;
                }
                return "";
            }
        }
    }
}
