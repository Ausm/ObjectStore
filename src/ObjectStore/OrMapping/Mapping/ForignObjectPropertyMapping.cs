#if  NETCOREAPP1_0
using ApplicationException = global::System.InvalidOperationException;
#endif

using System;
using System.Reflection;
using System.Reflection.Emit;
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
            MethodBuilder getMethod = typeBuilder.DefineMethod("get_" + MemberInfo.Name, MethodAttributes.Public | MethodAttributes.Virtual, _options.Member.PropertyType, Type.EmptyTypes);
            MethodBuilder setMethod = _options.Member.CanWrite ? typeBuilder.DefineMethod("set_" + MemberInfo.Name, MethodAttributes.Public | MethodAttributes.Virtual, null, new Type[] { _options.Member.PropertyType }) : null;
#endregion

#region Get und Set-Code schreiben
            ILGenerator ilGenerator = getMethod.GetILGenerator();
            ilGenerator.Emit(OpCodes.Ldarg_0);
            ilGenerator.Emit(OpCodes.Ldflda, _internalField);
            ilGenerator.Emit(OpCodes.Call, backingStoreType.GetProperty(nameof(ForeignObjectBackingStore<object, int>.Value)).GetGetMethod());
            ilGenerator.Emit(OpCodes.Ret);

            if (setMethod != null)
            {
                ilGenerator = setMethod.GetILGenerator();
                ilGenerator.Emit(OpCodes.Ldarg_0);
                ilGenerator.Emit(OpCodes.Ldflda, _internalField);
                ilGenerator.Emit(OpCodes.Ldarg_1);
                ilGenerator.Emit(OpCodes.Call, backingStoreType.GetProperty(nameof(ForeignObjectBackingStore<object,int>.Value)).GetSetMethod());
                ilGenerator.Emit(OpCodes.Ldarg_0);
                ilGenerator.Emit(OpCodes.Ldstr, _options.Member.Name);
                ilGenerator.Emit(OpCodes.Callvirt, notifyPropertyChangedMethode);
                ilGenerator.Emit(OpCodes.Ret);
            }
#endregion

#region Member zuweisen
            typeBuilder.DefineMethodOverride(getMethod, _options.Member.GetGetMethod());
            if (setMethod != null)
                typeBuilder.DefineMethodOverride(setMethod, _options.Member.GetSetMethod());
#endregion

        }

        protected override Type DataBaseValueType => _options.KeyType;
    }
}