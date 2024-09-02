using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Net.Http;
using System.Globalization;
using System.Runtime.Serialization;

namespace Wlniao.XServer
{
    /// <summary>
    /// 综合存储服务
    /// </summary>
    public class XStorage
    {
        private static string Suffix = "!w";
        private static string _UploadPath = null;
        private static string _XStorageUrl = null;
        private static string _XStorageType = null;
        private static string[] _XStorageUrls = null;

        /// <summary>
        /// 上传路径
        /// </summary>
        public static string UploadPath
        {
            get
            {
                if (_UploadPath == null)
                {
                    _UploadPath = Config.GetSetting("UploadPath");
                    if (string.IsNullOrEmpty(_UploadPath))
                    {
                        _UploadPath = "";
                    }
                    else
                    {
                        _UploadPath = _UploadPath.TrimEnd('/');
                    }
                }
                return _UploadPath;
            }
        }
        /// <summary>
        /// XStorage存储类型 local/aliyun/qcloud/upyun
        /// </summary>
        public static string XStorageType
        {
            get
            {
                if (_XStorageType == null)
                {
                    _XStorageType = Config.GetSetting("XStorageType", "local").ToLower();
                    if (_XStorageType != "local" && string.IsNullOrEmpty(XStorageUrl))
                    {
                        Log.Loger.Error("请先设置XStorageUrl参数");
                    }
                }
                return _XStorageType;
            }
        }

        /// <summary>
        /// XStorage访问地址
        /// </summary>
        public static string XStorageUrl
        {
            get
            {
                if (_XStorageUrl == null)
                {
                    var temp = Config.GetSetting("XStorageUrl");
                    if (string.IsNullOrEmpty(temp))
                    {
                        _XStorageUrl = "";
                    }
                    else if (temp.IndexOf("//") >= 0)
                    {
                        _XStorageUrl = temp.TrimEnd('/');
                    }
                    else
                    {
                        _XStorageUrl = "//" + temp.TrimEnd('/');
                    }
                }
                return _XStorageUrl;
            }
        }
        /// <summary>
        /// XStorage地址列表
        /// </summary>
        private static string[] XStorageUrls
        {
            get
            {
                if (_XStorageUrls == null)
                {
                    var temp = Config.GetConfigs("XStorageUrls");
                    if (string.IsNullOrEmpty(temp))
                    {
                        if (string.IsNullOrEmpty(XStorageUrl))
                        {
                            _XStorageUrls = new string[0];
                        }
                        else if (XStorageUrl.StartsWith("//"))
                        {
                            _XStorageUrls = new[] { "http:" + XStorageUrl, "https:" + XStorageUrl };
                        }
                        else if (_XStorageUrl.LastIndexOf("//") < 0)
                        {
                            _XStorageUrls = new[] { "http://" + XStorageUrl, "https://" + XStorageUrl };
                        }
                        else
                        {
                            _XStorageUrls = new[] { XStorageUrl };
                        }
                    }
                    else
                    {
                        if (!temp.Contains(XStorageUrl))
                        {
                            temp = XStorageUrl + "," + temp;
                        }
                        _XStorageUrls = temp.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    }
                }
                return _XStorageUrls;
            }
        }

        /// <summary>
        /// 转换为XStorage存储格式（去除Host部分）
        /// </summary>
        /// <param name="SourceUrl"></param>
        /// <returns></returns>
        public static string ConvertUrlToStorage(string SourceUrl)
        {
            if (string.IsNullOrEmpty(SourceUrl))
            {
                SourceUrl = "";
            }
            else if (SourceUrl.IndexOf("//") >= 0)
            {
                SourceUrl = SourceUrl.Substring(SourceUrl.IndexOf("//") + 2);
                SourceUrl = SourceUrl.Substring(SourceUrl.IndexOf('/'));
            }
            else if (SourceUrl.IndexOf('/') != 0)
            {
                SourceUrl = "/" + SourceUrl;
            }
            return SourceUrl;
        }
        /// <summary>
        /// 转换为XStorage存储格式（去除Host部分）
        /// </summary>
        /// <param name="SourceUrls"></param>
        /// <returns></returns>
        public static string ConvertUrlsToStorage(string SourceUrls)
        {
            if (string.IsNullOrEmpty(SourceUrls))
            {
                return "";
            }
            else
            {
                var urls = "";
                foreach (var url in SourceUrls.Split(",", StringSplitOptions.RemoveEmptyEntries))
                {
                    urls += Wlniao.XServer.XStorage.ConvertUrlToStorage(url) + ",";
                }
                return urls.Trim(',');
            }
        }

        /// <summary>
        /// 添加XStorageUrl
        /// </summary>
        /// <param name="SourceUrls"></param>
        /// <returns></returns>
        public static string ConvertUrlsToFullUrls(string SourceUrls)
        {
            if (string.IsNullOrEmpty(SourceUrls))
            {
                return "";
            }
            else
            {
                var urls = "";
                foreach (var url in SourceUrls.Split(",", StringSplitOptions.RemoveEmptyEntries))
                {
                    urls += Wlniao.XServer.XStorage.ConvertUrlToFullUrl(url, false) + ",";
                }
                return urls.Trim(',');
            }
        }

        /// <summary>
        /// 添加XStorageUrl
        /// </summary>
        /// <param name="SourceUrl"></param>
        /// <returns></returns>
        public static string ConvertUrlToFullUrl(string SourceUrl)
        {
            return ConvertUrlToFullUrl(SourceUrl, false);
        }
        /// <summary>
        /// 添加XStorageUrl
        /// </summary>
        /// <param name="SourceUrl"></param>
        /// <param name="darwing"></param>
        /// <param name="suffix"></param>
        /// <returns></returns>
        public static string ConvertUrlToFullUrl(string SourceUrl, bool darwing, string suffix = "")
        {
            if (string.IsNullOrEmpty(SourceUrl))
            {
                return "";
            }
            else if (SourceUrl.IndexOf("//") < 0)
            {
                SourceUrl = ConvertUrlToStorage(SourceUrl);
                SourceUrl = XStorageUrl + SourceUrl;
                return SourceUrl + (darwing ? (string.IsNullOrEmpty(suffix) ? Suffix : suffix) : "");
            }
            else
            {
                return SourceUrl;
            }
        }

