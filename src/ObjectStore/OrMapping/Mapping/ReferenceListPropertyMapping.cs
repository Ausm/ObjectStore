#if DNXCORE50
using ApplicationException = global::System.InvalidOperationException;
#endif

using System;
using System.Reflection;
using System.Linq.Expressions;
using System.Reflection.Emit;
using System.Collections.Generic;
using System.Linq;

namespace ObjectStore.OrMapping
{
    internal class ReferenceListPropertyMapping : Mapping
    {
        ReferenceListMappingAttribute _listMappingAttribute;
        Dictionary<PropertyInfo, object> _conditions;

        FieldBuilder _internalField;
        PropertyInfo _propertyInfo;

        public ReferenceListPropertyMapping(PropertyInfo info)
            : base(info)
        {
            _propertyInfo = info;

            object[] attributes =
#if DNXCORE50
                info.GetCustomAttributes(typeof(ReferenceListMappingAttribute), true).ToArray();
#else
                info.GetCustomAttributes(typeof(ReferenceListMappingAttribute), true);
#endif
            if (attributes.Length > 0)
            {
                _listMappingAttribute = attributes[0] as ReferenceListMappingAttribute;
            }
            else
            {
                throw new ApplicationException("PropertyInfo has no ReferenceListMappingAttribute set.");
            }

#region Conditions ermitteln
            _conditions = new Dictionary<PropertyInfo, object>();
            foreach (EqualsObjectConditionAttribute attribute in info.GetCustomAttributes(typeof(EqualsObjectConditionAttribute), true))
            {
                _conditions.Add(_listMappingAttribute.ForeignType.GetProperty(attribute.PropertyName), attribute.Value);
            }
#endregion
        }

#region Dynamic Code
        public override void AddDynamicGetKeyCode(ILGenerator dynamicMethod, int index, LocalBuilder array){}

        public override void AddDynamicGetKeyFromReaderCode(ILGenerator dynamicMethod, int index, LocalBuilder array){}

        public override void AddConstructorCode(ILGenerator generator)
        {
        }

        public override void AddInhertiedProperty(TypeBuilder typeBuilder, MethodInfo notifyChangeMethode)
        {
#if DNXCORE50
            if (!MemberInfo.DeclaringType.GetTypeInfo().IsAbstract)
#else
            if (!MemberInfo.DeclaringType.IsAbstract)
#endif
                throw new NotSupportedException("Inherited properties are only possible for Interfaces and abstract Classes.");

            if (_propertyInfo.CanWrite)
                throw new NotSupportedException("Inherited referencelist properties must not be writeable."); 

#region Member Definieren
            _internalField = typeBuilder.DefineField("__field" + MemberInfo.Name, _propertyInfo.PropertyType, FieldAttributes.Private);
            MethodBuilder getMethod = typeBuilder.DefineMethod("get_" + MemberInfo.Name, MethodAttributes.Public | MethodAttributes.Virtual, _propertyInfo.PropertyType, Type.EmptyTypes);
#endregion

#region Get-Code schreiben
            ILGenerator ilGenerator = getMethod.GetILGenerator();
            Label isInitializedLabel = ilGenerator.DefineLabel();
            LocalBuilder conditions = ilGenerator.DeclareLocal(typeof(Dictionary<PropertyInfo, object>));

            //Überprüfen ob das interne Feld Null ist
            ilGenerator.Emit(OpCodes.Ldarg_0);
            ilGenerator.Emit(OpCodes.Ldfld, _internalField);
            ilGenerator.Emit(OpCodes.Brtrue, isInitializedLabel);

            //Wenn Null dann Dictionary mit den conditions initialisieren
            ilGenerator.Emit(OpCodes.Newobj, typeof(Dictionary<PropertyInfo, object>).GetConstructor(Type.EmptyTypes));
            ilGenerator.Emit(OpCodes.Stloc, conditions);
            ilGenerator.Emit(OpCodes.Ldloc, conditions);
            ilGenerator.Emit(OpCodes.Ldtoken, _listMappingAttribute.ForeignType);
            ilGenerator.Emit(OpCodes.Call, typeof(System.Type).GetMethod("GetTypeFromHandle", new Type[]{typeof(System.RuntimeTypeHandle)}));
            ilGenerator.Emit(OpCodes.Ldstr, _listMappingAttribute.ForeignProperty.Name);
            ilGenerator.Emit(OpCodes.Call, typeof(System.Type).GetMethod("GetProperty", new Type[] { typeof(string) }));
            ilGenerator.Emit(OpCodes.Ldarg_0);
            ilGenerator.Emit(OpCodes.Callvirt, typeof(Dictionary<PropertyInfo, object>).GetMethod("Add", new Type[] { typeof(PropertyInfo), typeof(object) }));

            foreach (KeyValuePair<PropertyInfo,object> item in _conditions)
            {
                ilGenerator.Emit(OpCodes.Ldloc, conditions);
                ilGenerator.Emit(OpCodes.Ldtoken, item.Key.DeclaringType);
                ilGenerator.Emit(OpCodes.Call, typeof(System.Type).GetMethod("GetTypeFromHandle", new Type[] { typeof(System.RuntimeTypeHandle) }));
                ilGenerator.Emit(OpCodes.Ldstr, item.Key.Name);
                ilGenerator.Emit(OpCodes.Call, typeof(System.Type).GetMethod("GetProperty", new Type[] { typeof(string) }));

                if (item.Value is byte ||
                    item.Value is int ||
                    item.Value is short)
                {
                    ilGenerator.Emit(OpCodes.Ldc_I4, Convert.ToInt32(item.Value));
                    ilGenerator.Emit(OpCodes.Box, item.Value.GetType());
                }
                else if (item.Value is long)
                {
                    ilGenerator.Emit(OpCodes.Ldc_I8, (long)item.Value);
                    ilGenerator.Emit(OpCodes.Box, typeof(long));
                }
                else if (item.Value is string)
                {
                    ilGenerator.Emit(OpCodes.Ldstr, item.Value as string);
                }
                else
                {
                    throw new NotSupportedException("Type is not Supported in EqualsObjectCondition.");
                }

                ilGenerator.Emit(OpCodes.Callvirt, typeof(Dictionary<PropertyInfo, object>).GetMethod("Add", new Type[] { typeof(PropertyInfo), typeof(object) }));
            }

            //Reference-List erstellen und Feld zuweisen
            ilGenerator.Emit(OpCodes.Ldarg_0);
            ilGenerator.Emit(OpCodes.Ldloc, conditions);
            ilGenerator.Emit(OpCodes.Newobj, typeof(ReferenceList<>).MakeGenericType(_listMappingAttribute.ForeignType).GetConstructor(new Type[] { typeof(Dictionary<PropertyInfo, object>)}));
            ilGenerator.Emit(OpCodes.Stfld, _internalField);

            //Feld zurückgeben
            ilGenerator.MarkLabel(isInitializedLabel);
            ilGenerator.Emit(OpCodes.Ldarg_0);
            ilGenerator.Emit(OpCodes.Ldfld, _internalField);
            ilGenerator.Emit(OpCodes.Ret);
#endregion

#region Member zuweisen
            typeBuilder.DefineMethodOverride(getMethod, _propertyInfo.GetGetMethod());
#endregion
        }

