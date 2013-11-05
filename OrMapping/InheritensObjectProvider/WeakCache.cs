using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Linq.Expressions;
using System.Data;

namespace Ausm.ObjectStore.OrMapping
{
    public partial class InheritensObjectProvider<T> : IObjectProvider where T : class
    {
        private class WeakCache
        {
            public WeakCache(bool keepFullLoaded)
            {
                _keepFullLoaded = keepFullLoaded;
                _itemsByKey = new Dictionary<MappedObjectKeys, WeakReference<IFillAbleObject>>();
                _enumerables = new Dictionary<QueryContext, WeakReference<ContextEnumerable>>();
                _changedHandlers = new Dictionary<string, List<Action<IFillAbleObject>>>();
            }

            bool _keepFullLoaded;
            ContextEnumerable _baseKeptEnumerable;
            Dictionary<MappedObjectKeys, WeakReference<IFillAbleObject>> _itemsByKey;
            Dictionary<QueryContext, WeakReference<ContextEnumerable>> _enumerables;
            Dictionary<string, List<Action<IFillAbleObject>>> _changedHandlers;

            #region Subclasses
            public class ContextEnumerable : IEnumerable<T>, System.Collections.Specialized.INotifyCollectionChanged
            {
                #region Subclasses
                private class RollbackCommitContext : IRollbackCommitContext
                {
                    ContextEnumerable _contextEnumerable;
                    List<IFillAbleObject> _objectsToCommit;
                    List<T> _entriesToAdd;

                    public RollbackCommitContext(ContextEnumerable contextEnumerable)
                    {
                        _contextEnumerable = contextEnumerable;
                        _objectsToCommit = new List<IFillAbleObject>();
                        _entriesToAdd = new List<T>();
                    }

                    public void AddEntryToAdd(T entry)
                    {
                        if (_entriesToAdd != null)
                            _entriesToAdd.Add(entry);
                    }

                    public void AddItemToCommit(IFillAbleObject item)
                    {
                        if (_objectsToCommit != null)
                            _objectsToCommit.Add(item);
                    }

                    #region IRollbackCommitContext Members

                    public void Commit(bool unDoChanges)
                    {
                        try
                        {
                            _contextEnumerable._disableReportEntryValueChanged = true;
                            foreach (IFillAbleObject obj in _objectsToCommit)
                                obj.Commit(unDoChanges);
                            foreach (T entry in _entriesToAdd)
                                _contextEnumerable.AppendEntry(entry);
                        }
                        finally
                        {
                            _contextEnumerable._disableReportEntryValueChanged = false;
                        }
                    }

                    public void Rollback()
                    {
                    }

                    #endregion

                    #region IDisposable Members

                    public void Dispose()
                    {
                    }

                    #endregion
                }
                #endregion

                #region MemberVariablen
                QueryContext _context;
                List<T> _items;
                List<T> _deletedItems;
                int _expectedNextInsertIndex;
                bool _disableReportEntryValueChanged;

                WeakCache _cache;
                static Dictionary<string, IEnumerable<IFillAbleObject>> _objectsToCommit = new Dictionary<string, IEnumerable<IFillAbleObject>>();
                static bool _isWeakCacheExceptionSent = false;
                #endregion

                #region Construktor
                public ContextEnumerable(QueryContext context, WeakCache cache)
                {
                    _disableReportEntryValueChanged = false;
                    _context = context;
                    _cache = cache;

                    foreach (string propertyName in _context.PredicateRelatedProperties)
                    {
                        if (!_cache._changedHandlers.ContainsKey(propertyName))
                            _cache._changedHandlers.Add(propertyName, new List<Action<IFillAbleObject>>());

                        _cache._changedHandlers[propertyName].Add(this.ReportEntryValueChanged);
                    }
                }

                ~ContextEnumerable()
                {
                    try
                    {
                        foreach (string propertyName in _context.PredicateRelatedProperties)
                        {
                            if (_cache._changedHandlers.ContainsKey(propertyName))
                                _cache._changedHandlers[propertyName].Remove(this.ReportEntryValueChanged);
                        }
                    }
                    catch
                    {
                    }
                }


                #endregion

                #region IEnumerable<T> Members

                public IEnumerator<T> GetEnumerator()
                {
                    return Enumerate().GetEnumerator();
                }

                #endregion

                #region IEnumerable Members

                System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
                {
                    return GetEnumerator();
                }

                #endregion

