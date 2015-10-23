using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;

namespace ObjectStore
{
    public class SelectQueryable<T, T1> : IQueryable<T>, INotifyCollectionChanged
    {
        #region Subclasses
        class Enumerator : IEnumerator<T>
        {
            public Enumerator(SelectQueryable<T, T1> parentEnumerable)
            {
                _parentEnumerable = parentEnumerable;
                _innerEnumerator = parentEnumerable._source.GetEnumerator();
            }

            IEnumerator<T1> _innerEnumerator;
            SelectQueryable<T, T1> _parentEnumerable;

            public T Current
            {
                get 
                { 
                    return _parentEnumerable.GetValueFor(_innerEnumerator.Current);
                }
            }

            public void Dispose()
            {
                _parentEnumerable = null;
                _innerEnumerator.Dispose();
            }

            object System.Collections.IEnumerator.Current
            {
                get
                {
                    return Current;
                }
            }

            public bool MoveNext()
            {
                if(!_innerEnumerator.MoveNext())
                    return false;

                if (object.Equals(_innerEnumerator.Current, _parentEnumerable._currentlyWrapping))
                    return _innerEnumerator.MoveNext();

                return true;
            }

            public void Reset()
            {
                _innerEnumerator.Reset();
            }
        }
        #endregion

        #region Fields
        Dictionary<T1, T> _values;
        IQueryable<T1> _source;
        Func<T1, T> _wrapFunction;
        T1 _currentlyWrapping;
        #endregion

        #region Methods
        internal SelectQueryable(IQueryable<T1> source, Func<T1, T> wrapFunction)
        {
            if (!(source is INotifyCollectionChanged))
                throw new ArgumentException("Source must implement INotifyCollectionChanged.", "source");

            _source = source;
            _wrapFunction = wrapFunction;
            _values = new Dictionary<T1, T>();

            ((INotifyCollectionChanged)_source).CollectionChanged += SourceCollectionChanged;
        }

        void SourceCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add,
                        e.NewItems.Cast<T1>().ToList().Select(x => GetValueFor(x)).ToList(), e.NewStartingIndex));
                    break;
                case NotifyCollectionChangedAction.Move:
                    OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Move,
                        GetValueFor(e.OldItems.Cast<T1>().FirstOrDefault()), e.NewStartingIndex, e.OldStartingIndex));
                    break;
                case NotifyCollectionChangedAction.Remove:
                    {
                        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove,
                            e.OldItems.Cast<T1>().Select(x => GetValueFor(x)).ToList(), e.OldStartingIndex));

                        foreach (T1 key in e.OldItems)
                            _values.Remove(key);
                    }
                    break;
                case NotifyCollectionChangedAction.Replace:
                    {
                        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, GetValueFor(e.NewItems.Cast<T1>().FirstOrDefault()), GetValueFor(e.OldItems.Cast<T1>().FirstOrDefault())));
                        foreach (T1 item in e.OldItems)
                            _values.Remove(item);
                    }
                    break;
                case NotifyCollectionChangedAction.Reset:
                    {
                        _values = new Dictionary<T1, T>();
                        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
                    }
                    break;
            }
        }

        T GetValueFor(T1 item)
        {
            if (_values.ContainsKey(item))
                return _values[item];
            else
            {
                _currentlyWrapping = item;
                T returnValue = _wrapFunction(item);
                _values.Add(item, returnValue);
                _currentlyWrapping = default(T1);
                return returnValue;
            }
        }
        #endregion

        #region Indexer
        public T this[T1 item]
        {
            get
            {
                if (_source.Contains(item))
                    return GetValueFor(item);
                else
                    throw new NotSupportedException("Item is not part of the source collection.");
            }
        }
        #endregion

        #region IEnumberable-Member
        public IEnumerator<T> GetEnumerator()
        {
            return new Enumerator(this);
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        #endregion

        #region IQueryable-Member
        public Type ElementType
        {
            get 
            { 
                return typeof(T); 
            }
        }

        public System.Linq.Expressions.Expression Expression
        {
            get 
            {
                return null;
                //System.Linq.Expressions.Expression.Call(typeof(Queryable).GetMethods().Where( x=> x.Name == "Select" && x.GetParameters(
                //_source.Expression
            }
        }

        public IQueryProvider Provider
        {
            get { throw new NotImplementedException(); }
        }
        #endregion

        #region INotifyCollectionChanged-Member

        protected virtual void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (CollectionChanged != null)
                CollectionChanged(this, e);
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged;
        #endregion
    }
}
