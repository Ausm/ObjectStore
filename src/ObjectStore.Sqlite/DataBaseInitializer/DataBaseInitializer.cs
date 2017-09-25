using ObjectStore.Database;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Linq;

namespace ObjectStore.Sqlite
{
    public class DataBaseInitializer : IDatabaseInitializer
    {
        #region Subclasses
        public interface IStatement
        {
            string Tablename { get; }
        }

        public interface IAddTableStatement : IStatement
        {
            IEnumerable<IField> FieldStatements { get; }
        }

        public interface IAlterTableStatment : IStatement
        {
            IField FieldStatement { get; }
        }

        public interface IField
        {
            string Fieldname { get; }
            Type Type { get; }
            bool IsPrimaryKey { get; }
            bool IsAutoincrement { get; }
            bool HasForeignKey { get; }
            string ForeignTable { get; }
            string ForeignField { get; }
        }

        class AddTableStatement : IAddTableStatement
        {
            string _tablename;
            List<Field> _fieldStatements;

            public AddTableStatement(string tablename)
            {
                _tablename = tablename;
                _fieldStatements = new List<Field>();
            }

            public string Tablename => _tablename;

            public IEnumerable<Field> FieldStatements => _fieldStatements;

            IEnumerable<IField> IAddTableStatement.FieldStatements => _fieldStatements.Cast<IField>();

            public Field AddFieldStatement(string fieldname, Type type)
            {
                Field statement;
                _fieldStatements.Add(statement = new Field(fieldname, type));
                return statement;
            }
        }

        class AlterTableStatement : IAlterTableStatment
        {
            string _tablename;
            Field _fieldStatement;

            public AlterTableStatement(string tablename, Field fieldStatement)
            {
                _tablename = tablename;
                _fieldStatement = fieldStatement;
            }

            public string Tablename => _tablename;

            public Field FieldStatement => _fieldStatement;

            IField IAlterTableStatment.FieldStatement => _fieldStatement;
        }

        class Field : IField
        {
            string _fieldname;
            Type _type;
            bool _isPrimaryKey;
            bool _isAutoincrement;

            string _foreignKeyTableName;
            string _foreignKeyFieldName;

            public Field(string fieldname, Type type)
            {
                _fieldname = fieldname;
                _type = type;
                _isPrimaryKey = false;
                _isAutoincrement = false;
                _foreignKeyFieldName = null;
                _foreignKeyTableName = null;
            }

            public string Fieldname => _fieldname;

            public Type Type => _type;

            public bool IsPrimaryKey => _isPrimaryKey;

            public bool IsAutoincrement => _isPrimaryKey && _isAutoincrement;

            public bool HasForeignKey => string.IsNullOrWhiteSpace(_foreignKeyTableName) || string.IsNullOrWhiteSpace(_foreignKeyFieldName);

            public string ForeignTable => _foreignKeyTableName;

            public string ForeignField => _foreignKeyFieldName;

            public void SetForeignKey(string tableName, string fieldName)
            {
                _foreignKeyFieldName = fieldName;
                _foreignKeyTableName = tableName;
            }

            public void SetPrimaryKey(bool autoincrement)
            {
                _isPrimaryKey = true;
                _isAutoincrement = autoincrement;
            }
        }
        #endregion

        #region Fields
        string _connectionString;
        DataBaseProvider _databaseProvider;

        List<IStatement> _tableStatments;
        AddTableStatement _currentTableStatment;
        ITableInfo _currentTableInfo;
        Field _currentAddFieldStatment;

        List<Tuple<Func<IStatement, bool>, Func<IStatement, string>>> _registeredCreateTableParseMethods;
        List<Tuple<Func<IField, bool>, Func<IField, string>>> _registeredAddFieldParseMethods;
        List<Tuple<Func<IField, bool>, Func<IField, string>>> _registeredAddConstraintParseMethods;
        #endregion

