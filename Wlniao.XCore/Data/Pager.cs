/*==============================================================================
    文件名称：Pager.cs
    适用环境：CoreCLR 5.0,.NET Framework 2.0/4.0/5.0
    功能描述：数据分页工具
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
using System.Collections.Generic;
using System.Linq;
namespace Wlniao.Data
{
    /// <summary>
    /// 数据分页工具
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Pager<T>
    {
        private List<T> _data = null;
        private List<T> _list = null;
        private int _page = 1;
        private int _size = 10;
        private int _total = -1;
        private string _message = null;

        /// <summary>
        /// 跳过的记录数量
        /// </summary>
        public int skip
        {
            get
            {
                return (page - 1) * size;
            }
        }
        /// <summary>
        /// 当前页码（将在下一个大版本中取消，已改名为page）
        /// </summary>
        public int index
        {
            get
            {
                return _page;
            }
        }
        /// <summary>
        /// 当前页码（从1开始）
        /// </summary>
        public int page
        {
            get
            {
                return _page;
            }
            set
            {
                if (value > 0)
                {
                    _page = value;
                    _list = null;
                }
            }
        }
        /// <summary>
        /// 分页大小
        /// </summary>
        public int size
        {
            get
            {
                return _size > 0 ? _size : 10;
            }
            set
            {
                if (value != _size)
                {
                    _size = value;
                    _list = null;
                }
            }
        }
        /// <summary>
        /// 结果总数
        /// </summary>
        public int total
        {
            get
            {
                if (_total < 0)
                {
                    if (_data == null)
                    {
                        return 0;
                    }
                    _total = _data.Count;
                }
                return _total;
            }
            set
            {
                _total = value;
            }
        }
        /// <summary>
        /// 结果集
        /// </summary>
        public List<T> rows
        {
            get
            {
                if (_list == null)
                {
                    if (_data == null)
                    {
                        _list = new List<T>();
                    }
                    else
                    {
                        //分页
                        if (_data.Count <= size)
                        {
                            _list = _data;
                            _page = 1;
                        }
                        else if (_data.Count > (size * (_page - 1)))
                        {
                            if (_data.Count > size * _page)
                            {
                                _list = _data.Skip((_page - 1) * size).Take(size).ToList();
                            }
                            else
                            {
                                _list = _data.Skip((_page - 1) * size).ToList();
                            }
                        }
                        else
                        {
                            return new List<T>();
                        }
                    }
                }
                return _list;
            }
            set
            {
                _list = value;
            }
        }
        /// <summary>
        /// 消息输出
        /// </summary>
        public string message
        {
            get
            {
                if (string.IsNullOrEmpty(_message))
                {
                    if (total > 0)
                    {
                        return string.Format(lang.Get("", "findtotal", "{0} records found"), total);
                    }
                    else
                    {
                        return lang.Get("", "empty", "without data");
                    }
                }
                return _message;
            }
            set
            {
                _message = value;
            }
        }
        /// <summary>
        /// 将原始数据进行分页
        /// </summary>
        /// <param name="source"></param>
        /// <param name="page"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public static Pager<T> PutIn(List<T> source, Int32 page = 1, Int32 size = 10)
        {
            var pager = new Pager<T>();
            if (source == null)
            {
                pager._data = new List<T>();
            }
            else
            {
                pager._data = source;
                pager.size = size;
                pager.page = page;
            }
            return pager;
        }
        /// <summary>
        /// 将数据转为分页格式
        /// </summary>
        /// <param name="Data"></param>
        /// <param name="Total"></param>
        /// <param name="Page"></param>
        /// <param name="Size"></param>
        /// <returns></returns>
        public static Pager<T> Format(List<T> Data, Int32 Total, Int32 Page, Int32 Size = 0)
        {
            var pager = new Pager<T>();
            pager.page = Page < 1 ? 1 : Page;
            pager.size = Size < Data.Count ? Data.Count : Size;
            pager.total = Total < Data.Count ? Data.Count : Total;
            pager.rows = Data;
            return pager;
        }
    }
}
