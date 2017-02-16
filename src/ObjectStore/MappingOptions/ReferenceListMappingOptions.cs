using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace ObjectStore.MappingOptions
{
    public class ReferenceListMappingOptions : MemberMappingOptions
    {
        internal ReferenceListMappingOptions(MappingOptionsSet mappingOptionsSet, PropertyInfo member) : base(mappingOptionsSet, member) { }

        public override MappingType Type => MappingType.ReferenceListMapping;

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
    }
}