                #region Properties
                public bool Changed
                {
                    get
                    {
                        Init(false);
                        return _deletedItems.Count > 0 ||
                            _items.Cast<IFillAbleObject>().Any(x => x.State != State.Original) ||
                            _items.Cast<IFillAbleObject>().Any(x => x.CheckChildObjectsChanged());
                    }
                }
                #endregion

                #region Methoden
                public void Init(bool changedOnly)
                {
                    if (_items != null)
                        return;

                    Func<T, bool> predicate = _context.GetPredicateCompiled();
                    _items = changedOnly ?
                        _cache._itemsByKey.Values.Where(x => x.Value != null && x.Value.State != State.Original && predicate((T)x.Value)).Select(x => x.Value).Cast<T>().ToList() :
                        _cache._itemsByKey.Values.Where(x => x.Value != null && predicate((T)x.Value)).Select(x => x.Value).Cast<T>().ToList();

                    _deletedItems = _items.Where(x => ((IFillAbleObject)x).State == State.Deleted).ToList();
                    _items = _items.Where(x => ((IFillAbleObject)x).State != State.Deleted).ToList();

                    if (_context.OrderExpressions.Count > 0)
                    {
                        IOrderedEnumerable<T> enumerable = null;
                        foreach (QueryContext.IOrderItem item in _context.OrderExpressions)
                        {
                            if (enumerable == null)
                                enumerable = item.SetOrder(_items);
                            else
                                enumerable = item.SetOrder(enumerable);
                        }
                        _items = enumerable.ToList();
                        _expectedNextInsertIndex = 0;
                    }
                    else
                    {
                        _expectedNextInsertIndex = _items.Count;
                    }
                    foreach (System.ComponentModel.INotifyPropertyChanged item in _items.Union(_deletedItems))
                        item.PropertyChanged += ItemsNotifyPropertyChanged;
                }

                public void Delete()
                {
                    Init(false);
                    foreach (IFillAbleObject item in _items.ToList())
                        item.Delete();
                }

                private void ItemsNotifyPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
                {
                    if (e is StateChangedEventArgs && sender is T)
                    {
                        T item = (T)sender;
                        int index;
                        StateChangedEventArgs stateChangedEventArgs = (StateChangedEventArgs)e;
                        if (stateChangedEventArgs.OldState == State.Deleted)
                        {
                            _deletedItems.Remove(item);
                            if (stateChangedEventArgs.NewState != State.NotAttached)
                                AppendEntry(item);
                        }
                        else if (stateChangedEventArgs.NewState == State.NotAttached)
                        {
                            if ((index = _items.IndexOf(item)) == -1)
                                return;

                            _items.RemoveAt(index);
                            OnCollectionChanged(new System.Collections.Specialized.NotifyCollectionChangedEventArgs(System.Collections.Specialized.NotifyCollectionChangedAction.Remove, item, index));
                        }
                        else if (stateChangedEventArgs.NewState == State.Deleted)
                        {
                            if ((index = _items.IndexOf(item)) == -1)
                                return;

                            _items.RemoveAt(index);
                            OnCollectionChanged(new System.Collections.Specialized.NotifyCollectionChangedEventArgs(System.Collections.Specialized.NotifyCollectionChangedAction.Remove, item, index));
                            _deletedItems.Add(item);
                        }
                    }
                }

                public void ReportEntryValueChanged(IFillAbleObject item)
                {
                    if (_disableReportEntryValueChanged)
                        return;

                    Init(false);

                    T obj = (T)item;
                    bool match;
                    try
                    {
                        match = _context.GetPredicateCompiled()(obj);
                    }
                    catch (Exception ex)
                    {
                        //#if DEBUG Wird nun auch wieder in der Release versendet um festzustellen ob die Optimierung des CompiledPredicate ihren Zweck erfüllt, jedoch nur mehr einmal per Clientstart
                        if (!_isWeakCacheExceptionSent)
                        {
                            _isWeakCacheExceptionSent = true;
                            System.Diagnostics.Trace.TraceError("Error while evaluating Predicate in PropertyChanged event. Predicate: {0}\nException:\n{1}", _context.GetPredicate(), ex);
                        }
                        else
                        {
                            throw;
                        }
                        //#endif
                        match = false;
                    }
                    bool contains = item.State == State.Deleted ? _deletedItems.Contains(obj) : _items.Contains(obj);
                    if (match && !contains)
                    {
                        AppendEntry(obj);
                    }
                    else if (!match && contains)
                    {
                        if (((IFillAbleObject)item).State == State.Deleted)
                        {
                            if (_deletedItems.Remove(obj))
                                ((System.ComponentModel.INotifyPropertyChanged)item).PropertyChanged -= ItemsNotifyPropertyChanged;
                            return;
                        }
                        else
                        {
                            int index = _items.IndexOf(obj);
                            if (index == -1)
                                return;

                            _items.RemoveAt(index);
                            ((System.ComponentModel.INotifyPropertyChanged)item).PropertyChanged -= ItemsNotifyPropertyChanged;
                            OnCollectionChanged(new System.Collections.Specialized.NotifyCollectionChangedEventArgs(System.Collections.Specialized.NotifyCollectionChangedAction.Remove, item, index));
                        }
                    }
                }

