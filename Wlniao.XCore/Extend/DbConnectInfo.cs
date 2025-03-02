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
        private static string connstr_rw = null;  //读写链接
        private static string connstr_ro = null;  //只读链接
		private static string connstr_type = null;
		private static string connstr_mysql = null;
		private static string connstr_sqlite = null;
        private static string connstr_sqlsqlver = null;
		/// <summary>
		/// 数据库连接字符串
		/// </summary>
		public static string WLN_CONNSTR
		{
			get
			{
				if (connstr_rw == null)
				{
					connstr_rw = Wlniao.Config.GetConfigs("WLN_CONNSTR");
				}
				if (string.IsNullOrEmpty(connstr_rw))
				{
					connstr_rw = WLN_CONNSTR_MYSQL;
				}
				return connstr_rw;
			}
		}
		/// <summary>
		/// 数据库连接类型 默认：sqlite/mysql
		/// </summary>
		public static string WLN_CONNSTR_TYPE
		{
            get
            {
                if (string.IsNullOrEmpty(connstr_type))
                {
                    connstr_type = Wlniao.Config.GetConfigs("WLN_CONNSTR_TYPE");
                    if (string.IsNullOrEmpty(connstr_type))
                    {
                        if (!string.IsNullOrEmpty(WLN_CONNSTR_MYSQL))
                        {
                            connstr_type = "mysql";
                        }
                        else if (!string.IsNullOrEmpty(WLN_CONNSTR_SQLSERVER))
                        {
                            connstr_type = "sqlserver";
                        }
                        else
                        {
                            connstr_type = "sqlite";
                        }
                    }
                }
                return connstr_type;
            }
		}
		/// <summary>
		/// 连接的数据库服务器地址（默认为127.0.0.1）
		/// </summary>
		public static string WLN_CONNSTR_HOST
        {
            get
            {
                return Wlniao.Config.GetSetting("WLN_CONNSTR_HOST", "127.0.0.1");
            }
        }
        /// <summary>
        /// 连接的数据库名称
        /// </summary>
        public static string WLN_CONNSTR_NAME
        {
            get
            {
                return Wlniao.Config.GetSetting("WLN_CONNSTR_NAME");
            }
        }
        /// <summary>
        /// 连接数据库的用户账号
        /// </summary>
        public static string WLN_CONNSTR_UID
        {
            get
            {
                return Wlniao.Config.GetEncrypt("WLN_CONNSTR_UID", Config.Secret);
            }
        }
        /// <summary>
        /// 连接数据库的用户密码
        /// </summary>
        public static string WLN_CONNSTR_PWD
        {
            get
            {
                return Wlniao.Config.GetEncrypt("WLN_CONNSTR_PWD", Config.Secret);
            }
        }
        /// <summary>
        /// MySql数据库连接字符串
        /// </summary>
        public static string WLN_CONNSTR_READONLY
        {
            get
            {
                if (connstr_ro == null)
                {
                    connstr_ro = Wlniao.Config.GetConfigs("WLN_CONNSTR_READONLY");
                }
                if (string.IsNullOrEmpty(connstr_ro))
                {
                    connstr_ro = WLN_CONNSTR;
                }
                return connstr_ro;
            }
        }
        /// <summary>
        /// MySql数据库连接字符串
        /// </summary>
        public static string WLN_CONNSTR_MYSQL
        {
            get
            {
                if (connstr_mysql == null)
                {
                    connstr_mysql = Wlniao.Config.GetConfigs("WLN_CONNSTR_MYSQL");
                    if (string.IsNullOrEmpty(connstr_mysql))
                    {
                        var WLN_MYSQL_PORT = Wlniao.Config.GetConfigs("WLN_MYSQL_PORT", "3306");
                        var WLN_MYSQL_HOST = Wlniao.Config.GetConfigs("WLN_MYSQL_HOST", WLN_CONNSTR_HOST);
                        var WLN_MYSQL_NAME = Wlniao.Config.GetConfigs("WLN_MYSQL_NAME", WLN_CONNSTR_NAME);
                        var WLN_MYSQL_UID = Wlniao.Config.GetEncrypt("WLN_MYSQL_UID", Config.Secret, WLN_CONNSTR_UID);
                        var WLN_MYSQL_PWD = Wlniao.Config.GetEncrypt("WLN_MYSQL_PWD", Config.Secret, WLN_CONNSTR_PWD);
                        if (!string.IsNullOrEmpty(WLN_MYSQL_HOST) && !string.IsNullOrEmpty(WLN_MYSQL_UID) && !string.IsNullOrEmpty(WLN_MYSQL_PWD))
						{
							connstr_mysql = string.Format("Server={0};Port={1};Database={2};Uid={3};Pwd={4};CharSet=utf8;SslMode=none;"
								, WLN_MYSQL_HOST, WLN_MYSQL_PORT, WLN_MYSQL_NAME, WLN_MYSQL_UID, WLN_MYSQL_PWD);
						}
                    }
                }
                return connstr_mysql;
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
					if (string.IsNullOrEmpty(connstr_sqlite))
					{
						connstr_sqlite = "Data Source=" + Wlniao.IO.PathTool.Map(Wlniao.XCore.StartupRoot, XCore.FrameworkRoot, "xcore.db");
					}
				}
				return connstr_sqlite;
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
                    connstr_sqlsqlver = Wlniao.Config.GetConfigs("WLN_CONNSTR_SQLSERVER");
                    if (string.IsNullOrEmpty(connstr_sqlsqlver))
                    {
                        var WLN_MSSQL_HOST = Wlniao.Config.GetConfigs("WLN_MSSQL_HOST", WLN_CONNSTR_HOST);
                        var WLN_MSSQL_NAME = Wlniao.Config.GetConfigs("WLN_MSSQL_NAME", WLN_CONNSTR_NAME);
                        var WLN_MSSQL_UID = Wlniao.Config.GetEncrypt("WLN_MSSQL_UID", Config.Secret, WLN_CONNSTR_UID);
                        var WLN_MSSQL_PWD = Wlniao.Config.GetEncrypt("WLN_MSSQL_PWD", Config.Secret, WLN_CONNSTR_PWD);
                        if (!string.IsNullOrEmpty(WLN_MSSQL_NAME) && !string.IsNullOrEmpty(WLN_MSSQL_UID) && !string.IsNullOrEmpty(WLN_MSSQL_PWD))
						{
							var WLN_MSSQL_PORT = Wlniao.Config.GetConfigs("WLN_MSSQL_PORT", "1433");
							connstr_sqlsqlver = string.Format("Server={0},{1};Database={2};User Id={3};Password={4};TrustServerCertificate=true;"
								, WLN_MSSQL_HOST, WLN_MSSQL_PORT, WLN_MSSQL_NAME, WLN_MSSQL_UID, WLN_MSSQL_PWD);
						}
                    }
                }
                return connstr_sqlsqlver;
            }
        }

    }
}