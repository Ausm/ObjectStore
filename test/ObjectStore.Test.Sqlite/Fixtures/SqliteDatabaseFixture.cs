using ObjectStore.Interfaces;
using ObjectStore.OrMapping;
using ObjectStore.Sqlite;
using System;
using System.Data.Common;
using System.Reflection;
using ObjectStore.Test.Mocks;
using System.Text.RegularExpressions;
using ObjectStore.Test.Tests;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using System.Linq;
using ObjectStore.Test.Fixtures;
using ObjectStore.MappingOptions;
using static ObjectStore.DataBaseInitializer;
using System.IO;
using Xunit;

namespace ObjectStore.Test.Sqlite
{
    public class SqliteDatabaseFixture : IDisposable, IDatabaseFixture
    {
        #region Fields
        readonly ResultManager<Query> _resultManager;
        bool _isInitialized;
        readonly SqliteConnection _connection;
        SqliteTransaction _transaction;
        bool _useResultManager;
        #endregion

        #region Constructor
        public SqliteDatabaseFixture()
        {
            _isInitialized = false;

            string connectionString;
            {
                string directory = Path.GetDirectoryName(typeof(SqliteDatabaseFixture).GetTypeInfo().Assembly.Location);
                int i = 0;
                string databaseFileName = $"Database{i}.db";
                while (File.Exists(Path.Combine(directory, databaseFileName)))
                {
                    try
                    {
                        File.Delete(Path.Combine(directory, databaseFileName));
                        break;
                    }
                    catch
                    {
                        databaseFileName = $"Database{++i}.db";
                    }
                }

                connectionString = $"Data Source={databaseFileName}";
            }


            RelationalObjectStore relationalObjectProvider = new RelationalObjectStore(connectionString, DataBaseProvider.Instance, new MappingOptionsSet().AddDefaultRules(), true);
            ObjectStoreManager.DefaultObjectStore.RegisterObjectProvider(ObjectProvider = relationalObjectProvider);
            relationalObjectProvider.Register<Entities.DifferentTypes>();
            relationalObjectProvider.Register<Entities.DifferentWritabilityLevels>();
            relationalObjectProvider.Register<Entities.ForeignObjectKey>();
            relationalObjectProvider.Register<Entities.NonInitializedKey>();
            relationalObjectProvider.Register<Entities.SubTest>();
            relationalObjectProvider.Register<Entities.Test>();

            relationalObjectProvider.InitializeDatabase(databaseInitializer => {
                databaseInitializer.RegisterFieldStatment(x => x.Statement is IAddTableStatement && (x.Fieldname == "`Updateable`" || x.Fieldname == "`Readonly`") && x.Statement.Tablename == "`dbo.DifferentWritabilityLevels`", (x, s) => s + " DEFAULT 1");
                databaseInitializer.RegisterTableStatement(x => x is IAddTableStatement && x.Tablename == "`dbo.TestTable`", (x, s) => s + ";INSERT INTO `dbo.TestTable` (`Name`, `Description`) VALUES ('Testname', 'Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua. At vero eos et accusam et justo duo dolores et ea rebum. Stet clita kasd gubergren, no sea takimata sanctus est Lorem ipsum dolor sit amet.'), ('Testname 2', 'Duis autem vel eum iriure dolor in hendrerit in vulputate velit esse molestie consequat, vel illum dolore eu feugiat nulla facilisis at vero eros et accumsan et iusto odio dignissim qui blandit praesent luptatum zzril delenit augue duis dolore te feugait nulla facilisi. Lorem ipsum dolor sit amet, consectetuer adipiscing elit, sed diam nonummy nibh euismod tincidunt ut laoreet dolore magna aliquam erat volutpat.')");
                databaseInitializer.RegisterTableStatement(x => x is IAddTableStatement && x.Tablename == "`dbo.SubTestTable`", (x, s) => s + ";INSERT INTO `dbo.SubTestTable` (`Test`, `Name`, `First`, `Second`, `Nullable`) VALUES (1,'SubEntity0',0,10,NULL),(1,'SubEntity2',1,9,NULL),(1,'SubEntity4',2,8,NULL),(1,'SubEntity6',3,7,NULL),(1,'SubEntity8',4,6,NULL),(1,'SubEntity10',5,5,NULL),(1,'SubEntity12',6,4,NULL),(1,'SubEntity14',7,3,'2016-06-23 17:31:49'),(1,'SubEntity16',8,2,NULL),(1,'SubEntity18',9,1,NULL),(2,'SubEntity1',0,10,NULL),(2,'SubEntity3',1,9,NULL),(2,'SubEntity5',2,8,NULL),(2,'SubEntity7',3,7,NULL),(2,'SubEntity9',4,6,NULL),(2,'SubEntity11',5,5,NULL),(2,'SubEntity13',6,4,NULL),(2,'SubEntity15',7,3,'2016-06-23 17:31:49'),(2,'SubEntity17',8,2,NULL),(2,'SubEntity19',9,1,NULL)");
                databaseInitializer.RegisterTableStatement(x => x is IAddTableStatement && x.Tablename == "`dbo.ForeignObjectKeyTable`", (x, s) => s + ";INSERT INTO `dbo.ForeignObjectKeyTable` (`Id`, `Value`) VALUES (1, 'Testentry')");
                databaseInitializer.RegisterTableStatement(x => x is IAddTableStatement && x.Tablename == "`dbo.DifferentWritabilityLevels`", (x, s) =>
                {
                    s = s.Replace("`dbo.DifferentWritabilityLevels`", "`dbo.DifferentWritabilityLevelsTable`");
                    s += ";CREATE VIEW `dbo.DifferentWritabilityLevels` AS SELECT Id, Writeable, Updateable, Insertable, Readonly FROM`dbo.DifferentWritabilityLevelsTable`";
                    s += ";CREATE TRIGGER `dbo.II_DifferentWritabilityLevels` INSTEAD OF INSERT ON `dbo.DifferentWritabilityLevels` FOR EACH ROW BEGIN INSERT INTO `dbo.DifferentWritabilityLevelsTable` (Writeable, Insertable) VALUES(NEW.Writeable, NEW.Insertable); UPDATE sqlite_sequence SET seq = last_insert_rowid() WHERE name = \"dbo.DifferentWritabilityLevels\"; END";
                    s += ";CREATE TRIGGER `dbo.IU_DifferentWritabilityLevels` INSTEAD OF UPDATE ON `dbo.DifferentWritabilityLevels` FOR EACH ROW BEGIN UPDATE `dbo.DifferentWritabilityLevelsTable` SET Writeable = NEW.Writeable, Updateable = NEW.Updateable, Readonly = Readonly + 1 WHERE Id = OLD.Id; END";
                    s += ";INSERT INTO sqlite_sequence (name, seq) VALUES ('dbo.DifferentWritabilityLevels', 1)";
                    return s + ";INSERT INTO `dbo.DifferentWritabilityLevelsTable` (`Id`, `Writeable`, `Updateable`, `Insertable`, `Readonly`) VALUES (1, 5, 1, 10, 1)";
                });
            });

            Func<DbCommand> getCommand = () => new Command(GetReader);
            typeof(DataBaseProvider).GetTypeInfo().GetField("_getCommand", BindingFlags.NonPublic | BindingFlags.GetField | BindingFlags.Static)
                .SetValue(null, getCommand);

            _resultManager = new ResultManager<Query>();

            _connection = (SqliteConnection)DataBaseProvider.Instance.GetConnection(connectionString);
        }
        #endregion

