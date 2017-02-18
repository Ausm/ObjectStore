using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace ObjectStore.MappingOptions
{
    public class FieldMappingOptions : MemberMappingOptions
    {
        internal FieldMappingOptions(MappingOptionsSet mappingOptionsSet, PropertyInfo member) : base(mappingOptionsSet, member)
        {
            DatabaseFieldName = member.Name;
        }

        public override MappingType Type => MappingType.FieldMapping;

        public string DatabaseFieldName { get; set; }
    }
}
