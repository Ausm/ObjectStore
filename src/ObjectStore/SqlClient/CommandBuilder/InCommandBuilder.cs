using ObjectStore.OrMapping;
using System;
using System.Text;

namespace ObjectStore.SqlClient
{
    internal class InCommandBuilder : SubQueryCommandBuilder
    {
        public InCommandBuilder(string outherAlias, DataBaseProvider databaseProvider) : base(databaseProvider)
        {
            _outherAlias = outherAlias;
            _keyFieldName = string.Empty;
        }

        string _outherAlias;
        string _keyFieldName;

        public override void AddField(string fieldname, FieldType fieldtype)
        {
            if (fieldtype == FieldType.KeyField)
            {
                if (_keyFieldName != string.Empty && _keyFieldName != fieldname)
                    throw new InvalidOperationException("Queryable-Contains is only supported vor types with unsegmented Keys");

                _keyFieldName = fieldname;
            }
            base.AddField(fieldname, fieldtype);
        }


        public override string SubQuery
        {
            get
            {
                if (string.IsNullOrEmpty(Tablename)) throw new InvalidOperationException("Tablename is not set.");

                StringBuilder stringBuilder = new StringBuilder();

                stringBuilder.AppendFormat("{0}.{1} IN (SELECT {2}.{1} FROM {3} {2}", _outherAlias, _keyFieldName, Alias, Tablename);

                string whereClause = GetWhereClause();

                foreach (Join join in Joins)
                    stringBuilder.Append(" LEFT OUTER ").Append(join);

                if (string.IsNullOrEmpty(whereClause))
                    stringBuilder.Append(")");
                else
                    stringBuilder.AppendFormat(" WHERE {0})", whereClause);

                return stringBuilder.ToString();
            }
        }
    }
}
