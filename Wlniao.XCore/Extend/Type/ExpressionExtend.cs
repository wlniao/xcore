using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Wlniao
{
    /// <summary>
    /// 谓词表达式构建器
    /// </summary>
    public static class ExpressionExtend
    {
        /// <summary>
        /// 创建一个值恒为 <c>true</c> 的表达式。
        /// </summary>
        /// <typeparam name="T">表达式方法类型</typeparam>
        /// <returns>一个值恒为 <c>true</c> 的表达式。</returns>
        public static Expression<Func<T, bool>> True<T>() { return a => true; }

        /// <summary>
        /// 创建一个值恒为 <c>false</c> 的表达式。
        /// </summary>
        /// <typeparam name="T">表达式方法类型</typeparam>
        /// <returns>一个值恒为 <c>false</c> 的表达式。</returns>
        public static Expression<Func<T, bool>> False<T>() { return af => false; }

        /// <summary>
        /// 双元 Or 表达式
        /// </summary>
        /// <typeparam name="T">指定泛型 T</typeparam>
        /// <param name="exprleft">左表达式</param>
        /// <param name="exprright">右表达式</param>
        /// <returns>返回合并表达式</returns>
        public static Expression<Func<T, bool>> Or<T>(this Expression<Func<T, bool>> exprleft, Expression<Func<T, bool>> exprright)
        {
            var invokedExpr = Expression.Invoke(exprright, exprleft.Parameters.Cast<Expression>());
            return Expression.Lambda<Func<T, bool>>(Expression.Or(exprleft.Body, invokedExpr), exprleft.Parameters);
        }

        /// <summary>
        /// 双元 And 表达式
        /// </summary>
        /// <typeparam name="T">指定泛型 T</typeparam>
        /// <param name="exprleft">左表达式</param>
        /// <param name="exprright">右表达式</param>
        /// <returns>返回合并表达式</returns>
        public static Expression<Func<T, bool>> And<T>(this Expression<Func<T, bool>> exprleft, Expression<Func<T, bool>> exprright)
        {
            var invokedExpr = Expression.Invoke(exprright, exprleft.Parameters.Cast<Expression>());
            return Expression.Lambda<Func<T, bool>>(Expression.AndAlso(exprleft.Body, invokedExpr), exprleft.Parameters);
        }



        /// <summary>
        /// 单一条件排序
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="query"></param>
        /// <param name="order"></param>
        /// <param name="sortby"></param>
        /// <returns></returns>
        public static IQueryable<T> QueryableOrder<T>(this IQueryable<T> query, string order, string sortby)
        {
            if (string.IsNullOrEmpty(sortby)) throw new Exception("必须指定排序字段!");

            PropertyInfo sortProperty = typeof(T).GetProperty(sortby, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (sortProperty == null) throw new Exception("查询对象中不存在排序字段" + sortby + "！");

            ParameterExpression param = Expression.Parameter(typeof(T), "t");
            Expression body = param;
            if (Nullable.GetUnderlyingType(body.Type) != null)
            {
                body = Expression.Property(body, "Value");
            }
            body = Expression.MakeMemberAccess(body, sortProperty);
            LambdaExpression keySelectorLambda = Expression.Lambda(body, param);
            if (string.IsNullOrEmpty(order))
            {
                order = "ascending";
            }
            string queryMethod = order.StartsWith("desc") ? "OrderByDescending" : "OrderBy";
            query = query.Provider.CreateQuery<T>(Expression.Call(typeof(Queryable), queryMethod, new Type[] { typeof(T), body.Type }, query.Expression, Expression.Quote(keySelectorLambda)));
            return query;
        }

        /// <summary>
        /// 多条件组合排序
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="query"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public static IQueryable<T> GetOrderQuery<T>(IQueryable<T> query, params KeyValuePair<String, String>[] args)
        {
            if (args == null || args.Length == 0)
            {
                return query;
            }
            else
            {
                //创建表达式变量参数
                var parameter = Expression.Parameter(typeof(T), "o");
                foreach (var arg in args)
                {
                    //根据属性名获取属性
                    var property = typeof(T).GetRuntimeProperty(arg.Key);
                    //创建一个访问属性的表达式
                    var propertyAccess = Expression.MakeMemberAccess(parameter, property);
                    var orderByExp = Expression.Lambda(propertyAccess, parameter);
                    var OrderName = (!string.IsNullOrEmpty(arg.Value) && arg.Value.ToLower().Contains("desc")) ? "OrderByDescending" : "OrderBy";


                    MethodCallExpression resultExp = Expression.Call(typeof(Queryable), OrderName, new Type[] { typeof(T), property.PropertyType }, query.Expression, Expression.Quote(orderByExp));
                    query = query.Provider.CreateQuery<T>(resultExp);
                }
                return query;
            }
        }

        /// <summary>
        /// 单条件排序
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="query"></param>
        /// <param name="PropertyName"></param>
        /// <param name="AscOrDesc"></param>
        /// <returns></returns>
        public static IQueryable<T> GetOrderQuery<T>(IQueryable<T> query, String PropertyName, String AscOrDesc = "")
        {
            if (string.IsNullOrEmpty(PropertyName))
            {
                return GetOrderQuery(query);
            }
            else
            {
                return GetOrderQuery(query, new KeyValuePair<String, String>(PropertyName, AscOrDesc));
            }
        }
    }
}