        /// <summary>
        /// 简化HTML内容中的图片地址
        /// </summary>
        /// <param name="SourceStr"></param>
        /// <returns></returns>
        public static string ConvertHtmlToStorage(string SourceStr)
        {
            if (string.IsNullOrEmpty(SourceStr))
            {
                return "";
            }
            foreach (var url in XStorageUrls)
            {
                SourceStr = SourceStr.Replace("src=\"" + url + "//", "src=\"/").Replace("src=\"" + url + "/", "src=\"/");
            }
            return SourceStr;
        }
        /// <summary>
        /// 还原HTML内容中的图片地址
        /// </summary>
        /// <param name="DataStr"></param>
        /// <returns></returns>
        public static string ConvertHtmlToFullUrl(string DataStr)
        {
            if (string.IsNullOrEmpty(DataStr))
            {
                return "";
            }
            return DataStr.Replace("src=\"/", "src=\"" + XStorageUrl + "/");
        }
        /// <summary>
        /// 还原HTML内容中的图片地址
        /// </summary>
        /// <param name="DataStr"></param>
        /// <param name="darwing"></param>
        /// <param name="suffix"></param>
        /// <returns></returns>
        public static string ConvertHtmlToFullUrl(string DataStr, bool darwing = false, string suffix = "")
        {
            if (string.IsNullOrEmpty(DataStr))
            {
                return "";
            }
            DataStr = strUtil.HtmlDecode(DataStr);
            DataStr = DataStr.Replace("src=\"/", "src=\"" + XStorageUrl + "/");
            if (darwing)
            {
                var _temp = "";
                var lines = DataStr.Split(new[] { "src=\"" }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines)
                {
                    if (string.IsNullOrEmpty(_temp))
                    {
                        _temp = line;
                    }
                    else if (line.StartsWith(XStorageUrl))
                    {
                        var _line = line.Replace(".jpg\"", ".jpg" + (string.IsNullOrEmpty(suffix) ? Suffix : suffix) + "\"");
                        _line = _line.Replace(".png\"", ".png" + (string.IsNullOrEmpty(suffix) ? Suffix : suffix) + "\"");
                        _line = _line.Replace(".gif\"", ".gif" + (string.IsNullOrEmpty(suffix) ? Suffix : suffix) + "\"");
                        _temp += "src=\"" + _line;
                    }
                    else
                    {
                        _temp += "src=\"" + line;
                    }
                }
                DataStr = _temp;
            }
            return DataStr;
        }

