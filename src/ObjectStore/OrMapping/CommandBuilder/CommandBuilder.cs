using System.Collections.Generic;
using System.Data.Common;

namespace ObjectStore.OrMapping
{
    public interface ICommandBuilder
    {
        void AddField(string fieldname, FieldType fieldtype);

        void AddField(string fieldname, object value, FieldType fieldtype, KeyInitializer keyInitializer, bool isChanged);

        string Tablename { get; set; }
    }

    public interface IDbCommandBuilder : ICommandBuilder
    {
        DbCommand GetDbCommand();
    }

    public interface ISelectCommandBuilder : IDbCommandBuilder, IModifyableCommandBuilder
    {
    }

    public interface IModifyableCommandBuilder : ICommandBuilder
    {
        void AddJoin(string tablename, string onClausel);

        IEnumerable<DbParameter> Parameters { get; }

        DbParameter AddDbParameter(object value);

        string Alias { get; }

        string WhereClausel { get; set; }

        void SetOrderBy(string expression);

        void SetTop(int count);
    }

    public interface ISubQueryCommandBuilder : IModifyableCommandBuilder
    {
        string SubQuery { get; }
    }

    public enum FieldType
    {
        ReadOnlyField   = 0x0,
        InsertableField = 0x1,
        UpdateableField = 0x2,
        WriteableField  = 0x3,
        KeyField        = 0x4
    }
}
