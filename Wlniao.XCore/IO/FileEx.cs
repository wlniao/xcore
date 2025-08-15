/*==============================================================================
    文件名称：FileEx.cs
    适用环境：CoreCLR 5.0,.NET Framework 2.0/4.0/5.0
    功能描述：文件常用操作方法
================================================================================
 
    Copyright 2015 XieChaoyi

    Licensed under the Apache License, Version 2.0 (the "License");
    you may not use this file except in compliance with the License.
    You may obtain a copy of the License at

               http://www.apache.org/licenses/LICENSE-2.0

    Unless required by applicable law or agreed to in writing, software
    distributed under the License is distributed on an "AS IS" BASIS,
    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
    See the License for the specific language governing permissions and
    limitations under the License.

===============================================================================*/
using System;
using System.IO;
using System.Text;
using Wlniao.Text;
using Encoding = System.Text.Encoding;

namespace Wlniao.IO
{
    /// <summary>
    /// 文件常用操作方法
    /// </summary>
    public class FileEx
    {
        /// <summary>
        /// 获取文件编码格式
        /// </summary>
        /// <param name="absolutePath">文件的绝对路径</param>
        /// <returns>文件的内容</returns>
        public static System.Text.Encoding GetEncoding(string absolutePath)
        {
            var br = new System.IO.BinaryReader(new System.IO.FileStream(absolutePath.Replace(".html", ".md"), System.IO.FileMode.Open, System.IO.FileAccess.Read));
            var buffer = br.ReadBytes(2);
            var encoding = IO.IdentifyEncoding.GetEncodingName(buffer);
            if (!string.IsNullOrEmpty(encoding))
            {
                Encoding.GetEncoding(encoding);
            }
            return System.Text.Encoding.ASCII;
        }
        /// <summary>
        /// 读取文件的内容(采用UTF8编码)
        /// </summary>
        /// <param name="absolutePath">文件的绝对路径</param>
        /// <returns>文件的内容</returns>
        public static byte[] ReadByte(string absolutePath)
        {
            using (var fs = new FileStream(absolutePath, FileMode.Open))
            {
                var buffer = new byte[fs.Length];
                fs.Read(buffer, 0, buffer.Length);
                fs.Flush();
                return buffer;
            }
        }
        /// <summary>
        /// 读取文件的内容(采用UTF8编码)
        /// </summary>
        /// <param name="absolutePath">文件的绝对路径</param>
        /// <param name="buffer"></param>
        /// <returns>文件的内容</returns>
        public static void WriteByte(string absolutePath, byte[] buffer)
        {
            using (var sw = new StreamWriter(new FileStream(absolutePath, FileMode.Create)))
            {
                sw.Write(buffer);
                sw.Flush();
            }
        }
        /// <summary>
        /// 读取文件的内容(采用UTF8编码)
        /// </summary>
        /// <param name="absolutePath">文件的绝对路径</param>
        /// <param name="stream"></param>
        /// <returns>文件的内容</returns>
        public static void WriteStream(string absolutePath, System.IO.Stream stream)
        {
            using (var sw = new StreamWriter(new FileStream(absolutePath, FileMode.Create)))
            {
                var bytes = new byte[stream.Length];
                stream.Read(bytes, 0, bytes.Length);
                // 设置当前流的位置为流的开始 
                stream.Seek(0, System.IO.SeekOrigin.Begin);
                sw.Write(bytes);
                sw.Flush();
            }
        }
        /// <summary>
        /// 读取文件的内容(采用UTF8编码)
        /// </summary>
        /// <param name="absolutePath">文件的绝对路径</param>
        /// <returns>文件的内容</returns>
        public static string Read(string absolutePath)
        {
            return Read(absolutePath, Encoding.UTF8);
        }
        /// <summary>
        /// 以某种编码方式，读取文件的内容
        /// </summary>
        /// <param name="absolutePath">文件的绝对路径</param>
        /// <param name="encoding">编码方式</param>
        /// <returns>文件的内容</returns>
        public static string Read(string absolutePath, System.Text.Encoding encoding)
        {
            using (var fs = new FileStream(absolutePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                if (fs.CanRead)
                {
                    var bytes = new byte[(int)fs.Length];
                    var r = fs.Read(bytes, 0, bytes.Length);
                    if (encoding == System.Text.Encoding.UTF8 && bytes.Length > 3)
                    {
                        var bomBuffer = new byte[] { 0xef, 0xbb, 0xbf };
                        if (bytes[0] == bomBuffer[0]
                            && bytes[1] == bomBuffer[1]
                            && bytes[2] == bomBuffer[2])
                        {
                            return new UTF8Encoding(false).GetString(bytes, 3, bytes.Length - 3);
                        }
                    }
                    return encoding.GetString(bytes, 0, bytes.Length);
                }
            }
            return "";
        }
        /// <summary>
        /// 以UTF-8编码读取文件为字符串（其它编码自动转换）
        /// </summary>
        /// <param name="absolutePath"></param>
        /// <returns></returns>
        public static string ReadUTF8String(string absolutePath)
        {
            return StringUtil.GetUTF8String(ReadByte(absolutePath));
        }
        /// <summary>
        /// 读取文件各行内容(采用UTF8编码)，以数组形式返回
        /// </summary>
        /// <param name="absolutePath">文件的绝对路径</param>
        /// <returns>文件各行内容</returns>
        public static string[] ReadAllLines(string absolutePath)
        {
            return ReadAllLines(absolutePath, Encoding.UTF8);
        }
        /// <summary>
        /// 以某种编码方式，读取文件各行内容(采用UTF8编码)，以数组形式返回
        /// </summary>
        /// <param name="absolutePath">文件的绝对路径</param>
        /// <param name="encoding">编码方式</param>
        /// <returns>文件各行内容</returns>
        public static string[] ReadAllLines(string absolutePath, System.Text.Encoding encoding)
        {
            var list = new System.Collections.Generic.List<string>();
            using (var fs = new FileStream(absolutePath, FileMode.Open))
            {
                var reader = new StreamReader(fs, Encoding.UTF8);
                string str;
                while ((str = reader.ReadLine()) != null)
                {
                    list.Add(str);
                }
            }
            return list.ToArray();
        }
        /// <summary>
        /// 将字符串写入某个文件中(采用UTF8编码)
        /// </summary>
        /// <param name="absolutePath">文件的绝对路径</param>
        /// <param name="fileContent">需要写入文件的字符串</param>
        public static void Write(string absolutePath, string fileContent)
        {
            Write(absolutePath, fileContent, true);
        }
        /// <summary>
        /// 将字符串写入某个文件中(采用UTF8编码)
        /// </summary>
        /// <param name="absolutePath">文件的绝对路径</param>
        /// <param name="fileContent">需要写入文件的字符串</param>
        /// <param name="autoCreateDir">是否自动创建目录</param>
        public static void Write(string absolutePath, string fileContent, bool autoCreateDir)
        {
            if (!System.IO.Directory.Exists(System.IO.Path.GetDirectoryName(absolutePath)))
            {
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(absolutePath));
            }
            Write(absolutePath, fileContent, Encoding.UTF8);
        }
        /// <summary>
        /// 将字符串写入某个文件中(需要指定文件编码方式)
        /// </summary>
        /// <param name="absolutePath">文件的绝对路径</param>
        /// <param name="fileContent">需要写入文件的字符串</param>
        /// <param name="encoding">编码方式</param>
        public static void Write(string absolutePath, string fileContent, System.Text.Encoding encoding)
        {
            if (!System.IO.Directory.Exists(System.IO.Path.GetDirectoryName(absolutePath)))
            {
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(absolutePath));
            }
            using (var fs = new FileStream(absolutePath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite))
            {
                var writer = new StreamWriter(fs, encoding);
                writer.Write(fileContent);
                writer.Flush();
            }
        }
        /// <summary>
        /// 删除文件
        /// </summary>
        /// <param name="absolutePath">文件的绝对路径</param>
        public static void Delete(string absolutePath)
        {
            System.IO.File.Delete(absolutePath);
        }
        /// <summary>
        /// 判断文件是否存在
        /// </summary>
        /// <param name="absolutePath">文件的绝对路径</param>
        /// <returns></returns>
        public static bool Exists(string absolutePath)
        {
            if (!string.IsNullOrEmpty(absolutePath) && System.IO.Directory.Exists(System.IO.Path.GetDirectoryName(absolutePath)))
            {
                return System.IO.File.Exists(absolutePath);
            }
            return false;
        }
        /// <summary>
        /// 移动文件
        /// </summary>
        /// <param name="sourceFileName">原来的路径</param>
        /// <param name="destFileName">需要挪到的新路径</param>
        public static void Move(string sourceFileName, string destFileName)
        {
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(destFileName));
            System.IO.File.Move(sourceFileName, destFileName);
        }
        /// <summary>
        /// 拷贝文件(如果目标存在，不覆盖)
        /// </summary>
        /// <param name="sourceFileName">原来的路径</param>
        /// <param name="destFileName">需要挪到的新路径</param>
        public static void Copy(string sourceFileName, string destFileName)
        {
            System.IO.File.Copy(sourceFileName, destFileName, false);
        }
        /// <summary>
        /// 拷贝文件
        /// </summary>
        /// <param name="sourceFileName">原来的路径</param>
        /// <param name="destFileName">需要挪到的新路径</param>
        /// <param name="overwrite">如果目标存在，是否覆盖</param>
        public static void Copy(string sourceFileName, string destFileName, bool overwrite)
        {
            System.IO.File.Copy(sourceFileName, destFileName, overwrite);
        }
        /// <summary>
        /// 将内容追加到文件中(采用UTF8编码)
        /// </summary>
        /// <param name="absolutePath">文件的绝对路径</param>
        /// <param name="fileContent">需要追加的内容</param>
        public static void Append(string absolutePath, string fileContent)
        {
            Append(absolutePath, fileContent, Encoding.UTF8);
        }
        /// <summary>
        /// 将内容追加到文件中
        /// </summary>
        /// <param name="absolutePath">文件的绝对路径</param>
        /// <param name="fileContent">需要追加的内容</param>
        /// <param name="encoding">编码方式</param>
        public static void Append(string absolutePath, string fileContent, System.Text.Encoding encoding)
        {
            using (var fs = new FileStream(absolutePath, FileMode.Append))
            {
                var writer = new StreamWriter(fs, encoding);
                writer.Write(fileContent);
                writer.Flush();
            }
        }
        /// <summary>
        /// 使用ZIP格式压缩文件（未实现）
        /// </summary>
        /// <param name="sourceFileName">压缩的文件名</param>
        public static void Zip(string sourceFileName)
        {
            Zip(sourceFileName, sourceFileName + ".zip");
        }
        /// <summary>
        /// 使用ZIP格式压缩文件（未实现）
        /// </summary>
        /// <param name="sourceFileName">压缩的文件名</param>
        /// <param name="destFileName">压缩后输出的文件名</param>
        public static void Zip(string sourceFileName, string destFileName)
        {
            throw new Exception("Zip 方法未实现");
        }
        /// <summary>
        /// 解包ZIP压缩文件（未实现）
        /// </summary>
        /// <param name="sourceFileName">需要解包的ZIP文件名</param>
        public static void UnZip(string sourceFileName)
        {
            throw new Exception("UnZip 方法未实现");
        }
        /// <summary>
        /// 解包ZIP压缩文件（未实现）
        /// </summary>
        /// <param name="sourceFileName">需要解包的ZIP文件名</param>
        /// <param name="destFilePath">接包后的文件输出路径</param>
        public static void UnZip(string sourceFileName, string destFilePath)
        {
            throw new Exception("UnZip 方法未实现");
        }
    }
}