                public void Update(DataBaseWorker dbWorker)
                {
                    Init(false);

                    List<IFillAbleObject> dropCommitEntries = new List<IFillAbleObject>(_items.Count);
                    List<IFillAbleObject> deattach = new List<IFillAbleObject>(_items.Count);
                    try
                    {
                        dbWorker.SaveObjects(_items.Union(_deletedItems).Cast<IFillAbleObject>(),
                                x =>
                                {
                                    switch (((IFillAbleObject)x).State)
                                    {
                                        case State.Created:
                                            return new InsertCommandBuilder();
                                        case State.Changed:
                                            return new UpdateCommandBuilder();
                                        case State.Deleted:
                                            x.DeleteChildObjects();
                                            x.SaveChildObjects();
                                            return new DeleteCommandBuilder();
                                        case State.Original:
                                            x.SaveChildObjects();
                                            return null;
                                        default:
                                            return null;
                                    }
                                },
                                (x, reader) =>
                                {
                                    switch (x.State)
                                    {
                                        case State.Changed:
                                        case State.Created:
                                            if (reader == null) x.Deattach();
                                            else x.Fill(reader);
                                            break;
                                        default:
                                            break;
                                    }
                                },
                                x =>
                                {
                                    switch (x.State)
                                    {
                                        case State.Changed:
                                        case State.Created:
                                            dropCommitEntries.Add(x);
                                            x.SaveChildObjects();
                                            break;
                                        case State.Deleted:
                                            x.Deattach();
                                            break;
                                        default:
                                            x.SaveChildObjects();
                                            break;
                                    }

                                }
                                );
                        if (System.Transactions.Transaction.Current == null)
                        {
                            foreach (IFillAbleObject obj in dropCommitEntries)
                                obj.Commit(true);
                        }
                        else
                        {
                            string transactionLocalIdentifier = System.Transactions.Transaction.Current.TransactionInformation.LocalIdentifier;
                            if (_objectsToCommit.ContainsKey(transactionLocalIdentifier))
                                _objectsToCommit[transactionLocalIdentifier] = _objectsToCommit[transactionLocalIdentifier].Union(dropCommitEntries);
                            else
                            {
                                _objectsToCommit.Add(transactionLocalIdentifier, dropCommitEntries);
                                System.Transactions.Transaction.Current.TransactionCompleted +=
                                    (s, e) =>
                                    {
                                        switch (e.Transaction.TransactionInformation.Status)
                                        {
                                            case System.Transactions.TransactionStatus.Aborted:
                                                foreach (IFillAbleObject obj in _objectsToCommit[e.Transaction.TransactionInformation.LocalIdentifier])
                                                    obj.Rollback();
                                                break;
                                            case System.Transactions.TransactionStatus.Committed:
                                                foreach (IFillAbleObject obj in _objectsToCommit[e.Transaction.TransactionInformation.LocalIdentifier])
                                                    obj.Commit(true);
                                                break;
                                            default:
                                                break;
                                        }
                                        _objectsToCommit.Remove(e.Transaction.TransactionInformation.LocalIdentifier);
                                    };
                            }
                        }
                    }
                    catch (EntitySaveException)
                    {
                        foreach (IFillAbleObject obj in dropCommitEntries)
                            obj.Rollback();

                        throw;
                    }
                }

