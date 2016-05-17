#if  NETCOREAPP1_0
using ApplicationException = global::System.InvalidOperationException;
#endif

using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Linq;

namespace ObjectStore.OrMapping
{
    internal class ForeignObjectPropertyMapping : PropertyMapping
    {
        ForeignObjectMappingAttribute _foreignObjectMappingAttribute;
        PropertyInfo _foreignProperty;

        public ForeignObjectPropertyMapping(PropertyInfo info)
            : base(info)
        {
            _propertyInfo = info;

            object[] attributes =
#if  NETCOREAPP1_0
                info.GetCustomAttributes(typeof(ForeignObjectMappingAttribute), true).ToArray();
#else
                info.GetCustomAttributes(typeof(ForeignObjectMappingAttribute), true);
#endif
            if (attributes.Length > 0)
            {
                _foreignObjectMappingAttribute = attributes[0] as ForeignObjectMappingAttribute;
            }
            else
            {
                throw new ApplicationException("PropertyInfo has no ForeignObjectMappingAttribute set.");
            }

            PropertyInfo[] propertyInfos = _propertyInfo.PropertyType.GetProperties().Where(x => x.GetCustomAttributes(typeof(IsPrimaryKeyAttribute), true).Any()).ToArray();
            if (propertyInfos.Length != 1)
                throw new ApplicationException("ForeignObject must have only one IsPrimaryKey marked property.");

            _foreignProperty = propertyInfos[0];
        }

        public override string FieldName
        {
            get
            {
                return _foreignObjectMappingAttribute.Fieldname;
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
            Type backingStoreType = typeof(ForeignObjectBackingStore<,>).MakeGenericType(_foreignObjectMappingAttribute.ForeignObjectType ?? _propertyInfo.PropertyType, DataBaseValueType);
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
                ilGenerator.Emit(OpCodes.Call, backingStoreType.GetProperty("Value").GetSetMethod());
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
#if  NETCOREAPP1_0
                if (_foreignProperty.PropertyType.GetTypeInfo().IsValueType && !(_foreignProperty.PropertyType.GetTypeInfo().IsGenericType && _foreignProperty.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>)))
#else
                if (_foreignProperty.PropertyType.IsValueType && !(_foreignProperty.PropertyType.IsGenericType && _foreignProperty.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>)))
#endif
                    return typeof(Nullable<>).MakeGenericType(_foreignProperty.PropertyType);
                else
                    return _foreignProperty.PropertyType;
            }
        }

        public override bool IsReadOnly
        {
            get
            {
                return _foreignObjectMappingAttribute.ReadOnly;
            }
        }

        public override bool IsInsertable
        {
            get
            {
                return _foreignObjectMappingAttribute.Insertable;
            }
        }

        public override bool IsUpdateAble
        {
            get
            {
                return _foreignObjectMappingAttribute.Updateable;
            }
        }


    }
}