using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Linq.Expressions;
using System.Collections.Specialized;

namespace ObjectStore.OrMapping
{
    public class ReferenceList<T> : ICollection<T>, IObjectStoreQueryable<T>, INotifyCollectionChanged where T : class
    {
        Dictionary<PropertyInfo, object> _conditions;
        IQueryable<T> _queryable;

        public ReferenceList(Dictionary<PropertyInfo, object> conditions)
        {
            _conditions = new Dictionary<PropertyInfo, object>(conditions);
        }

        #region Private Funktionen
        private IQueryable<T> GetQueryable()
        {
            if (_queryable == null)
            {

                ParameterExpression param = Expression.Parameter(typeof(T), "param");
                Expression expression = null;
                foreach (KeyValuePair<PropertyInfo, object> condition in _conditions)
                {
                    if (expression == null)
                    {
                        expression = Expression.Equal(
                            Expression.Property(param, condition.Key),
                            Expression.Constant(condition.Value));
                    }
                    else
                    {
                        expression = Expression.AndAlso(expression,
                                        Expression.Equal(
                                            Expression.Property(param, condition.Key),
                                            Expression.Constant(condition.Value)));
                    }
                }

                _queryable = expression == null ?
                    ObjectStoreManager.DefaultObjectStore.GetQueryable<T>() :
                    ObjectStoreManager.DefaultObjectStore.GetQueryable<T>().Where(Expression.Lambda<Func<T, bool>>(expression, param));

                ((INotifyCollectionChanged)_queryable).CollectionChanged += (s, e) => OnCollectionChanged(e);
            }
            return _queryable;
        }
        #endregion

        #region IEnumerable<T> Members

        public IEnumerator<T> GetEnumerator()
        {
            return GetQueryable().GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetQueryable().GetEnumerator();
        }

        #endregion

        #region ICollection<T> Members

        public void Add(T item)
        {
            foreach (KeyValuePair<PropertyInfo, object> condition in _conditions)
            {
                condition.Key.SetValue(item, condition.Value, new object[] { });
            }
        }

        public void Clear()
        {
            GetQueryable().Delete();
        }

        public bool Contains(T item)
        {
            return GetQueryable().Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            if (array == null)
            {
                throw new ArgumentNullException("array");
            }
            if (arrayIndex < 0)
            {
                throw new ArgumentOutOfRangeException("arrayIndex");
            }
            int index = arrayIndex;
            foreach (T item in this)
            {
                array[index++] = item;
            }
        }

        public int Count
        {
            get
            {
                int i = 0;
                for (IEnumerator<T> enumerator = GetEnumerator(); enumerator.MoveNext(); i++) { }
                return i;
            }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(T item)
        {
            if (item == null)
            {
                return false;
            }
            if(GetQueryable().Where(x => x == item).Delete())
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        #endregion

        #region INotifyCollectionChanged Members

        public void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if(CollectionChanged != null)
                CollectionChanged(this, e);
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        #endregion

        #region IQueryable Members

        public Type ElementType
        {
            get { return typeof(T); }
        }

        public Expression Expression
        {
            get { return GetQueryable().Expression; }
        }

        public IQueryProvider Provider
        {
            get { return GetQueryable().Provider; }
        }

        #endregion
    }
}
