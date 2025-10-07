using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Wlniao
{
    /// <summary>
    /// 条件过滤类
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    public class ExpressionList<TEntity> : IEnumerable<Expression<Func<TEntity, bool>>> where TEntity : class
    {
        List<Expression<Func<TEntity, bool>>> expressionList;
        /// <summary>
        /// 
        /// </summary>
        public ExpressionList()
        {
            expressionList = new List<Expression<Func<TEntity, bool>>>();
        }
        /// <summary>
        /// 添加到集合
        /// </summary>
        /// <param name="predicate"></param>
        public void Add(Expression<Func<TEntity, bool>> predicate)
        {
            expressionList.Add(predicate);
        }
        /// <summary>
        /// Or操作添加到集合
        /// </summary>
        /// <param name="exprleft"></param>
        /// <param name="exprright"></param>
        public void AddForOr(Expression<Func<TEntity, bool>> exprleft, Expression<Func<TEntity, bool>> exprright)
        {
            expressionList.Add(exprleft.Or(exprright));
        }
        /// <summary>
        /// And操作添加到集合
        /// </summary>
        /// <param name="exprleft"></param>
        /// <param name="exprright"></param>
        public void AddForAnd(Expression<Func<TEntity, bool>> exprleft, Expression<Func<TEntity, bool>> exprright)
        {
            expressionList.Add(exprleft.And(exprright));
        }

        #region IEnumerable 成员
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public IEnumerator GetEnumerator()
        {
            return expressionList.GetEnumerator();
        }

        IEnumerator<Expression<Func<TEntity, bool>>> IEnumerable<Expression<Func<TEntity, bool>>>.GetEnumerator()
        {
            return expressionList.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return expressionList.GetEnumerator();
        }
        #endregion
    }
}