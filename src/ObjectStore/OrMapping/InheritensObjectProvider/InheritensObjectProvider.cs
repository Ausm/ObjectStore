using ObjectStore.Interfaces;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
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
        public static DataBaseWorkerQueue GetQueue(string connectionStringName)
        {
            lock (_instances)
            {
                return _instances.ContainsKey(connectionStringName) ? _instances[connectionStringName] : _instances[connectionStringName] = new DataBaseWorkerQueue(connectionStringName);
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

        static Dictionary<string, DataBaseWorkerQueue> _instances = new Dictionary<string, DataBaseWorkerQueue>();
        string _connectionStringName;
        Thread _workerThread;
        List<Tuple<AsyncResult, SqlCommand, Func<SqlDataReader, IAsyncResult, ICommitContext>>> _queue = new List<Tuple<AsyncResult, SqlCommand, Func<SqlDataReader, IAsyncResult, ICommitContext>>>();

        private DataBaseWorkerQueue(string connectionString)
        {
            _connectionStringName = connectionString;
        }

        public IAsyncResult EnqueueCommand(object context, SqlCommand command, Func<SqlDataReader, IAsyncResult, ICommitContext> fillAction)
        {
            Tuple<AsyncResult, SqlCommand, Func<SqlDataReader, IAsyncResult, ICommitContext>> entry = new Tuple<AsyncResult, SqlCommand, Func<SqlDataReader, IAsyncResult, ICommitContext>>(new AsyncResult(context), command, fillAction);

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
            SqlConnection connection = ConnectionProvider.Instance.GetConnection(_connectionStringName);
            try
            {
                if (connection.State == System.Data.ConnectionState.Closed)
                    connection.Open();
                while (true)
                {
                    List<Tuple<AsyncResult, SqlCommand, Func<SqlDataReader, IAsyncResult, ICommitContext>>> queue;
                    lock (this)
                    {
                        if (_queue.Count == 0)
                            return;

                        queue = _queue;
                        _queue = new List<Tuple<AsyncResult, SqlCommand, Func<SqlDataReader, IAsyncResult, ICommitContext>>>();
                    }

                    SqlCommand command = queue[0].Item2;
                    for (int i = 1; i < queue.Count; i++)
                    {
                        command.CommandText += ";" + queue[i].Item2.CommandText;
                        command.Parameters.AddRange(queue[i].Item2.Parameters.OfType<SqlParameter>().Select(x => new SqlParameter(x.ParameterName, x.Value)).ToArray());
                    }

                    List<ICommitContext> commitContexts = new List<ICommitContext>(queue.Count);

                    using (command)
                    {
                        command.Connection = connection;
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            for (int i = 0; i < queue.Count; i++)
                            {
                                commitContexts.Add(queue[i].Item3(reader, queue[i].Item1));
                                queue[i].Item1.SetCompleted();
                                reader.NextResult();
                            }
                        }
                    }

                    foreach (ICommitContext context in commitContexts.Where(x => x != null))
                        context.Commit();

                    lock(this)
                    {
                        if (_queue.Count == 0)
                            return;
                    }
                }
            }
            finally
            {
                _workerThread = null;
                ConnectionProvider.Instance.ReleaseConnection(connection);
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
                _queue = DataBaseWorkerQueue.GetQueue(_connectionStringName = connectionStringName);
            }

            private SqlConnection GetConnection()
            {
                SqlConnection connection = ConnectionProvider.Instance.GetConnection(_connectionStringName);
                if (connection.State == System.Data.ConnectionState.Closed)
                    connection.Open();
                return connection;
            }

            public IAsyncResult FillCacheAsync(QueryProvider provider)
            {
                QueryContext context = provider.Context.FillCacheContext;
                if (context.Load)
                    return null;

                SelectCommandBuilder commandBuilder = _objectProvider._mappingInfoContainer.FillCommand(new SelectCommandBuilder());
                context.PrepareSelectCommand(commandBuilder);
                context.SetLoaded();

                return _queue.EnqueueCommand(provider, commandBuilder.GetSqlCommand(), (reader, result) => _objectProvider._cache.Fill(reader, x => _objectProvider._mappingInfoContainer.GetKeyValues(x), () => (T)_objectProvider._mappingInfoContainer.CreateObject(), ((QueryProvider)result.AsyncState).Context.FillCacheContext));
            }

            public void FillCache(QueryContext context)
            {
                if (!context.Load)
                    return;

                SelectCommandBuilder commandBuilder = _objectProvider._mappingInfoContainer.FillCommand(new SelectCommandBuilder());
                context.PrepareSelectCommand(commandBuilder);

                using (SqlCommand command = commandBuilder.GetSqlCommand())
                {
                    command.Connection = GetConnection();
                    try
                    {
                        ICommitContext commitContext;
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            MappingInfo mappingInfo = _objectProvider._mappingInfoContainer;
                            commitContext = _objectProvider._cache.Fill(reader, x => mappingInfo.GetKeyValues(x), () => (T)mappingInfo.CreateObject(), context);
                        }
                        if (commitContext != null) commitContext.Commit();
                    }
                    finally
                    {
                        ConnectionProvider.Instance.ReleaseConnection(command.Connection);
                    }
                }
                context.SetLoaded();
            }

            public void SaveObjects(
                IEnumerable<IFillAbleObject> items,
                Func<IFillAbleObject, ISqlCommandBuilder> getBuilder,
                Action<IFillAbleObject, SqlDataReader> refill,
                Action<IFillAbleObject> afterFill)
            {
                SqlConnection connection = GetConnection();
                try
                {
                    foreach (IFillAbleObject item in items.ToList())
                    {
                        try
                        {

                            ISqlCommandBuilder commandBuilder = getBuilder(item);
                            if (commandBuilder == null) continue;

                            item.FillCommand(commandBuilder);
                            using (SqlCommand command = commandBuilder.GetSqlCommand())
                            {
                                command.Connection = connection;
                                using (SqlDataReader reader = command.ExecuteReader())
                                {
                                    refill(item, reader.Read() ? reader : null);
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
                    ConnectionProvider.Instance.ReleaseConnection(connection);
                }
            }
        }
        #endregion

        #region Membervariablen
        string _connectionString;

        MappingInfo _mappingInfoContainer;
        WeakCache _cache;
        DataBaseWorker _dbWorker;
        #endregion

        #region Konstruktoren
        public InheritensObjectProvider(string connectionString)
        {
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

        private SqlConnection GetConnection()
        {
            SqlConnection returnValue = new SqlConnection();
            returnValue.ConnectionString = _connectionString;
            returnValue.Open();
            return returnValue;
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
