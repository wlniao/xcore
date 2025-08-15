/*==============================================================================
    文件名称：Reflection.cs
    适用环境：CoreCLR 5.0,.NET Framework 2.0/4.0/5.0
    功能描述：封装了反射的常用操作方法
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
using System.Collections;
using System.Reflection;
using System.Collections.Generic;
using Wlniao.Text;

namespace Wlniao.Runtime
{
    /// <summary>
    /// 封装了反射的常用操作方法
    /// </summary>
    public class Reflection
    {
        /// <summary>
        /// 为类型创建对象(直接指定类型的完全限定名称)
        /// </summary>
        /// <param name="typeFullName"></param>
        /// <returns></returns>
        public static object GetInstance(string typeFullName)
        {
            if (typeFullName.IndexOf(',') <= 0)
            {
                var tp = typeof(Reflection);
                var tmp = tp.AssemblyQualifiedName;
                typeFullName = typeFullName + tmp.Substring(tmp.IndexOf(','));
            }
            return GetInstance(Type.GetType(typeFullName));
        }
        /// <summary>
        /// 通过反射创建对象(Activator.CreateInstance)，并提供构造函数
        /// </summary>
        /// <param name="t"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public static object GetInstance(Type t, params object[] args)
        {
            return Activator.CreateInstance(t, args);
        }
        /// <summary>
        /// 为类型创建对象(通过加载指定程序集中的类型)
        /// </summary>
        /// <param name="asmName">不需要后缀名</param>
        /// <param name="typeName"></param>
        /// <returns></returns>
        public static object GetInstance(string asmName, string typeName)
        {
            // Load不需要ext，LoadFrom需要
            var asm = Assembly.Load(new AssemblyName(asmName));
            var type = asm.GetType(typeName, false, false);
            return GetInstance(type);
        }
        /// <summary>
        /// 为类型创建对象(通过加载指定程序集中的类型)
        /// </summary>
        /// <param name="asm">程序集</param>
        /// <param name="typeName"></param>
        /// <returns></returns>
        public static object GetInstance(Assembly asm, string typeName)
        {
            var type = asm.GetType(typeName, false, false);
            return GetInstance(type);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public static PropertyInfo[] GetPropertyList(Type t)
        {
            var list = new List<PropertyInfo>();
            var arrp = t.GetRuntimeProperties();
            var _em = arrp.GetEnumerator();
            while (_em.MoveNext())
            {
                list.Add(_em.Current);
            }
            return list.ToArray();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="currentObject"></param>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        public static object GetPropertyValue(object currentObject, string propertyName)
        {
            if (currentObject == null) return null;
            if (StringUtil.IsNullOrEmpty(propertyName)) return null;
            var p = currentObject.GetType().GetRuntimeProperty(propertyName);
            if (p == null) return null;
            return p.GetValue(currentObject, null);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="currentObject"></param>
        /// <param name="propertyName"></param>
        /// <param name="propertyValue"></param>
        public static void SetPropertyValue(object currentObject, string propertyName, object propertyValue)
        {
            if (currentObject == null)
            {
                throw new NullReferenceException(string.Format("propertyName={0}, propertyValue={1}", propertyName, propertyValue));
            }
            var p = currentObject.GetType().GetRuntimeProperty(propertyName);
            if (p != null)
            {
                try
                {
                    var pt = p.PropertyType;
                    if (IsBaseType(pt))
                    {
                        propertyValue = System.Convert.ChangeType(propertyValue, p.PropertyType);
                        p.SetValue(currentObject, propertyValue, null);
                    }
                    else if (IsInterface(pt, typeof(IList)))
                    {
                        var list = (List<object>)propertyValue;
                        if (list.Count > 0)
                        {
                            if (list[0] is string)
                            {
                                var _list = new List<string>();
                                foreach (var item in list)
                                {
                                    _list.Add(item as string);
                                }
                                p.SetValue(currentObject, _list, null);
                            }
                            else if (list[0] is int)
                            {
                                var _list = new List<int>();
                                foreach (var item in list)
                                {
                                    _list.Add((int)item);
                                }
                                p.SetValue(currentObject, _list, null);
                            }
                            else if (list[0] is long)
                            {
                                var _list = new List<long>();
                                foreach (var item in list)
                                {
                                    _list.Add((long)item);
                                }
                                p.SetValue(currentObject, _list, null);
                            }
                            else if (list[0] is DateTime)
                            {
                                var _list = new List<DateTime>();
                                foreach (var item in list)
                                {
                                    _list.Add((DateTime)item);
                                }
                                p.SetValue(currentObject, _list, null);
                            }
                            else
                            {
                                //取得List对象包含项的实际类型（可用）
                                var type = p.PropertyType.GenericTypeArguments[0];
                                //根据Type对象反射创建List<T>
                                var typeOfList = typeof(List<>).MakeGenericType(type);
                                var listTrue = Activator.CreateInstance(typeOfList);
                                foreach(var obj in list)
                                {
                                    var item = Reflection.GetInstance(type);
                                    var dic = (Dictionary<string, object>)obj;
                                    var em = dic.GetEnumerator();
                                    while (em.MoveNext())
                                    {
                                        SetPropertyValue(item, em.Current.Key, em.Current.Value);
                                    }
                                    typeOfList.GetMethod("Add").Invoke(listTrue, new object[] { item });
                                }
                                p.SetValue(currentObject, listTrue);
                            }
                        }
                    }
                    else
                    {
                        //propertyValue = System.Convert.ChangeType(propertyValue, p.PropertyType);
                        p.SetValue(currentObject, propertyValue, null);
                    }
                }
                catch { }
            }
        }
        /// <summary>
        /// 获取属性的类型的fullName(对泛型名称做了特殊处理)
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        public static string getPropertyTypeName(PropertyInfo p)
        {
            if (p.PropertyType.IsConstructedGenericType == false)
                return p.PropertyType.FullName;
            var pGenericType = p.PropertyType.GetGenericTypeDefinition();
            var genericTypeName = pGenericType.FullName.Split('`')[0];            
            var ts = p.PropertyType.GenericTypeArguments;
            string args = null;
            foreach (var at in ts)
            {
                if (args != null) args += ", ";
                args += at.FullName;
            }
            return genericTypeName + "<" + args + ">";
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="methodName"></param>
        /// <returns></returns>
        public static object CallMethod(object obj, string methodName)
        {
            return CallMethod(obj, methodName, null);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="currentType"></param>
        /// <param name="methodName"></param>
        /// <returns></returns>
        public static object CallMethod(Type currentType, string methodName)
        {
            return CallMethod(currentType, methodName, null);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="methodName"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public static object CallMethod(object obj, string methodName, object[] args)
        {
            //var method = obj.GetType().GetRuntimeMethod(methodName,new Type[] { obj.GetType() });
            //return method.Invoke(obj, args);
            var method = obj.GetType().GetMethod(methodName, BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
            return method.Invoke(obj, args);
            //return obj.GetType().InvokeMember(methodName, BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance, null, obj, args);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="currentType"></param>
        /// <param name="methodName"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public static object CallMethod(Type currentType, string methodName, object[] args)
        {
            return CallMethod(GetInstance(currentType), methodName, args);
        }
        /// <summary>
        /// 获取 public 实例方法，不包括继承的方法
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public static MethodInfo[] GetMethods(Type t)
        {
            var methods = new List<MethodInfo>();
            var arr= t.GetRuntimeMethods();
            var em = arr.GetEnumerator();
            while (em.MoveNext())
            {
                methods.Add(em.Current);
            }
            return methods.ToArray();
        }
        /// <summary>
        /// 获取 public 实例方法
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public static MethodInfo[] GetMethodsWithInheritance(Type t)
        {
            var methods = new List<MethodInfo>();
            var arr = t.GetRuntimeMethods();
            var em = arr.GetEnumerator();
            while (em.MoveNext())
            {
                if (em.Current.IsPublic)
                {
                    methods.Add(em.Current);
                }
            }
            return methods.ToArray();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="t"></param>
        /// <param name="attributeType"></param>
        /// <returns></returns>
        public static Attribute GetAttribute(Type t, Type attributeType)
        {
            var members = t.GetTypeInfo().GetMembers();
            foreach (var member in members)
            {
                var iCustomAttributes = member.GetCustomAttributes(attributeType, false);
                var customAttributes = iCustomAttributes.GetEnumerator();
                while (customAttributes.MoveNext())
                {
                    return (Attribute)customAttributes.Current;
                }
            }
            return null;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="memberInfo"></param>
        /// <param name="attributeType"></param>
        /// <returns></returns>
        public static Attribute GetAttribute(MemberInfo memberInfo, Type attributeType)
        {
            var iCustomAttributes = memberInfo.GetCustomAttributes(attributeType, false);
            var customAttributes = iCustomAttributes.GetEnumerator();
            while (customAttributes.MoveNext())
            {
                return (Attribute)customAttributes.Current;
            }
            return null;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="memberInfo"></param>
        /// <returns></returns>
        public static object[] GetAttributes(MemberInfo memberInfo)
        {
            var attributes = new List<object>();
            var iCustomAttributes = memberInfo.GetCustomAttributes(false);
            var customAttributes = iCustomAttributes.GetEnumerator();
            while (customAttributes.MoveNext())
            {
                attributes.Add(customAttributes.Current);
            }
            return attributes.ToArray();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="memberInfo"></param>
        /// <param name="attributeType"></param>
        /// <returns></returns>
        public static object[] GetAttributes(MemberInfo memberInfo, Type attributeType)
        {
            var attributes = new List<object>();
            var iCustomAttributes = memberInfo.GetCustomAttributes(attributeType, false);
            var customAttributes = iCustomAttributes.GetEnumerator();
            while (customAttributes.MoveNext())
            {
                attributes.Add(customAttributes.Current);
            }
            return attributes.ToArray();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool IsBaseType(Type type)
        {
            return type == typeof(int) ||
                type == typeof(long) ||
                type == typeof(short) ||
                type == typeof(string) ||
                type == typeof(DateTime) ||
                type == typeof(bool) ||
                type == typeof(double) ||
                type == typeof(float) ||
                type == typeof(object) ||
                type == typeof(decimal);
        }
        /// <summary>
        /// 判断 t 是否实现了某种接口
        /// </summary>
        /// <param name="t">需要判断的类型</param>
        /// <param name="interfaceType">是否实现的接口</param>
        /// <returns></returns>
        public static bool IsInterface(Type t, Type interfaceType)
        {
            var interfaces = t.GetTypeInfo().GetInterfaces();
            foreach (var type in interfaces)
            {
                if (interfaceType.FullName.Equals(type.FullName)) return true;
            }
            return false;
        }
    }
}