using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using ObjectStore.Interfaces;

namespace ObjectStore
{
    internal class ConnectionProvider : IConnectionProvider
    {
        #region Singleton-Implementierung
        static ConnectionProvider _instance;

        public static ConnectionProvider Instance
        {
            get
            {
                return _instance ?? (_instance = new ConnectionProvider());
            }
        }

        private ConnectionProvider()
        {
        }
        #endregion

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
#if !DNXCORE50
                                        try
                                        {
                                            Thread.BeginCriticalRegion();
#endif
                                            SqlConnection connection = _connection;
                                            _connection = null;
                                            connection.Dispose();
                                            _disposingThread = null;
#if !DNXCORE50

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

            SqlConnection _connection;
            int _referenceCount;
            DateTime? _dereferenceTime;
            Thread _disposingThread;
            string _connectionString;

            public SqlConnection IncreaseReferencCount()
            {
                _dereferenceTime = null;
                SqlConnection returnValue;
                lock (this)
                {
                    if (_connection == null)
                    {
                        _referenceCount = 1;
                        _connection = new SqlConnection(_connectionString);
                    }
                    else
                    {
                        if (_connection.State != System.Data.ConnectionState.Open && _connection.ConnectionString != _connectionString)
                            _connection.ConnectionString = _connectionString;

                        _referenceCount++;
                    }
                    returnValue = _connection;
                }
                ConnectionProvider.Instance.OnConnectionOpened(returnValue);
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
#endregion

#region Fields
        Dictionary<string, Dictionary<System.Threading.Thread, ReferencedConnection>> _connections = new Dictionary<string, Dictionary<System.Threading.Thread, ReferencedConnection>>();
        DateTime _lastCleanUpTime = DateTime.Now;
#endregion

#region IConnectionProvider-Member
        public SqlConnection GetConnection()
        {
            return GetConnection("default");
        }

        public SqlConnection GetConnection(string connectionString)
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

        public void ReleaseConnection(SqlConnection connection)
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

        private void OnConnectionOpened(SqlConnection connection)
        {
            if (ConnectionOpened != null)
                ConnectionOpened(connection, EventArgs.Empty);
        }

        public event EventHandler ConnectionOpened;
#endregion

#region Methods
        private void CleanUpClosedThreads()
        {
#if !DNXCORE50 && DEBUG
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
    }
}
