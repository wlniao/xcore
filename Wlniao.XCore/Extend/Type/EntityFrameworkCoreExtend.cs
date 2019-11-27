/*==============================================================================
    文件名称：EntityFrameworkCoreExtend.cs
    适用环境：CoreCLR 5.0,.NET Framework 2.0/4.0/5.0
    功能描述：EntityFramework功能扩展
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
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using System.Reflection;
using System.Dynamic;
/// <summary>
/// EntityFramework功能扩展
/// </summary>
public static class EntityFrameworkCoreExtend
{
    private static readonly TypeInfo QueryCompilerTypeInfo = typeof(QueryCompiler).GetTypeInfo();
    private static readonly FieldInfo QueryCompilerField = typeof(EntityQueryProvider).GetTypeInfo().DeclaredFields.First(x => x.Name == "_queryCompiler");
    private static readonly FieldInfo QueryModelGeneratorField = typeof(QueryCompiler).GetTypeInfo().DeclaredFields.First(x => x.Name == "_queryModelGenerator");
    private static readonly FieldInfo DataBaseField = QueryCompilerTypeInfo.DeclaredFields.Single(x => x.Name == "_database");
    private static readonly PropertyInfo DatabaseDependenciesField = typeof(Database).GetTypeInfo().DeclaredProperties.Single(x => x.Name == "Dependencies");

    /// <summary>
    /// 获取本次查询SQL语句
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <param name="query"></param>
    /// <returns></returns>
    public static string ToSql<TEntity>(this IQueryable<TEntity> query)
    {
        var queryCompiler = (QueryCompiler)QueryCompilerField.GetValue(query.Provider);
        var queryModelGenerator = (QueryModelGenerator)QueryModelGeneratorField.GetValue(queryCompiler);
        var queryModel = queryModelGenerator.ParseQuery(query.Expression);
        var database = DataBaseField.GetValue(queryCompiler);
        var databaseDependencies = (DatabaseDependencies)DatabaseDependenciesField.GetValue(database);
        var queryCompilationContext = databaseDependencies.QueryCompilationContextFactory.Create(false);
        var modelVisitor = (RelationalQueryModelVisitor)queryCompilationContext.CreateQueryModelVisitor();
        modelVisitor.CreateQueryExecutor<TEntity>(queryModel);
        var sql = modelVisitor.Queries.First().ToString();

        return sql;
    }
    /// <summary>
    /// 根据sql语句查询并返回动态列表
    /// </summary>
    /// <param name="db"></param>
    /// <param name="sql"></param>
    /// <returns></returns>
    public static List<dynamic> GetFromSql(this Microsoft.EntityFrameworkCore.DbContext db, string sql)
    {
        var conn = db.Database.GetDbConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        conn.Open();
        var list = new List<dynamic>();
        var reader = cmd.ExecuteReader();
        if (reader.HasRows)
        {
            var columns = new string[reader.FieldCount];
            for (var i = 0; i < reader.FieldCount; i++)
            {
                columns[i] = reader.GetName(i);
            }
            while (reader.Read())
            {
                dynamic model = new ExpandoObject();
                var dict = (IDictionary<string, object>)model;
                for (var i = 0; i < reader.FieldCount; i++)
                {
                    dict[columns[i]] = reader[i];
                }
                if (dict.Count > 0)
                {
                    list.Add(model);
                }
            }
        }
        conn.Close();
        return list;
    }
}