                public IRollbackCommitContext Fill(IDataReader reader, Func<IDataReader, MappedObjectKeys> getKeyFunction, Func<T> createObject)
                {
                    Init(true);

                    Dictionary<MappedObjectKeys, WeakReference<IFillAbleObject>> entriesByKey = _cache._itemsByKey;
                    RollbackCommitContext rollbackCommitContext = new RollbackCommitContext(this);
                    while (reader.Read())
                    {
                        MappedObjectKeys keys = getKeyFunction(reader);
                        if (entriesByKey.ContainsKey(keys))
                        {
                            IFillAbleObject item = entriesByKey[keys].Value;
                            if (item == null)
                            {
                                entriesByKey[keys] = new WeakReference<IFillAbleObject>(item = (IFillAbleObject)createObject());
                                ((System.ComponentModel.INotifyPropertyChanged)item).PropertyChanged += _cache.EntryValuePropertyChanged;
                            }
                            item.Fill(reader);
                            rollbackCommitContext.AddItemToCommit(item);
                            rollbackCommitContext.AddEntryToAdd((T)item);
                        }
                        else
                        {
                            IFillAbleObject newObject = (IFillAbleObject)createObject();
                            newObject.Fill(reader);
                            entriesByKey.Add(keys, new WeakReference<IFillAbleObject>(newObject));
                            rollbackCommitContext.AddItemToCommit(newObject);
                            rollbackCommitContext.AddEntryToAdd((T)newObject);
                            ((System.ComponentModel.INotifyPropertyChanged)newObject).PropertyChanged += _cache.EntryValuePropertyChanged;
                        }
                    }

                    return rollbackCommitContext;
                }

                public void DropChanges()
                {
                    Init(false);

                    List<T> items = _items.Union(_deletedItems).ToList();

                    foreach (IFillAbleObject item in items)
                        item.DropChanges();

                    foreach (IFillAbleObject item in items)
                        item.DropChangesChildObjects();
                }

                #region Private

                private void AppendEntry(T item)
                {
                    Init(false);
                    IFillAbleObject fillableObject = (IFillAbleObject)item;

                    if (fillableObject.State == State.Deleted)
                    {
                        if (_deletedItems.Contains(item))
                            return;

                        _deletedItems.Add(item);
                        ((System.ComponentModel.INotifyPropertyChanged)item).PropertyChanged += ItemsNotifyPropertyChanged;
                    }
                    else
                    {
                        if (_items.Contains(item))
                            return;

                        ((System.ComponentModel.INotifyPropertyChanged)item).PropertyChanged += ItemsNotifyPropertyChanged;
                        if (_context.OrderExpressions == null || _context.OrderExpressions.Count == 0)
                            InsertAt(_items.Count, item);
                        else
                        {
                            #region Ordered Insert
                            if (_items.Count == 0)
                            {
                                InsertAt(0, item);
                                _expectedNextInsertIndex = 1;
                            }
                            else if (
                                (_items.Count == _expectedNextInsertIndex && _context.Compare(item, _items[_expectedNextInsertIndex - 1]) >= 0) ||
                                (_expectedNextInsertIndex == 0 && _context.Compare(item, _items[0]) <= 0) ||
                                (_expectedNextInsertIndex < _items.Count && _expectedNextInsertIndex > 0 &&
                                    _context.Compare(item, _items[_expectedNextInsertIndex]) <= 0 &&
                                    _context.Compare(item, _items[_expectedNextInsertIndex - 1]) >= 0)
                                )
                            {
                                InsertAt(_expectedNextInsertIndex++, item);
                            }
                            else
                            {
                                Action<int, int> binarySearch = null;
                                binarySearch = (min, max) =>
                                    {
                                        int spread = max - min;
                                        if (spread == 0)
                                            InsertAt(_expectedNextInsertIndex = (_context.Compare(item, _items[min]) < 0 ? min : ++min), item);
                                        else if (spread == 1)
                                            InsertAt(_expectedNextInsertIndex = (_context.Compare(item, _items[min]) < 0 ? min : _context.Compare(item, _items[max]) < 0 ? max : ++max), item);
                                        else
                                        {
                                            int mid = min + (spread / 2);
                                            int compare = _context.Compare(item, _items[mid]);
                                            if (compare == 0)
                                                InsertAt(_expectedNextInsertIndex = mid, item);
                                            else if (compare > 0)
                                                binarySearch(mid, max);
                                            else
                                                binarySearch(min, mid);

                                        }
                                    };
                                binarySearch(0, _items.Count - 1);

                            }
                            #endregion
                        }
                    }
                }

