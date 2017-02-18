using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace ObjectStore.MappingOptions
{
    public class MemberTypeOptions
    {
        #region Fields
        MemberInfo _member;
        #endregion

        #region Constructor
        internal MemberTypeOptions(MemberInfo member)
        {
            _member = member;
        }
        #endregion

        #region Properties
        public MemberInfo Member => _member;

        public MappingType MappingType { get; set; } = MappingType.FieldMapping;
        #endregion

    }

    public enum MappingType
    {
        FieldMapping,
        ForeignObjectMapping,
        ReferenceListMapping
    }

}
