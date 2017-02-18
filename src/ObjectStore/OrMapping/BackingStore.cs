#if  NETCOREAPP1_0
using ApplicationException = global::System.InvalidOperationException;
#endif

using ObjectStore.MappingOptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace ObjectStore.OrMapping
{
    public struct BackingStore<T>
    {
        T _originalValue;
        T _uncommittedValue;
        T _changedValue;
        bool _isOriginalSet;
        bool _isChanged;
        bool _isUncommitted;

        public T Value
        {
            get
            {
                return _isChanged ? _changedValue : _originalValue;
            }
            set
            {
                if (object.Equals(_originalValue, value))
                {
                    _changedValue = default(T);
                    _isChanged = false;
                }
                else
                {
                    _changedValue = value;
                    _isChanged = true;
                }
            }
        }

        public bool IsChanged
        {
            get
            {
                return _isChanged;
            }
        }

        public T GetUncommittedValue()
        {
            return _isUncommitted ? _uncommittedValue :
                    _isOriginalSet ? _originalValue :
                    _isChanged ? _changedValue : default(T);
        }

        public T GetChangedValue()
        {
            return Value;
        }

        public void SetUnCommittedValue(T value)
        {
            if (object.Equals(_originalValue, value))
            {
                _uncommittedValue = default(T);
                _isUncommitted = false;
            }
            else
            {
                _uncommittedValue = value;
                _isUncommitted = true;
            }
        }

        public bool Commit(bool withUndo)
        {
            if (!_isUncommitted)
            {
                if (withUndo && _isChanged)
                {
                    _changedValue = default(T);
                    _isChanged = false;
                    return true;
                }
                return false;
            }

            bool returnvalue = withUndo && _isChanged ? !object.Equals(_uncommittedValue, _changedValue) :
                _isOriginalSet ? !object.Equals(_uncommittedValue, _originalValue) : true;

            _originalValue = _uncommittedValue;
            _isUncommitted = false;
            _isOriginalSet = true;

            if (withUndo)
            { 
                _changedValue = default(T);
                _isChanged = false;
            }
            return returnvalue;
        }

        public void Rollback()
        {
            if (_isUncommitted)
            {
                _uncommittedValue = default(T);
                _isUncommitted = false;
            }
        }

        public void Undo()
        {
            _changedValue = default(T);
            _isChanged = false;
        }
    }

    public struct ReadOnlyBackingStore<T>
    {
        T _originalValue;
        T _uncommittedValue;
        bool _isOriginalSet;
        bool _isUncommitted;

        public T Value
        {
            get
            {
                return _originalValue;
            }
        }

        public T GetUncommittedValue()
        {
            return _isUncommitted ? _uncommittedValue :
                    _isOriginalSet ? _originalValue : default(T);
        }

        public void SetUnCommittedValue(T value)
        {
            if (object.Equals(_originalValue, value))
            {
                _uncommittedValue = default(T);
                _isUncommitted = false;
            }
            else
            {
                _uncommittedValue = value;
                _isUncommitted = true;
            }
        }

        public bool Commit(bool withUndo)
        {
            if (!_isUncommitted)
                return false;

            _originalValue = _uncommittedValue;
            _isUncommitted = false;
            _isOriginalSet = true;

            return true;
        }

        public void Rollback()
        {
            if (_isUncommitted)
            {
                _uncommittedValue = default(T);
                _isUncommitted = false;
            }
        }
    }

    public struct ForeignObjectBackingStore<T, T1> where T : class
    {
        #region Fields
        static MemberMappingOptions _foreignKeyMemberOptions;
        static Func<T, T1> _fromValue;

        T1 _uncommittedValue;
        T1 _originalValue;
        T _originalConvertedValue;
        IQueryable<T> _originalQuery;
        T _changedValue;
        bool _isOriginalSetted;
        bool _isOriginalConvertedSetted;
        bool _isChanged;
        bool _isUncommitted;
        #endregion

        #region Properties
        public T Value
        {
            get
            {
                if (_isChanged)
                    return _changedValue;

                if (_isOriginalConvertedSetted)
                    return _originalConvertedValue;

                if (!_isOriginalSetted)
                    return default(T);

                _originalConvertedValue = _originalQuery == null ? default(T) : _originalQuery.FirstOrDefault();
                _isOriginalConvertedSetted = true;
                return _originalConvertedValue;
            }
            set
            {
                if (value == null)
                {
                    _changedValue = null;
                    _isChanged = !object.Equals(_originalValue, default(T1));
                }
                else if ((_isOriginalConvertedSetted && value == _originalConvertedValue) ||
                    (_isOriginalSetted && object.Equals(FromValue(value),_originalValue)))
                {
                    _changedValue = null;
                    _isChanged = false;
                }
                else
                {
                    _changedValue = value;
                    _isChanged = true;
                }
            }
        }

        public bool IsChanged
        {
            get { return _isChanged; }
        }

        static MemberMappingOptions ForeignKeyMemberOptions
        {
            get
            {
                if (_foreignKeyMemberOptions != null)
                    return _foreignKeyMemberOptions;

                List<MemberMappingOptions> options = MappingOptionsSet.GetExistingTypeMappingOptions(typeof(T)).MemberMappingOptions.Where(x => x.IsPrimaryKey).ToList();
                if(options.Count != 1)
                    throw new ApplicationException("ForeignObject must have only one IsPrimaryKey marked property.");

                return _foreignKeyMemberOptions = options[0];
            }
        }

        static Func<T, T1> FromValue
        {
            get
            {
                if (_fromValue != null)
                    return _fromValue;

                return _fromValue = x => (T1)(((IFillAbleObject)x).Keys.Single());
            }
        }
        #endregion

        #region Methods
        public T1 GetUncommittedValue()
        {
            return 
                _isUncommitted ? _uncommittedValue : 
                _isOriginalSetted ? _originalValue :
                _isChanged ? FromValue(_changedValue) : default(T1);
        }

        public T1 GetChangedValue()
        {
            if (_isChanged)
                return _changedValue == null ? default(T1) : FromValue(_changedValue);
            else
                return _originalValue;
        }

        public void SetUnCommittedValue(T1 value)
        {
            if (object.Equals(_originalValue, value))
            {
                _uncommittedValue = default(T1);
                _isUncommitted = false;
            }
            else
            {
                _uncommittedValue = value;
                _isUncommitted = true;
            }
        }

        public bool Commit(bool withUndo)
        {
            if (!_isUncommitted)
            {
                if (withUndo && _isChanged)
                {
                    _isChanged = false;
                    _changedValue = default(T);
                    return true;
                }
                return false;
            }

            _originalValue = _uncommittedValue;
            _originalConvertedValue = default(T);
            _isOriginalConvertedSetted = false;

            ParameterExpression paramT = Expression.Parameter(typeof(T), "T");
            if (object.Equals(_originalValue, default(T1)))
                _originalQuery = null;
            else if (_changedValue is IFillAbleObject &&
                ((IFillAbleObject)_changedValue).Keys.Count == 1 &&
                ((IFillAbleObject)_changedValue).Keys.Single() is T1 &&
                object.Equals((T1)((IFillAbleObject)_changedValue).Keys.Single(), _originalValue))
            {
                _originalConvertedValue = _changedValue;
                _isOriginalConvertedSetted = true;
            }
            else
            {
                Expression propertyAccess = null;
                if (ForeignKeyMemberOptions is ForeignObjectMappingOptions)
                {
                    propertyAccess = Expression.Property(paramT, ForeignKeyMemberOptions.Member);
                    ForeignObjectMappingOptions currentOptions = (ForeignObjectMappingOptions)ForeignKeyMemberOptions;
                    while (true)
                    {
                        propertyAccess = Expression.Property(propertyAccess, currentOptions.ForeignMember.Member);
                        if (currentOptions.ForeignMember is ForeignObjectMappingOptions)
                            currentOptions = (ForeignObjectMappingOptions)currentOptions.ForeignMember;
                        else
                            break;
                    }
                }
                else
                {
                    propertyAccess = Expression.Property(paramT, ForeignKeyMemberOptions.Member);
                }

                ((System.Collections.Specialized.INotifyCollectionChanged)(_originalQuery = ObjectStoreManager.DefaultObjectStore.GetQueryable<T>().Where(
                                Expression.Lambda<Func<T, bool>>(
                                    Expression.Equal(
                                        propertyAccess,
                                        Expression.Constant(_originalValue)), paramT)).Take(1))).CollectionChanged += CollectionChangedHandler;
            }

            _uncommittedValue = default(T1);
            _isUncommitted = false;
            _isOriginalSetted = true;

            if (withUndo)
            {
                _isChanged = false;
                _changedValue = default(T);
            }
            return true;
        }

        private void CollectionChangedHandler(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
            {
                _originalConvertedValue = e.NewItems.Cast<T>().FirstOrDefault();
                _isOriginalConvertedSetted = true;
                ((System.Collections.Specialized.INotifyCollectionChanged)_originalQuery).CollectionChanged -= CollectionChangedHandler;
            }
        }

        public void Rollback()
        {
            if (_isUncommitted)
            {
                _uncommittedValue = default(T1);
                _isUncommitted = false;
            }
        }

        public void Undo()
        {
            _changedValue = default(T);
            _isChanged = false;
        }
        #endregion
    }
}
