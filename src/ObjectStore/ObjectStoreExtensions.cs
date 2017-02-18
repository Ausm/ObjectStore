using ObjectStore.OrMapping;
using ObjectStore.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using ObjectStore.MappingOptions;
using System.Reflection;
using System;
using System.Linq;

namespace ObjectStore
{
    public static class ObjectStoreExtensions
    {
        public static void AddObjectStore(this IServiceCollection services, IDataBaseProvider databaseProvider, string connectionString, MappingOptionsSet mappingOptionsSet = null)
        {
            if (mappingOptionsSet == null)
                mappingOptionsSet = new MappingOptionsSet().AddDefaultRules();

            RelationalObjectStore relationalObjectStore = new RelationalObjectStore(connectionString, databaseProvider, mappingOptionsSet, true);
            ObjectStoreManager.DefaultObjectStore.RegisterObjectProvider(relationalObjectStore);
            services.Add(new ServiceDescriptor(typeof(IObjectProvider), relationalObjectStore));
        }

        public static MappingOptionsSet AddDefaultRules(this MappingOptionsSet mappingOptionsSet)
        {
            mappingOptionsSet.AddTypeRule(x => !x.GetTypeInfo().IsAbstract, o => { throw new NotSupportedException("InheritedPropertyMapping supports abstract classes and interfaces only."); });
            mappingOptionsSet.AddTypeRule(x => x.GetCustomAttribute<TableAttribute>() != null, o => {
                    TableAttribute attribute = o.Type.GetCustomAttribute<TableAttribute>();
                    o.TableName = attribute.TableName;
                    o.LoadBehavior = attribute.LoadBehavior;
                });

            mappingOptionsSet.AddMemberTypeRule(x => x.GetCustomAttribute<ForeignObjectMappingAttribute>() != null, x => { x.MappingType = MappingType.ForeignObjectMapping; });
            mappingOptionsSet.AddMemberTypeRule(x => x.GetCustomAttribute<ReferenceListMappingAttribute>() != null, x => { x.MappingType = MappingType.ReferenceListMapping; });

            mappingOptionsSet.AddMemberMappingRule(x => x.GetCustomAttribute<ForeignObjectMappingAttribute>() != null, o => {
                ForeignObjectMappingOptions options = (ForeignObjectMappingOptions)o;
                ForeignObjectMappingAttribute attribute = options.Member.GetCustomAttribute<ForeignObjectMappingAttribute>();
                options.DatabaseFieldName = attribute.Fieldname;

                if(attribute.ForeignObjectType != null)
                    options.ForeignObjectType = attribute.ForeignObjectType;

                if (attribute.ReadOnly)
                    options.IsReadonly = true;
                else
                {
                    options.IsInsertable = attribute.Insertable;
                    options.IsUpdateable = attribute.Updateable;
                }
                options.IsPrimaryKey = options.Member.GetCustomAttribute<IsPrimaryKeyAttribute>() != null;
            });

            mappingOptionsSet.AddMemberMappingRule(x => x.GetCustomAttribute<ReferenceListMappingAttribute>() != null, o => {
                ReferenceListMappingOptions options = (ReferenceListMappingOptions)o;
                ReferenceListMappingAttribute attribute = options.Member.GetCustomAttribute<ReferenceListMappingAttribute>();

                options.SaveCascade = attribute.SaveCascade;
                options.DeleteCascade = attribute.DeleteCascade;
                options.DropChangesCascade = attribute.DropChangesCascade;

                options.ForeignProperty = attribute.ForeignProperty ?? attribute.ForeignType.GetProperties().Where(x => x.PropertyType == options.Member.DeclaringType).Single();
                foreach (EqualsObjectConditionAttribute equalsObjectAttribute in options.Member.GetCustomAttributes<EqualsObjectConditionAttribute>())
                    options.Conditions.Add(attribute.ForeignType.GetProperty(equalsObjectAttribute.PropertyName), equalsObjectAttribute.Value);
            });

            mappingOptionsSet.AddMemberMappingRule(x => x.GetCustomAttribute<ForeignObjectMappingAttribute>() == null && x.GetCustomAttribute<ReferenceListMappingAttribute>() == null, o => {
                FieldMappingOptions options = (FieldMappingOptions)o;
                MappingAttribute attribute = options.Member.GetCustomAttribute<MappingAttribute>();
                if (attribute != null)
                {
                    options.DatabaseFieldName = attribute.FieldName ?? options.DatabaseFieldName;
                    if (attribute.ReadOnly)
                        options.IsReadonly = true;
                    else
                    {
                        options.IsInsertable = attribute.Insertable;
                        options.IsUpdateable = attribute.Updateable;
                    }
                }
                options.IsPrimaryKey = options.Member.GetCustomAttribute<IsPrimaryKeyAttribute>() != null;
            });

            return mappingOptionsSet;
        }


        static T GetCustomAttribute<T>(this Type type) where T : Attribute
        {
            return type.GetTypeInfo().GetCustomAttribute(typeof(T), true) as T;
        }

        static T GetCustomAttribute<T>(this MemberInfo memberInfo) where T : Attribute
        {
            return memberInfo.GetCustomAttribute(typeof(T), true) as T;
        }

    }
}
