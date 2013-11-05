using System;
using System.Collections.Generic;
using System.Text;
using System.Data.SqlClient;
using System.Reflection;
using System.Linq.Expressions;
using System.Linq;
using System.Diagnostics;

namespace Ausm.ObjectStore.OrMapping
{
    public partial class InheritensObjectProvider<T> : IObjectProvider
    {
        internal class Queryable : IObjectStoreQueryable<T>, System.Collections.Specialized.INotifyCollectionChanged
        {
            #region Membervariablen
            protected Expression _returnedExpression;
            protected QueryProvider _querryProvider;
            #endregion

            #region Konstruktoren
            private Queryable(InheritensObjectProvider<T> objectProvider, Expression expression)
            {
                _returnedExpression = expression == null ? Expression.Constant(this) : expression;
                _querryProvider = QueryProvider.Create(Expression, objectProvider);
            }
            #endregion

            #region Methoden
            internal static Queryable Create(InheritensObjectProvider<T> objectProvider, Expression expression)
            {
                return new Queryable(objectProvider, expression);
            }

            public override string ToString()
            {
                return _querryProvider.ToString();
            }

            public override int GetHashCode()
            {
                if (_returnedExpression is ConstantExpression && ((ConstantExpression)_returnedExpression).Value == this)
                    return typeof(T).GetHashCode() ^ 0x1010000;
                else
                    return ((ValueComparedExpression<Expression>)_returnedExpression).GetHashCode();
            }

            public override bool Equals(object obj)
            {
                Queryable queryable = obj as Queryable;

                if (queryable == null)
                    return false;

                if (queryable._returnedExpression is ConstantExpression &&
                    ((ConstantExpression)queryable._returnedExpression).Value == queryable)
                    return _returnedExpression is ConstantExpression && ((ConstantExpression)_returnedExpression).Value == this;

                return queryable.Provider == Provider;
            }
            #endregion

            #region IEnumerable<T> Members

            public virtual IEnumerator<T> GetEnumerator()
            {
                return _querryProvider.GetValues().GetEnumerator();
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return this.GetEnumerator();
            }

            #endregion

            #region IQueryable Members

            public Type ElementType
            {
                get { return typeof(T); }
            }

            public Expression Expression
            {
                get
                {
                    return _returnedExpression;
                }
            }

            public IQueryProvider Provider { get { return _querryProvider; } }
            #endregion

            public event System.Collections.Specialized.NotifyCollectionChangedEventHandler CollectionChanged
            {
                add { _querryProvider.CollectionChanged += value; }
                remove { _querryProvider.CollectionChanged -= value; }
            }
        }
    }
}
