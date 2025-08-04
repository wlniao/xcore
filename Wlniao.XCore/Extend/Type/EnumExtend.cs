/*==============================================================================
    文件名称：IEnumerableExtend.cs
    适用环境：CoreCLR 5.0,.NET Framework 2.0/4.0/5.0
    功能描述：IEnumerable扩展
================================================================================
 
    Copyright 2014 XieChaoyi

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
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Wlniao
{
	/// <summary>
	/// 枚举类型功能扩展类
	/// </summary>
	public static class EnumExtend
	{
		/// <summary>
		/// 获得枚举的Description
		/// </summary>
		/// <param name="value">枚举值</param>
		/// <param name="nameInstead">当枚举值没有定义DescriptionAttribute，是否使用枚举名代替，默认是使用</param>
		/// <returns>枚举的Description</returns>
		public static string GetDescription(this Enum value, bool nameInstead = true)
		{
			Type type = value.GetType();
			string name = Enum.GetName(type, value);
			if (name == null)
			{
				return null;
			}

			FieldInfo field = type.GetField(name);
			DescriptionAttribute attribute = System.Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute)) as DescriptionAttribute;

			if (attribute == null && nameInstead == true)
			{
				return name;
			}
			return attribute?.Description;
		}
	}
}