        #region Properties
        public IObjectProvider ObjectProvider { get; }

        public bool UseResultManager
        {
            get
            {
                return _useResultManager;
            }
            set
            {
                _useResultManager = false;
            }
        }
        #endregion

        #region Methods
        public void SetResult(Query key, IEnumerable<object[]> values)
        {
            _resultManager.SetValues(key, values);
        }

        public void InitializeSupportedQueries(Func<Query, IEnumerable<object[]>> getDefaultResult, Func<Query, string[]> getColumnNames, Func<Query, string> getPattern)
        {
            if (_isInitialized)
                return;

            _isInitialized = true;

            foreach (Query query in Enum.GetValues(typeof(Query)))
            {
                IEnumerable<object[]> values = getDefaultResult(query);
                _resultManager.AddItem(query, x => new Regex(getPattern(query)).IsMatch(x.CommandText), getColumnNames(query), getDefaultResult(query));
            }
        }

        public SqliteTransaction BeginTransaction()
        {
            _transaction?.Rollback();
            if (_connection.State == System.Data.ConnectionState.Closed)
                _connection.Open();
            _transaction = _connection.BeginTransaction();
            _useResultManager = true;
            return _transaction;
        }

        public void RollbackTransaction()
        {
            _useResultManager = true;
            _transaction?.Rollback();
            _transaction = null;
        }

        public void Dispose()
        {
        }

        DbDataReader GetReader(Command command)
        {
            List<Query> keys = new List<Query>();

            if (_useResultManager)
            {
                DataReader returnValue = _resultManager.GetReader(command, keys);
                if (returnValue == null)
                    throw new NotImplementedException($"No result for Query: \"{command.CommandText}\"");
                if (HitCommand != null)
                    foreach (Query key in keys)
                        HitCommand(this, new HitCommandEventArgs(key));
            }

            SqliteCommand sqliteCommand = new SqliteCommand(command.CommandText, _connection) {
                Transaction = _transaction
            };
            sqliteCommand.Parameters.AddRange(command.Parameters.Cast<SqliteParameter>());

            return sqliteCommand.ExecuteReader();
        }
        #endregion

        #region Events
        public event EventHandler<HitCommandEventArgs> HitCommand;
        #endregion
    }

    [CollectionDefinition("SqliteDatabaseCollection")]
    public class SqliteDatabaseCollection : ICollectionFixture<SqliteDatabaseFixture> { }
}
