﻿using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Xml.Linq;
using ObjectStore.Expressions;
using ObjectStore.Database;
using Microsoft.Data.Sqlite;

namespace ObjectStore.Sqlite
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
            readonly string _connectionString;

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

                if (typeof(T) == typeof(bool) || typeof(T) == typeof(bool?))
                    returnValue = _dataReader.GetBoolean(ordinal);
                else if (typeof(T) == typeof(int) || typeof(T) == typeof(int?))
                    returnValue = _dataReader.GetInt32(ordinal);
                else if (typeof(T) == typeof(byte) || typeof(T) == typeof(byte?))
                    returnValue = _dataReader.GetByte(ordinal);
                else if (typeof(T) == typeof(short) || typeof(T) == typeof(short?))
                    returnValue = _dataReader.GetInt16(ordinal);
                else if (typeof(T) == typeof(long) || typeof(T) == typeof(long?))
                    returnValue = _dataReader.GetInt64(ordinal);
                else if (typeof(T) == typeof(Guid) || typeof(T) == typeof(Guid?))
                    returnValue = _dataReader.GetGuid(ordinal);
                else if (typeof(T) == typeof(decimal) || typeof(T) == typeof(decimal?))
                    returnValue = _dataReader.GetDecimal(ordinal);
                else if (typeof(T) == typeof(DateTime) || typeof(T) == typeof(DateTime?))
                    returnValue = _dataReader.GetDateTime(ordinal);
                else if (typeof(T) == typeof(XElement))
                    returnValue = XElement.Parse(_dataReader.GetString(ordinal));
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

        class TableInfo : ITableInfo
        {
            List<string> _fieldNames;

            public TableInfo(string tableName, IEnumerable<string> fieldNames)
            {
                TableName = tableName;
                _fieldNames = fieldNames.ToList();
            }

            public string TableName { get; }

            public IEnumerable<string> FieldNames => _fieldNames.AsReadOnly();
        }

        class SqliteDataBaseInitializer : DataBaseInitializer
        {
            DataBaseProvider _databaseProvider;
            readonly string _connectionString;

            public SqliteDataBaseInitializer(string connectionString, DataBaseProvider databaseProvider, Func<DbCommand> getCommandFunc) : base(connectionString, databaseProvider, getCommandFunc)
            {
                _databaseProvider = databaseProvider;
                _connectionString = connectionString;
            }

            protected override string DefaultParseCreateTableStatement(IStatement completeStatement, string previousParseResult)
            {
                if (completeStatement is IAddTableStatement addTableStatement)
                {
                    List<string> fieldStatements = addTableStatement.FieldStatements.Select(x => ParseFieldStatement(x)).Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
                    fieldStatements.AddRange(addTableStatement.FieldStatements.Where(x => x.HasForeignKey).Select(x => ParseConstraintStatement(x)).Where(x => !string.IsNullOrWhiteSpace(x)));

                    if (addTableStatement.FieldStatements.Where(x => x.IsPrimaryKey).Count() > 1)
                        fieldStatements.Add($"PRIMARY KEY({string.Join(",", addTableStatement.FieldStatements.Where(x => x.IsPrimaryKey).Select(x => x.Fieldname))})");

                    StringBuilder stringBuilder = new StringBuilder($"CREATE TABLE {addTableStatement.Tablename} (").Append(string.Join(",", fieldStatements)).AppendLine(")");
                    return stringBuilder.ToString();
                }
                else if (completeStatement is IAlterTableStatment alterTableStatement)
                {
                    if (alterTableStatement.ExistsAlready)
                        return previousParseResult;

                    StringBuilder stringBuilder = new StringBuilder($"ALTER TABLE {alterTableStatement.Tablename} ADD ");

                    stringBuilder.Append(ParseFieldStatement(alterTableStatement.FieldStatement));
                    if (alterTableStatement.FieldStatement.HasForeignKey)
                        stringBuilder.Append(" ").Append(ParseConstraintStatement(alterTableStatement.FieldStatement));

                    return stringBuilder.ToString();
                }
                return null;
            }

            protected override string DefaultParseAddFieldStatement(IField addFieldStatement, string previousParseResult)
            {
                StringBuilder stringBuilder = new StringBuilder(addFieldStatement.Fieldname).Append(" ").Append(GetDbTypeString(addFieldStatement.Type));
                if (addFieldStatement.IsPrimaryKey)
                {
                    if (addFieldStatement.Statement is IAddTableStatement addTableStatement &&
                        addTableStatement.FieldStatements.Where(x => x.IsPrimaryKey).Count() > 1)
                    {
                        if(addFieldStatement.IsAutoincrement)
                            throw new NotSupportedException($"Autoincremtent is not supported for table {addTableStatement.Tablename} because it has more then one primary key columns.");
                    }
                    else
                    {
                        stringBuilder.Append(" PRIMARY KEY");
                        if (addFieldStatement.IsAutoincrement)
                            stringBuilder.Append(" AUTOINCREMENT");
                    }
                }

                return stringBuilder.ToString();
            }

            protected override string DefaultParseAddConstraintStatement(IField addFieldStatement, string previousParseResult)
            {
                StringBuilder stringBuilder = new StringBuilder();
                if (addFieldStatement.HasForeignKey)
                {
                    stringBuilder.Append("FOREIGN KEY(")
                        .Append(addFieldStatement.Fieldname)
                        .Append(") REFERENCES ")
                        .Append($"{addFieldStatement.ForeignTable}({addFieldStatement.ForeignField})");

                    if (addFieldStatement.ForeignKeyOnDeleteCascade)
                        stringBuilder.Append(" ON DELETE CASCADE ");
                }

                return stringBuilder.Length == 0 ? default(string) : stringBuilder.ToString();
            }

            protected override string Qoute(string value)
            {
                if (value.StartsWith("[") && value.EndsWith("]"))
                    value = value.Substring(1, value.Length - 2);

                return "`" + value.Replace("`", "``") + "`";
            }

            protected override string GetDbTypeString(Type type)
            {

                if (type.GetTypeInfo().IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
                    return GetDbTypeStringNotNull(type.GenericTypeArguments[0]);

                return GetDbTypeStringNotNull(type) + " NOT NULL";
            }

            protected override string GetDbTypeStringNotNull(Type type)
            {
                if (type == typeof(int) ||
                    type == typeof(long) ||
                    type == typeof(short) ||
                    type == typeof(byte) ||
                    type == typeof(bool))
                    return "INTEGER";
                if (type == typeof(string) ||
                    type == typeof(XElement))
                    return "TEXT";
                if (type == typeof(DateTime))
                    return "TIMESTAMP";
                if (type == typeof(Guid) ||
                    type == typeof(byte[]))
                    return "BLOB";
                if (type == typeof(Double) ||
                    type == typeof(Single) ||
                    type == typeof(Decimal))
                    return "REAL";

                throw new NotSupportedException($"DataType {type.FullName} is not supported for database initialization.");
            }

            protected override DbCommand GetCommand()
                => base.GetCommand();

            protected override ITableInfo GetTableInfo(string tableName)
            {
                using (DbConnection connection = _databaseProvider.GetConnection(_connectionString))
                {
                    connection.Open();

                    tableName = tableName.Replace('`', '\'');

                    using (DbCommand command = GetCommand())
                    {
                        command.Connection = connection;
                        command.CommandText = $"SELECT COUNT(*) FROM sqlite_master WHERE tbl_name = {tableName} AND type in ('table', 'view')";

                        object value = command.ExecuteScalar();
                        if (!(value is long) || (long)value != 1L)
                            return null;
                    }

                    using (DbCommand command = GetCommand())
                    {
                        command.Connection = connection;
                        command.CommandText = $"PRAGMA TABLE_INFO({tableName});";
                        using (DbDataReader reader = command.ExecuteReader())
                        {
                            List<string> fieldNames = new List<string>();
                            int nameOrdinal = reader.GetOrdinal("name");

                            while (reader.Read())
                                fieldNames.Add(Qoute(reader.GetString(nameOrdinal)));

                            return new TableInfo(tableName, fieldNames);
                        }
                    }
                }
            }
        }
        #endregion

        #region Fields
        Dictionary<string, Dictionary<Thread, ReferencedConnection>> _connections = new Dictionary<string, Dictionary<Thread, ReferencedConnection>>();
        DateTime _lastCleanUpTime = DateTime.Now;
        int _currentUniqe = 0;
        static Func<DbCommand> _getCommand = () => new SqliteCommand();
        static Func<string, DbConnection> _getConnection = c => new SqliteConnection(c);
        #endregion

        #region Singleton Implementation
        public static DataBaseProvider _instance;

        public static DataBaseProvider Instance
            => _instance ?? (_instance = new DataBaseProvider());

        private DataBaseProvider()
        {
            InitExpressionParser();
        }
        #endregion

        #region CommandBuilders
        public IModifyableCommandBuilder GetSelectCommandBuilder()
            => new SelectCommandBuilder(this);

        public ICommandBuilder GetInsertCommandBuilder()
            => new InsertCommandBuilder();

        public ICommandBuilder GetUpdateCommandBuilder()
            => new UpdateCommandBuilder();

        public ICommandBuilder GetDeleteCommandBuilder()
            => new DeleteCommandBuilder();

        public IValueSource GetValueSource(DbCommand command)
            => new ValueSource(command.ExecuteReader());
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
                foreach (SqliteParameter parameter in commandsEnumerator.Current.Parameters)
                    command.Parameters.Add(parameter);
            }

            return command;
        }

        public DataBaseInitializer GetDatabaseInitializer(string connectionString) =>  new SqliteDataBaseInitializer(connectionString, this, GetCommand);

        internal static DbCommand GetCommand() => _getCommand();

        internal static SqliteParameter GetParameter(string parameterName, object value)
        {
            if (value is XElement)
                return new SqliteParameter(parameterName, SqliteType.Text) { Value = ((XElement)value).ToString() };

            return new SqliteParameter(parameterName, value);
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
        internal ExpressionParser ExpressionParser { get; private set; }
        #endregion
    }
}