        /// <summary>
        /// 保存在线文件
        /// </summary>
        /// <param name="FileName"></param>
        /// <param name="url"></param>
        /// <returns></returns>
        public static Boolean SaveUrl(String FileName, String url)
        {
            using (var client = new System.Net.Http.HttpClient())
            {
                try
                {
                    byte[] data = null;
                    var reqest = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Get, url);
                    reqest.Headers.Date = DateTime.UtcNow;
                    client.SendAsync(reqest).ContinueWith((requestTask) =>
                    {
                        requestTask.Result.Content.ReadAsByteArrayAsync().ContinueWith((readTask) =>
                        {
                            data = readTask.Result;
                        }).Wait();
                    }).Wait();
                    if (data != null)
                    {
                        if (XStorageType == "local")
                        {
                            string toFileName = IO.PathTool.Map(UploadPath, FileName);
                            string toFilePath = System.IO.Path.GetDirectoryName(toFileName);
                            string toFileExt = System.IO.Path.GetExtension(toFileName);
                            if (!Directory.Exists(toFilePath))
                            {
                                Directory.CreateDirectory(toFilePath);
                            }
                            else if (File.Exists(toFileName))
                            {
                                File.Delete(toFileName);
                            }
                            using (var fs = new System.IO.FileStream(toFileName, FileMode.CreateNew))
                            {
                                fs.Write(data, 0, data.Length);
                                return true;
                            }
                        }
                        else
                        {
                            return Upload(UploadPath + FileName, data);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Loger.Error(ex.Message);
                }
            }
            return false;
        }
        /// <summary>
        /// 上传文件
        /// </summary>
        /// <param name="FileName"></param>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static Boolean Upload(String FileName, System.IO.Stream stream)
        {
            var rlt = Upload(FileName, cvt.ToBytes(stream));
            stream.Dispose();
            return rlt;
        }
        /// <summary>
        /// 上传文件
        /// </summary>
        /// <param name="FileName"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public static Boolean Upload(String FileName, byte[] data)
        {
            try
            {
                if (XStorageType == "aliyun")
                {
                    if (FileName[0] == '/')
                    {
                        if (UploadPath.EndsWith("/"))
                        {
                            return Aliyun.WriteFile(UploadPath.TrimEnd('/') + FileName, data);
                        }
                        else
                        {
                            return Aliyun.WriteFile(UploadPath + FileName, data);
                        }
                    }
                    else if (UploadPath.EndsWith("/"))
                    {
                        return Aliyun.WriteFile(UploadPath + FileName, data);
                    }
                    else
                    {
                        return Aliyun.WriteFile(UploadPath + "/" + FileName, data);
                    }
                }
                else if (XStorageType == "qcloud")
                {
                    if (FileName[0] == '/')
                    {
                        if (UploadPath.EndsWith("/"))
                        {
                            return QCloud.WriteFile(UploadPath.TrimEnd('/') + FileName, data);
                        }
                        else
                        {
                            return QCloud.WriteFile(UploadPath + FileName, data);
                        }
                    }
                    else if (UploadPath.EndsWith("/"))
                    {
                        return QCloud.WriteFile(UploadPath + FileName, data);
                    }
                    else
                    {
                        return QCloud.WriteFile(UploadPath + "/" + FileName, data);
                    }
                }
                else if (XStorageType == "upyun")
                {
                    return Upyun.WriteFile(UploadPath + FileName, data);
                }
                else if (XStorageType == "local")
                {
                    var toFileName = IO.PathTool.Map(UploadPath, FileName);
                    var toFilePath = System.IO.Path.GetDirectoryName(toFileName);
                    var toFileExt = System.IO.Path.GetExtension(toFileName);
                    if (!Directory.Exists(toFilePath))
                    {
                        Directory.CreateDirectory(toFilePath);
                    }
                    using (var fs = new System.IO.FileStream(toFileName, FileMode.CreateNew))
                    {
                        fs.Write(data, 0, data.Length);
                        return true;
                    }
                }
                else
                {
                    Log.Loger.Warn("参数“XStorageType”配置无效，请重新配置");
                }
            }
            catch (Exception ex)
            {
                Log.Loger.Error(ex.Message);
            }
            return false;
        }

        /// <summary>
        /// 上传文件至本地目录
        /// </summary>
        /// <param name="absolutePath">文件的绝对路径</param>
        /// <param name="data"></param>
        /// <returns></returns>
        public static Boolean UploadLocal(String absolutePath, byte[] data)
        {
            try
            {
                var toFilePath = System.IO.Path.GetDirectoryName(absolutePath);
                var toFileExt = System.IO.Path.GetExtension(absolutePath);
                if (!Directory.Exists(toFilePath))
                {
                    Directory.CreateDirectory(toFilePath);
                }
                using (var fs = new System.IO.FileStream(absolutePath, FileMode.CreateNew))
                {
                    fs.Write(data, 0, data.Length);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Log.Loger.Error(ex.Message);
            }
            return false;
        }
        /// <summary>
        /// 读取文件
        /// </summary>
        /// <param name="FileName"></param>
        /// <returns></returns>
        public static Byte[] Read(String FileName)
        {
            byte[] buffur = null;
            if (File.Exists(FileName))
            {
                var fs = new FileStream(FileName.StartsWith(UploadPath) ? FileName : UploadPath + FileName, FileMode.Open, FileAccess.Read);
                try
                {
                    buffur = new byte[fs.Length];
                    fs.Read(buffur, 0, (int)fs.Length);
                }
                finally
                {
                    if (fs != null)
                    {
                        //关闭资源
                        fs.Dispose();
                    }
                }
            }
            if (buffur == null && XStorageType == "upyun")
            {
                buffur = cvt.ToBytes(Upyun.ReadFile(FileName));
            }
            return buffur;
        }
        /// <summary>
        /// 本地存储
        /// </summary>
        public static class Local
        {
            /// <summary>
            /// 本地存储的路径
            /// </summary>
            public static String Path
            {
                get
                {
                    return UploadPath;
                }
            }
        }
        /// <summary>
        /// Upyun设置
        /// </summary>
        public static class Upyun
        {
            internal static String bucketname = null;
            internal static String username = null;
            internal static String password = null;
            internal static String formapi = null;
            private static bool upAuth = true;
            private static string api_domain = "v0.api.upyun.com";
            private static string DL = "/";

            /// <summary>
            /// 加载配置的参数
            /// </summary>
            private static void Load()
            {
                if (bucketname == null)
                {
                    bucketname = Config.GetConfigs("UpyunBucket");
                    username = Config.GetConfigs("UpyunUsername");
                    password = Config.GetConfigs("UpyunPassword");
                    formapi = Config.GetConfigs("UpyunFormApi");
                    if (string.IsNullOrEmpty(bucketname))
                    {
                        bucketname = "";
                    }
                }
            }
            /// <summary>
            /// 是否启用
            /// </summary>
            private static Boolean canUsing
            {
                get
                {
                    Load();
                    if (string.IsNullOrEmpty(bucketname) || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                    {
                        return false;
                    }
                    else
                    {
                        return true;
                    }
                }
            }

            /// <summary>
            /// FormAPI参数
            /// </summary>
            /// <param name="expire">过期时间（单位：秒）</param>
            /// <param name="max">文件最大大小</param>
            /// <returns></returns>
            public static String FormApi(int expire = 5400, int max = 200)
            {
                return FormApi(expire, max, null);
            }
            /// <summary>
            /// FormAPI参数
            /// </summary>
            /// <param name="expire">过期时间（单位：秒）</param>
            /// <param name="max">文件最大大小</param>
            /// <param name="dir">上传目录</param>
            /// <returns></returns>
            public static String FormApi(int expire, int max, string dir)
            {
                Load();
                max = max * 1024 * 1024;
                if (!string.IsNullOrEmpty(bucketname) && !string.IsNullOrEmpty(formapi))
                {
                    if (string.IsNullOrEmpty(dir))
                    {
                        dir = "/{year}{mon}/{mon}{day}/";
                    }
                    else
                    {
                        if (!dir.StartsWith("/"))
                        {
                            dir = "/" + dir;
                        }
                        if (!dir.EndsWith("/"))
                        {
                            dir = dir + "/";
                        }
                    }
                    var json = "{\"bucket\":\"" + bucketname + "\",\"save-key\":\"" + dir + "{random}{.suffix}\",\"expiration\":\"" + (XCore.NowUnix + expire) + "\"}";
                    var policy = Encryptor.Base64Encrypt(json);
                    var signature = Encryptor.Md5Encryptor32(policy + "&" + formapi);
                    return Json.ToString(new { to = "upyun", host = XStorageUrl, bucket = bucketname, policy, signature });
                }
                return "";
            }
            #region Upyun接口

            private static string ConvertPath(string path)
            {
                if (string.IsNullOrEmpty(path))
                {
                    return "/";
                }
                else
                {
                    path = path.Replace("\\", "/");
                    if (path.StartsWith("/"))
                    {
                        return path;
                    }
                    else
                    {
                        return "/" + path;
                    }
                }
            }
            /// <summary>
            /// 文件信息
            /// </summary>
            public class FileInfo
            {
                /// <summary>
                /// 
                /// </summary>
                public string type;
                /// <summary>
                /// 
                /// </summary>
                public string size;
                /// <summary>
                /// 
                /// </summary>
                public string date;
            }
            /// <summary>
            /// 文件夹信息
            /// </summary>
            public class FolderItem
            {
                /// <summary>
                /// 
                /// </summary>
                public string filename;
                /// <summary>
                /// 
                /// </summary>
                public string filetype;
                /// <summary>
                /// 
                /// </summary>
                public int size;
                /// <summary>
                /// 
                /// </summary>
                public int number;
                /// <summary>
                /// 
                /// </summary>
                /// <param name="filename"></param>
                /// <param name="filetype"></param>
                /// <param name="size"></param>
                /// <param name="number"></param>
                public FolderItem(string filename, string filetype, int size, int number)
                {
                    this.filename = filename;
                    this.filetype = filetype;
                    this.size = size;
                    this.number = number;
                }
            }


            /**
            * 切换 API 接口的域名
            * @param $domain {默认 v0.api.upyun.com 自动识别, v1.api.upyun.com 电信, v2.api.upyun.com 联通, v3.api.upyun.com 移动}
            * return null;
            */
            internal static void setApiDomain(string domain)
            {
                api_domain = domain;
            }
            /// <summary>
            /// 是否启用 又拍签名认证（默认 false）
            /// true:启用又拍签名认证
            /// false:直接使用basic auth
            /// </summary>
            /// <param name="authType"></param>
            internal static void setAuthType(bool authType)
            {
                upAuth = authType;
            }
            private static HttpResponseMessage newWorker(string method, string Url, byte[] postData = null, Hashtable headers = null)
            {
                using (var client = new HttpClient())
                {
                    var request = new HttpRequestMessage(new HttpMethod(method), "http://" + api_domain + Url);
                    if (headers == null)
                    {
                        headers = new Hashtable();
                    }
                    headers.Add("mkdir", "true");
                    if (upAuth)
                    {
                        var date = DateTools.ConvertToGMT();
                        var auth = Encryptor.Md5Encryptor32(method + '&' + Url + '&' + date + '&' + (postData == null ? 0 : postData.Length) + '&' + Encryptor.Md5Encryptor32(password).ToLower());
                        request.Headers.TryAddWithoutValidation("Date", date);
                        request.Headers.TryAddWithoutValidation("Authorization", "UpYun " + username + ':' + auth);
                    }
                    else
                    {
                        request.Headers.TryAddWithoutValidation("Authorization", "Basic " +
                            System.Convert.ToBase64String(new System.Text.ASCIIEncoding().GetBytes(username + ":" + password)));
                    }
                    foreach (DictionaryEntry var in headers)
                    {
                        request.Headers.Add(var.Key.ToString(), var.Value.ToString());
                    }
                    if (postData != null)
                    {
                        request.Content = new StreamContent(new MemoryStream(postData));
                    }
                    var res = new HttpResponseMessage() { StatusCode = System.Net.HttpStatusCode.InternalServerError };
                    client.SendAsync(request).ContinueWith((requestTask) =>
                    {
                        res = requestTask.Result;
                    }).Wait();
                    return res;
                }
            }
            private static bool delete(string path, Hashtable headers = null)
            {
                var resp = newWorker("DELETE", DL + bucketname + path, null, headers);
                if (resp.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }

            /// <summary>
            /// 获取某个子目录的占用信息
            /// </summary>
            /// <param name="folder">文件夹路径（为空即整个Bucket）</param>
            /// <returns></returns>
            public static int GetFolderUsage(string folder = "")
            {
                var resp = newWorker("GET", DL + bucketname + folder + "?usage");
                try
                {
                    if (resp.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        resp.Content.ReadAsStringAsync().ContinueWith((readTask) =>
                        {
                            return cvt.ToInt(readTask.Result);
                        }).Wait();
                    }
                }
                catch (Exception ex)
                {
                    Log.Loger.Error("Upyun GetFolderUsage Error:" + ex.Message);
                }
                return 0;
            }
            /// <summary>
            /// 创建目录
            /// </summary>
            /// <param name="path"></param>
            /// <returns></returns>
            public static bool mkDir(string path)
            {
                path = ConvertPath(path);
                var headers = new Hashtable();
                headers.Add("folder", "create");
                var resp = newWorker("POST", DL + bucketname + path, null, headers);
                if (resp.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            /// <summary>
            /// 删除目录
            /// </summary>
            /// <param name="path"></param>
            /// <returns></returns>
            public static bool rmDir(string path)
            {
                path = ConvertPath(path);
                return delete(path, new Hashtable());
            }
            /**
            * 读取目录列表
            * @param $path 目录路径
            * return array 数组 或 null
            */
            public static List<FolderItem> readDir(string url)
            {
                var AL = new List<FolderItem>();
                try
                {
                    var resp = newWorker("GET", DL + bucketname);
                    if (resp.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        resp.Content.ReadAsStringAsync().ContinueWith(a =>
                        {
                            var str = a.Result;
                            str = str.Replace("\t", "\\");
                            str = str.Replace("\n", "\\");
                            var ss = str.Split('\\');
                            int i = 0;
                            while (i < ss.Length)
                            {
                                FolderItem fi = new FolderItem(ss[i], ss[i + 1], int.Parse(ss[i + 2]), int.Parse(ss[i + 3]));
                                AL.Add(fi);
                                i += 4;
                            }
                        }).Wait();
                    }
                }
                catch { }
                return AL;
            }

            /// <summary>
            /// 将字符串写入文件
            /// </summary>
            /// <param name="path"></param>
            /// <param name="str"></param>
            /// <returns></returns>
            public static bool WriteStr(string path, string str)
            {
                return WriteFile(path, System.Text.Encoding.UTF8.GetBytes(str));
            }
            /// <summary>
            /// 上传文件
            /// </summary>
            /// <param name="path"></param>
            /// <param name="data"></param>
            /// <returns></returns>
            public static bool WriteFile(string path, byte[] data)
            {
                path = ConvertPath(path);
                var resp = newWorker("POST", DL + bucketname + path, data);
                if (resp.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            /**
            * 删除文件
            * @param $file 文件路径（包含文件名）
            * return true or false
            */
            public static bool DeleteFile(string path)
            {
                path = ConvertPath(path);
                return delete(path);
            }

            /// <summary>
            /// 从文件读取字符串
            /// </summary>
            /// <param name="path"></param>
            /// <returns></returns>
            public static string ReadStr(string path)
            {
                var stream = ReadFile(path);
                if (stream != null)
                {
                    return System.Text.Encoding.UTF8.GetString(cvt.ToBytes(stream));
                }
                return null;
            }
            /**
            * 读取文件
            * @param $file 文件路径（包含文件名）
            * @param $output_file 可传递文件IO数据流（默认为 null，结果返回文件内容，如设置文件数据流，将返回 true or false）
            * return 文件内容 或 null
            */
            public static Stream ReadFile(string path)
            {
                path = ConvertPath(path);
                var resp = newWorker("GET", DL + bucketname + path);
                if (resp.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    resp.Content.ReadAsStreamAsync().ContinueWith((readTask) =>
                    {
                        return readTask.Result;
                    }).Wait();
                }
                return null;
            }
            /**
            * 获取文件信息
            * @param $file 文件路径（包含文件名）
            * return array('type'=> file | folder, 'size'=> file size, 'date'=> unix time) 或 null
            */
            public static Hashtable GetFileInfo(Hashtable tmp_infos, string file)
            {
                var resp = newWorker("HEAD", DL + bucketname + file);
                try
                {
                    var ht = new Hashtable();
                    ht.Add("type", tmp_infos["x-upyun-file-type"]);
                    ht.Add("size", tmp_infos["x-upyun-file-size"]);
                    ht.Add("date", tmp_infos["x-upyun-file-date"]);
                    return ht;
                }
                catch (Exception ex)
                {
                    Log.Loger.Error("Upyun GetFileInfo Error:" + ex.Message);
                }
                return new Hashtable();
            }
            /// <summary>
            /// 计算文件的MD5码
            /// </summary>
            /// <param name="pathName"></param>
            /// <returns></returns>
            public static string md5_file(string pathName)
            {
                string strResult = "";
                string strHashData = "";

                byte[] arrbytHashValue;
                var oFileStream = new System.IO.FileStream(pathName, System.IO.FileMode.Open,
                        System.IO.FileAccess.Read, System.IO.FileShare.ReadWrite);
                arrbytHashValue = MD5.Create().ComputeHash(oFileStream);//计算指定Stream 对象的哈希值
                                                                        //由以连字符分隔的十六进制对构成的String，其中每一对表示value 中对应的元素；例如“F-2C-4A”
                strHashData = System.BitConverter.ToString(arrbytHashValue);
                //替换-
                strHashData = strHashData.Replace("-", "");
                strResult = strHashData;
                return strResult.ToLower();
            }
            #endregion
        }
        /// <summary>
        /// Aliyun设置
        /// </summary>
        public class Aliyun
        {
            private static Aliyun instance = new Aliyun();
            private String bucket = null;
            private String ossregion = null;
            private String ossdomain = null;
            private String ossaccesskeyid = null;
            private String ossaccesskeySecret = null;
            /// <summary>
            /// 
            /// </summary>
            public Aliyun()
            {
                bucket = Config.GetSetting("XStorageBucket");
                ossregion = Config.GetSetting("XStorageRegion");
                ossdomain = Config.GetSetting("XStorageEndPoint", "");
                ossaccesskeyid = Config.GetSetting("XStorageAccessKeyId", "");
                ossaccesskeySecret = Config.GetSetting("XStorageAccessKeySecret", "");
                ConfigInit();
            }
            /// <summary>
            /// 
            /// </summary>
            public Aliyun(String bucket, String domain, String accesskeyid, String accesskeySecret)
            {
                this.bucket = bucket;
                ossdomain = domain;
                ossaccesskeyid = accesskeyid;
                ossaccesskeySecret = accesskeySecret;
                ConfigInit();
            }
            private void ConfigInit()
            {
                if (!string.IsNullOrEmpty(ossdomain))
                {
                    ossdomain = ossdomain.Trim().Trim('/');
                    if (ossdomain.IndexOf("://") > 0)
                    {
                        ossdomain = ossdomain.Substring(ossdomain.IndexOf("://") + 3);
                    }
                    var tos = ossdomain.Split('.');
                    var region = tos.Where(o => o.StartsWith("oss-")).FirstOrDefault();
                    if (!string.IsNullOrEmpty(region))
                    {
                        if (string.IsNullOrEmpty(bucket) && tos[1] == region)
                        {
                            bucket = tos[0];
                        }
                        else if (!string.IsNullOrEmpty(bucket) && tos[0] == region)
                        {
                            ossdomain = bucket + "." + ossdomain;
                        }
                        if (string.IsNullOrEmpty(ossregion))
                        {
                            ossregion = region.Substring(4);
                        }
                    }
                }
            }

            /// <summary>
            /// 是否可用
            /// </summary>
            public Boolean canUsing
            {
                get
                {
                    if (string.IsNullOrEmpty(bucket) || string.IsNullOrEmpty(ossregion) || string.IsNullOrEmpty(ossdomain) || string.IsNullOrEmpty(ossaccesskeyid) || string.IsNullOrEmpty(ossaccesskeySecret))
                    {
                        return false;
                    }
                    else
                    {
                        return true;
                    }
                }
            }

            /// <summary>
            /// 执行一个新任务
            /// </summary>
            /// <param name="method"></param>
            /// <param name="path"></param>
            /// <param name="postData"></param>
            /// <param name="headers"></param>
            /// <param name="message"></param>
            /// <returns></returns>
            public HttpResponseMessage newWorker(string method, string path, byte[] postData, Hashtable headers, out string message)
            {
                using (var client = new HttpClient())
                {
                    var request = new HttpRequestMessage(new HttpMethod(method), "https://" + ossdomain + path);
                    if (postData != null)
                    {
                        request.Content = new StreamContent(new MemoryStream(postData));
                    }
                    var time = DateTime.UtcNow;
                    //var time = DateTools.ConvertToUtc(1720574407);
                    var days = time.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
                    var date = time.ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture);
                    var scope = string.Format("{0}/{1}/{2}/aliyun_v4_request", days, ossregion, "oss");
                    var payload = "UNSIGNED-PAYLOAD";
                    var contentType = Wlniao.MimeMapping.GetMimeMapping(path);
                    request.Content.Headers.ContentMD5 = MD5.Create().ComputeHash(postData);
                    request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
                    request.Content.Headers.ContentLength = postData.Length;
                    // 步骤1：构造CanonicalRequest
                    var canonicalRequest = method + "\n" + "/" + bucket + path + "\n" + "\n" +
                         "content-md5:" + System.Convert.ToBase64String(request.Content.Headers.ContentMD5) + "\n" +
                         "content-type:" + contentType + "\n" +
                         "host:" + ossdomain + "\n" +
                         "x-oss-content-sha256:" + payload + "\n" +
                         "x-oss-credential:" + ossaccesskeyid + "/" + scope + "\n" +
                         "x-oss-date:" + date + "\n" +
                         "\n" + "host" + "\n" + payload;

                    // 步骤2：构造待签名字符串（StringToSign）
                    var stringToSign = string.Format("OSS4-HMAC-SHA256\n{0}\n{1}\n{2}", date, scope, Encryptor.Sha256(canonicalRequest).ToLower());

                    // 步骤3：计算Signature
                    var SigningKey = Encryptor.HmacSHA256(System.Text.Encoding.UTF8.GetBytes("aliyun_v4_request"), Encryptor.HmacSHA256(System.Text.Encoding.UTF8.GetBytes("oss"), Encryptor.HmacSHA256(System.Text.Encoding.UTF8.GetBytes(ossregion), Encryptor.HmacSHA256(System.Text.Encoding.UTF8.GetBytes(days), System.Text.Encoding.UTF8.GetBytes("aliyun_v4" + ossaccesskeySecret)))));
                    var Signature = System.Convert.ToHexString(Encryptor.HmacSHA256(System.Text.Encoding.UTF8.GetBytes(stringToSign), SigningKey)).ToLower();

                    // 步骤4：拼接Authorization
                    var Authorization = "OSS4-HMAC-SHA256 Credential=" + ossaccesskeyid + "/" + scope + ",AdditionalHeaders=host,Signature=" + Signature;

                    request.Headers.TryAddWithoutValidation("Host", ossdomain);
                    request.Headers.TryAddWithoutValidation("Date", time.ToString("r"));
                    request.Headers.TryAddWithoutValidation("Authorization", Authorization);
                    request.Headers.TryAddWithoutValidation("x-oss-content-sha256", payload);
                    request.Headers.TryAddWithoutValidation("x-oss-date", date);
                    request.Headers.TryAddWithoutValidation("x-oss-credential", ossaccesskeyid + "/" + scope);

                    if (headers != null)
                    {
                        foreach (DictionaryEntry kv in headers)
                        {
                            if (!request.Headers.TryAddWithoutValidation(kv.Key.ToString(), kv.Value.ToString()))
                            {
                                request.Content.Headers.TryAddWithoutValidation(kv.Key.ToString(), kv.Value.ToString());
                            }
                        }
                    }
                    var msg = "";
                    var res = new HttpResponseMessage() { StatusCode = System.Net.HttpStatusCode.InternalServerError };
                    client.SendAsync(request).ContinueWith((requestTask) =>
                    {
                        res = requestTask.Result;
                        if (res.StatusCode != System.Net.HttpStatusCode.OK)
                        {
                            res.Content.ReadAsStringAsync().ContinueWith((readTask) =>
                            {
                                if (!string.IsNullOrEmpty(readTask.Result))
                                {
                                    var doc = new System.Xml.XmlDocument();
                                    doc.LoadXml(readTask.Result);
                                    msg = doc.GetElementsByTagName("Message")[0].InnerText;
                                    Log.Loger.Debug("XStorage Aliyun：" + msg);
                                }
                            }).Wait();
                        }
                    }).Wait();
                    message = msg;
                    return res;
                }
            }
            /// <summary>
            /// FormAPI参数
            /// </summary>
            /// <param name="expire">过期时间（单位：秒）</param>
            /// <param name="max">文件最大大小</param>
            /// <returns></returns>
            public String formApi(int expire = 5400, int max = 200)
            {
                return formApi(null, expire, max);
            }
            /// <summary>
            /// FormAPI参数
            /// </summary>
            /// <param name="dir">上传目录</param>
            /// <param name="expire">过期时间（单位：秒）</param>
            /// <param name="max">文件最大大小（单位：M）</param>
            /// <returns></returns>
            public String formApi(string dir, int expire = 5400, int max = 200)
            {
                if (canUsing)
                {
                    max = max * 1024 * 1024;
                    if (string.IsNullOrEmpty(dir))
                    {
                        dir = DateTools.Format("yyyyMM/MMdd/");
                    }
                    else
                    {
                        if (dir.StartsWith("/"))
                        {
                            dir = dir.TrimStart('/');
                        }
                        if (!dir.EndsWith("/"))
                        {
                            dir = dir + "/";
                        }
                    }
                    var json = "{\"expiration\":\"" + DateTime.UtcNow.AddSeconds(expire).ToString("yyyy-MM-ddTHH:mm:ssZ") + "\",\"conditions\":[[\"content-length-range\", 0, " + max + "],[\"starts-with\",\"$key\",\"" + dir + "\"]]}";
                    var policy = Encryptor.Base64Encrypt(json);
                    var bytesPolicy = Wlniao.Text.Encoding.UTF8.GetBytes(policy);
                    var bytesSecret = Wlniao.Text.Encoding.UTF8.GetBytes(ossaccesskeySecret);
                    var signature = System.Convert.ToBase64String(Encryptor.HmacSHA1(bytesPolicy, bytesSecret));
                    var host = ossdomain.IndexOf("://") < 0 ? "//" + ossdomain : ossdomain;
                    return Json.ToString(new { to = "oss", host = string.IsNullOrEmpty(XStorageUrl) ? host : XStorageUrl, ossdomain = host, ossaccesskeyid, dir, policy, signature });
                }
                return "";
            }
            /// <summary>
            /// 上传文件
            /// </summary>
            /// <param name="path"></param>
            /// <param name="data"></param>
            /// <param name="message"></param>
            /// <returns></returns>
            public Boolean writeFile(string path, byte[] data, out string message)
            {
                message = "";
                if (canUsing)
                {
                    try
                    {
                        var resp = newWorker("PUT", path, data, null, out message);
                        if (resp.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            return true;
                        }
                    }
                    catch (Exception ex) { message = "上传异常：" + ex.Message; }
                }
                else
                {
                    message = "XStorage上传配置无效";
                }
                return false;
            }


            /// <summary>
            /// FormAPI参数
            /// </summary>
            /// <param name="expire">过期时间（单位：秒）</param>
            /// <param name="max">文件最大大小</param>
            /// <returns></returns>
            public static String FormApi(int expire = 5400, int max = 200)
            {
                return instance.formApi(null, expire, max);
            }
            /// <summary>
            /// FormAPI参数
            /// </summary>
            /// <param name="dir">上传目录</param>
            /// <param name="expire">过期时间（单位：秒）</param>
            /// <param name="max">文件最大大小（单位：M）</param>
            /// <returns></returns>
            public static String FormApi(string dir, int expire = 5400, int max = 200)
            {
                return instance.formApi(dir, expire, max);
            }
            /// <summary>
            /// 上传文件
            /// </summary>
            /// <param name="path"></param>
            /// <param name="data"></param>
            /// <returns></returns>
            public static bool WriteFile(string path, byte[] data)
            {
                return instance.writeFile(path, data, out _);
            }
        }
        /// <summary>
        /// QCloud设置
        /// </summary>
        public class QCloud
        {
            private static QCloud instance = new QCloud();
            private String bucket = null;
            private String cosdomain = null;
            private String cosaccesskeyid = null;
            private String cosaccesskeySecret = null;
            /// <summary>
            /// 
            /// </summary>
            public QCloud()
            {
                bucket = Config.GetSetting("CosBucket");
                cosdomain = Config.GetSetting("CosDomain");
                cosaccesskeyid = Config.GetSetting("CosAccessKeyId");
                cosaccesskeySecret = Config.GetSetting("CosAccessKeySecret");
            }
            /// <summary>
            /// 
            /// </summary>
            public QCloud(String bucket, String domain, String accesskeyid, String accesskeySecret)
            {
                this.bucket = bucket;
                cosdomain = domain;
                cosaccesskeyid = accesskeyid;
                cosaccesskeySecret = accesskeySecret;
            }

            /// <summary>
            /// 是否可用
            /// </summary>
            public Boolean canUsing
            {
                get
                {
                    if (string.IsNullOrEmpty(bucket) || string.IsNullOrEmpty(cosaccesskeyid) || string.IsNullOrEmpty(cosaccesskeyid) || string.IsNullOrEmpty(cosaccesskeySecret))
                    {
                        return false;
                    }
                    else
                    {
                        return true;
                    }
                }
            }
            /// <summary>
            /// 执行一个新任务
            /// </summary>
            /// <param name="method"></param>
            /// <param name="path"></param>
            /// <param name="postData"></param>
            /// <param name="headers"></param>
            /// <param name="message"></param>
            /// <returns></returns>
            public HttpResponseMessage newWorker(string method, string path, byte[] postData, Hashtable headers, out string message)
            {
                using (var client = new HttpClient())
                {
                    var host = cosdomain.IndexOf("://") < 0 ? "https://" + cosdomain : cosdomain;
                    var request = new HttpRequestMessage(new HttpMethod(method), host + path);
                    if (postData != null)
                    {
                        request.Content = new StreamContent(new MemoryStream(postData));
                    }
                    if (headers == null)
                    {
                        headers = new Hashtable();
                    }
                    var date = DateTools.ConvertToGMT();
                    var contentType = Wlniao.MimeMapping.GetMimeMapping(path);
                    headers.Add("Date", date);
                    headers.Add("Host", cosdomain);
                    request.Content.Headers.ContentMD5 = MD5.Create().ComputeHash(postData);
                    request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
                    request.Content.Headers.ContentLength = postData.Length;

                    var kvList = new List<KeyValuePair<String, String>>();
                    if (path.IndexOf('?') > 0)
                    {
                        var param = path.Substring(path.IndexOf('?') + 1);
                        foreach (var kvs in param.SplitBy("&"))
                        {
                            var kv = kvs.SplitBy("=");
                            if (kv.Length == 1)
                            {
                                kvList.Add(new KeyValuePair<string, string>(strUtil.UrlEncodeSymbol(kv[0].ToLower()), ""));
                            }
                            else if (kv.Length == 2)
                            {
                                kvList.Add(new KeyValuePair<string, string>(strUtil.UrlEncodeSymbol(kv[0].ToLower()), strUtil.UrlEncodeSymbol(kv[1])));
                            }
                        }
                    }


                    var hdList = new List<KeyValuePair<String, String>>();
                    foreach (DictionaryEntry kv in headers)
                    {
                        hdList.Add(new KeyValuePair<string, string>(strUtil.UrlEncodeSymbol(kv.Key.ToString().ToLower()), strUtil.UrlEncodeSymbol(kv.Value.ToString())));
                    }


                    var urlParamList = "";
                    var httpParameters = "";
                    foreach (var kv in kvList.OrderBy(o => o.Key).ToList())
                    {
                        if (string.IsNullOrEmpty(urlParamList))
                        {
                            urlParamList = kv.Key;
                            httpParameters = kv.Key + "=" + kv.Value;
                        }
                        else
                        {
                            urlParamList += ";" + kv.Key;
                            httpParameters += "&" + kv.Key + "=" + kv.Value;
                        }
                    }

                    var headerList = "";
                    var httpHeaders = "";
                    foreach (var kv in hdList.OrderBy(o => o.Key).ToList())
                    {
                        if (string.IsNullOrEmpty(headerList))
                        {
                            headerList = kv.Key;
                            httpHeaders = kv.Key + "=" + kv.Value;
                        }
                        else
                        {
                            headerList += ";" + kv.Key;
                            httpHeaders += "&" + kv.Key + "=" + kv.Value;
                        }
                    }


                    var keytime = (XCore.NowUnix - 10) + ";" + (XCore.NowUnix + 3590);
                    var signKey = Encryptor.HmacSHA1(keytime, cosaccesskeySecret);
                    var contentMd5 = System.Convert.ToBase64String(request.Content.Headers.ContentMD5);
                    var httpString = method.ToLower() + '\n' + path + '\n' + httpParameters + '\n' + httpHeaders + '\n';
                    var stringToSign = "sha1\n" + keytime + '\n' + Encryptor.Sha1(httpString).ToLower() + '\n';
                    var signature = Encryptor.HmacSHA1(stringToSign, signKey);
                    var authorization = "q-sign-algorithm=sha1"
                        + "&q-ak=" + cosaccesskeyid
                        + "&q-sign-time=" + keytime
                        + "&q-key-time=" + keytime
                        + "&q-header-list=" + headerList
                        + "&q-url-param-list=" + urlParamList
                        + "&q-signature=" + signature;

                    headers.Add("Authorization", authorization);
                    foreach (DictionaryEntry kv in headers)
                    {
                        request.Headers.TryAddWithoutValidation(kv.Key.ToString(), kv.Value.ToString());
                    }
                    var msg = "";
                    var res = new HttpResponseMessage() { StatusCode = System.Net.HttpStatusCode.InternalServerError };
                    client.SendAsync(request).ContinueWith((requestTask) =>
                    {
                        res = requestTask.Result;
                        if (res.StatusCode != System.Net.HttpStatusCode.OK)
                        {
                            res.Content.ReadAsStringAsync().ContinueWith((readTask) =>
                            {
                                if (!string.IsNullOrEmpty(readTask.Result))
                                {
                                    var doc = new System.Xml.XmlDocument();
                                    doc.LoadXml(readTask.Result);
                                    msg = doc.GetElementsByTagName("Message")[0].InnerText;
                                    Log.Loger.Debug("XStorage QCloud：" + msg);
                                }
                            }).Wait();
                        }
                    }).Wait();
                    message = msg;
                    return res;
                }
            }
            /// <summary>
            /// FormAPI参数
            /// </summary>
            /// <param name="expire">过期时间（单位：秒）</param>
            /// <param name="max">文件最大大小</param>
            /// <returns></returns>
            public String formApi(int expire = 5400, int max = 200)
            {
                return formApi(null, expire, max);
            }
            /// <summary>
            /// FormAPI参数
            /// </summary>
            /// <param name="expire">过期时间（单位：秒）</param>
            /// <param name="max">文件最大大小（单位：M）</param>
            /// <param name="dir">上传目录</param>
            /// <returns></returns>
            public String formApi(string dir, int expire = 5400, int max = 200)
            {
                if (canUsing)
                {
                    max = max * 1024 * 1024;
                    if (string.IsNullOrEmpty(dir))
                    {
                        dir = DateTools.Format("yyyyMM/MMdd/");
                    }
                    else
                    {
                        if (dir.StartsWith("/"))
                        {
                            dir = dir.TrimStart('/');
                        }
                        if (!dir.EndsWith("/"))
                        {
                            dir = dir + "/";
                        }
                    }
                    var keytime = XCore.NowUnix + ";" + (XCore.NowUnix + expire);
                    var json = "{\"expiration\":\"" + DateTime.UtcNow.AddSeconds(expire).ToString("yyyy-MM-ddTHH:mm:ssZ") + "\",\"conditions\":[[\"content-length-range\", 0, " + max + "],[\"starts-with\",\"$key\",\"" + dir + "\"],{\"q-sign-algorithm\":\"sha1\"},{\"q-ak\":\"" + cosaccesskeyid + "\"},{\"q-sign-time\":\"" + keytime + "\"}]}";
                    var policy = Encryptor.Base64Encrypt(json);
                    var stringToSign = Encryptor.Sha1(json).ToLower();
                    var signKey = Encryptor.HmacSHA1(keytime, cosaccesskeySecret);
                    var signature = Encryptor.HmacSHA1(stringToSign, signKey);
                    var host = cosdomain.IndexOf("://") < 0 ? "//" + cosdomain : cosdomain;
                    return Json.ToString(new { to = "cos", host = string.IsNullOrEmpty(XStorageUrl) ? host : XStorageUrl, domain = host, keytime = keytime, secretid = cosaccesskeyid, dir, max, policy, signature });
                }
                return "";
            }


            /// <summary>
            /// 上传文件
            /// </summary>
            /// <param name="path"></param>
            /// <param name="data"></param>
            /// <param name="message"></param>
            /// <returns></returns>
            public Boolean writeFile(string path, byte[] data, out string message)
            {
                message = "";
                if (canUsing)
                {
                    try
                    {
                        var resp = newWorker("PUT", path, data, null, out message);
                        if (resp.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            return true;
                        }
                    }
                    catch (Exception ex) { message = "上传异常：" + ex.Message; }
                }
                else
                {
                    message = "XStorage上传配置无效";
                }
                return false;
            }

            /// <summary>
            /// FormAPI参数
            /// </summary>
            /// <param name="expire">过期时间（单位：秒）</param>
            /// <param name="max">文件最大大小</param>
            /// <returns></returns>
            public static String FormApi(int expire = 5400, int max = 200)
            {
                return instance.formApi(null, expire, max);
            }
            /// <summary>
            /// FormAPI参数
            /// </summary>
            /// <param name="expire">过期时间（单位：秒）</param>
            /// <param name="max">文件最大大小（单位：M）</param>
            /// <param name="dir">上传目录</param>
            /// <returns></returns>
            public static String FormApi(string dir, int expire = 5400, int max = 200)
            {
                return instance.formApi(dir, expire, max);
            }
            /// <summary>
            /// 上传文件
            /// </summary>
            /// <param name="path"></param>
            /// <param name="data"></param>
            /// <returns></returns>
            public static bool WriteFile(string path, byte[] data)
            {
                return instance.writeFile(path, data, out _);
            }
        }
    }
}