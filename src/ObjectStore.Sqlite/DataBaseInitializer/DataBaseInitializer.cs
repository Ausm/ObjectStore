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
        class AddTableStatement
        {
            string _tablename;
            List<AddFieldStatment> _fieldStatements;

            public AddTableStatement(string tablename)
            {
                _tablename = tablename;
                _fieldStatements = new List<AddFieldStatment>();
            }

            public string Tablename => _tablename;

            public IEnumerable<AddFieldStatment> FieldStatements => _fieldStatements;

            public AddFieldStatment AddFieldStatement(string fieldname, Type type)
            {
                AddFieldStatment statement;
                _fieldStatements.Add(statement = new AddFieldStatment(this, fieldname, type));
                return statement;
            }
        }

        class AddFieldStatment
        {
            AddTableStatement _parentTableStatment;
            string _tableName;

            string _fieldname;
            Type _type;
            bool _isPrimaryKey;
            bool _isAutoincrement;

            string _foreignKeyTableName;
            string _foreignKeyFieldName;

            public AddFieldStatment(AddTableStatement addTableStatment, string fieldname, Type type) : this(fieldname, type)
            {
                _parentTableStatment = addTableStatment;
                _tableName = null;
            }

            public AddFieldStatment(string tableName, string fieldname, Type type) : this(fieldname, type)
            {
                _parentTableStatment = null;
                _tableName = tableName;
            }

            AddFieldStatment(string fieldname, Type type)
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

        List<AddTableStatement> _tableStatments;
        List<AddFieldStatment> _fieldStatments;

        AddTableStatement _currentTableStatment;
        ITableInfo _currentTableInfo;
        AddFieldStatment _currentAddFieldStatment;
        #endregion

        #region Contructors
        static DataBaseInitializer()
        {
        }
        public DataBaseInitializer(string connectionString, DataBaseProvider databaseProvider)
        {
            _databaseProvider = databaseProvider;
            _connectionString = connectionString;

            _tableStatments = new List<AddTableStatement>();
            _fieldStatments = new List<AddFieldStatment>();

            _currentTableInfo = null;
            _currentTableStatment = null;
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
                _fieldStatments.Add(new AddFieldStatment(_currentTableInfo.TableName, fieldname, type));
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
            StringBuilder stringBuilder = new StringBuilder();

            foreach (AddTableStatement tableStatment in _tableStatments)
            {
                stringBuilder.Append($"CREATE TABLE {Qoute(tableStatment.Tablename)} (");
                bool first = true;
                foreach (AddFieldStatment fieldStatement in tableStatment.FieldStatements)
                {
                    if (first)
                        first = false;
                    else
                        stringBuilder.AppendLine(",");

                    stringBuilder.Append(Qoute(fieldStatement.Fieldname)).Append(" ").Append(GetDbTypeString(fieldStatement.Type));

                    if (fieldStatement.IsPrimaryKey)
                    {
                        stringBuilder.Append(" PRIMARY KEY");
                        if (fieldStatement.IsAutoincrement)
                            stringBuilder.Append(" AUTOINCREMENT");
                    }
                }

                foreach (AddFieldStatment fieldStatement in tableStatment.FieldStatements.Where(x => x.HasForeignKey))
                    stringBuilder.AppendLine(",").Append("FOREIGN KEY(").Append(Qoute(fieldStatement.Fieldname)).Append(") REFERENCES ").Append(Qoute($"{fieldStatement.ForeignTable}({fieldStatement.ForeignField})"));

                stringBuilder.AppendLine(");");
            }

            foreach (AddFieldStatment tableStatment in _fieldStatments)
            {
                throw new NotImplementedException();
            }

            using (DbCommand command = DataBaseProvider.GetCommand())
            {
                using (DbConnection connection = _databaseProvider.GetConnection(_connectionString))
                {
                    if (connection.State != System.Data.ConnectionState.Open)
                        connection.Open();

                    command.Connection = connection;
                    command.CommandText = stringBuilder.ToString();
                    command.ExecuteNonQuery();
                }
            }


        }

        static string Qoute(string value) => "`" + value.Replace("`", "``") + "`";

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
