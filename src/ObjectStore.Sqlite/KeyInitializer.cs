using System;
using System.Collections.Generic;

namespace ObjectStore.Sqlite
{
    internal class KeyInitializer
    {
        #region Multington-Implementierung *g*
        private static Dictionary<Type, KeyInitializer> keyInitializer = new Dictionary<Type, KeyInitializer>();

        public static void RegisterKeyInitializer<T>(Microsoft.Data.Sqlite.SqliteType sqlDbType, T emptyValue)
        {
            keyInitializer.Add(typeof(T), new KeyInitializer(sqlDbType, emptyValue));
        }

        public static KeyInitializer GetInitializer(Type type)
        {
            if (keyInitializer.ContainsKey(type))
            {
                return keyInitializer[type];
            }
            return null;
        }

        static KeyInitializer()
        {
            //RegisterKeyInitializer<Guid>("SET {parameter} = NEWID()", string.Empty, true, Microsoft.Data.Sqlite.SqliteType.UniqueIdentifier, Guid.Empty);
            RegisterKeyInitializer<int>(Microsoft.Data.Sqlite.SqliteType.Integer, default(int));
            //RegisterKeyInitializer<short>(string.Empty, "SET {parameter} = ISNULL(SCOPE_IDENTITY(), @@IDENTITY)", false, System.Data.SqlDbType.SmallInt, default(short));
            //RegisterKeyInitializer<long>(string.Empty, "SET {parameter} = ISNULL(SCOPE_IDENTITY(), @@IDENTITY)", false, System.Data.SqlDbType.BigInt, default(long));
        }
        #endregion

        #region Membervariablen
        object _emptyValue;
        Microsoft.Data.Sqlite.SqliteType _sqlDbType;
        #endregion

        #region Konstruktoren
        public KeyInitializer(Microsoft.Data.Sqlite.SqliteType sqlDbType, object emptyValue)
        {
            _emptyValue = emptyValue;
            _sqlDbType = sqlDbType;
        }
        #endregion

        #region Funktionen
        public bool CheckEmpty(object value)
        {
            return _emptyValue.Equals(value);
        }
        #endregion

        #region Properties

        public string GetWhereClause(string identityFieldname)
        {
            return $"{identityFieldname} = last_insert_rowid()";
        }

        public Microsoft.Data.Sqlite.SqliteType SqlDbType
        {
            get
            {
                return _sqlDbType;
            }
        }
        #endregion
    }
}
