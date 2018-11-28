using Xunit;
using Xunit.Abstractions;
using System;
using ObjectStore.OrMapping;
using System.Reflection;
using ObjectStore.Sqlite;
using System.Data.Common;
using Microsoft.Data.Sqlite;
using System.Data;

namespace ObjectStore.Test.Sqlite
{
    [Collection("SqliteDatabaseCollection")]
    public class DatabaseInitializeTests : IDisposable
    {
        #region Command-Mock
        class Command : DbCommand
        {
            DbCommand _innerCommand;
            DatabaseInitializeTests _parent;

            public Command(DbCommand innerCommand, DatabaseInitializeTests parent)
            {
                _innerCommand = innerCommand;
                _parent = parent;
            }

            public override string CommandText
            {
                get
                {
                    return _innerCommand.CommandText;
                }

                set
                {
                    _innerCommand.CommandText = value;
                }
            }

            public override int CommandTimeout
            {
                get
                {
                    throw new NotImplementedException();
                }

                set
                {
                    throw new NotImplementedException();
                }
            }

            public override CommandType CommandType
            {
                get
                {
                    throw new NotImplementedException();
                }

                set
                {
                    throw new NotImplementedException();
                }
            }

            public override bool DesignTimeVisible
            {
                get
                {
                    throw new NotImplementedException();
                }

                set
                {
                    throw new NotImplementedException();
                }
            }

            public override UpdateRowSource UpdatedRowSource
            {
                get
                {
                    throw new NotImplementedException();
                }

                set
                {
                    throw new NotImplementedException();
                }
            }

            protected override DbConnection DbConnection
            {
                get
                {
                    return _innerCommand.Connection;
                }

                set
                {
                    _innerCommand.Connection = value;
                }
            }

            protected override DbParameterCollection DbParameterCollection => _innerCommand.Parameters;

            protected override DbTransaction DbTransaction
            {
                get
                {
                    throw new NotImplementedException();
                }

                set
                {
                    throw new NotImplementedException();
                }
            }

            public override void Cancel()
            {
                throw new NotImplementedException();
            }

            public override int ExecuteNonQuery()
            {
                _parent._commandCalls++;
                return _innerCommand.ExecuteNonQuery();
            }

            public override object ExecuteScalar() => _innerCommand.ExecuteScalar();

            public override void Prepare()
            {
                throw new NotImplementedException();
            }

            protected override DbParameter CreateDbParameter()
            {
                throw new NotImplementedException();
            }

            protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) => _innerCommand.ExecuteReader(behavior);
        }
        #endregion

        #region Test-Entities
        public abstract class BaseEntity
        {
            [IsPrimaryKey]
            public abstract int Id { get; }

            public abstract string Name { get; set; }
        }

        public abstract class Entity1 : BaseEntity { }
        public abstract class Entity2 : BaseEntity { }
        public abstract class Entity3 : BaseEntity { }
        public abstract class Entity4 : BaseEntity { }
        public abstract class Entity5 : BaseEntity { }
        public abstract class Entity6 : BaseEntity { }
        #endregion

        #region Fields
        RelationalObjectStore _objectProvider;
        int _commandCalls = 0;
        SqliteDatabaseFixture _databaseFixture;
        Func<DbCommand> _oldGetCommand;
        #endregion

        #region Constructor
        public DatabaseInitializeTests(SqliteDatabaseFixture databaseFixture, ITestOutputHelper output)
        {
            _databaseFixture = databaseFixture;
            _objectProvider = _databaseFixture.ObjectProvider as RelationalObjectStore;
            databaseFixture.UseResultManager = false;

            FieldInfo fieldInfo = typeof(DataBaseProvider).GetTypeInfo().GetField("_getCommand", BindingFlags.NonPublic | BindingFlags.GetField | BindingFlags.Static);
            _oldGetCommand = fieldInfo.GetValue(null) as Func<DbCommand>;
            Func<DbCommand> getCommand = () => {
                return new Command(new SqliteCommand(), this);
            };
            fieldInfo.SetValue(null, getCommand);
        }
        #endregion

        #region Tests
        [Fact]
        public void InitializeAInitializedDatabaseShouldDoNothingTest()
        {
            _objectProvider.Register<Entity1>();
            _objectProvider.Register<Entity2>();
            _objectProvider.InitializeDatabase();
            _commandCalls = 0;
            _objectProvider.InitializeDatabase();
            Assert.Equal(0, _commandCalls);
        }

        [Fact]
        public void InitializeAPartiallyInitializedDatabaseShouldDoOnlyDifferentialScriptsTest()
        {
            _objectProvider.Register<Entity3>();
            _objectProvider.InitializeDatabase();
            int commandCalls = _commandCalls;
            _commandCalls = 0;

            _objectProvider.Register<Entity4>();
            _objectProvider.InitializeDatabase();
            Assert.Equal(commandCalls, _commandCalls);
        }

        [Fact]
        public void InitializeAUninitializedDatabaseShouldWorkTest()
        {
            _objectProvider.Register<Entity5>();
            _objectProvider.Register<Entity6>();
            _objectProvider.InitializeDatabase();
            Assert.NotEqual(0, _commandCalls);
        }
        #endregion

        public void Dispose()
        {
            typeof(DataBaseProvider).GetTypeInfo().GetField("_getCommand", BindingFlags.NonPublic | BindingFlags.GetField | BindingFlags.Static).SetValue(null, _oldGetCommand);
        }
    }
}
