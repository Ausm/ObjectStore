using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Linq.Expressions;
using ObjectStore.OrMapping;
using Microsoft.Framework.DependencyInjection;
using ObjectStore.Interfaces;

#if !DNXCORE50
using System.Transactions;
#endif

namespace ObjectStore
{
    public static class Extensions
	{
        #region IoC Extensions
        public static void AddObjectStore(this IServiceCollection services, string connectionString)
        {
            RelationalObjectStore relationalObjectStore = new RelationalObjectStore(connectionString, true);
            ObjectStoreManager.DefaultObjectStore.RegisterObjectProvider(relationalObjectStore);
            services.Add(new ServiceDescriptor(typeof(IObjectProvider), relationalObjectStore));
        }
        #endregion

        #region Value Extensions
        [SqlSubstitute("({0} COLLATE Latin1_General_CI_AI) LIKE {1}")]
        public static bool Like(this string str, string pattern)
        {
            if (str == null) return false;

            str = str.ToUpper();

            string[] patterns = pattern.ToUpper().Split('%');
            if (!str.StartsWith(patterns[0])) return false;

            int curPos = 0;
            for (int i = 0; i < patterns.Length; i++)
            {
                var index = str.IndexOf(patterns[i], curPos);
                if (index != -1)
                {
                    curPos = index + patterns[i].Length;
                }
                else return false;
            }

            if (curPos != str.Length && !pattern.EndsWith("%"))
                return false;
            else
                return true;
        }

        [SqlSubstitute("{0} BETWEEN {1} AND {2}")]
        public static bool Between(this int value, int first, int second)
        {
            return (first <= value && value <= second);
        }
        
        [SqlSubstitute("{0} BETWEEN {1} AND {2}")]
        public static bool Between(this DateTime value, DateTime first, DateTime second)
        {
            return (first <= value && value <= second);
        }
        #endregion

        #region Queryable Extensions

#if DNXCORE50
        public static bool Save<T>(this IQueryable<T> source)
        {
            if (typeof(IObjectStoreQueryable<T>).IsAssignableFrom(source.GetType()))
            {
                return source.Provider.Execute<bool>(Expression.Call(null, GetMethodInfoOf(() => default(IQueryable<T>).Save()), new Expression[] { source.Expression }));
            }
            else
            {
                return false;
            }
        }

        public static bool DropChanges<T>(this IQueryable<T> source)
        {
            if (typeof(IObjectStoreQueryable<T>).IsAssignableFrom(source.GetType()))
            {
                return source.Provider.Execute<bool>(Expression.Call(null, GetMethodInfoOf(() => default(IQueryable<T>).DropChanges()), new Expression[] { source.Expression }));
            }
            else
            {
                return false;
            }
        }

        public static bool CheckChanged<T>(this IQueryable<T> source)
        {
            if (typeof(IObjectStoreQueryable<T>).IsAssignableFrom(source.GetType()))
            {
                return source.Provider.Execute<bool>(Expression.Call(null, GetMethodInfoOf(() => default(IQueryable<T>).CheckChanged()), new Expression[] { source.Expression }));
            }
            else
            {
                throw new InvalidOperationException("Source is not a IStoreQueryable.");
            }
        }

        public static bool Delete<T>(this IQueryable<T> source)
        {
            if (typeof(IObjectStoreQueryable<T>).IsAssignableFrom(source.GetType()))
            {
                return source.Provider.Execute<bool>(Expression.Call(null, GetMethodInfoOf(() => default(IQueryable<T>).Delete()), new Expression[] { source.Expression }));
            }
            else
            {
                return false;
            }
        }

        public static IAsyncResult BeginFetch<T>(this IQueryable<T> source)
        {
            if (typeof(IObjectStoreQueryable<T>).IsAssignableFrom(source.GetType()))
            {
                return source.Provider.Execute<IAsyncResult>(Expression.Call(null, GetMethodInfoOf(() => default(IQueryable<T>).BeginFetch()), new Expression[] { source.Expression }));
            }
            else
            {
                return null;
            }
        }

        public static IQueryable<T> ForceLoad<T>(this IQueryable<T> source)
        {
            if (typeof(IObjectStoreQueryable<T>).IsAssignableFrom(source.GetType()))
            {
                return source.Provider.CreateQuery<T>(
                    Expression.Call(null,
                    GetMethodInfoOf(() => default(IQueryable<T>).ForceLoad()),
                    new Expression[] { source.Expression })
                    );
            }
            throw new InvalidOperationException("Source is not a IStoreQueryable.");
        }

        public static IQueryable<T> ForceCache<T>(this IQueryable<T> source)
        {
            if (typeof(IObjectStoreQueryable<T>).IsAssignableFrom(source.GetType()))
            {
                return source.Provider.CreateQuery<T>(
                    Expression.Call(null,
                    GetMethodInfoOf(() => default(IQueryable<T>).ForceCache()),
                    new Expression[] { source.Expression }));
            }
            throw new InvalidOperationException("Source is not a IStoreQueryable.");
        }

