using ObjectStore.Database;
using ObjectStore.Expressions;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Linq;
using System.Threading;
using System.Xml.Linq;

namespace ObjectStore.SqlClient
{
    public partial class DataBaseProvider : IDataBaseProvider
    {
        #region Subclasses
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

                _disposingThread = new Thread(
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
#if !NETCOREAPP1_0
                                        try
                                        {
                                            Thread.BeginCriticalRegion();
#endif
                                            DbConnection connection = _connection;
                                            _connection = null;
                                            connection.Dispose();
                                            _disposingThread = null;
#if !NETCOREAPP1_0

                                        }
                                        finally
                                        {
                                            Thread.EndCriticalRegion();
                                        }
#endif
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

            DbConnection _connection;
            int _referenceCount;
            DateTime? _dereferenceTime;
            Thread _disposingThread;
            string _connectionString;

            public DbConnection IncreaseReferencCount()
            {
                _dereferenceTime = null;
                DbConnection returnValue;
                lock (this)
                {
                    if (_connection == null)
                    {
                        _referenceCount = 1;
                        _connection = _getConnection(_connectionString);
                    }
                    else
                    {
                        if (_connection.State != System.Data.ConnectionState.Open && _connection.ConnectionString != _connectionString)
                            _connection.ConnectionString = _connectionString;

                        _referenceCount++;
                    }
                    returnValue = _connection;
                }
                DataBaseProvider.Instance.OnConnectionOpened(returnValue);
                return returnValue;
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

        class ValueSource : IValueSource
        {
            DbDataReader _dataReader;

            public ValueSource(DbDataReader dataReader)
            {
                _dataReader = dataReader;
            }

            public void Dispose()
            {
                _dataReader?.Dispose();
                _dataReader = null;
            }

            public T GetValue<T>(string name)
            {
                int ordinal = _dataReader.GetOrdinal(name);

                if (_dataReader.IsDBNull(ordinal))
                    return default(T);

                object returnValue;

                if (typeof(T) == typeof(XElement))
                {
                    SqlXml xml = _dataReader.GetFieldValue<SqlXml>(ordinal);
                    using (System.Xml.XmlReader reader = xml.CreateReader())
                        returnValue = XElement.Load(reader);
                }
                else
                    returnValue = _dataReader.GetValue(ordinal);

                return (T)returnValue;
            }

            public bool Next()
            {
                if (_dataReader.Read())
                    return true;

                _dataReader.NextResult();
                return false;
            }
        }
        #endregion

        #region Fields
        Dictionary<string, Dictionary<Thread, ReferencedConnection>> _connections = new Dictionary<string, Dictionary<Thread, ReferencedConnection>>();
        DateTime _lastCleanUpTime = DateTime.Now;
        int _currentUniqe = 0;
        ExpressionParser _expressionParser;
        static Func<DbCommand> _getCommand = () => new SqlCommand();
        static Func<string, DbConnection> _getConnection = c => new SqlConnection(c);
        #endregion

        #region Singleton Implementation
        public static DataBaseProvider _instance;

        public static DataBaseProvider Instance
        {
            get
            {
                return _instance ?? (_instance = new DataBaseProvider());
            }
        }

        private DataBaseProvider()
        {
            InitExpressionParser();
        }
        #endregion

        #region CommandBuilders
        public IModifyableCommandBuilder GetSelectCommandBuilder()
        {
            return new SelectCommandBuilder(this);
        }

        public ICommandBuilder GetInsertCommandBuilder()
        {
            return new InsertCommandBuilder();
        }

        public ICommandBuilder GetUpdateCommandBuilder()
        {
            return new UpdateCommandBuilder();
        }

        public ICommandBuilder GetDeleteCommandBuilder()
        {
            return new DeleteCommandBuilder();
        }

        public IValueSource GetValueSource(DbCommand command)
        {
            return new ValueSource(command.ExecuteReader());
        }
        #endregion

        #region Connections
        public DbConnection GetConnection(string connectionString)
        {
            ReferencedConnection referencedConnection;
            lock (_connections)
            {
                if (!_connections.ContainsKey(connectionString))
                {
                    Dictionary<Thread, ReferencedConnection> referencedConnections = new Dictionary<Thread, ReferencedConnection>();
                    _connections.Add(connectionString, referencedConnections);
                    referencedConnections.Add(Thread.CurrentThread, referencedConnection = new ReferencedConnection(connectionString));
                }
                else
                {
                    Dictionary<Thread, ReferencedConnection> referencedConnections = _connections[connectionString];
                    if (referencedConnections.ContainsKey(Thread.CurrentThread))
                        referencedConnection = referencedConnections[Thread.CurrentThread];
                    else
                        referencedConnections.Add(Thread.CurrentThread, referencedConnection = new ReferencedConnection(connectionString));
                }
                return referencedConnection.IncreaseReferencCount();
            }
        }

        public void ReleaseConnection(DbConnection connection)
        {
            lock (_connections)
            {
                if (!_connections.ContainsKey(connection.ConnectionString))
                    return;

                Dictionary<Thread, ReferencedConnection> referencedConnections = _connections[connection.ConnectionString];
                if (!referencedConnections.ContainsKey(Thread.CurrentThread))
                    return;

                referencedConnections[Thread.CurrentThread].DecreaseReferenceCount();

                if ((DateTime.Now - _lastCleanUpTime).Minutes > 10)
                {
                    CleanUpClosedThreads();
                    _lastCleanUpTime = DateTime.Now;
                }
            }
        }

        private void OnConnectionOpened(DbConnection connection)
        {
            ConnectionOpened?.Invoke(connection, EventArgs.Empty);
        }

        public event EventHandler ConnectionOpened;
        #endregion

        #region Methods
        public DbCommand CombineCommands(IEnumerable<DbCommand> commands)
        {
            DbCommand command = commands.FirstOrDefault();
            if (commands.Count() == 1)
                return commands.First();


            IEnumerator<DbCommand> commandsEnumerator = commands.Skip(1).GetEnumerator();
            while (commandsEnumerator.MoveNext())
            {
                command.CommandText += ";" + commandsEnumerator.Current.CommandText;
                command.Parameters.AddRange(commandsEnumerator.Current.Parameters.OfType<DbParameter>().ToArray());
            }

            return command;
        }

        public IDatabaseInitializer GetDatabaseInitializer(string connectionString)
        {
            throw new NotImplementedException();
        }
        
        internal static DbCommand GetCommand() => _getCommand();

        internal static SqlParameter GetParameter(string parameterName, object value)
        {
            if (value is XElement)
                return new SqlParameter(parameterName, System.Data.SqlDbType.Xml) { Value = new SqlXml(((XElement)value).CreateReader()) };

            if (value is DateTime && ((DateTime)value) < new DateTime(1753, 1, 1))
                value = new DateTime(1753, 1, 1);

            return new SqlParameter(parameterName, value);
        }

        internal int GetUniqe()
        {
            lock (this)
            {
                if (_currentUniqe == int.MaxValue)
                    _currentUniqe = 0;

                return _currentUniqe++;
            }
        }

        #region private Methods
        partial void InitExpressionParser();
        void CleanUpClosedThreads()
        {
#if DEBUG && !NETCOREAPP1_0
            System.Diagnostics.Debug.Print("CleanUpConnectionThreads");
#endif
            bool threadsRemoved = false;
            lock (_connections)
            {
                foreach (KeyValuePair<string, Dictionary<Thread, ReferencedConnection>> referencedConnectionsByConnectionString in _connections)
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
        #endregion
        #endregion

        #region Properties
        internal ExpressionParser ExpressionParser => _expressionParser;
        #endregion
    }
}