        public override void AddSaveChildObjectsCode(ILGenerator generator)
        {
            if (_listMappingAttribute.SaveCascade)
            {
                generator.Emit(OpCodes.Ldarg_0);
                generator.Emit(OpCodes.Callvirt, _propertyInfo.GetGetMethod());
                generator.Emit(OpCodes.Castclass, typeof(System.Linq.IQueryable<>).MakeGenericType(_listMappingAttribute.ForeignType));
                generator.Emit(OpCodes.Call, typeof(Extensions).GetMethods().Where(x => x.Name == "Save" && x.GetParameters().Length == 1).First().MakeGenericMethod(_listMappingAttribute.ForeignType));
                generator.Emit(OpCodes.Pop);
            }
        }

        public override void AddDeleteChildObjectsCode(ILGenerator generator)
        {
            if (_listMappingAttribute.DeleteCascade)
            {
                generator.Emit(OpCodes.Ldarg_0);
                generator.Emit(OpCodes.Callvirt, _propertyInfo.GetGetMethod());
                generator.Emit(OpCodes.Castclass, typeof(System.Linq.IQueryable<>).MakeGenericType(_listMappingAttribute.ForeignType));
                generator.Emit(OpCodes.Call, typeof(Extensions).GetMethod("Delete", BindingFlags.Static | BindingFlags.Public).MakeGenericMethod(_listMappingAttribute.ForeignType));
                generator.Emit(OpCodes.Pop);
            }
        }

        public override void AddDropChangesCodeChildObjects(ILGenerator generator)
        {
            if (_listMappingAttribute.DropChangesCascade)
            {
                generator.Emit(OpCodes.Ldarg_0);
                generator.Emit(OpCodes.Callvirt, _propertyInfo.GetGetMethod());
                generator.Emit(OpCodes.Castclass, typeof(System.Linq.IQueryable<>).MakeGenericType(_listMappingAttribute.ForeignType));
                generator.Emit(OpCodes.Call, typeof(Extensions).GetMethod("DropChanges", BindingFlags.Static | BindingFlags.Public).MakeGenericMethod(_listMappingAttribute.ForeignType));
                generator.Emit(OpCodes.Pop);
            }
        }

        public override void AddCheckChildObjectsChangedCode(ILGenerator generator) 
        {
            if (_listMappingAttribute.SaveCascade)
            {
                generator.Emit(OpCodes.Ldarg_0);
                generator.Emit(OpCodes.Callvirt, _propertyInfo.GetGetMethod());
                generator.Emit(OpCodes.Castclass, typeof(System.Linq.IQueryable<>).MakeGenericType(_listMappingAttribute.ForeignType));
                generator.Emit(OpCodes.Call, typeof(Extensions).GetMethods()
                    .Where(x => x.Name == "CheckChanged" && x.GetParameters().Length == 1).First().MakeGenericMethod(_listMappingAttribute.ForeignType));
                Label label = generator.DefineLabel();
                generator.Emit(OpCodes.Brfalse_S, label);
                generator.Emit(OpCodes.Ldc_I4_1);
                generator.Emit(OpCodes.Ret);
                generator.MarkLabel(label);
            }
        }

#endregion

        public override void FillCommandBuilder(ICommandBuilder updateCommandBuilder) { }

        public override string ParseExpression(Expressions.Expression expression)
        {
            throw new NotImplementedException();
        }

        public Dictionary<PropertyInfo, object> GetConditions(Expression expression)
        {
            Dictionary<PropertyInfo, object> returnValue = new Dictionary<PropertyInfo, object>(_conditions);
            returnValue[_listMappingAttribute.ForeignProperty] = expression;
            return returnValue;
        }

#region Properties
        public PropertyInfo PropertyInfo
        {
            get
            {
                return (PropertyInfo)MemberInfo;
            }
        }

        public override Type FieldType
        {
            get { return PropertyInfo.PropertyType; }
        }

        public override bool IsPrimaryKey
        {
            get
            {
                return false;
            }
        }

        public override string FieldName
        {
            get { throw new NotSupportedException("ReferenceListPropertyMapping does not have a field behind."); }
        }
#endregion
    }
}