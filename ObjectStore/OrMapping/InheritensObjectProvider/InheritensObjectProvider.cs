using System;
using System.Collections.Generic;
using System.Text;
using System.Data.SqlClient;
using System.Reflection;
using System.Linq.Expressions;
using System.Linq;
using System.Threading;
using System.Runtime.Serialization;

namespace Ausm.ObjectStore.OrMapping
{
    internal static class ConnectionManager
    {
        class ReferencedConnection
        {
            public ReferencedConnection(string connectionString)
            {
                _connectionString = connectionString;
                _connection = null;
                _referenceCount = 0;
            }

            private void StartDisposingThread()
            {
                if (_disposingThread != null && _disposingThread.IsAlive)
                    return;

                _disposingThread = new System.Threading.Thread(
                    () =>
                    {
                        Thread.Sleep(5000);
                        while (true)
                        {
                            TimeSpan span;
                            lock (this)
                            {
                                if (!_dereferenceTime.HasValue)
                                    return;
                                if (_dereferenceTime.Value.AddMilliseconds(4500) < DateTime.Now)
                                {
                                    if (_connection != null)
                                    {
                                        Thread.BeginCriticalRegion();
                                        SqlConnection connection = _connection;
                                        _connection = null;
                                        connection.Dispose();
                                        _disposingThread = null;
                                        Thread.EndCriticalRegion();
                                    }
                                    return;
                                }
                                span = _dereferenceTime.HasValue ? (_dereferenceTime.Value.AddMilliseconds(5000) - DateTime.Now) : TimeSpan.Zero;
                            }
                            if (span > TimeSpan.Zero)
                                Thread.Sleep(span);
                        }
                    });
                _disposingThread.Start();
            }

            SqlConnection _connection;
            int _referenceCount;
            DateTime? _dereferenceTime;
            Thread _disposingThread;
            string _connectionString;

            public SqlConnection IncreaseReferencCount()
            {
                _dereferenceTime = null;
                lock (this)
                {
                    if (_connection == null)
                    {
                        _referenceCount = 1;
                        return _connection = new SqlConnection(_connectionString);
                    }
                    else if (_connection.ConnectionString != _connectionString)
                        _connection.ConnectionString = _connectionString;

                    _referenceCount++;
                    return _connection;
                }
            }

            public bool DecreaseReferenceCount()
            {
                lock (this)
                {
                    _referenceCount--;
                    if (_referenceCount < 1)
                    {
                        _dereferenceTime = DateTime.Now;
                        StartDisposingThread();
                        return false;
                    }
                    return true;
                }
            }
        }
        static Dictionary<string, Dictionary<System.Threading.Thread, ReferencedConnection>> _connections = new Dictionary<string, Dictionary<System.Threading.Thread, ReferencedConnection>>();
        static DateTime _lastCleanUpTime = DateTime.Now;

        public static SqlConnection GetConnection(string connectionString)
        {
            ReferencedConnection referencedConnection;
            lock (_connections)
            {
                if (!_connections.ContainsKey(connectionString))
                {
                    Dictionary<System.Threading.Thread, ReferencedConnection> referencedConnections = new Dictionary<System.Threading.Thread, ReferencedConnection>();
                    _connections.Add(connectionString, referencedConnections);
                    referencedConnections.Add(System.Threading.Thread.CurrentThread, referencedConnection = new ReferencedConnection(connectionString));
                }
                else
                {
                    Dictionary<System.Threading.Thread, ReferencedConnection> referencedConnections = _connections[connectionString];
                    if (referencedConnections.ContainsKey(System.Threading.Thread.CurrentThread))
                        referencedConnection = referencedConnections[System.Threading.Thread.CurrentThread];
                    else
                        referencedConnections.Add(System.Threading.Thread.CurrentThread, referencedConnection = new ReferencedConnection(connectionString));
                }
                return referencedConnection.IncreaseReferencCount();
            }
        }

        public static void ReleaseConnection(SqlConnection connection)
        {
            lock (_connections)
            {
                if (!_connections.ContainsKey(connection.ConnectionString))
                    return;

                Dictionary<System.Threading.Thread, ReferencedConnection> referencedConnections = _connections[connection.ConnectionString];
                if (!referencedConnections.ContainsKey(System.Threading.Thread.CurrentThread))
                    return;

                referencedConnections[System.Threading.Thread.CurrentThread].DecreaseReferenceCount();

                if ((DateTime.Now - _lastCleanUpTime).Minutes > 10)
                {
                    CleanUpClosedThreads();
                    _lastCleanUpTime = DateTime.Now;
                }
            }
        }