        #region Contructors
        static DataBaseInitializer()
        {
        }
        public DataBaseInitializer(string connectionString, DataBaseProvider databaseProvider)
        {
            _databaseProvider = databaseProvider;
            _connectionString = connectionString;

            _tableStatments = new List<IStatement>();

            _currentTableInfo = null;
            _currentTableStatment = null;

            _registeredCreateTableParseMethods = new List<Tuple<Func<IStatement, bool>, Func<IStatement, string>>>();
            _registeredAddFieldParseMethods = new List<Tuple<Func<IField, bool>, Func<IField, string>>>();
            _registeredAddConstraintParseMethods = new List<Tuple<Func<IField, bool>, Func<IField, string>>>();

            InsertParseFunc(_registeredCreateTableParseMethods, x => true, DefaultParseCreateTableStatement, false);
            InsertParseFunc(_registeredAddFieldParseMethods, x => true, DefaultParseAddFieldStatement, false);
            InsertParseFunc(_registeredAddConstraintParseMethods, x => true, DefaultParseAddConstraintStatement, false);
        }
        #endregion

        #region Methods
        public void RegisterCreateTableStatement(Func<IStatement, bool> predicate, Func<IStatement, string> parseFunc)
        {
            InsertParseFunc(_registeredCreateTableParseMethods, predicate ?? (x => true), parseFunc, predicate == null);
        }

        public void RegisterAddFieldStatment(Func<IField, bool> predicate, Func<IField, string> parseFunc)
        {
            InsertParseFunc(_registeredAddFieldParseMethods, predicate ?? (x => true), parseFunc, predicate == null);
        }

        public void RegisterAddConstraintStatment(Func<IField, bool> predicate, Func<IField, string> parseFunc)
        {
            InsertParseFunc(_registeredAddConstraintParseMethods, predicate ?? (x => true), parseFunc, predicate == null);
        }

        void InsertParseFunc<T1, T2>(List<Tuple<T1, T2>> methodsList, T1 predicate, T2 method, bool clear)
        {
            if (clear)
                methodsList.Clear();

            methodsList.Insert(0, Tuple.Create(predicate, method));
        }

        string ParseStatement(IStatement addTableStatement)
        {
            foreach (var item in _registeredCreateTableParseMethods)
            {
                if (item.Item1(addTableStatement))
                    return item.Item2(addTableStatement);
            }

            return DefaultParseCreateTableStatement(addTableStatement);
        }

        string ParseFieldStatement(IField addFieldStatement)
        {
            foreach (var item in _registeredAddFieldParseMethods)
            {
                if (item.Item1(addFieldStatement))
                    return item.Item2(addFieldStatement);
            }

            return DefaultParseAddFieldStatement(addFieldStatement);
        }

        string ParseConstraintStatement(IField addFieldStatement)
        {
            foreach (var item in _registeredAddConstraintParseMethods)
            {
                if (item.Item1(addFieldStatement))
                    return item.Item2(addFieldStatement);
            }

            return DefaultParseAddConstraintStatement(addFieldStatement);
        }

        protected virtual string DefaultParseCreateTableStatement(IStatement completeStatement)
        {
            if (completeStatement is IAddTableStatement)
            {
                IAddTableStatement addTableStatement = (IAddTableStatement)completeStatement;

                List<string> fieldStatements = addTableStatement.FieldStatements.Select(x => ParseFieldStatement(x)).Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
                fieldStatements.AddRange(addTableStatement.FieldStatements.Where(x => x.HasForeignKey).Select(x => ParseConstraintStatement(x)).Where(x => !string.IsNullOrWhiteSpace(x)));

                StringBuilder stringBuilder = new StringBuilder($"CREATE TABLE {Qoute(addTableStatement.Tablename)} (").Append(string.Join(",", fieldStatements)).AppendLine(")");
                return stringBuilder.ToString();
            }
            else if (completeStatement is IAlterTableStatment)
            {
                IAlterTableStatment alterTableStatement = (IAlterTableStatment)completeStatement;
                StringBuilder stringBuilder = new StringBuilder($"CREATE TABLE {Qoute(alterTableStatement.Tablename)} ADD ");

                stringBuilder.Append(ParseFieldStatement(alterTableStatement.FieldStatement));
                if (alterTableStatement.FieldStatement.HasForeignKey)
                    stringBuilder.Append(" ").Append(ParseConstraintStatement(alterTableStatement.FieldStatement));

                return stringBuilder.ToString();
            }
            return null;
        }

