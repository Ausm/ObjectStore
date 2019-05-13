using System;
using System.Linq;
using System.Reflection;
using System.Linq.Expressions;
using ObjectStore.Interfaces;
using System.Threading.Tasks;

namespace ObjectStore
{
    public static class Extensions
	{
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
        public static bool Save<T>(this IQueryable<T> source)
        {
            if (typeof(IObjectStoreQueryable<T>).IsAssignableFrom(source.GetType()))
            {
                bool returnValue = source.Provider.Execute<bool>(Expression.Call(null, ((MethodInfo)MethodBase.GetCurrentMethod()).MakeGenericMethod(new Type[] { typeof(T) }), new Expression[] { source.Expression }));
                return returnValue;
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

        public static Task FetchAsync<T>(this IQueryable<T> source)
        {
            if (typeof(IObjectStoreQueryable<T>).IsAssignableFrom(source.GetType()))
                return source.Provider.Execute<Task>(Expression.Call(null, ((MethodInfo)MethodBase.GetCurrentMethod()).MakeGenericMethod(new Type[] { typeof(T) }), new Expression[] { source.Expression }));
            else
                throw new InvalidOperationException("Source is not a IStoreQueryable.");
        }
        #endregion
    }
}
