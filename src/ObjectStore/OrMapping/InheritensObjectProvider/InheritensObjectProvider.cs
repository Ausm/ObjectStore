﻿using ObjectStore.Interfaces;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading;

namespace ObjectStore.OrMapping
{
    internal interface ICommitContext : IDisposable
    {
        void Commit();
    }

    internal class DataBaseWorkerQueue
    {
        public static DataBaseWorkerQueue GetQueue(string connectionStringName, IDataBaseProvider dataBaseProvider)
        {
            lock (_instances)
            {
                DataBaseWorkerQueue returnValue;
                if (!_instances.ContainsKey(dataBaseProvider))
                    _instances.Add(dataBaseProvider, new DataBaseWorkerQueue[] { returnValue = new DataBaseWorkerQueue(connectionStringName, dataBaseProvider) }.ToDictionary(x => connectionStringName));
                else if (!_instances[dataBaseProvider].ContainsKey(connectionStringName))
                    _instances[dataBaseProvider].Add(connectionStringName, returnValue = new DataBaseWorkerQueue(connectionStringName, dataBaseProvider));
                else
                    returnValue = _instances[dataBaseProvider][connectionStringName];

                return returnValue;
            }
        }

        private class AsyncResult : IAsyncResult
        {
            object _context;
            ManualResetEvent _waitHandle;
            bool _isCompleted;

            public AsyncResult(object context)
            {
                _context = context;
                _waitHandle = new ManualResetEvent(false);
                _isCompleted = false;
            }

            public void SetCompleted()
            {
                if (!_isCompleted)
                {
                    _waitHandle.Set();
                    _isCompleted = true;
                }
            }

            #region IAsyncResult Members

            object IAsyncResult.AsyncState
            {
                get { return _context; }
            }

            public WaitHandle AsyncWaitHandle
            {
                get { return _waitHandle; }
            }

            public bool CompletedSynchronously
            {
                get { return false; }
            }

            public bool IsCompleted
            {
                get { return _isCompleted; }
            }

            #endregion
        }

        static Dictionary<IDataBaseProvider, Dictionary<string, DataBaseWorkerQueue>> _instances = new Dictionary<IDataBaseProvider, Dictionary<string, DataBaseWorkerQueue>>();
        string _connectionStringName;
        IDataBaseProvider _dataBaseProvider;
        Thread _workerThread;
        List<Tuple<AsyncResult, DbCommand, Func<IValueSource, IAsyncResult, ICommitContext>>> _queue = new List<Tuple<AsyncResult, DbCommand, Func<IValueSource, IAsyncResult, ICommitContext>>>();

        private DataBaseWorkerQueue(string connectionString, IDataBaseProvider dataBaseProvider)
        {
            _connectionStringName = connectionString;
            _dataBaseProvider = dataBaseProvider;
        }

        public IAsyncResult EnqueueCommand(object context, DbCommand command, Func<IValueSource, IAsyncResult, ICommitContext> fillAction)
        {
            Tuple<AsyncResult, DbCommand, Func<IValueSource, IAsyncResult, ICommitContext>> entry = new Tuple<AsyncResult, DbCommand, Func<IValueSource, IAsyncResult, ICommitContext>>(new AsyncResult(context), command, fillAction);

            lock (this)
            {
                _queue.Add(entry);
                StartThread();
                return entry.Item1;
            }
        }

        private void StartThread()
        {
            lock (this)
            {
                if (_workerThread == null || !_workerThread.IsAlive)
                    (_workerThread = new Thread(ThreadFunction) { IsBackground = true }).Start();
            }
        }

        private void ThreadFunction()
        {
            DbConnection connection = _dataBaseProvider.GetConnection(_connectionStringName);
            try
            {
                if (connection.State == System.Data.ConnectionState.Closed)
                    connection.Open();
                while (true)
                {
                    List<Tuple<AsyncResult, DbCommand, Func<IValueSource, IAsyncResult, ICommitContext>>> queue;
                    lock (this)
                    {
                        if (_queue.Count == 0)
                            return;

                        queue = _queue;
                        _queue = new List<Tuple<AsyncResult, DbCommand, Func<IValueSource, IAsyncResult, ICommitContext>>>();
                    }

                    try
                    {
                        List<ICommitContext> commitContexts = new List<ICommitContext>(queue.Count);

                        using (DbCommand command = _dataBaseProvider.CombineCommands(queue.Select(x => x.Item2)))
                        {
                            command.Connection = connection;
                            using (IValueSource valueSource = _dataBaseProvider.GetValueSource(command))
                            {
                                for (int i = 0; i < queue.Count; i++)
                                {
                                    commitContexts.Add(queue[i].Item3(valueSource, queue[i].Item1));
                                }
                            }
                        }

                        foreach (ICommitContext context in commitContexts.Where(x => x != null))
                            context.Commit();
                    }
                    finally
                    {
                        foreach (AsyncResult result in queue.Select(x => x.Item1))
                            result.SetCompleted();
                    }

                    lock (this)
                    {
                        if (_queue.Count == 0)
                            return;
                    }
                }
            }
            finally
            {
                _workerThread = null;
                _dataBaseProvider.ReleaseConnection(connection);
            }
        }
    }

    public partial class InheritensObjectProvider<T> : IObjectProvider where T : class
    {
        #region Subklassen
        private class DataBaseWorker
        {
            string _connectionStringName;
            InheritensObjectProvider<T> _objectProvider;
            DataBaseWorkerQueue _queue;

