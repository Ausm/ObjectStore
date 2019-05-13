using System;
using System.Reflection;

namespace ObjectStore
{
    [global::System.AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, Inherited = true, AllowMultiple = false)]
    public sealed class TableAttribute : Attribute
    {
        public TableAttribute(string tableName)
        {
            TableName = tableName;
            LoadBehavior = LoadBehavior.OnDemandPartialLoad;
        }

        public TableAttribute(string tableName, LoadBehavior loadBehavior)
        {
            TableName = tableName;
            LoadBehavior = loadBehavior; 
        }

        public string TableName { get; }

        public LoadBehavior LoadBehavior { get; }
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
        public ForeignObjectMappingAttribute(string fieldname)
        {
            Fieldname = fieldname;
            Insertable = Updateable = true;
        }

        public string Fieldname { get; }

        public bool Insertable { get; set; }

        public bool Updateable { get; set; }

        public bool ReadOnly
        {
            get
            {
                return !(Insertable || Updateable);
            }
            set
            {
                Insertable = Updateable = !value;
            }
        }

        public Type ForeignObjectType { get; set; }

    }

    [global::System.AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
    public sealed class ReferenceListMappingAttribute : Attribute
    {
        public ReferenceListMappingAttribute(Type foreignType, string foreignPropertyName)
        {
            ForeignType = foreignType;
            ForeignProperty = foreignType.GetProperty(foreignPropertyName);
            DeleteCascade = true;
            SaveCascade = true;
            DropChangesCascade = true;
        }

        public PropertyInfo ForeignProperty { get; }
        public Type ForeignType { get; }
        public bool DeleteCascade { get; set; }
        public bool SaveCascade { get; set; }
        public bool DropChangesCascade { get; set; }
    }

    [global::System.AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = true)]
    public sealed class EqualsObjectConditionAttribute : Attribute
    {
        private EqualsObjectConditionAttribute(string propertyName, object equalValue)
        {
            PropertyName = propertyName;
            Value = equalValue;
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

        public string PropertyName { get; }

        public object Value { get; }
    }


    [global::System.AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public sealed class IsRaisePropertyChangeMethodAttribute : Attribute { }

    [global::System.AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public sealed class SqlSubstituteAttribute : Attribute 
    {
        public SqlSubstituteAttribute(string sqlSubstitution)
        {
            SqlSubstitution = sqlSubstitution;
        }

        public string SqlSubstitution { get; }
    }

}