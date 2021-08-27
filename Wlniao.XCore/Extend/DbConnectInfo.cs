using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Wlniao
{
    /// <summary>
    /// 数据库链接信息
    /// </summary>
    public partial class DbConnectInfo
    {
        private static string connstr_mysql_rw = null;  //读写链接
        private static string connstr_mysql_ro = null;  //只读链接
        private static string connstr_sqlite = null;
        private static string connstr_sqlsqlver = null;
        /// <summary>
        /// 连接的Mysql数据库用户名
        /// </summary>
        public static string WLN_MYSQL_UID
        {
            get
            {
                return Wlniao.Config.GetSetting("WLN_MYSQL_UID");
            }
        }
        /// <summary>
        /// 密码
        /// </summary>
        public static string WLN_MYSQL_PWD
        {
            get
            {
                if (string.IsNullOrEmpty(WLN_MYSQL_UID))
                {
                    return Wlniao.Config.GetSetting("WLN_MYSQL_PWD");
                }
                else
                {
                    return Wlniao.Config.GetSetting("WLN_MYSQL_PWD", "");
                }
            }
        }
        /// <summary>
        /// 连接的Mysql数据库名称
        /// </summary>
        public static string WLN_MYSQL_NAME
        {
            get
            {
                if (string.IsNullOrEmpty(WLN_MYSQL_UID))
                {
                    return Wlniao.Config.GetSetting("WLN_MYSQL_NAME");
                }
                else
                {
                    return Wlniao.Config.GetSetting("WLN_MYSQL_NAME", WLN_MYSQL_UID);
                }
            }
        }
        /// <summary>
        /// 连接的Mysql数据库端口号（默认为3306）
        /// </summary>
        public static string WLN_MYSQL_PORT
        {
            get
            {
                var port = Wlniao.Config.GetSetting("WLN_MYSQL_PORT");
                if (string.IsNullOrEmpty(port))
                {
                    return "3306";
                }
                return port;
            }
        }
        /// <summary>
        /// 连接的Mysql数据库端口号（默认为3306，只读）
        /// </summary>
        public static string WLN_MYSQL_PORT_READONLY
        {
            get
            {
                var port = Wlniao.Config.GetSetting("WLN_MYSQL_PORT_READONLY");
                if (string.IsNullOrEmpty(port))
                {
                    return WLN_MYSQL_PORT;
                }
                return port;
            }
        }
        /// <summary>
        /// 连接的Mysql数据库服务器地址（默认为127.0.0.1）
        /// </summary>
        public static string WLN_MYSQL_HOST
        {
            get
            {
                var host = Wlniao.Config.GetSetting("WLN_MYSQL_HOST");
                if (!string.IsNullOrEmpty(WLN_MYSQL_UID) && string.IsNullOrEmpty(host))
                {
                    host = "127.0.0.1";
                    Wlniao.Config.SetConfigs("WLN_MYSQL_HOST", host);
                }
                return host;
            }
        }
        /// <summary>
        /// 连接的Mysql数据库服务器地址（默认为127.0.0.1，只读）
        /// </summary>
        public static string WLN_MYSQL_HOST_READONLY
        {
            get
            {
                var host = Wlniao.Config.GetSetting("WLN_MYSQL_HOST_READONLY");
                if (string.IsNullOrEmpty(host))
                {
                    return WLN_MYSQL_HOST;
                }
                return host;
            }
        }
        /// <summary>
        /// 数据库连接字符串
        /// </summary>
        public static string WLN_CONNSTR
        {
            get
            {
                var env = Wlniao.Config.GetSetting("WLN_CONNSTR");
                if (string.IsNullOrEmpty(env))
                {
                    env = WLN_CONNSTR_MYSQL;
                }
                return env;
            }
        }
        /// <summary>
        /// MySql数据库连接字符串
        /// </summary>
        public static string WLN_CONNSTR_MYSQL
        {
            get
            {
                if (connstr_mysql_rw == null)
                {
                    connstr_mysql_rw = Wlniao.Config.GetSetting("WLN_CONNSTR_MYSQL");
                    if (string.IsNullOrEmpty(connstr_mysql_rw))
                    {
                        connstr_mysql_rw = string.Format("Server={0};Port={1};Database={2};Uid={3};Pwd={4};CharSet=utf8;"
                            , WLN_MYSQL_HOST, WLN_MYSQL_PORT, WLN_MYSQL_NAME, WLN_MYSQL_UID, WLN_MYSQL_PWD);
                        if (string.IsNullOrEmpty(WLN_MYSQL_UID) || string.IsNullOrEmpty(WLN_MYSQL_PWD))
                        {
                            connstr_mysql_rw = "";
                        }
                    }
                }
                return connstr_mysql_rw;
            }
        }
        /// <summary>
        /// MySql数据库连接字符串
        /// </summary>
        public static string WLN_CONNSTR_MYSQL_READONLY
        {
            get
            {
                if (connstr_mysql_ro == null)
                {
                    connstr_mysql_ro = Wlniao.Config.GetSetting("WLN_CONNSTR_MYSQL_READONLY");
                    if (string.IsNullOrEmpty(connstr_mysql_ro))
                    {
                        connstr_mysql_ro = string.Format("Server={0};Port={1};Database={2};Uid={3};Pwd={4};CharSet=utf8;"
                            , WLN_MYSQL_HOST_READONLY, WLN_MYSQL_PORT_READONLY, WLN_MYSQL_NAME, WLN_MYSQL_UID, WLN_MYSQL_PWD);
                        if (string.IsNullOrEmpty(WLN_MYSQL_UID) || string.IsNullOrEmpty(WLN_MYSQL_PWD))
                        {
                            connstr_mysql_ro = "";
                        }
                    }
                }
                return connstr_mysql_ro;
            }
        }
        /// <summary>
        /// SqlServer数据库连接字符串
        /// </summary>
        public static string WLN_CONNSTR_SQLSERVER
        {
            get
            {
                if (connstr_sqlsqlver == null)
                {
                    connstr_sqlsqlver = Wlniao.Config.GetSetting("WLN_CONNSTR_SQLSERVER");
                }
                return connstr_sqlsqlver;
            }
        }
        /// <summary>
        /// Sqlite数据库连接字符串
        /// </summary>
        public static string WLN_CONNSTR_SQLITE
        {
            get
            {
                if (connstr_sqlite == null)
                {
                    connstr_sqlite = Wlniao.Config.GetSetting("WLN_CONNSTR_SQLITE");
                    if (string.IsNullOrEmpty(connstr_sqlite) && string.IsNullOrEmpty(WLN_MYSQL_NAME))
                    {
                        var sqlite = Wlniao.IO.PathTool.Map(Wlniao.XCore.StartupRoot, XCore.FrameworkRoot, "xcore.db");
                        if (IO.FileEx.Exists(sqlite))
                        {
                            connstr_sqlite = "Data Source=" + sqlite;
                        }
                    }
                }
                return connstr_sqlite;
            }
        }
    }
}