            public DataBaseWorker(string connectionStringName, InheritensObjectProvider<T> objectProvider)
            {
                _objectProvider = objectProvider;
                _queue = DataBaseWorkerQueue.GetQueue(_connectionStringName = connectionStringName, objectProvider._databaseProvider);
            }

            private DbConnection GetConnection()
            {
                DbConnection connection = _objectProvider._databaseProvider.GetConnection(_connectionStringName);
                if (connection.State == System.Data.ConnectionState.Closed)
                    connection.Open();
                return connection;
            }

            public IAsyncResult FillCacheAsync(QueryProvider provider)
            {
                QueryContext context = provider.Context.FillCacheContext;
                if (context.ForceLoad)
                    return null;

                IModifyableCommandBuilder commandBuilder = _objectProvider._mappingInfoContainer.FillCommand(_objectProvider._databaseProvider.GetSelectCommandBuilder());
                context.PrepareSelectCommand(commandBuilder);
                context.SetLoaded();

                return _queue.EnqueueCommand(provider, commandBuilder.GetDbCommand(), (valueSource, result) => _objectProvider._cache.Fill(valueSource, x => _objectProvider._mappingInfoContainer.GetKeyValues(x), () => (T)_objectProvider._mappingInfoContainer.CreateObject(), ((QueryProvider)result.AsyncState).Context.FillCacheContext));
            }

            public void FillCache(QueryContext context)
            {
                if (!context.Load)
                    return;

                IModifyableCommandBuilder commandBuilder = _objectProvider._mappingInfoContainer.FillCommand(_objectProvider._databaseProvider.GetSelectCommandBuilder());
                context.PrepareSelectCommand(commandBuilder);

                using (DbCommand command = commandBuilder.GetDbCommand())
                {
                    command.Connection = GetConnection();
                    try
                    {
                        ICommitContext commitContext;
                        using (IValueSource valueSource = _objectProvider._databaseProvider.GetValueSource(command))
                        {
                            MappingInfo mappingInfo = _objectProvider._mappingInfoContainer;
                            commitContext = _objectProvider._cache.Fill(valueSource, x => mappingInfo.GetKeyValues(x), () => (T)mappingInfo.CreateObject(), context);
                        }
                        if (commitContext != null) commitContext.Commit();
                    }
                    finally
                    {
                        _objectProvider._databaseProvider.ReleaseConnection(command.Connection);
                    }
                }
                context.SetLoaded();
            }

            public void SaveObjects(
                IEnumerable<IFillAbleObject> items,
                Func<IFillAbleObject, ICommandBuilder> getBuilder,
                Action<IFillAbleObject, IValueSource> refill,
                Action<IFillAbleObject> afterFill)
            {
                DbConnection connection = GetConnection();
                try
                {
                    foreach (IFillAbleObject item in items.ToList())
                    {
                        try
                        {

                            ICommandBuilder commandBuilder = getBuilder(item);
                            if (commandBuilder == null) continue;

                            item.FillCommand(commandBuilder);
                            using (DbCommand command = commandBuilder.GetDbCommand())
                            {
                                command.Connection = connection;
                                using (IValueSource valueSource = _objectProvider._databaseProvider.GetValueSource(command))
                                {
                                    refill(item, valueSource.Next() ? valueSource : null);
                                }
                            }
                            afterFill(item);
                        }
                        catch (Exception ex)
                        {
                            if (ex is EntitySaveException)
                                throw;
                            else
                                throw new EntitySaveException(item, ex);
                        }
                    }
                }
                finally
                {
                    _objectProvider._databaseProvider.ReleaseConnection(connection);
                }
            }
        }
        #endregion

        #region Membervariablen
        string _connectionString;

        MappingInfo _mappingInfoContainer;
        WeakCache _cache;
        DataBaseWorker _dbWorker;
        IDataBaseProvider _databaseProvider;
        #endregion

        #region Konstruktoren
        public InheritensObjectProvider(string connectionString, IDataBaseProvider databaseProvider)
        {
            _databaseProvider = databaseProvider;
            _connectionString = connectionString;
            InitializeMapping();
        }
        #endregion

        #region Funktionen
        #region Privat
        private void InitializeMapping()
        {
            if (_mappingInfoContainer == null)
            {
                _mappingInfoContainer = MappingInfo.GetMappingInfo(typeof(T));
                _dbWorker = new DataBaseWorker(_connectionString, this);
                _cache = new WeakCache(_mappingInfoContainer.LoadBehavior == LoadBehavior.OnFirstAccessFullLoad);
            }
        }
        #endregion

        #region IObjectProvider Members
        public bool SupportsType(Type type)
        {
            return type == typeof(T);
        }

        public IQueryable<T1> GetQueryable<T1>() where T1 : class
        {
            if (typeof(T1) != typeof(T))
            {
                return null;
            }
            return Queryable.Create(this, null) as IQueryable<T1>;
        }

        public T1 CreateObject<T1>() where T1 : class
        {
            object value = _mappingInfoContainer.CreateObject();
            _cache.AddNew(value);
            return value as T1;
        }

        #endregion
        #endregion
    }

    public class EntitySaveException : Exception
    {
        object _entity;

        public EntitySaveException(object entity, Exception innerException)
            : base("Saving entity failed.", innerException)
        {
            _entity = entity;
        }

        public object Entity
        {
            get
            {
                return _entity;
            }
        }
    }
}