        private static void CleanUpClosedThreads()
        {
#if (DEBUG)
            System.Diagnostics.Debug.Print("CleanUpConnectionThreads");
#endif
            bool threadsRemoved = false;
            lock (_connections)
            {
                foreach (KeyValuePair<string, Dictionary<System.Threading.Thread, ReferencedConnection>> referencedConnectionsByConnectionString in _connections)
                {
                    foreach (KeyValuePair<Thread, ReferencedConnection> referencedConnectionByThread in referencedConnectionsByConnectionString.Value.Where(x => !x.Key.IsAlive).ToList())
                    {
                        while (referencedConnectionByThread.Value.DecreaseReferenceCount()) { }
                        referencedConnectionsByConnectionString.Value.Remove(referencedConnectionByThread.Key);
                        threadsRemoved = true;
                    }
                }
                if (threadsRemoved)
                    GC.Collect();
            }
        }
    }

    internal interface IRollbackCommitContext : IDisposable
    {
        void Commit(bool unDoChanges);
        void Rollback();
    }

    internal class DataBaseWorkerQueue
    {
        public static DataBaseWorkerQueue GetQueue(string connectionString)
        {
            lock (_instances)
            {
                return _instances.ContainsKey(connectionString) ? _instances[connectionString] : _instances[connectionString] = new DataBaseWorkerQueue(connectionString);
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
        string _connectionString;
        Thread _workerThread;
        List<Class<AsyncResult, SqlCommand, Func<SqlDataReader, IAsyncResult, IRollbackCommitContext>>> _queue = new List<Class<AsyncResult, SqlCommand, Func<SqlDataReader, IAsyncResult, IRollbackCommitContext>>>();

        private DataBaseWorkerQueue(string connectionString)
        {
            _connectionString = connectionString;
        }

        public IAsyncResult EnqueueCommand(object context, SqlCommand command, Func<SqlDataReader, IAsyncResult, IRollbackCommitContext> fillAction)
        {
            Class<AsyncResult, SqlCommand, Func<SqlDataReader, IAsyncResult, IRollbackCommitContext>> entry = new Class<AsyncResult, SqlCommand, Func<SqlDataReader, IAsyncResult, IRollbackCommitContext>>(new AsyncResult(context), command, fillAction);

            lock (this)
            {
                _queue.Add(entry);
                StartThread();
                return entry.A;
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
            SqlConnection connection = ConnectionManager.GetConnection(_connectionString);
            try
            {
                if (connection.State == System.Data.ConnectionState.Closed)
                    connection.Open();
                while (true)
                {
                    List<Class<AsyncResult, SqlCommand, Func<SqlDataReader, IAsyncResult, IRollbackCommitContext>>> queue;
                    lock (this)
                    {
                        if (_queue.Count == 0)
                            return;

                        queue = _queue;
                        _queue = new List<Class<AsyncResult, SqlCommand, Func<SqlDataReader, IAsyncResult, IRollbackCommitContext>>>();
                    }

                    SqlCommand command = queue[0].B;
                    for (int i = 1; i < queue.Count; i++)
                    {
                        command.CommandText += ";" + queue[i].B.CommandText;
                        command.Parameters.AddRange(queue[i].B.Parameters.OfType<SqlParameter>().Select(x => new SqlParameter(x.ParameterName, x.Value)).ToArray());
                    }

                    List<IRollbackCommitContext> commitContexts = new List<IRollbackCommitContext>(queue.Count);

                    using (command)
                    {
                        command.Connection = connection;
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            for (int i = 0; i < queue.Count; i++)
                            {
                                commitContexts.Add(queue[i].C(reader, queue[i].A));
                                queue[i].A.SetCompleted();
                                reader.NextResult();
                            }
                        }
                    }

                    foreach (IRollbackCommitContext context in commitContexts.Where(x => x != null))
                        context.Commit(false);

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
                ConnectionManager.ReleaseConnection(connection);
            }
        }
    }

    public partial class InheritensObjectProvider<T> : IObjectProvider where T : class
    {
        #region Subklassen
        private class DataBaseWorker
        {
            string _connectionString;
            InheritensObjectProvider<T> _objectProvider;
            DataBaseWorkerQueue _queue;

            public DataBaseWorker(string connectionString, InheritensObjectProvider<T> objectProvider)
            {
                _objectProvider = objectProvider;
                _queue = DataBaseWorkerQueue.GetQueue(_connectionString = connectionString);
            }

            private SqlConnection GetConnection()
            {
                SqlConnection connection = ConnectionManager.GetConnection(_connectionString);
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
                        IRollbackCommitContext rollbackCommitContext;
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            MappingInfo mappingInfo = _objectProvider._mappingInfoContainer;
                            rollbackCommitContext = _objectProvider._cache.Fill(reader, x => mappingInfo.GetKeyValues(x), () => (T)mappingInfo.CreateObject(), context);
                        }
                        if (rollbackCommitContext != null) rollbackCommitContext.Commit(false);
                    }
                    finally
                    {
                        ConnectionManager.ReleaseConnection(command.Connection);
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
                    ConnectionManager.ReleaseConnection(connection);
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

    [Serializable]
    public class EntitySaveException : Exception
    {
        object _entity;

        public EntitySaveException(SerializationInfo si, StreamingContext context)
            : base(si, context) { }

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
