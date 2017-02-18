using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace ObjectStore.MappingOptions
{
    public abstract class MemberMappingOptions
    {
        MappingOptionsSet _mappingOptionsSet;
        PropertyInfo _member;

        internal MemberMappingOptions(MappingOptionsSet mappingOptionsSet, PropertyInfo member)
        {
            _mappingOptionsSet = mappingOptionsSet;
            _member = member;
        }

        public abstract MappingType Type { get; }

        public PropertyInfo Member => _member;

        public virtual bool IsPrimaryKey { get; set; } = false;

        public virtual bool IsInsertable { get; set; } = true;

        public virtual bool IsUpdateable { get; set; } = true;

        public virtual bool IsReadonly
        {
            get
            {
                return !IsInsertable && !IsUpdateable;
            }
            set
            {
                if (value != IsReadonly)
                {
                    IsInsertable =
                    IsUpdateable = !value;
                }
            }
        }

    }
}
