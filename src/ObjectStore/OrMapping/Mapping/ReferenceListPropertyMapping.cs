#if  NETCOREAPP1_0
using ApplicationException = global::System.InvalidOperationException;
#endif

using System;
using System.Reflection;
using System.Linq.Expressions;
using System.Reflection.Emit;
using System.Collections.Generic;
using System.Linq;
using ObjectStore.MappingOptions;

namespace ObjectStore.OrMapping
{
    internal class ReferenceListPropertyMapping : MemberMapping
    {
        ReferenceListMappingOptions _options;

        FieldBuilder _internalField;

        public ReferenceListPropertyMapping(ReferenceListMappingOptions options)
            : base(options)
        {
            _options = options;
        }

#region Dynamic Code
        public override void AddDynamicGetKeyCode(ILGenerator dynamicMethod, int index, LocalBuilder array){}

        public override void AddDynamicGetKeyFromReaderCode(ILGenerator dynamicMethod, int index, LocalBuilder array){}

        public override void AddConstructorCode(ILGenerator generator)
        {
        }

        public override void AddInhertiedProperty(TypeBuilder typeBuilder, MethodInfo notifyChangeMethode)
        {
#if  NETCOREAPP1_0
            if (!MemberInfo.DeclaringType.GetTypeInfo().IsAbstract)
#else
            if (!MemberInfo.DeclaringType.IsAbstract)
#endif
                throw new NotSupportedException("Inherited properties are only possible for Interfaces and abstract Classes.");

            if (_options.Member.CanWrite)
                throw new NotSupportedException("Inherited referencelist properties must not be writeable."); 

#region Member Definieren
            _internalField = typeBuilder.DefineField("__field" + MemberInfo.Name, _options.Member.PropertyType, FieldAttributes.Private);
            MethodBuilder getMethod = typeBuilder.DefineMethod("get_" + MemberInfo.Name, MethodAttributes.Public | MethodAttributes.Virtual, _options.Member.PropertyType, Type.EmptyTypes);
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
            ilGenerator.Emit(OpCodes.Ldtoken, _options.ForeignProperty.DeclaringType);
            ilGenerator.Emit(OpCodes.Call, typeof(Type).GetMethod("GetTypeFromHandle", new Type[]{typeof(RuntimeTypeHandle) }));
            ilGenerator.Emit(OpCodes.Ldstr, _options.ForeignProperty.Name);
            ilGenerator.Emit(OpCodes.Call, typeof(Type).GetMethod("GetProperty", new Type[] { typeof(string) }));
            ilGenerator.Emit(OpCodes.Ldarg_0);
            ilGenerator.Emit(OpCodes.Callvirt, typeof(Dictionary<PropertyInfo, object>).GetMethod("Add", new Type[] { typeof(PropertyInfo), typeof(object) }));

            foreach (KeyValuePair<PropertyInfo,object> item in _options.Conditions)
            {
                ilGenerator.Emit(OpCodes.Ldloc, conditions);
                ilGenerator.Emit(OpCodes.Ldtoken, item.Key.DeclaringType);
                ilGenerator.Emit(OpCodes.Call, typeof(Type).GetMethod("GetTypeFromHandle", new Type[] { typeof(RuntimeTypeHandle) }));
                ilGenerator.Emit(OpCodes.Ldstr, item.Key.Name);
                ilGenerator.Emit(OpCodes.Call, typeof(Type).GetMethod("GetProperty", new Type[] { typeof(string) }));

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
            ilGenerator.Emit(OpCodes.Newobj, typeof(ReferenceList<>).MakeGenericType(_options.ForeignProperty.DeclaringType).GetConstructor(new Type[] { typeof(Dictionary<PropertyInfo, object>)}));
            ilGenerator.Emit(OpCodes.Stfld, _internalField);

            //Feld zurückgeben
            ilGenerator.MarkLabel(isInitializedLabel);
            ilGenerator.Emit(OpCodes.Ldarg_0);
            ilGenerator.Emit(OpCodes.Ldfld, _internalField);
            ilGenerator.Emit(OpCodes.Ret);
#endregion

#region Member zuweisen
            typeBuilder.DefineMethodOverride(getMethod, _options.Member.GetGetMethod());
#endregion
        }

        public override void AddSaveChildObjectsCode(ILGenerator generator)
        {
            if (_options.SaveCascade)
            {
                generator.Emit(OpCodes.Ldarg_0);
                generator.Emit(OpCodes.Callvirt, _options.Member.GetGetMethod());
                generator.Emit(OpCodes.Castclass, typeof(System.Linq.IQueryable<>).MakeGenericType(_options.ForeignProperty.DeclaringType));
                generator.Emit(OpCodes.Call, typeof(Extensions).GetMethods().Where(x => x.Name == "Save" && x.GetParameters().Length == 1).First().MakeGenericMethod(_options.ForeignProperty.DeclaringType));
                generator.Emit(OpCodes.Pop);
            }
        }

        public override void AddDeleteChildObjectsCode(ILGenerator generator)
        {
            if (_options.DeleteCascade)
            {
                generator.Emit(OpCodes.Ldarg_0);
                generator.Emit(OpCodes.Callvirt, _options.Member.GetGetMethod());
                generator.Emit(OpCodes.Castclass, typeof(IQueryable<>).MakeGenericType(_options.ForeignProperty.DeclaringType));
                generator.Emit(OpCodes.Call, typeof(Extensions).GetMethod("Delete", BindingFlags.Static | BindingFlags.Public).MakeGenericMethod(_options.ForeignProperty.DeclaringType));
                generator.Emit(OpCodes.Pop);
            }
        }

        public override void AddDropChangesCodeChildObjects(ILGenerator generator)
        {
            if (_options.DropChangesCascade)
            {
                generator.Emit(OpCodes.Ldarg_0);
                generator.Emit(OpCodes.Callvirt, _options.Member.GetGetMethod());
                generator.Emit(OpCodes.Castclass, typeof(IQueryable<>).MakeGenericType(_options.ForeignProperty.DeclaringType));
                generator.Emit(OpCodes.Call, typeof(Extensions).GetMethod("DropChanges", BindingFlags.Static | BindingFlags.Public).MakeGenericMethod(_options.ForeignProperty.DeclaringType));
                generator.Emit(OpCodes.Pop);
            }
        }

        public override void AddCheckChildObjectsChangedCode(ILGenerator generator) 
        {
            if (_options.SaveCascade)
            {
                generator.Emit(OpCodes.Ldarg_0);
                generator.Emit(OpCodes.Callvirt, _options.Member.GetGetMethod());
                generator.Emit(OpCodes.Castclass, typeof(IQueryable<>).MakeGenericType(_options.ForeignProperty.DeclaringType));
                generator.Emit(OpCodes.Call, typeof(Extensions).GetMethods()
                    .Where(x => x.Name == "CheckChanged" && x.GetParameters().Length == 1).First().MakeGenericMethod(_options.ForeignProperty.DeclaringType));
                Label label = generator.DefineLabel();
                generator.Emit(OpCodes.Brfalse_S, label);
                generator.Emit(OpCodes.Ldc_I4_1);
                generator.Emit(OpCodes.Ret);
                generator.MarkLabel(label);
            }
        }

#endregion

        public override void FillCommandBuilder(ICommandBuilder updateCommandBuilder) { }

        public Dictionary<PropertyInfo, object> GetConditions(Expression expression)
        {
            Dictionary<PropertyInfo, object> returnValue = new Dictionary<PropertyInfo, object>(_options.Conditions);
            returnValue[_options.ForeignProperty] = expression;
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