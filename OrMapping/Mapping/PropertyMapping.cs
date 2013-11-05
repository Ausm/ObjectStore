using System;
using System.Reflection;
using System.Data.SqlClient;
using System.Linq.Expressions;
using System.Reflection.Emit;
using System.Collections.Generic;
using System.Text;
using System.Data;

namespace Ausm.ObjectStore.OrMapping
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

            object[] attributes = _propertyInfo.GetCustomAttributes(typeof(MappingAttribute), true);
            if (attributes.Length > 0)
            {
                _mappingAttribute = attributes[0] as MappingAttribute;
            }
            else
            {
                _mappingAttribute = new MappingAttribute();
            }
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

            if (DataBaseValueType.IsValueType)
            {
                dynamicMethod.Emit(OpCodes.Box, DataBaseValueType);
            }
            dynamicMethod.Emit(OpCodes.Stelem_Ref);
        }

        public override void AddDynamicGetKeyFromReaderCode(ILGenerator dynamicMethod, int index, LocalBuilder array)
        {
            if (!IsPrimaryKey)
                return;

            MethodInfo getValueMethod = typeof(IDataRecord).GetMethod("get_Item", new Type[] { typeof(string) });

            // Array in Stack Laden
            dynamicMethod.Emit(OpCodes.Ldloc, array);
            dynamicMethod.Emit(OpCodes.Ldc_I4, index);
            // Object in Stack Laden und Casten
            dynamicMethod.Emit(OpCodes.Ldarg_0);
            dynamicMethod.Emit(OpCodes.Ldstr, FieldName);
            dynamicMethod.Emit(OpCodes.Callvirt, getValueMethod);
            dynamicMethod.Emit(OpCodes.Stelem_Ref);
        }

        public override void AddInhertiedProperty(TypeBuilder typeBuilder, MethodInfo notifyPropertyChangedMethode)
        {
            if (!MemberInfo.DeclaringType.IsAbstract)
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
            MethodInfo getOrdinalMethod = typeof(IDataRecord).GetMethod("GetOrdinal", new Type[] { typeof(string) });
            MethodInfo getValueMethod = typeof(IDataRecord).GetMethod("GetValue", new Type[] { typeof(int) });
            MethodInfo isDbNullMethod = typeof(IDataRecord).GetMethod("IsDBNull", new Type[] { typeof(int) });

            LocalBuilder ordinal = generator.DeclareLocal(typeof(int));
            Label endLabel = generator.DefineLabel();
            Label nullLabel = generator.DefineLabel();
            generator.Emit(OpCodes.Ldarg_1);
            generator.Emit(OpCodes.Ldstr, FieldName);
            generator.Emit(OpCodes.Callvirt, getOrdinalMethod);
            generator.Emit(OpCodes.Stloc, ordinal);

            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldflda, _internalField);
            generator.Emit(OpCodes.Ldarg_1);
            generator.Emit(OpCodes.Ldloc, ordinal);
            generator.Emit(OpCodes.Callvirt, isDbNullMethod);
            generator.Emit(OpCodes.Brtrue, nullLabel);

            generator.Emit(OpCodes.Ldarg_1);
            generator.Emit(OpCodes.Ldloc, ordinal);
            generator.Emit(OpCodes.Callvirt, getValueMethod);
            if (DataBaseValueType.IsValueType)
            {
                generator.Emit(OpCodes.Unbox_Any, DataBaseValueType);
            }
            else
            {
                generator.Emit(OpCodes.Castclass, DataBaseValueType);
            }
            generator.Emit(OpCodes.Br_S, endLabel);
            generator.MarkLabel(nullLabel);
            if (DataBaseValueType.IsValueType)
            {
                LocalBuilder defaultValue = generator.DeclareLocal(DataBaseValueType);
                generator.Emit(OpCodes.Ldloca_S, defaultValue);
                generator.Emit(OpCodes.Initobj, defaultValue.LocalType);
                generator.Emit(OpCodes.Ldloc, defaultValue);
            }
            else
            {
                generator.Emit(OpCodes.Ldnull);
            }
            generator.MarkLabel(endLabel);
            generator.Emit(OpCodes.Call, _internalField.FieldType.GetMethod("SetUnCommittedValue"));
        }

        public override void AddFillCommandBuilderCode(ILGenerator generator)
        {
            generator.Emit(OpCodes.Ldarg_1);
            generator.Emit(OpCodes.Ldstr, FieldName);
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldflda, _internalField);
            if(IsPrimaryKey || IsReadOnly)
                generator.Emit(OpCodes.Call, _internalField.FieldType.GetMethod("GetUncommittedValue"));
            else
                generator.Emit(OpCodes.Call, _internalField.FieldType.GetMethod("GetChangedValue"));

            if (DataBaseValueType.IsValueType)
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
                generator.Emit(OpCodes.Call, typeof(System.Type).GetMethod("GetTypeFromHandle", new Type[] { typeof(System.RuntimeTypeHandle) }));
                generator.Emit(OpCodes.Call, typeof(KeyInitializer).GetMethod("GetInitializer", new Type[] { typeof(Type) }));
            }
            generator.Emit(OpCodes.Callvirt, typeof(ICommandBuilder).GetMethod("AddField", new Type[] { typeof(string), typeof(object), typeof(FieldType), typeof(KeyInitializer) }));
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

        public override string ParseExpression(Ausm.ObjectStore.OrMapping.Expressions.Expression expression)
        {
            if(expression is Ausm.ObjectStore.OrMapping.Expressions.MemberExpression)
                return string.Format("{0}.{1}", (expression as Ausm.ObjectStore.OrMapping.Expressions.MemberExpression).Alias, FieldName);
            if (expression is Ausm.ObjectStore.OrMapping.Expressions.ParameterExpression)
            {
                if (IsPrimaryKey)
                    return string.Format("{0}.{1}", ((Ausm.ObjectStore.OrMapping.Expressions.ParameterExpression)expression).Alias, FieldName);
                else
                    throw new Ausm.ObjectStore.OrMapping.Expressions.NotParsableException("Identificationvalue of parameter must be the uniqe primary key.");
            }

            throw new Ausm.ObjectStore.OrMapping.Expressions.NotParsableException("Cannot parse unknown expression.");
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