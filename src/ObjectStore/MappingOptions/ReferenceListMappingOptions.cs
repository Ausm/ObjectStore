using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace ObjectStore.MappingOptions
{
    public class ReferenceListMappingOptions : MemberMappingOptions
    {
        internal ReferenceListMappingOptions(MappingOptionsSet mappingOptionsSet, PropertyInfo member) : base(mappingOptionsSet, member)
        {
            Conditions = new Dictionary<PropertyInfo, object>();
        }

        public override MappingType Type => MappingType.ReferenceListMapping;

        public Dictionary<PropertyInfo, object> Conditions { get; private set; }

        public PropertyInfo ForeignProperty { get; set; }

        public override bool IsUpdateable
        {
            get
            {
                return false;
            }

            set
            {
            }
        }

        public override bool IsInsertable
        {
            get
            {
                return false;
            }

            set
            {
            }
        }

        public override bool IsPrimaryKey
        {
            get
            {
                return false;
            }

            set { }
        }

        public bool SaveCascade { get; set; } = true;
        public bool DeleteCascade { get; set; } = true;
        public bool DropChangesCascade { get; set; } = true;
    }
}
