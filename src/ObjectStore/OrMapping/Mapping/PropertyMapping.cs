using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Linq;

namespace ObjectStore.OrMapping
{
    internal class PropertyMapping : Mapping
    {
        MappingAttribute _mappingAttribute;
        protected FieldBuilder _internalField;
        protected PropertyInfo _propertyInfo;

        public PropertyMapping(PropertyInfo propertyInfo) : base(propertyInfo)
        {
            _internalField = null;
            _propertyInfo = propertyInfo;

            MappingAttribute attribute =
#if  NETCOREAPP1_0
                _propertyInfo.GetCustomAttribute(typeof(MappingAttribute), true) as MappingAttribute;
#else
                _propertyInfo.GetCustomAttributes(typeof(MappingAttribute), true).FirstOrDefault() as MappingAttribute;
#endif
            _mappingAttribute = attribute ?? new MappingAttribute();
        }

#region Dynamic Code
        public override void AddDynamicGetKeyCode(ILGenerator dynamicMethod, int index, LocalBuilder array)
        {
            if (!IsPrimaryKey)
                return;
            // Array in Stack Laden
            dynamicMethod.Emit(OpCodes.Ldloc, array);
            dynamicMethod.Emit(OpCodes.Ldc_I4, index);
            // Object in Stack Laden und Casten
            dynamicMethod.Emit(OpCodes.Ldarg_0);
            dynamicMethod.Emit(OpCodes.Ldflda, _internalField);
            dynamicMethod.Emit(OpCodes.Call, _internalField.FieldType.GetMethod("GetUncommittedValue"));

#if  NETCOREAPP1_0
            if (DataBaseValueType.GetTypeInfo().IsValueType)
#else
            if (DataBaseValueType.IsValueType)
#endif
            {
                dynamicMethod.Emit(OpCodes.Box, DataBaseValueType);
            }
            dynamicMethod.Emit(OpCodes.Stelem_Ref);
        }

        public override void AddDynamicGetKeyFromReaderCode(ILGenerator dynamicMethod, int index, LocalBuilder array)
        {
            if (!IsPrimaryKey)
                return;

            MethodInfo getValueMethod = typeof(IValueSource).GetMethod(nameof(IValueSource.GetValue), new Type[] { typeof(string) }).MakeGenericMethod(typeof(object));

            // Array in Stack Laden
            dynamicMethod.Emit(OpCodes.Ldloc, array);
            dynamicMethod.Emit(OpCodes.Ldc_I4, index);
            // Object in Stack Laden und Casten
            dynamicMethod.Emit(OpCodes.Ldarg_0);
            dynamicMethod.Emit(OpCodes.Ldstr, RemoveBrackets(FieldName));
            dynamicMethod.Emit(OpCodes.Callvirt, getValueMethod);
            dynamicMethod.Emit(OpCodes.Stelem_Ref);
        }

        public override void AddInhertiedProperty(TypeBuilder typeBuilder, MethodInfo notifyPropertyChangedMethode)
        {
#if  NETCOREAPP1_0
            if (!MemberInfo.DeclaringType.GetTypeInfo().IsAbstract)
#else
            if (!MemberInfo.DeclaringType.IsAbstract)
#endif
                throw new NotSupportedException("Inherited properties are only possible for Interfaces and abstract classes.");

            if(!_propertyInfo.CanWrite)
                _mappingAttribute.ReadOnly = true;

#region Member Definieren
            Type backingStoreType =
                 IsReadOnly ? 
                    typeof(ReadOnlyBackingStore<>).MakeGenericType(_propertyInfo.PropertyType) :
                    typeof(BackingStore<>).MakeGenericType(_propertyInfo.PropertyType);
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
                if (IsReadOnly)
                    ilGenerator.Emit(OpCodes.Ret);
                else
                {
                    ilGenerator.Emit(OpCodes.Ldarg_0);
                    ilGenerator.Emit(OpCodes.Ldflda, _internalField);
                    ilGenerator.Emit(OpCodes.Ldarg_1);
                    ilGenerator.Emit(OpCodes.Call, backingStoreType.GetProperty("Value").GetSetMethod());
                    ilGenerator.Emit(OpCodes.Ldarg_0);
                    ilGenerator.Emit(OpCodes.Ldstr, _propertyInfo.Name);
                    ilGenerator.Emit(OpCodes.Callvirt, notifyPropertyChangedMethode);
                    ilGenerator.Emit(OpCodes.Ret);
                }
            }
#endregion

#region Member zuweisen
            typeBuilder.DefineMethodOverride(getMethod, _propertyInfo.GetGetMethod());
            if (setMethod != null)
                typeBuilder.DefineMethodOverride(setMethod, _propertyInfo.GetSetMethod());
#endregion
        }

