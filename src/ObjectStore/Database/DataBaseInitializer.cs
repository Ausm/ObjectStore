using ObjectStore.Database;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;

namespace ObjectStore
{
    public abstract class DataBaseInitializer : IDatabaseInitializer
    {
        #region Nested classes
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
            bool ExistsAlready { get; }
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
            IStatement Statement { get; }
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
                _fieldStatements.Add(statement = new Field(fieldname, type, this));
                return statement;
            }
        }

        class AlterTableStatement : IAlterTableStatment
        {
            string _tablename;
            Field _fieldStatement;
            ITableInfo _tableInfo;

            public AlterTableStatement(string tablename, string fieldname, Type type, ITableInfo tableInfo)
            {
                _tableInfo = tableInfo;
                _tablename = tablename;
                _fieldStatement = new Field(fieldname, type, this);
            }

            public string Tablename => _tablename;

            public Field FieldStatement => _fieldStatement;

            IField IAlterTableStatment.FieldStatement => _fieldStatement;

            bool IAlterTableStatment.ExistsAlready => _tableInfo.FieldNames.Contains(FieldStatement.Fieldname);

        }

        class Field : IField
        {
            string _fieldname;
            Type _type;
            bool _isPrimaryKey;
            bool _isAutoincrement;
            IStatement _statement;

            string _foreignKeyTableName;
            string _foreignKeyFieldName;

            public Field(string fieldname, Type type, IStatement statement)
            {
                _fieldname = fieldname;
                _type = type;
                _statement = statement;
                _isPrimaryKey = false;
                _isAutoincrement = false;
                _foreignKeyFieldName = null;
                _foreignKeyTableName = null;
            }

            public string Fieldname => _fieldname;

            public Type Type => _type;

            public bool IsPrimaryKey => _isPrimaryKey;

            public bool IsAutoincrement => _isPrimaryKey && _isAutoincrement;

            public bool HasForeignKey => !string.IsNullOrWhiteSpace(_foreignKeyTableName) || !string.IsNullOrWhiteSpace(_foreignKeyFieldName);

            public string ForeignTable => _foreignKeyTableName;

            public string ForeignField => _foreignKeyFieldName;

            public IStatement Statement => _statement;

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
        IDataBaseProvider _databaseProvider;
        Func<DbCommand> _getCommandFunc;

        List<IStatement> _tableStatments;
        AddTableStatement _currentTableStatment;
        ITableInfo _currentTableInfo;
        Field _currentAddFieldStatment;

        List<Tuple<Func<IStatement, bool>, Func<IStatement, string, string>>> _registeredCreateTableParseMethods;
        List<Tuple<Func<IField, bool>, Func<IField, string, string>>> _registeredAddFieldParseMethods;
        List<Tuple<Func<IField, bool>, Func<IField, string, string>>> _registeredAddConstraintParseMethods;
        #endregion

        #region Contructors
        static DataBaseInitializer()
        {
        }
        public DataBaseInitializer(string connectionString, IDataBaseProvider databaseProvider, Func<DbCommand> getCommandFunc)
        {
            _databaseProvider = databaseProvider;
            _connectionString = connectionString;
            _getCommandFunc = getCommandFunc;

            _tableStatments = new List<IStatement>();

            _currentTableInfo = null;
            _currentTableStatment = null;

            _registeredCreateTableParseMethods = new List<Tuple<Func<IStatement, bool>, Func<IStatement, string, string>>>();
            _registeredAddFieldParseMethods = new List<Tuple<Func<IField, bool>, Func<IField, string, string>>>();
            _registeredAddConstraintParseMethods = new List<Tuple<Func<IField, bool>, Func<IField, string, string>>>();

            AddParseFunc(_registeredCreateTableParseMethods, x => true, DefaultParseCreateTableStatement, false);
            AddParseFunc(_registeredAddFieldParseMethods, x => true, DefaultParseAddFieldStatement, false);
            AddParseFunc(_registeredAddConstraintParseMethods, x => true, DefaultParseAddConstraintStatement, false);
        }
        #endregion

        #region Methods
        void AddParseFunc<T1, T2>(List<Tuple<T1, T2>> methodsList, T1 predicate, T2 method, bool clear)
        {
            methodsList.Add(Tuple.Create(predicate, method));
        }

        protected string ParseStatement(IStatement addTableStatement) => Parse(addTableStatement, _registeredCreateTableParseMethods);

        protected string ParseFieldStatement(IField addFieldStatement) => Parse(addFieldStatement, _registeredAddFieldParseMethods);

        protected string ParseConstraintStatement(IField addFieldStatement) => Parse(addFieldStatement, _registeredAddConstraintParseMethods);

        string Parse<T>(T statment, IEnumerable<Tuple<Func<T, bool>, Func<T, string, string>>> parseMethods)
        {
            string returnValue = null;
            foreach (Tuple<Func<T, bool>, Func<T, string, string>> item in parseMethods.Where(x => x.Item1(statment)))
            {
                returnValue = item.Item2(statment, returnValue);
            }
            return returnValue;
        }

        internal void AddField(string fieldname, Type type)
        {
            fieldname = Qoute(fieldname);

            if (_currentTableStatment != null)
            {
                _currentAddFieldStatment = _currentTableStatment.AddFieldStatement(fieldname, type);
            }
            else if (_currentTableInfo != null)
            {
                AlterTableStatement alterTableStatement = new AlterTableStatement(_currentTableInfo.TableName, fieldname, type, _currentTableInfo);
                _currentAddFieldStatment = alterTableStatement.FieldStatement;
                _tableStatments.Add(alterTableStatement);
            }
            else
                throw new InvalidOperationException("No Table selected");
        }

        internal void AddForeignKey(string foreignTableName, string foreignKeyFieldName)
        {
            _currentAddFieldStatment.SetForeignKey(Qoute(foreignTableName), Qoute(foreignKeyFieldName));
        }

        internal void AddTable(string tableName)
        {
            tableName = Qoute(tableName);
            _currentTableInfo = GetTableInfo(tableName);
            _currentTableStatment = _currentTableInfo == null ? new AddTableStatement(tableName) : null;
            if (_currentTableStatment != null)
                _tableStatments.Add(_currentTableStatment);
        }

        internal void Flush()
        {
            _currentAddFieldStatment = null;
            _currentTableInfo = null;
            _currentTableStatment = null;

            string commandText = string.Join(";", _tableStatments.Select(x => ParseStatement(x)).Where(x => !string.IsNullOrWhiteSpace(x)));

            if (string.IsNullOrWhiteSpace(commandText))
                return;

            using (DbCommand command = _getCommandFunc())
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

        internal void SetIsKeyField(bool isAutoIncrement)
        {
            _currentAddFieldStatment.SetPrimaryKey(isAutoIncrement);
        }

        #region Abstract
        protected abstract string DefaultParseCreateTableStatement(IStatement completeStatement, string previousParseResult);

        protected abstract string DefaultParseAddFieldStatement(IField addFieldStatement, string previousParseResult);

        protected abstract string DefaultParseAddConstraintStatement(IField addFieldStatement, string previousParseResult);

        protected abstract string Qoute(string value);

        protected abstract string GetDbTypeString(Type type);

        protected abstract string GetDbTypeStringNotNull(Type type);

        protected virtual DbCommand GetCommand()
        {
            return _getCommandFunc();
        }

        protected abstract ITableInfo GetTableInfo(string tableName);
        #endregion
        #endregion

        #region IDatabaseInitializer members
        public void RegisterCreateTableStatement(Func<IStatement, bool> predicate, Func<IStatement, string, string> parseFunc)
        {
            AddParseFunc(_registeredCreateTableParseMethods, predicate ?? (x => true), parseFunc, predicate == null);
        }

        public void RegisterAddFieldStatment(Func<IField, bool> predicate, Func<IField, string, string> parseFunc)
        {
            AddParseFunc(_registeredAddFieldParseMethods, predicate ?? (x => true), parseFunc, predicate == null);
        }

        public void RegisterAddConstraintStatment(Func<IField, bool> predicate, Func<IField, string, string> parseFunc)
        {
            AddParseFunc(_registeredAddConstraintParseMethods, predicate ?? (x => true), parseFunc, predicate == null);
        }
        #endregion
    }
}
