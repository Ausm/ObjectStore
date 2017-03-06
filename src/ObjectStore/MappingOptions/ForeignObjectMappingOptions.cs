using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace ObjectStore.MappingOptions
{
    public class ForeignObjectMappingOptions : FieldMappingOptions
    {
        MappingOptionsSet _parent;
        MemberMappingOptions _foreignMember;

        internal ForeignObjectMappingOptions(MappingOptionsSet mappingOptionsSet, PropertyInfo member) : base(mappingOptionsSet, member)
        {
            _parent = mappingOptionsSet;
            ForeignObjectType = member.PropertyType;
        }

        public override MappingType Type => MappingType.ForeignObjectMapping;

        public Type ForeignObjectType { get; set; }

        public Type KeyType
        {
            get
            {
                Type foreignPropertyType = null;
                if (ForeignMember is ForeignObjectMappingOptions)
                {
                    ForeignObjectMappingOptions currentOption = (ForeignObjectMappingOptions)ForeignMember;
                    while (true)
                    {
                        if (currentOption.ForeignMember is ForeignObjectMappingOptions)
                        {
                            currentOption = (ForeignObjectMappingOptions)currentOption.ForeignMember;
                            if (currentOption.ForeignMember == ForeignMember)
                                throw new InvalidOperationException($"Circlereference with foreign object mappings. Property: {Member}");
                            continue;
                        }

                        foreignPropertyType = currentOption.ForeignMember.Member.PropertyType;
                        break;
                    }
                }
                else
                    foreignPropertyType = ForeignMember.Member.PropertyType;


                if (foreignPropertyType.GetTypeInfo().IsValueType && Nullable.GetUnderlyingType(foreignPropertyType) == null)
                    return typeof(Nullable<>).MakeGenericType(foreignPropertyType);
                else
                    return foreignPropertyType;
            }
        }

        internal MemberMappingOptions ForeignMember
        {
            get
            {
                if (_foreignMember != null)
                    return _foreignMember;

                List<MemberMappingOptions> foreignPrimaryKeys = _parent.GetTypeMappingOptions((Member as PropertyInfo).PropertyType).MemberMappingOptions.Where(x => x.IsPrimaryKey).ToList();
                if(foreignPrimaryKeys.Count > 1)
                    throw new Exception("ForeignObject must have only one IsPrimaryKey marked property.");
                else if(foreignPrimaryKeys.Count == 0)
                    throw new Exception("ForeignObject has no property which is market with IsPrimaryKey.");

                return _foreignMember = _parent.GetTypeMappingOptions((Member as PropertyInfo).PropertyType).MemberMappingOptions.Where(x => x.IsPrimaryKey).First();
            }
        }

    }
}
