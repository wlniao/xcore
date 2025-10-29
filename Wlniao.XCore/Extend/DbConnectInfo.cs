namespace Wlniao
{
    /// <summary>
    /// 数据库链接信息
    /// </summary>
    public partial class DbConnectInfo
    {
        private static string _connstrRw = null;  //读写链接
        private static string _connstrRo = null;  //只读链接
		private static string _connstrType = null;
		private static string _connstrMysql = null;
		private static string _connstrSqlite = null;
        private static string _connstrSqlsqlver = null;
		/// <summary>
		/// 数据库连接字符串
		/// </summary>
		public static string WLN_CONNSTR
		{
			get
			{
				_connstrRw ??= Wlniao.Config.GetConfigs("WLN_CONNSTR");

				if (!string.IsNullOrEmpty(_connstrRw))
				{
					return _connstrRw;
				}
				_connstrRw = WLN_CONNSTR_TYPE == "mysql" ? WLN_CONNSTR_MYSQL : WLN_CONNSTR_SQLITE;
				return _connstrRw;
			}
		}
		/// <summary>
		/// 数据库连接类型 默认：sqlite/mysql
		/// </summary>
		public static string WLN_CONNSTR_TYPE
		{
            get
            {
	            if (!string.IsNullOrEmpty(_connstrType))
	            {
		            return _connstrType;
	            }
	            _connstrType = Wlniao.Config.GetConfigs("WLN_CONNSTR_TYPE");
	            if (!string.IsNullOrEmpty(_connstrType))
	            {
		            return _connstrType;
	            }
	            if (!string.IsNullOrEmpty(WLN_CONNSTR_MYSQL))
                {
	                _connstrType = "mysql";
                }
                else if (!string.IsNullOrEmpty(WLN_CONNSTR_SQLSERVER))
                {
	                _connstrType = "sqlserver";
                }
                else
                {
	                _connstrType = "sqlite";
                }
                return _connstrType;
            }
		}
		/// <summary>
		/// 连接的数据库服务器地址（默认为127.0.0.1）
		/// </summary>
		public static string WLN_CONNSTR_HOST => Wlniao.Config.GetSetting("WLN_CONNSTR_HOST", "127.0.0.1");

		/// <summary>
		/// 连接的数据库名称
		/// </summary>
		public static string WLN_CONNSTR_NAME => Wlniao.Config.GetSetting("WLN_CONNSTR_NAME");
        /// <summary>
        /// 连接数据库的用户账号
        /// </summary>
        public static string WLN_CONNSTR_UID => Wlniao.Config.GetEncrypt("WLN_CONNSTR_UID", Config.Secret);

        /// <summary>
        /// 连接数据库的用户密码
        /// </summary>
        public static string WLN_CONNSTR_PWD => Wlniao.Config.GetEncrypt("WLN_CONNSTR_PWD", Config.Secret);

        /// <summary>
        /// MySql数据库连接字符串
        /// </summary>
        public static string WLN_CONNSTR_READONLY
        {
            get
            {
                _connstrRo ??= Wlniao.Config.GetConfigs("WLN_CONNSTR_READONLY");
                if (string.IsNullOrEmpty(_connstrRo))
                {
                    _connstrRo = WLN_CONNSTR;
                }
                return _connstrRo;
            }
        }
        /// <summary>
        /// MySql数据库连接字符串
        /// </summary>
        public static string WLN_CONNSTR_MYSQL
        {
            get
            {
	            if (_connstrMysql != null) return _connstrMysql;
	            _connstrMysql = Wlniao.Config.GetConfigs("WLN_CONNSTR_MYSQL");
	            if (!string.IsNullOrEmpty(_connstrMysql)) return _connstrMysql;
	            var wlnMysqlSsl = Wlniao.Config.GetConfigs("WLN_MYSQL_SSL", "none");
                var wlnMysqlPort = Wlniao.Config.GetConfigs("WLN_MYSQL_PORT", "3306");
                var wlnMysqlHost = Wlniao.Config.GetConfigs("WLN_MYSQL_HOST", WLN_CONNSTR_HOST);
                var wlnMysqlName = Wlniao.Config.GetConfigs("WLN_MYSQL_NAME", WLN_CONNSTR_NAME);
                var wlnMysqlUid = Wlniao.Config.GetEncrypt("WLN_MYSQL_UID", Config.Secret, WLN_CONNSTR_UID);
                var wlnMysqlPwd = Wlniao.Config.GetEncrypt("WLN_MYSQL_PWD", Config.Secret, WLN_CONNSTR_PWD);
                if (!string.IsNullOrEmpty(wlnMysqlHost) && !string.IsNullOrEmpty(wlnMysqlUid) && !string.IsNullOrEmpty(wlnMysqlPwd))
                {
	                _connstrMysql = $"Server={wlnMysqlHost};Port={wlnMysqlPort};Database={wlnMysqlName};Uid={wlnMysqlUid};Pwd={wlnMysqlPwd};CharSet=utf8;SslMode={wlnMysqlSsl};";
                }
                return _connstrMysql;
            }
        }

		/// <summary>
		/// Sqlite数据库连接字符串
		/// </summary>
		public static string WLN_CONNSTR_SQLITE
		{
			get
			{
				if (_connstrSqlite != null) return _connstrSqlite;
				var connStr = Wlniao.Config.GetSetting("WLN_CONNSTR_SQLITE");
				if (string.IsNullOrEmpty(connStr))
				{
					_connstrSqlite = "Data Source=" + Wlniao.IO.PathTool.Map(Wlniao.XCore.StartupRoot, XCore.FrameworkRoot, "xcore.db");
				}
				else if(connStr.IndexOf('=') < 0)
				{
					var file = Wlniao.IO.PathTool.Map(connStr);
					var folder = System.IO.Path.GetDirectoryName(file);
					if (!string.IsNullOrEmpty(folder) && (folder.LastIndexOf('/') > 0 || folder.LastIndexOf('\\') > 0) && !System.IO.Directory.Exists(folder))
					{
						// 数据库文件所在目录不存在时自动创建目录
						System.IO.Directory.CreateDirectory(folder);
					}
					_connstrSqlite = "Data Source=" + file;
				}
				else
				{
					_connstrSqlite = connStr;
				}

				return _connstrSqlite;
			}
		}

		/// <summary>
		/// SqlServer数据库连接字符串
		/// </summary>
		public static string WLN_CONNSTR_SQLSERVER
        {
            get
            {
	            if (_connstrSqlsqlver != null) return _connstrSqlsqlver;
	            _connstrSqlsqlver = Wlniao.Config.GetConfigs("WLN_CONNSTR_SQLSERVER");
	            if (!string.IsNullOrEmpty(_connstrSqlsqlver)) return _connstrSqlsqlver;
	            var wlnMssqlHost = Wlniao.Config.GetConfigs("WLN_MSSQL_HOST", WLN_CONNSTR_HOST);
                var wlnMssqlName = Wlniao.Config.GetConfigs("WLN_MSSQL_NAME", WLN_CONNSTR_NAME);
                var wlnMssqlUid = Wlniao.Config.GetEncrypt("WLN_MSSQL_UID", Config.Secret, WLN_CONNSTR_UID);
                var wlnMssqlPwd = Wlniao.Config.GetEncrypt("WLN_MSSQL_PWD", Config.Secret, WLN_CONNSTR_PWD);
                if (string.IsNullOrEmpty(wlnMssqlName) || string.IsNullOrEmpty(wlnMssqlUid) || string.IsNullOrEmpty(wlnMssqlPwd))
                {
	                _connstrSqlsqlver = "";
	                return _connstrSqlsqlver;
                }
                var wlnMssqlPort = Wlniao.Config.GetConfigs("WLN_MSSQL_PORT", "1433");
                _connstrSqlsqlver = $"Server={wlnMssqlHost},{wlnMssqlPort};Database={wlnMssqlName};User Id={wlnMssqlUid};Password={wlnMssqlPwd};TrustServerCertificate=true;";
                return _connstrSqlsqlver;
            }
        }

    }
}