using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ausm.ObjectStore.OrMapping.Expressions
{
    internal interface IParseAbleQueryProvider : IQueryProvider
    {
        string Parse(System.Linq.Expressions.Expression expression, Ausm.ObjectStore.OrMapping.Expressions.ParsedExpression parsedExpression);
    }

    public class QueryableExpression : CallExpression
    {
        System.Linq.Expressions.Expression _queryExpression;
        ParsedExpression _parsedExpression;
        IQueryable _queryable;

        public QueryableExpression(System.Linq.Expressions.Expression queryExpression, Expression parentExpression, ParsedExpression parsedExpression)
            : base(parentExpression)
        {
            _parsedExpression = parsedExpression;
            Type type = (queryExpression as System.Linq.Expressions.MethodCallExpression).Arguments[0].Type.GetGenericArguments()[0];
            typeof(QueryableExpression).GetMethods(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .Where(x => x.Name == "Initialize").First().MakeGenericMethod(type).Invoke(this, new object[] { queryExpression });
        }

        private void Initialize<T>(System.Linq.Expressions.Expression queryExpression) where T : class
        {
            System.Linq.Expressions.Expression initialExpression = queryExpression;
            while (initialExpression is System.Linq.Expressions.MethodCallExpression)
            {
                if (initialExpression is System.Linq.Expressions.MethodCallExpression)
                {
                    System.Linq.Expressions.MethodCallExpression callExpression = initialExpression as System.Linq.Expressions.MethodCallExpression;
                    if (callExpression.Arguments.Count > 0 &&
                        typeof(IQueryable).IsAssignableFrom(callExpression.Arguments[0].Type))
                    {
                        initialExpression = callExpression.Arguments[0];
                        continue;
                    }
                    
                    if (callExpression.Arguments.Count > 0 &&
                        typeof(System.Collections.IEnumerable).IsAssignableFrom(callExpression.Arguments[0].Type) &&
                        callExpression.Method.Name == "AsQueryable" &&
                        callExpression.Arguments[0] is System.Linq.Expressions.MemberExpression &&
                        (callExpression.Arguments[0] as System.Linq.Expressions.MemberExpression).Expression is System.Linq.Expressions.ParameterExpression)
                    {
                        System.Linq.Expressions.MemberExpression memberExpression = callExpression.Arguments[0] as System.Linq.Expressions.MemberExpression;
                        //TODO Expression bauen welche der Verknüpfungsabfrage entspricht.

                        IQueryable<T> queryable = ObjectStoreManager.DefaultObjectStore.GetQueryable<T>();

                        Dictionary<System.Reflection.PropertyInfo, object> contitions = ((Mapping.GetMapping(memberExpression.Member as System.Reflection.PropertyInfo) as ReferenceListPropertyMapping)).GetConditions(memberExpression);
                        foreach (KeyValuePair<System.Reflection.PropertyInfo, object> contition in contitions)
                        {
                            if (contition.Value == memberExpression)
                            {
                                System.Linq.Expressions.ParameterExpression parameterExpression = System.Linq.Expressions.Expression.Parameter(typeof(T), "y");

                                queryable = queryable.Where(System.Linq.Expressions.Expression.Lambda<Func<T, bool>>(
                                    System.Linq.Expressions.Expression.Equal(
                                        System.Linq.Expressions.Expression.Property(parameterExpression, contition.Key),
                                        memberExpression.Expression),
                                        parameterExpression));
                            }
                            else
                            {
                                System.Linq.Expressions.ParameterExpression parameterExpression = System.Linq.Expressions.Expression.Parameter(typeof(T), "y");

                                queryable = queryable.Where(System.Linq.Expressions.Expression.Lambda<Func<T, bool>>(
                                    System.Linq.Expressions.Expression.Equal(
                                        System.Linq.Expressions.Expression.Property(parameterExpression, contition.Key),
                                        System.Linq.Expressions.Expression.Constant(contition.Value)),
                                        parameterExpression));
                            }
                        }

                        _queryExpression = ExpressionHelper.ReplaceExpressionParts(queryExpression,
                            x => x == callExpression ? queryable.Expression : x);

                        _queryable = queryable;

                        return;
                    }
                    
                    if (callExpression.Arguments.Count == 0 &&
                        typeof(IObjectProvider).IsAssignableFrom(callExpression.Method.DeclaringType) &&
                        callExpression.Method.Name == "GetQueryable")
                    {
                        _queryable = System.Linq.Expressions.Expression.Lambda<Func<IQueryable>>(callExpression).Compile()();

                        _queryExpression = ExpressionHelper.ReplaceExpressionParts(queryExpression,
                            x => x == callExpression ? System.Linq.Expressions.Expression.Constant(_queryable) : x);
                        return;
                    }
                    else
                        throw new NotParsableException("Queryable has a unknown initial Expression", queryExpression);
                }
                else
                {
                    throw new NotParsableException("Queryable has a unknown initial Expression", queryExpression);
                }
            }

            if (initialExpression is System.Linq.Expressions.ConstantExpression &&
                ((System.Linq.Expressions.ConstantExpression)initialExpression).Value is IQueryable<T>)
            {
                _queryable = (initialExpression as System.Linq.Expressions.ConstantExpression).Value as IQueryable;
                _queryExpression = ExpressionHelper.ReplaceExpressionParts(queryExpression, x => x == initialExpression ? _queryable.Expression : x);
            }
            else
            {
                throw new NotSupportedException("The IQueryable is not supported in this Expression.");
            }
        }

        public override string SqlExpression
        {
            get 
            {
                return (_queryable.Provider as IParseAbleQueryProvider).Parse(_queryExpression, _parsedExpression);
            }
        }
    }

}