                private void InsertAt(int index, T item)
                {
                    _expectedNextInsertIndex = index + 1;
                    if (_items.Count == 0)
                    {
                        _items.Add(item);
                        _expectedNextInsertIndex = index = 1;
                    }
                    else if (_items.Count == index)
                        _items.Add(item);
                    else if (_items[index] != item)
                        _items.Insert(index, item);
                    else return;
                    OnCollectionChanged(new System.Collections.Specialized.NotifyCollectionChangedEventArgs(System.Collections.Specialized.NotifyCollectionChangedAction.Add, item, index));
                }

                private IEnumerable<T> Enumerate()
                {
                    Init(false);
                    return _context.TopCount.HasValue ? _items.Take(_context.TopCount.Value) : _items.AsReadOnly();
                }
                #endregion
                #endregion

                #region INotifyCollectionChanged Members

                protected virtual void OnCollectionChanged(System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
                {
                    if (CollectionChanged != null)
                        try
                        {
                            CollectionChanged(this, e);
                        }
                        catch (Exception ex)
                        {
//#if DEBUG
                            System.Diagnostics.Trace.TraceError("Error in  CollectionChanged-Call.\nException:\n{0}", ex);
//#else
                            try
                            {
                                CollectionChanged(this, new System.Collections.Specialized.NotifyCollectionChangedEventArgs(System.Collections.Specialized.NotifyCollectionChangedAction.Reset));
                            } catch{}
//#endif
                        }
                }

                public event System.Collections.Specialized.NotifyCollectionChangedEventHandler CollectionChanged;

                #endregion
            }
            #endregion

            public ContextEnumerable CreateEnumerable(QueryContext context)
            {
                ContextEnumerable contextEnumerable;
                if (_enumerables.ContainsKey(context))
                {
                    contextEnumerable = _enumerables[context];
                    if (contextEnumerable != null)
                        return contextEnumerable;
                }
                if (_baseKeptEnumerable == null && _keepFullLoaded && context == QueryContext.BaseContext)
                    contextEnumerable = _baseKeptEnumerable = new ContextEnumerable(context, this);
                else
                    contextEnumerable = new ContextEnumerable(context, this);

                if (_enumerables.ContainsKey(context))
                    _enumerables[context] = new WeakReference<ContextEnumerable>(contextEnumerable);
                else
                    _enumerables.Add(context, new WeakReference<ContextEnumerable>(contextEnumerable));

                return contextEnumerable;
            }

            #region EntryEventHandlers
            private void EntryValuePropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
            {
                if (sender is IFillAbleObject)
                {
                    IFillAbleObject fillableObject = (IFillAbleObject)sender;
                    if (e is StateChangedEventArgs)
                    {
                        StateChangedEventArgs e2 = (StateChangedEventArgs)e;
                        if (e2.NewState == State.NotAttached)
                            _itemsByKey.Remove(e2.OldState == State.Created ? new MappedObjectKeys(new object[]{fillableObject}) : fillableObject.Keys);
                        else if (e2.OldState == State.Created)
                        {
                            _itemsByKey.Remove(new MappedObjectKeys(new object[]{fillableObject}));
                            _itemsByKey.Add(fillableObject.Keys, new WeakReference<IFillAbleObject>(fillableObject));
                        }
                    }
                    else if (string.IsNullOrEmpty(e.PropertyName))
                    {
                        foreach (ContextEnumerable enumerable in _enumerables.Values.Select(x => (ContextEnumerable)x).Where(x => x != null).ToList())
                            enumerable.ReportEntryValueChanged(fillableObject);
                    }
                    else if (_changedHandlers.ContainsKey(e.PropertyName))
                    {
                        foreach (Action<IFillAbleObject> handler in _changedHandlers[e.PropertyName])
                            handler(fillableObject);
                    }
                }
            }
            #endregion

            #region ICache Members
            public IRollbackCommitContext Fill(IDataReader reader, Func<IDataReader, MappedObjectKeys> getKeyFunction, Func<T> createObject, QueryContext context)
            {
                return CreateEnumerable(context).Fill(reader, getKeyFunction, createObject);
            }

            public void AddNew(object item)
            {
                ((System.ComponentModel.INotifyPropertyChanged)item).PropertyChanged += EntryValuePropertyChanged;
                _itemsByKey.Add(new MappedObjectKeys(new object[] { item }), new WeakReference<IFillAbleObject>((IFillAbleObject)item));
                EntryValuePropertyChanged(item, new System.ComponentModel.PropertyChangedEventArgs(null));
            }
            #endregion
        }
    }
}