        protected virtual string DefaultParseAddFieldStatement(IField addFieldStatement)
        {
            StringBuilder stringBuilder = new StringBuilder(Qoute(addFieldStatement.Fieldname)).Append(" ").Append(GetDbTypeString(addFieldStatement.Type));
            if (addFieldStatement.IsPrimaryKey)
            {
                stringBuilder.Append(" PRIMARY KEY");
                if (addFieldStatement.IsAutoincrement)
                    stringBuilder.Append(" AUTOINCREMENT");
            }

            return stringBuilder.ToString();
        }

        protected virtual string DefaultParseAddConstraintStatement(IField addFieldStatement)
        {
            StringBuilder stringBuilder = new StringBuilder();
            if(addFieldStatement.HasForeignKey)
                stringBuilder.Append("FOREIGN KEY(").Append(Qoute(addFieldStatement.Fieldname)).Append(") REFERENCES ").Append(Qoute($"{addFieldStatement.ForeignTable}({addFieldStatement.ForeignField})"));

            return stringBuilder.Length == 0 ? default(string) : stringBuilder.ToString();
        }
        #endregion

        #region IDatabaseInitializer member
        public void AddField(string fieldname, Type type)
        {
            if (_currentTableStatment != null)
            {
                _currentAddFieldStatment = _currentTableStatment.AddFieldStatement(fieldname, type);
            }
            else if (_currentTableInfo != null)
            {
                _tableStatments.Add(new AlterTableStatement(_currentTableInfo.TableName, new Field(fieldname, type)));
            }
            else
                throw new InvalidOperationException("No Table selected");
        }

        public void AddForeignKey(string foreignTableName, string foreignKeyFieldName)
        {
            _currentAddFieldStatment.SetForeignKey(foreignTableName, foreignKeyFieldName);
        }

        public void AddTable(string tableName)
        {
            _currentTableInfo = _databaseProvider.GetTableInfo(tableName, _connectionString);
            _currentTableStatment = _currentTableInfo == null ? new AddTableStatement(tableName) : null;
            if (_currentTableStatment != null)
                _tableStatments.Add(_currentTableStatment);
        }

        public void Flush()
        {
            _currentAddFieldStatment = null;
            _currentTableInfo = null;
            _currentTableStatment = null;

            string commandText = string.Join(";", _tableStatments.Select(x => ParseStatement(x)).Where(x => !string.IsNullOrWhiteSpace(x)));

            using (DbCommand command = DataBaseProvider.GetCommand())
            {
                DbConnection connection = _databaseProvider.GetConnection(_connectionString);
                try
                {
                    if (connection.State != System.Data.ConnectionState.Open)
                        connection.Open();

                    command.Connection = connection;
                    command.CommandText = commandText;
                    command.ExecuteNonQuery();
                }
                finally
                {
                    _databaseProvider.ReleaseConnection(connection);
                }
            }


        }

        static string Qoute(string value)
        {
            if (value.StartsWith("[") && value.EndsWith("]"))
                value = value.Substring(1, value.Length - 2);

            return "`" + value.Replace("`", "``") + "`";
        }

        static string GetDbTypeString(Type type)
        {
            
            if (type.GetTypeInfo().IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
                return GetDbTypeStringNotNull(type.GenericTypeArguments[0]);

            return GetDbTypeStringNotNull(type) + " NOT NULL";
        }

        static string GetDbTypeStringNotNull(Type type)
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

        public void SetIsKeyField(bool isAutoIncrement)
        {
            _currentAddFieldStatment.SetPrimaryKey(isAutoIncrement);
        }
        #endregion
    }
}
