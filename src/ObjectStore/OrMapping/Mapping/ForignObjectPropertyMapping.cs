#if  NETCOREAPP1_0
using ApplicationException = global::System.InvalidOperationException;
#endif

using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Linq;
using ObjectStore.MappingOptions;

namespace ObjectStore.OrMapping
{
    internal class ForeignObjectPropertyMapping : PropertyMapping
    {
        ForeignObjectMappingOptions _options;

        public ForeignObjectPropertyMapping(ForeignObjectMappingOptions options)
            : base(options)
        {
            _options = options;
        }

        public override string FieldName
        {
            get
            {
                return _options.DatabaseFieldName;
            }
        }

        public override void AddInhertiedProperty(TypeBuilder typeBuilder, MethodInfo notifyPropertyChangedMethode)
        {
#if  NETCOREAPP1_0
            if (!MemberInfo.DeclaringType.GetTypeInfo().IsAbstract)
#else
            if (!MemberInfo.DeclaringType.IsAbstract)
#endif
                throw new NotSupportedException("Inherited properties are only possible for Interfaces and abstract classes.");

#region Member Definieren
            Type backingStoreType = typeof(ForeignObjectBackingStore<,>).MakeGenericType(_options.ForeignObjectType, DataBaseValueType);
            _internalField = typeBuilder.DefineField("__field" + MemberInfo.Name, backingStoreType, FieldAttributes.Private);
            MethodBuilder getMethod = typeBuilder.DefineMethod("get_" + MemberInfo.Name, MethodAttributes.Public | MethodAttributes.Virtual, _propertyInfo.PropertyType, Type.EmptyTypes);
            MethodBuilder setMethod = _propertyInfo.CanWrite ? typeBuilder.DefineMethod("set_" + MemberInfo.Name, MethodAttributes.Public | MethodAttributes.Virtual, null, new Type[] { _propertyInfo.PropertyType }) : null;
#endregion

#region Get und Set-Code schreiben
            ILGenerator ilGenerator = getMethod.GetILGenerator();
            ilGenerator.Emit(OpCodes.Ldarg_0);
            ilGenerator.Emit(OpCodes.Ldflda, _internalField);
            ilGenerator.Emit(OpCodes.Call, backingStoreType.GetProperty("Value").GetGetMethod());
            ilGenerator.Emit(OpCodes.Ret);

            if (setMethod != null)
            {
                ilGenerator = setMethod.GetILGenerator();
                ilGenerator.Emit(OpCodes.Ldarg_0);
                ilGenerator.Emit(OpCodes.Ldflda, _internalField);
                ilGenerator.Emit(OpCodes.Ldarg_1);
                ilGenerator.Emit(OpCodes.Call, backingStoreType.GetProperty(nameof(ForeignObjectBackingStore<object,int>.Value)).GetSetMethod());
                ilGenerator.Emit(OpCodes.Ldarg_0);
                ilGenerator.Emit(OpCodes.Ldstr, _propertyInfo.Name);
                ilGenerator.Emit(OpCodes.Callvirt, notifyPropertyChangedMethode);
                ilGenerator.Emit(OpCodes.Ret);
            }
#endregion

#region Member zuweisen
            typeBuilder.DefineMethodOverride(getMethod, _propertyInfo.GetGetMethod());
            if (setMethod != null)
                typeBuilder.DefineMethodOverride(setMethod, _propertyInfo.GetSetMethod());
#endregion

        }

        protected override Type DataBaseValueType
        {
            get
            {
                Type foreignPropertyType = null;
                if (_options.ForeignMember is ForeignObjectMappingOptions)
                {
                    ForeignObjectMappingOptions currentOption = (ForeignObjectMappingOptions)_options.ForeignMember;
                    while (true)
                    {
                        if (currentOption.ForeignMember is ForeignObjectMappingOptions)
                        {
                            currentOption = (ForeignObjectMappingOptions)currentOption.ForeignMember;
                            if (currentOption.ForeignMember == _options.ForeignMember)
                                throw new ApplicationException($"Circlereference with foreign object mappings. Property: {_options.Member}");
                            continue;
                        }

                        foreignPropertyType = currentOption.ForeignMember.Member.PropertyType;
                        break;
                    }
                }
                else
                    foreignPropertyType = _options.ForeignMember.Member.PropertyType;


                if(foreignPropertyType.GetTypeInfo().IsValueType && Nullable.GetUnderlyingType(foreignPropertyType) == null)
                    return typeof(Nullable<>).MakeGenericType(foreignPropertyType);
                else
                    return foreignPropertyType;
            }
        }
    }
}