        public static void ApplyAll<TKey, TTarget, TSource>(this IEnumerable<TTarget> source, Func<TKey[], IDictionary<TKey, TSource>> getSource, Func<TTarget, TKey> getKey, Action<TTarget, TSource> apply)
        {
            IDictionary<TKey, TSource> dictionary = getSource(source.Select(getKey).Distinct().ToArray());
            foreach (TTarget target in source)
            {
                TKey key = getKey(target);
                if(dictionary.ContainsKey(key))
                    apply(target, dictionary[key]);
            }
        }

        static MethodInfo GetMethodInfoOf<T>(Expression<Func<T>> expression)
        {
            return ((MethodCallExpression)expression.Body).Method;
        }

#else
        public static bool Save<T>(this IQueryable<T> source)
        {
            if (typeof(IObjectStoreQueryable<T>).IsAssignableFrom(source.GetType()))
            {
                using(TransactionScope transactionScope = Transaction.Current == null ? new TransactionScope() : null)
                {
                    bool returnValue = source.Provider.Execute<bool>(Expression.Call(null, ((MethodInfo)MethodBase.GetCurrentMethod()).MakeGenericMethod(new Type[] { typeof(T) }), new Expression[] { source.Expression }));
                    if (transactionScope != null) transactionScope.Complete();
                    return returnValue;
                }
            }
            else
            {
                return false;
            }
        }

        public static bool DropChanges<T>(this IQueryable<T> source)
        {
            if (typeof(IObjectStoreQueryable<T>).IsAssignableFrom(source.GetType()))
            {
                return source.Provider.Execute<bool>(Expression.Call(null, ((MethodInfo)MethodBase.GetCurrentMethod()).MakeGenericMethod(new Type[] { typeof(T) }), new Expression[] { source.Expression }));
            }
            else
            {
                return false;
            }
        }

        public static bool CheckChanged<T>(this IQueryable<T> source)
        {
            if (typeof(IObjectStoreQueryable<T>).IsAssignableFrom(source.GetType()))
            {
                return source.Provider.Execute<bool>(Expression.Call(null, ((MethodInfo)MethodBase.GetCurrentMethod()).MakeGenericMethod(new Type[] { typeof(T) }), new Expression[] { source.Expression }));
            }
            else
            {
                throw new InvalidOperationException("Source is not a IStoreQueryable.");
            }
        }

        public static bool Delete<T>(this IQueryable<T> source)
        {
            if (typeof(IObjectStoreQueryable<T>).IsAssignableFrom(source.GetType()))
            {
                return source.Provider.Execute<bool>(Expression.Call(null, ((MethodInfo)MethodBase.GetCurrentMethod()).MakeGenericMethod(new Type[] { typeof(T) }), new Expression[] { source.Expression }));
            }
            else
            {
                return false;
            }
        }

        public static IAsyncResult BeginFetch<T>(this IQueryable<T> source)
        {
            if (typeof(IObjectStoreQueryable<T>).IsAssignableFrom(source.GetType()))
            {
                return source.Provider.Execute<IAsyncResult>(Expression.Call(null, ((MethodInfo)MethodBase.GetCurrentMethod()).MakeGenericMethod(new Type[] { typeof(T) }), new Expression[] { source.Expression }));
            }
            else
            {
                return null;
            }
        }

        public static IQueryable<T> ForceLoad<T>(this IQueryable<T> source)
        {
            if (typeof(IObjectStoreQueryable<T>).IsAssignableFrom(source.GetType()))
            {
                return source.Provider.CreateQuery<T>(
                    Expression.Call(null,
                    ((MethodInfo)MethodBase.GetCurrentMethod()).MakeGenericMethod(new Type[] { typeof(T) }),
                    new Expression[] { source.Expression })
                    );
            }
            throw new InvalidOperationException("Source is not a IStoreQueryable.");
        }

        public static IQueryable<T> ForceCache<T>(this IQueryable<T> source)
        {
            if (typeof(IObjectStoreQueryable<T>).IsAssignableFrom(source.GetType()))
            {
                return source.Provider.CreateQuery<T>(
                    Expression.Call(null,
                    ((MethodInfo)MethodBase.GetCurrentMethod()).MakeGenericMethod(new Type[] { typeof(T) }),
                    new Expression[] { source.Expression })
                    );
            }
            throw new InvalidOperationException("Source is not a IStoreQueryable.");
        }

        public static void ApplyAll<TKey, TTarget, TSource>(this IEnumerable<TTarget> source, Func<TKey[], IDictionary<TKey, TSource>> getSource, Func<TTarget, TKey> getKey, Action<TTarget, TSource> apply)
        {
            IDictionary<TKey, TSource> dictionary = getSource(source.Select(getKey).Distinct().ToArray());
            foreach (TTarget target in source)
            {
                TKey key = getKey(target);
                if(dictionary.ContainsKey(key))
                    apply(target, dictionary[key]);
            }
        }
#endif
        #endregion
    }
}
