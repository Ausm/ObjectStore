using System;
using System.Collections.Generic;
using System.Text;
using System.Data.SqlClient;
using System.Reflection;

namespace Ausm.ObjectStore.OrMapping
{
    [global::System.AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, Inherited = true, AllowMultiple = false)]
    public sealed class TableAttribute : Attribute
    {
        readonly string _tableName;
        readonly LoadBehavior _loadBehavior;
        public TableAttribute(string tableName)
        {
            _tableName = tableName;
            _loadBehavior = LoadBehavior.OnDemandPartialLoad;
        }

        public TableAttribute(string tableName, LoadBehavior loadBehavior)
        {
            _tableName = tableName;
            _loadBehavior = loadBehavior; 
        }

        public string TableName
        {
            get { return _tableName; }
        }

        public LoadBehavior LoadBehavior
        {
            get { return _loadBehavior; }
        }
    }

    public enum LoadBehavior
    {
        OnFirstAccessFullLoad,
        OnDemandPartialLoad,
        OnForceOnlyLoad
    }

    [global::System.AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
    public sealed class IsPrimaryKeyAttribute : Attribute
    { }

    [global::System.AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
    public sealed class DisableMappingAttribute : Attribute
    {}

    [global::System.AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
    public sealed class MappingAttribute : Attribute
    {
        #region MemberVariablen
        string _fieldname;
        bool _insertable;
        bool _updateable;
        #endregion

        #region Konstruktor
        public MappingAttribute()
        {
            _fieldname = null;
            _insertable = 
            _updateable = true;
        }
        #endregion

        #region Properties
        public string FieldName
        {
            get
            {
                return _fieldname;
            }
            set
            {
                _fieldname = value;
            }
        }

        public bool Insertable
        {
            get
            {
                return _insertable;
            }
            set
            {
                _insertable = value;
            }
        }

        public bool Updateable
        {
            get
            {
                return _updateable;
            }
            set
            {
                _updateable = value;
            }
        }

        public bool ReadOnly
        {
            get
            {
                return !(_insertable || _updateable);
            }
            set
            {
                _insertable = _updateable = !value;
            }
        }
        #endregion
    }

    [global::System.AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
    public sealed class ForeignObjectMappingAttribute : Attribute
    {
        string _fieldname;
        bool _insertable;
        bool _updateable;
        Type _foreignObjectType;

        public ForeignObjectMappingAttribute(string fieldname)
        {
            _fieldname = fieldname;
            _insertable = _updateable = true;
        }

        public string Fieldname { get { return _fieldname; } }

        public bool Insertable
        {
            get
            {
                return _insertable;
            }
            set
            {
                _insertable = value;
            }
        }

        public bool Updateable
        {
            get
            {
                return _updateable;
            }
            set
            {
                _updateable = value;
            }
        }

        public bool ReadOnly
        {
            get
            {
                return !(_insertable || _updateable);
            }
            set
            {
                _insertable = _updateable = !value;
            }
        }

        public Type ForeignObjectType
        {
            get
            {
                return _foreignObjectType;
            }
            set
            {
                _foreignObjectType = value;
            }
        }

    }

    [global::System.AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
    public sealed class ReferenceListMappingAttribute : Attribute
    {
        PropertyInfo _foreignProperty;
        Type _foreignType;
        bool _deleteCascade;
        bool _saveCascade;
        bool _dropChangesCascade;

        public ReferenceListMappingAttribute(Type foreignType, string foreignPropertyName)
        {
            _foreignType = foreignType;
            _foreignProperty = foreignType.GetProperty(foreignPropertyName);
            _deleteCascade = true;
            _saveCascade = true;
            _dropChangesCascade = true;
        }

        public PropertyInfo ForeignProperty { get { return _foreignProperty; } }
        public Type ForeignType { get { return _foreignType; } }
        public bool DeleteCascade { get { return _deleteCascade; } set { _deleteCascade = value; } }
        public bool SaveCascade { get { return _saveCascade; } set { _saveCascade = value; } }
        public bool DropChangesCascade { get { return _dropChangesCascade; } set { _dropChangesCascade = value; } }
    }

    [global::System.AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = true)]
    public sealed class EqualsObjectConditionAttribute : Attribute
    {
        string _propertyName;
        object _equalValue;

        private EqualsObjectConditionAttribute(string propertyName, object equalValue)
        {
            _propertyName = propertyName;
            _equalValue = equalValue;
        }

        public EqualsObjectConditionAttribute(string propertyName, byte equalValue)
            : this (propertyName, (object)equalValue)
        {
        }

        public EqualsObjectConditionAttribute(string propertyName, string equalValue)
            : this(propertyName, (object)equalValue)
        {
        }

        public EqualsObjectConditionAttribute(string propertyName, int equalValue)
            : this(propertyName, (object)equalValue)
        {
        }

        public string PropertyName
        {
            get
            {
                return _propertyName;
            }
        }

        public object Value
        {
            get
            {
                return _equalValue;
            }
        }
    }


    [global::System.AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public sealed class IsRaisePropertyChangeMethodAttribute : Attribute { }

    [global::System.AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public sealed class SqlSubstituteAttribute : Attribute 
    {
        string _sqlSubstitution;

        public SqlSubstituteAttribute(string sqlSubstitution)
        {
            _sqlSubstitution = sqlSubstitution;
        }

        public string SqlSubstitution
        {
            get
            {
                return _sqlSubstitution;
            }
        }
    }

}