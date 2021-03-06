﻿using System;
using System.Collections.Generic;

namespace ObjectStore.SqlClient
{
    internal class KeyInitializer
    {
        #region Multington-Implementierung *g*
        private static Dictionary<Type, KeyInitializer> keyInitializer = new Dictionary<Type, KeyInitializer>();

        public static void RegisterKeyInitializer<T>(string beforInsert, string afterInsert, bool setInInsert, System.Data.SqlDbType sqlDbType, T emptyValue)
        {
            keyInitializer.Add(typeof(T), new KeyInitializer(beforInsert, afterInsert, setInInsert, sqlDbType, emptyValue));
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
            RegisterKeyInitializer<Guid>("SET {parameter} = NEWID()", string.Empty, true, System.Data.SqlDbType.UniqueIdentifier, Guid.Empty);
            RegisterKeyInitializer<int>(string.Empty, "SET {parameter} = ISNULL(SCOPE_IDENTITY(), @@IDENTITY)", false, System.Data.SqlDbType.BigInt, default(int));
            RegisterKeyInitializer<short>(string.Empty, "SET {parameter} = ISNULL(SCOPE_IDENTITY(), @@IDENTITY)", false, System.Data.SqlDbType.SmallInt, default(short));
            RegisterKeyInitializer<long>(string.Empty, "SET {parameter} = ISNULL(SCOPE_IDENTITY(), @@IDENTITY)", false, System.Data.SqlDbType.BigInt, default(long));
        }
        #endregion

        #region Membervariablen
        object _emptyValue;
        string _beforInsert;
        string _afterInsert;
        bool _setInInsert;
        System.Data.SqlDbType _sqlDbType;
        #endregion

        #region Konstruktoren
        public KeyInitializer(string beforInsert, string afterInsert, bool setInInsert, System.Data.SqlDbType sqlDbType, object emptyValue)
        {
            _beforInsert = beforInsert;
            _afterInsert = afterInsert;
            _emptyValue = emptyValue;
            _setInInsert = setInInsert;
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
        public bool SetInInsert
        {
            get
            {
                return _setInInsert;
            }
        }

        public string BeforInsert
        {
            get
            {
                return _beforInsert;
            }
        }

        public string AfterInsert
        {
            get
            {
                return _afterInsert;
            }
        }

        public System.Data.SqlDbType SqlDbType
        {
            get
            {
                return _sqlDbType;
            }
        }
        #endregion
    }
}
