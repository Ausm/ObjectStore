using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Linq.Expressions;
using Ausm.ObjectStore.OrMapping;
using System.Transactions;

namespace Ausm.ObjectStore
{
	public static class Extensions
	{
        [SqlSubstitute("({0} COLLATE Latin1_General_CI_AI) LIKE {1}")]
        public static bool Like(this string str, string pattern)
        {
            if (str == null) return false;

            str = RemoveDiacritics(str.ToUpper());
            pattern = RemoveDiacritics(pattern);

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

        /// <summary>
        /// Entfernt Accents, Umlaut-Punkte etc. von Strings.
        /// </summary>
        /// <param name="s">String mit diakritischen Zeichen</param>
        /// <returns>String ohne diakritischen Zeichen</returns>
        private static string RemoveDiacritics(string s)
        {
            char[] result = new char[s.Length];
            s = s.Normalize(NormalizationForm.FormD);

            int j = 0;
            for (int i = 0; i < s.Length; i++)
            {
                if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(s[i]) != System.Globalization.UnicodeCategory.NonSpacingMark)
                    result[j++] = s[i];
            }

            return new string(result);
        }

        /// <summary>
        /// Speichert alle Änderungen des angegebene IQueryable in dem ObjectProvider von dem es abgefragt wurde.
        /// </summary>
        /// <returns>True wenn Speichern ausgeführt wurde.</returns>
        public static bool Save<T>(this IQueryable<T> source)
        {
            if (typeof(IObjectStoreQueryable<T>).IsAssignableFrom(source.GetType()))
            {
                using(TransactionScope transactionScope = System.Transactions.Transaction.Current == null ? new TransactionScope() : null)
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

        [Obsolete("Bitte die Überladung ohne Exception Verwenden")]
        public static bool Save<T>(this IQueryable<T> source, Action<T, Exception> onError) where T:class
        {
            try
            {
                return Save(source);
            }
            catch (EntitySaveException exception)
            {
                onError(exception.Entity as T, exception.InnerException);
                return false;
            }
        }

        /// <summary>
        /// Verwirft alle nichtgespeicherten Änderungen des Queryables in dem ObjectProvider von dem es abgefragt wurde.
        /// </summary>
        /// <returns>True wenn das Verwerfen ausgeführt wurde.</returns>
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

        /// <summary>
        /// Gibt an ob das übergebene Queryable ungespeicherte Änderungen enthält.
        /// </summary>
        /// <returns>True wenn Änderungen im Queryable gefunden wurde, False wenn die Objekte ungeändert sind.</returns>
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

        /// <summary>
        /// Markiert alle Objekte im angegebenen Querryable als gelöscht.
        /// Geschrieben werden diese Änderungen erst beim Speichern.
        /// </summary>
        /// <returns>rue wenn das Löschen ausgeführt wurde.</returns>
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

        /// <summary>
        /// Beginnt das Asynchrone abrufen der Entities
        /// </summary>
        /// <returns>rue wenn das Löschen ausgeführt wurde.</returns>
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

        /// <summary>
        /// Manipuliert das angegebene Queryable das dieses bei Ausführung zwingend auf der Quelle (bei ORMappern z.B. aus der Datenbank) ausgeführt wird.
        /// </summary>
        /// <returns>Manipulierte Queryable</returns>
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

        /// <summary>
        /// Manipuliert das angegebene Queryable das dieses bei Ausführung nicht auf der Quelle (bei ORMappern z.B. aus der Datenbank) ausgeführt wird.
        /// </summary>
        /// <returns>Manipulierte Queryable</returns>
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

        /// <summary>
        /// Übernimmt daten aus einer Auflistung von Objekten in eine Objekte einer zweiten Auflistung
        /// </summary>
        /// <typeparam name="TKey">Type des Schlüssels welcher Quell- und Zieltabelle miteinander verbindet.</typeparam>
        /// <typeparam name="TTarget">Type des Zielobjekts</typeparam>
        /// <typeparam name="TSource">Type des Quellobjekts</typeparam>
        /// <param name="source">Auflistung der Zielobjekte</param>
        /// <param name="getSource">Funktion zum Abrufen der Quellauflistung</param>
        /// <param name="getKey">Funktion um aus dem Zielobjekt den Schlüssel abrufen zu können.</param>
        /// <param name="apply">Funktion zum übernehmen der Daten aus dem Quell in das Zielobjekt</param>
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
    }
}