        public override void AddFillMethodeCode(ILGenerator generator)
        {
            MethodInfo getValueMethod = typeof(IValueSource).GetMethod(nameof(IValueSource.GetValue), new Type[] { typeof(string) }).MakeGenericMethod(DataBaseValueType);

            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldflda, _internalField);
            generator.Emit(OpCodes.Ldarg_1);
            generator.Emit(OpCodes.Ldstr, RemoveBrackets(FieldName));
            generator.Emit(OpCodes.Callvirt, getValueMethod);
            generator.Emit(OpCodes.Call, _internalField.FieldType.GetMethod(nameof(BackingStore<object>.SetUnCommittedValue)));
        }

        public override void AddFillCommandBuilderCode(ILGenerator generator)
        {
            generator.Emit(OpCodes.Ldarg_1);
            generator.Emit(OpCodes.Ldstr, FieldName);
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldflda, _internalField);
            if(IsPrimaryKey || IsReadOnly)
                generator.Emit(OpCodes.Call, _internalField.FieldType.GetMethod(nameof(BackingStore<object>.GetUncommittedValue)));
            else
                generator.Emit(OpCodes.Call, _internalField.FieldType.GetMethod(nameof(BackingStore<object>.GetChangedValue)));

#if  NETCOREAPP1_0
            if (DataBaseValueType.GetTypeInfo().IsValueType)
#else
            if (DataBaseValueType.IsValueType)
#endif
                generator.Emit(OpCodes.Box, DataBaseValueType);
            
            generator.Emit(OpCodes.Ldc_I4, (int)
                (IsPrimaryKey ? OrMapping.FieldType.KeyField :
                IsInsertable && IsUpdateAble ? OrMapping.FieldType.WriteableField :
                IsInsertable ? OrMapping.FieldType.InsertableField :
                IsUpdateAble ? OrMapping.FieldType.UpdateableField :
                    OrMapping.FieldType.ReadOnlyField));

            if (!IsPrimaryKey || IsInsertable)
                generator.Emit(OpCodes.Ldnull);
            else
            {
                generator.Emit(OpCodes.Ldtoken, DataBaseValueType);
                generator.Emit(OpCodes.Call, typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle), new Type[] { typeof(RuntimeTypeHandle) }));
            }

            if (IsReadOnly)
                generator.Emit(OpCodes.Ldc_I4_0);
            else
            {
                generator.Emit(OpCodes.Ldarg_0);
                generator.Emit(OpCodes.Ldflda, _internalField);
                generator.Emit(OpCodes.Call, _internalField.FieldType.GetProperty(nameof(BackingStore<object>.IsChanged)).GetGetMethod());
            }

            generator.Emit(OpCodes.Callvirt, typeof(ICommandBuilder).GetMethod(nameof(ICommandBuilder.AddField), new Type[] { typeof(string), typeof(object), typeof(FieldType), typeof(Type), typeof(bool) }));
        }

        public override void AddCommitCode(ILGenerator generator, MethodInfo raisePropertyChanged)
        {
            Label label = generator.DefineLabel();
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldflda, _internalField);
            generator.Emit(OpCodes.Ldarg_1);
            generator.Emit(OpCodes.Call, _internalField.FieldType.GetMethod("Commit"));
            generator.Emit(OpCodes.Brfalse_S, label);
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldstr, _propertyInfo.Name);
            generator.Emit(OpCodes.Callvirt, raisePropertyChanged);
            generator.MarkLabel(label);
        }

        public override void AddRollbackCode(ILGenerator generator)
        {
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldflda, _internalField);
            generator.Emit(OpCodes.Call, _internalField.FieldType.GetMethod("Rollback"));
        }

        public override void AddDropChangesCode(ILGenerator generator)
        {
            if (IsReadOnly)
                return;

            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldflda, _internalField);
            generator.Emit(OpCodes.Call, _internalField.FieldType.GetMethod("Undo"));
        }

        public override void AddModifiedCode(ILGenerator generator, Label returnTrueLabel)
        {
            if (IsReadOnly)
                return;

            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldflda, _internalField);
            generator.Emit(OpCodes.Call, _internalField.FieldType.GetProperty("IsChanged").GetGetMethod());
            generator.Emit(OpCodes.Brtrue, returnTrueLabel);
        }
#endregion

        private static string RemoveBrackets(string value)
        {
            return (value.StartsWith("[") && value.EndsWith("]")) ? value.Substring(1, value.Length - 2) : value;
        }
        protected virtual Type DataBaseValueType
        {
            get
            {
                return _propertyInfo.PropertyType;
            }
        }

        public override void FillCommandBuilder(ICommandBuilder commandBuilder)
        {
            OrMapping.FieldType fieldType = 
                IsPrimaryKey ? OrMapping.FieldType.KeyField :
                IsInsertable && IsUpdateAble ? OrMapping.FieldType.WriteableField :
                IsInsertable ? OrMapping.FieldType.InsertableField :
                IsUpdateAble ? OrMapping.FieldType.UpdateableField :
                    OrMapping.FieldType.ReadOnlyField;

            commandBuilder.AddField(FieldName, fieldType);
        }

        public override Type FieldType
        {
            get { return _propertyInfo.PropertyType; }
        }

        public override string FieldName
        {
            get
            {
                if (string.IsNullOrEmpty(_mappingAttribute.FieldName))
                {
                    return _propertyInfo.Name;
                }
                else
                {
                    return _mappingAttribute.FieldName;
                }
            }
        }

        public virtual bool IsInsertable
        {
            get
            {
                return _mappingAttribute.Insertable;
            }
        }

        public virtual bool IsUpdateAble
        {
            get
            {
                return _mappingAttribute.Updateable;
            }
        }

        public virtual bool IsReadOnly
        {
            get
            {
                return _mappingAttribute.ReadOnly;
            }
        }
    }
}