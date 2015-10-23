#if DNXCORE50
using IDataReader = global::System.Data.Common.DbDataReader;
using IDataRecord = global::System.Data.Common.DbDataReader;
#else
using System.Data;
#endif

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq.Expressions;
using System.Reflection.Emit;
using System.Collections;
using System.Collections.ObjectModel;
using System.Linq;

namespace ObjectStore.OrMapping
{
    public enum State
    {
        Original = 1,
        Created = 2,
        Changed = 3,
        Deleted = 4,
        NotAttached = 5
    }

    public class StateChangedEventArgs : System.ComponentModel.PropertyChangedEventArgs
    {
        public StateChangedEventArgs(State oldState, State newState) : base("State")
        {
            _oldState = oldState;
            _newState = newState;
        }

        State _oldState;
        State _newState;

        public State OldState
        {
            get
            {
                return _oldState;
            }
        }

        public State NewState
        {
            get
            {
                return _newState;
            }
        }
    }

    public interface IFillAbleObject
    {
        void Fill(IDataRecord reader);
        void Commit(bool unDoChanges);
        void Rollback();
        void DropChanges();

        MappedObjectKeys Keys { get; }
        bool Modified { get; }
        State State { get; }

        void FillCommand(ICommandBuilder commandBuilder);

        void Delete();
        void Deattach();

        void DeleteChildObjects();
        void SaveChildObjects();
        bool CheckChildObjectsChanged();
        void DropChangesChildObjects();
    }

    internal class MappingInfo
    {
        #region PerType-SingeltonImplementierung
        private static Dictionary<Type, MappingInfo> mappingInfos = new Dictionary<Type, MappingInfo>();

        public static MappingInfo GetMappingInfo(Type type)
        {
            return GetMappingInfo(type, false);
        }

        public static MappingInfo GetMappingInfo(Type type, bool existingOnly)
        {
            if (mappingInfos.ContainsKey(type))
            {
                return mappingInfos[type];
            }
            else if(existingOnly)
            {
                return null;
            }
            else
            {
                MappingInfo mappingInfo = new MappingInfo(type);
                mappingInfos.Add(mappingInfo.DynamicType, mappingInfo);
                return mappingInfos[type] = mappingInfo;
            }
        }
        #endregion

        #region Membervariablen
        protected Type _type;
        protected List<Mapping> _mappingInfos;

        private List<Mapping> _keyMappingInfos;
        private string _tableName;

        protected Type _dynamicType;
        Func<object> _constructor;
        protected Func<IDataReader, MappedObjectKeys> _getKeyObjectFromReader;
        protected LoadBehavior _loadBehavior;

        #endregion

        #region GetTypeBuilder
        static ModuleBuilder _moduleBuilder = null;

        private static TypeBuilder GetTypeBuilder(string name)
        {
            if (_moduleBuilder == null)
            {
                AssemblyName assemblyName = new AssemblyName("InheritensObjectProviderClasses");
#if DNXCORE50
                AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndCollect);
#else
                AssemblyBuilder assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndSave);
#endif
                _moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName.Name + ".dll");
            }
            return _moduleBuilder.DefineType(name);
        }
#endregion

#region Konstruktoren
        public MappingInfo(Type type)
        {
#if DNXCORE50
            if (!type.GetTypeInfo().IsAbstract)
#else
            if (!type.IsAbstract)
#endif
                throw new NotSupportedException("InheritedPropertyMapping supports abstract classes and interfaces only.");

            _type = type;
            Initialize();
        }
#endregion

#region Funktionen
        protected virtual void Initialize()
        {
#region Tabellennamen bestimmen
            {
#if DNXCORE50
                TableAttribute attribute = Type.GetTypeInfo().GetCustomAttribute(typeof(TableAttribute), true) as TableAttribute;
#else
                TableAttribute attribute = Type.GetCustomAttributes(typeof(TableAttribute), true).FirstOrDefault() as TableAttribute;
#endif
                if (attribute == null)
                {
                    TableName = Type.Name;
                    _loadBehavior = LoadBehavior.OnDemandPartialLoad;
                }
                else
                {
                    TableName = ((TableAttribute)attribute).TableName;
                    _loadBehavior = ((TableAttribute)attribute).LoadBehavior;
                }
            }
#endregion

#region MappingInfos erstellen
            _mappingInfos = new List<Mapping>();
            foreach (PropertyInfo item in Type.GetProperties(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance
#if !DNXCORE50
                | BindingFlags.GetProperty
#endif
                ).Where(x =>
            {
                MethodInfo methodInfo = x.GetGetMethod();
                return methodInfo != null && methodInfo.IsAbstract;
            }))
            {
                Mapping mappingInfo = Mapping.GetMapping(item);
                if (mappingInfo != null)
                {
                    _mappingInfos.Add(mappingInfo);
                }
            }
#endregion

#region Initialize GetKeyFromReader
            {
                DynamicMethod method = new DynamicMethod("getKey", typeof(MappedObjectKeys), new Type[] { typeof(IDataReader) }, true);
                ILGenerator getKeyGenerator = method.GetILGenerator();

                LocalBuilder keyArray = getKeyGenerator.DeclareLocal(typeof(object[]));

                getKeyGenerator.Emit(OpCodes.Nop);
                getKeyGenerator.Emit(OpCodes.Ldc_I4, KeyMappingInfos.Count);
                getKeyGenerator.Emit(OpCodes.Newarr, typeof(object));
                getKeyGenerator.Emit(OpCodes.Stloc, keyArray);

                int i = 0;
                foreach (Mapping mapping in KeyMappingInfos)
                {
                    mapping.AddDynamicGetKeyFromReaderCode(getKeyGenerator, i++, keyArray);
                }

                getKeyGenerator.Emit(OpCodes.Ldloc, keyArray);
                getKeyGenerator.Emit(OpCodes.Newobj, typeof(MappedObjectKeys).GetConstructor(new Type[] { typeof(IEnumerable) }));
                getKeyGenerator.Emit(OpCodes.Ret);
                _getKeyObjectFromReader = (Func<IDataReader, MappedObjectKeys>)method.CreateDelegate(typeof(Func<IDataReader, MappedObjectKeys>));
            }
#endregion

#region Dynamischen Type erstellen

            TypeBuilder typeBuilder = GetTypeBuilder(string.Format("{1}.Dynamic.{0}", Type.Name, Type.Namespace));
#if DNXCORE50
            if (Type.GetTypeInfo().IsInterface)
#else
            if (Type.IsInterface)
#endif
                typeBuilder.AddInterfaceImplementation(Type);
            else
                typeBuilder.SetParent(Type);

            typeBuilder.AddInterfaceImplementation(typeof(IFillAbleObject));

#region Fields für Object-State Definieren
            FieldBuilder isDeattachedField = typeBuilder.DefineField("___isDeattached", typeof(bool), FieldAttributes.Private);
            FieldBuilder isDeletedField = typeBuilder.DefineField("___isDeleted", typeof(bool), FieldAttributes.Private);
            FieldBuilder isNewField = typeBuilder.DefineField("___isNew", typeof(bool), FieldAttributes.Private);
#endregion

            ILGenerator generator;

#region INotifyPropertyChanged implementieren
            MethodBuilder raiseMethode = typeBuilder.DefineMethod("OnPropertyChanged", MethodAttributes.Private, null, new Type[] { typeof(string) });
            MethodBuilder raiseStateChangedMethode = typeBuilder.DefineMethod("OnStateChanged", MethodAttributes.Private, null, new Type[] { typeof(State), typeof(State) });
            if (!Type.GetInterfaces().Contains(typeof(System.ComponentModel.INotifyPropertyChanged)))
            {
                typeBuilder.AddInterfaceImplementation(typeof(System.ComponentModel.INotifyPropertyChanged));

                EventBuilder propertyChangedEvent = typeBuilder.DefineEvent("PropertyChanged", EventAttributes.None, typeof(System.ComponentModel.PropertyChangedEventHandler));
                FieldBuilder propertyChangedField = typeBuilder.DefineField("PropertyChanged", typeof(System.ComponentModel.PropertyChangedEventHandler), FieldAttributes.Private);
                MethodBuilder addMethode = typeBuilder.DefineMethod("add_PropertyChanged", MethodAttributes.Private | MethodAttributes.Virtual, null, new Type[] { typeof(System.ComponentModel.PropertyChangedEventHandler) });
                MethodBuilder removeMethode = typeBuilder.DefineMethod("remove_PropertyChanged", MethodAttributes.Private | MethodAttributes.Virtual, null, new Type[] { typeof(System.ComponentModel.PropertyChangedEventHandler) });

                generator = addMethode.GetILGenerator();
                generator.Emit(OpCodes.Ldarg_0);
                generator.Emit(OpCodes.Ldarg_0);
                generator.Emit(OpCodes.Ldfld, propertyChangedField);
                generator.Emit(OpCodes.Ldarg_1);
                generator.Emit(OpCodes.Call, typeof(System.Delegate).GetMethod("Combine", new Type[] { typeof(System.Delegate), typeof(System.Delegate) }));
                generator.Emit(OpCodes.Castclass, typeof(System.ComponentModel.PropertyChangedEventHandler));
                generator.Emit(OpCodes.Stfld, propertyChangedField);
                generator.Emit(OpCodes.Ret);
                typeBuilder.DefineMethodOverride(addMethode, typeof(System.ComponentModel.INotifyPropertyChanged).GetEvent("PropertyChanged").GetAddMethod());


                generator = removeMethode.GetILGenerator();
                generator.Emit(OpCodes.Ldarg_0);
                generator.Emit(OpCodes.Ldarg_0);
                generator.Emit(OpCodes.Ldfld, propertyChangedField);
                generator.Emit(OpCodes.Ldarg_1);
                generator.Emit(OpCodes.Call, typeof(System.Delegate).GetMethod("Remove", new Type[] { typeof(System.Delegate), typeof(System.Delegate) }));
                generator.Emit(OpCodes.Castclass, typeof(System.ComponentModel.PropertyChangedEventHandler));
                generator.Emit(OpCodes.Stfld, propertyChangedField);
                generator.Emit(OpCodes.Ret);
                typeBuilder.DefineMethodOverride(removeMethode, typeof(System.ComponentModel.INotifyPropertyChanged).GetEvent("PropertyChanged").GetRemoveMethod());


                generator = raiseMethode.GetILGenerator();
                Label end = generator.DefineLabel();
                generator.Emit(OpCodes.Ldarg_0);
                generator.Emit(OpCodes.Ldfld, propertyChangedField);
                generator.Emit(OpCodes.Ldnull);
                generator.Emit(OpCodes.Ceq);
                generator.Emit(OpCodes.Brtrue, end);
                generator.Emit(OpCodes.Ldarg_0);
                generator.Emit(OpCodes.Ldfld, propertyChangedField);
                generator.Emit(OpCodes.Ldarg_0);
                generator.Emit(OpCodes.Ldarg_1);
                generator.Emit(OpCodes.Newobj, typeof(System.ComponentModel.PropertyChangedEventArgs).GetConstructor(new Type[] { typeof(string) }));
                generator.Emit(OpCodes.Callvirt, typeof(System.ComponentModel.PropertyChangedEventHandler).GetMethod("Invoke", new Type[] { typeof(object), typeof(System.ComponentModel.PropertyChangedEventArgs) }));
                generator.MarkLabel(end);
                generator.Emit(OpCodes.Ret);

                generator = raiseStateChangedMethode.GetILGenerator();
                end = generator.DefineLabel();
                generator.Emit(OpCodes.Ldarg_1);
                generator.Emit(OpCodes.Ldarg_2);
                generator.Emit(OpCodes.Ceq);
                generator.Emit(OpCodes.Brtrue, end);
                generator.Emit(OpCodes.Ldarg_0);
                generator.Emit(OpCodes.Ldfld, propertyChangedField);
                generator.Emit(OpCodes.Ldnull);
                generator.Emit(OpCodes.Ceq);
                generator.Emit(OpCodes.Brtrue, end);
                generator.Emit(OpCodes.Ldarg_0);
                generator.Emit(OpCodes.Ldfld, propertyChangedField);
                generator.Emit(OpCodes.Ldarg_0);
                generator.Emit(OpCodes.Ldarg_1);
                generator.Emit(OpCodes.Ldarg_2);
                generator.Emit(OpCodes.Newobj, typeof(StateChangedEventArgs).GetConstructor(new Type[] { typeof(State), typeof(State) }));
                generator.Emit(OpCodes.Callvirt, typeof(System.ComponentModel.PropertyChangedEventHandler).GetMethod("Invoke", new Type[] { typeof(object), typeof(System.ComponentModel.PropertyChangedEventArgs) }));
                generator.MarkLabel(end);
                generator.Emit(OpCodes.Ret);

                propertyChangedEvent.SetAddOnMethod(addMethode);
                propertyChangedEvent.SetRemoveOnMethod(removeMethode);
            }
            else
            {
                MethodInfo raiseMethodeInfo = Type.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).Where(x =>
                        x.GetParameters().Length == 1 &&
                        x.GetParameters()[0].ParameterType == typeof(System.ComponentModel.PropertyChangedEventArgs) &&
                        x.GetCustomAttributes(typeof(IsRaisePropertyChangeMethodAttribute), true).Any()).FirstOrDefault();

                if (raiseMethode == null)
                    throw new MissingMemberException("No void method(System.ComponentModel.PropertyChangedEventArgs) with IsRaisePropertyChangeMethodAttribute found. Remove INotifyPropertyChanged-Interface or implement raise method.");

                generator = raiseMethode.GetILGenerator();
                generator.Emit(OpCodes.Ldarg_0);
                generator.Emit(OpCodes.Ldarg_1);
                generator.Emit(OpCodes.Newobj, typeof(System.ComponentModel.PropertyChangedEventArgs).GetConstructor(new Type[] { typeof(string) }));
                generator.Emit(OpCodes.Callvirt, raiseMethodeInfo);
                generator.Emit(OpCodes.Ret);

                generator = raiseStateChangedMethode.GetILGenerator();
                Label notEqualLabel = generator.DefineLabel();
                generator.Emit(OpCodes.Ldarg_1);
                generator.Emit(OpCodes.Ldarg_2);
                generator.Emit(OpCodes.Ceq);
                generator.Emit(OpCodes.Brfalse, notEqualLabel);
                generator.Emit(OpCodes.Ret);
                generator.MarkLabel(notEqualLabel);
                generator.Emit(OpCodes.Ldarg_0);
                generator.Emit(OpCodes.Ldarg_1);
                generator.Emit(OpCodes.Ldarg_2);
                generator.Emit(OpCodes.Newobj, typeof(StateChangedEventArgs).GetConstructor(new Type[] { typeof(State), typeof(State) }));
                generator.Emit(OpCodes.Callvirt, raiseMethodeInfo);
                generator.Emit(OpCodes.Ret);
            }
#endregion

#region Properties Hinzufügen
            foreach (Mapping mapping in _mappingInfos)
            {
                mapping.AddInhertiedProperty(typeBuilder, raiseMethode);
            }
#endregion

#region Parameterloser Konstruktor hinzufügen
            ConstructorBuilder constructorBuilder = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, Type.EmptyTypes);
            generator = constructorBuilder.GetILGenerator();

            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Call,
                Type.GetConstructor(Type.EmptyTypes) ??
                Type.GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance).Where(x => x.GetParameters().Length == 0).FirstOrDefault() ??
                typeof(object).GetConstructor(Type.EmptyTypes));

            foreach (Mapping mapping in _mappingInfos)
            {
                mapping.AddConstructorCode(generator);
            }

            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldc_I4_1);
            generator.Emit(OpCodes.Stfld, isNewField);

            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldc_I4_0);
            generator.Emit(OpCodes.Stfld, isDeattachedField);

            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldc_I4_0);
            generator.Emit(OpCodes.Stfld, isDeletedField);
            generator.Emit(OpCodes.Ret);
#endregion

#region StateProperty
            PropertyBuilder stateProperty = typeBuilder.DefineProperty("IFillAbleObject.State", System.Reflection.PropertyAttributes.None, typeof(State), Type.EmptyTypes);
            MethodBuilder getStateMethode = typeBuilder.DefineMethod("IFillAbleObject.get_State", MethodAttributes.Public | MethodAttributes.Virtual, typeof(State), Type.EmptyTypes);
            generator = getStateMethode.GetILGenerator();

            Label isNotDeattachedLabel = generator.DefineLabel();
            Label isNotDeletedLabel = generator.DefineLabel();
            Label isNotNewLabel = generator.DefineLabel();
            Label isModifiedTrueLabel = generator.DefineLabel();

            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldfld, isDeattachedField);
            generator.Emit(OpCodes.Brfalse, isNotDeattachedLabel);
            generator.Emit(OpCodes.Ldc_I4, (int)State.NotAttached);
            generator.Emit(OpCodes.Ret);
            generator.MarkLabel(isNotDeattachedLabel);

            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldfld, isDeletedField);
            generator.Emit(OpCodes.Brfalse, isNotDeletedLabel);
            generator.Emit(OpCodes.Ldc_I4, (int)State.Deleted);
            generator.Emit(OpCodes.Ret);
            generator.MarkLabel(isNotDeletedLabel);

            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldfld, isNewField);
            generator.Emit(OpCodes.Brfalse, isNotNewLabel);
            generator.Emit(OpCodes.Ldc_I4, (int)State.Created);
            generator.Emit(OpCodes.Ret);
            generator.MarkLabel(isNotNewLabel);

            foreach (Mapping mapping in _mappingInfos)
            {
                mapping.AddModifiedCode(generator, isModifiedTrueLabel);
            }
            generator.Emit(OpCodes.Ldc_I4, (int)State.Original);
            generator.Emit(OpCodes.Ret);
            generator.MarkLabel(isModifiedTrueLabel);
            generator.Emit(OpCodes.Ldc_I4, (int)State.Changed);
            generator.Emit(OpCodes.Ret);

            typeBuilder.DefineMethodOverride(getStateMethode, typeof(IFillAbleObject).GetProperty("State").GetGetMethod());
#endregion

#region FillMethode
            MethodBuilder fillMethode = typeBuilder.DefineMethod("IFillAbleObject.Fill", MethodAttributes.Public | MethodAttributes.Virtual, null, new Type[] { typeof(IDataRecord) });
            generator = fillMethode.GetILGenerator();
            Label afterStateChangeLable = generator.DefineLabel();
            foreach (Mapping mapping in _mappingInfos)
            {
                mapping.AddFillMethodeCode(generator);
            }

            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldfld, isNewField);
            generator.Emit(OpCodes.Brfalse, afterStateChangeLable);

            generator.Emit(OpCodes.Ldarg_0);

            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Callvirt, getStateMethode);

            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldc_I4_0);
            generator.Emit(OpCodes.Stfld, isNewField);

            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Callvirt, getStateMethode);

            generator.Emit(OpCodes.Callvirt, raiseStateChangedMethode);
            generator.MarkLabel(afterStateChangeLable);
            generator.Emit(OpCodes.Ret);

            typeBuilder.DefineMethodOverride(fillMethode, typeof(IFillAbleObject).GetMethod("Fill"));
#endregion

#region CommitMethode
            MethodBuilder commitMethode = typeBuilder.DefineMethod("IFillAbleObject.Commit", MethodAttributes.Public | MethodAttributes.Virtual, null, new Type[] { typeof(bool) });
            generator = commitMethode.GetILGenerator();
            foreach (Mapping mapping in _mappingInfos)
            {
                mapping.AddCommitCode(generator, raiseMethode);
            }

            generator.Emit(OpCodes.Ret);
            typeBuilder.DefineMethodOverride(commitMethode, typeof(IFillAbleObject).GetMethod("Commit"));
#endregion

#region RollbackMethode
            MethodBuilder rollbackMethode = typeBuilder.DefineMethod("IFillAbleObject.Rollback", MethodAttributes.Public | MethodAttributes.Virtual, null, Type.EmptyTypes);
            generator = rollbackMethode.GetILGenerator();
            foreach (Mapping mapping in _mappingInfos)
            {
                mapping.AddRollbackCode(generator);
            }

            generator.Emit(OpCodes.Ret);
            typeBuilder.DefineMethodOverride(rollbackMethode, typeof(IFillAbleObject).GetMethod("Rollback"));
#endregion

#region KeysProperty
            PropertyBuilder keyProperty = typeBuilder.DefineProperty("IFillAbleObject.Keys", System.Reflection.PropertyAttributes.None, typeof(MappedObjectKeys), Type.EmptyTypes);
            MethodBuilder getKeyMethode = typeBuilder.DefineMethod("IFillAbleObject.get_Keys", MethodAttributes.Public | MethodAttributes.Virtual, typeof(MappedObjectKeys), Type.EmptyTypes);
            generator = getKeyMethode.GetILGenerator();
            LocalBuilder array = generator.DeclareLocal(typeof(object[]));

            generator.Emit(OpCodes.Nop);
            generator.Emit(OpCodes.Ldc_I4, KeyMappingInfos.Count);
            generator.Emit(OpCodes.Newarr, typeof(object));
            generator.Emit(OpCodes.Stloc, array);

            int currentMapping = 0;
            foreach (Mapping mapping in KeyMappingInfos)
            {
                mapping.AddDynamicGetKeyCode(generator, currentMapping++, array);
            }

            generator.Emit(OpCodes.Ldloc, array);
            generator.Emit(OpCodes.Newobj, typeof(MappedObjectKeys).GetConstructor(new Type[] { typeof(IEnumerable) }));
            generator.Emit(OpCodes.Ret);
            typeBuilder.DefineMethodOverride(getKeyMethode, typeof(IFillAbleObject).GetProperty("Keys").GetGetMethod());
#endregion


#region ModifiedProperty
            PropertyBuilder modifiedProperty = typeBuilder.DefineProperty("IFillAbleObject.Modified", System.Reflection.PropertyAttributes.None, typeof(MappedObjectKeys), Type.EmptyTypes);
            MethodBuilder getModifiedMethode = typeBuilder.DefineMethod("IFillAbleObject.get_Modified", MethodAttributes.Public | MethodAttributes.Virtual, typeof(bool), Type.EmptyTypes);
            generator = getModifiedMethode.GetILGenerator();

            Label returnTrueLabel = generator.DefineLabel();
            foreach (Mapping mapping in _mappingInfos)
            {
                mapping.AddModifiedCode(generator, returnTrueLabel);
            }
            generator.Emit(OpCodes.Ldc_I4_0);
            generator.Emit(OpCodes.Ret);
            generator.MarkLabel(returnTrueLabel);
            generator.Emit(OpCodes.Ldc_I4_1);
            generator.Emit(OpCodes.Ret);
            typeBuilder.DefineMethodOverride(getModifiedMethode, typeof(IFillAbleObject).GetProperty("Modified").GetGetMethod());
#endregion

#region DeleteChildObjects-Method
            MethodBuilder deleteChildObjects = typeBuilder.DefineMethod("IFillAbleObject.DeleteChildObjects", MethodAttributes.Public | MethodAttributes.Virtual, null, Type.EmptyTypes);
            generator = deleteChildObjects.GetILGenerator();
            foreach (Mapping mapping in _mappingInfos)
            {
                mapping.AddDeleteChildObjectsCode(generator);
            }
            generator.Emit(OpCodes.Ret);
            typeBuilder.DefineMethodOverride(deleteChildObjects, typeof(IFillAbleObject).GetMethod("DeleteChildObjects"));
#endregion

#region SaveChildObjects-Method
            MethodBuilder saveChildObjects = typeBuilder.DefineMethod("IFillAbleObject.SaveChildObjects", MethodAttributes.Public | MethodAttributes.Virtual, null, Type.EmptyTypes);
            generator = saveChildObjects.GetILGenerator();
            foreach (Mapping mapping in _mappingInfos)
            {
                mapping.AddSaveChildObjectsCode(generator);
            }
            generator.Emit(OpCodes.Ret);
            typeBuilder.DefineMethodOverride(saveChildObjects, typeof(IFillAbleObject).GetMethod("SaveChildObjects"));
#endregion

#region CheckChildObjectsChanged-Method
            MethodBuilder checkChildObjectsChanged = typeBuilder.DefineMethod("IFillAbleObject.CheckChildObjectsChanged", MethodAttributes.Public | MethodAttributes.Virtual, typeof(bool), Type.EmptyTypes);
            generator = checkChildObjectsChanged.GetILGenerator();
            foreach (Mapping mapping in _mappingInfos)
            {
                mapping.AddCheckChildObjectsChangedCode(generator);
            }
            generator.Emit(OpCodes.Ldc_I4_0);
            generator.Emit(OpCodes.Ret);
            typeBuilder.DefineMethodOverride(checkChildObjectsChanged, typeof(IFillAbleObject).GetMethod("CheckChildObjectsChanged"));
#endregion

#region DropChanges-Method
            MethodBuilder dropChanges = typeBuilder.DefineMethod("IFillAbleObject.DropChanges", MethodAttributes.Public | MethodAttributes.Virtual, null, Type.EmptyTypes);
            generator = dropChanges.GetILGenerator();
            isNotNewLabel = generator.DefineLabel();
            isNotDeletedLabel = generator.DefineLabel();
            foreach (Mapping mapping in _mappingInfos)
            {
                mapping.AddDropChangesCode(generator);
            }

            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldfld, isNewField);
            generator.Emit(OpCodes.Brfalse, isNotNewLabel);

            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Callvirt, getStateMethode);

            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldc_I4_1);
            generator.Emit(OpCodes.Stfld, isDeattachedField);

            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Callvirt, getStateMethode);
            generator.Emit(OpCodes.Callvirt, raiseStateChangedMethode);
            generator.Emit(OpCodes.Ret);

            generator.MarkLabel(isNotNewLabel);

            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldfld, isDeletedField);
            generator.Emit(OpCodes.Brfalse, isNotDeletedLabel);

            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Callvirt, getStateMethode);

            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldc_I4_0);
            generator.Emit(OpCodes.Stfld, isDeletedField);

            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Callvirt, getStateMethode);
            generator.Emit(OpCodes.Callvirt, raiseStateChangedMethode);
            generator.MarkLabel(isNotDeletedLabel);

            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldnull);
            generator.Emit(OpCodes.Callvirt, raiseMethode);

            generator.Emit(OpCodes.Ret);
            typeBuilder.DefineMethodOverride(dropChanges, typeof(IFillAbleObject).GetMethod("DropChanges"));
#endregion

#region Delete-Method
            MethodBuilder delete = typeBuilder.DefineMethod("IFillAbleObject.Delete", MethodAttributes.Public | MethodAttributes.Virtual, null, Type.EmptyTypes);
            generator = delete.GetILGenerator();
            Label isDeletedLabel = generator.DefineLabel();
            Label newIsFalseLabel = generator.DefineLabel();


            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldfld, isDeletedField);
            generator.Emit(OpCodes.Brtrue, isDeletedLabel);

            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Callvirt, getStateMethode);

            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldfld, isNewField);
            generator.Emit(OpCodes.Brfalse, newIsFalseLabel);
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldc_I4_1);
            generator.Emit(OpCodes.Stfld, isDeattachedField);
            generator.MarkLabel(newIsFalseLabel);
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldc_I4_1);
            generator.Emit(OpCodes.Stfld, isDeletedField);

            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Callvirt, getStateMethode);
            generator.Emit(OpCodes.Callvirt, raiseStateChangedMethode);
            generator.MarkLabel(isDeletedLabel);
            generator.Emit(OpCodes.Ret);
            typeBuilder.DefineMethodOverride(delete, typeof(IFillAbleObject).GetMethod("Delete"));
#endregion

#region Deattach-Method
            MethodBuilder deattach = typeBuilder.DefineMethod("IFillAbleObject.Deattach", MethodAttributes.Public | MethodAttributes.Virtual, null, Type.EmptyTypes);
            generator = deattach.GetILGenerator();
            Label isNotDeattached = generator.DefineLabel();

            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldfld, isDeattachedField);
            generator.Emit(OpCodes.Brfalse, isNotDeattached);
            generator.Emit(OpCodes.Ret);
            generator.MarkLabel(isNotDeattached);
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Callvirt, getStateMethode);
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldc_I4_1);
            generator.Emit(OpCodes.Stfld, isDeattachedField);
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Callvirt, getStateMethode);
            generator.Emit(OpCodes.Callvirt, raiseStateChangedMethode);
            generator.Emit(OpCodes.Ret);
            typeBuilder.DefineMethodOverride(deattach, typeof(IFillAbleObject).GetMethod("Deattach"));
#endregion

#region DropChangesChildObjects-Method
            MethodBuilder dropChangesChildObjects = typeBuilder.DefineMethod("IFillAbleObject.DropChangesChildObjects", MethodAttributes.Public | MethodAttributes.Virtual, null, Type.EmptyTypes);
            generator = dropChangesChildObjects.GetILGenerator();

            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Call, dropChanges);

            foreach (Mapping mapping in _mappingInfos)
            {
                mapping.AddDropChangesCodeChildObjects(generator);
            }
            generator.Emit(OpCodes.Ret);
            typeBuilder.DefineMethodOverride(dropChangesChildObjects, typeof(IFillAbleObject).GetMethod("DropChangesChildObjects"));
#endregion

#region FillCommand-Funktion
            //void FillCommand(ICommandBuilder commandBuilder);
            MethodBuilder getInsertCommandBuilder = typeBuilder.DefineMethod("IFillAbleObject.GetInsertCommand", MethodAttributes.Virtual | MethodAttributes.Public, null, new Type[] { typeof(ICommandBuilder) });
            typeBuilder.DefineMethodOverride(getInsertCommandBuilder, typeof(IFillAbleObject).GetMethod("FillCommand"));
            generator = getInsertCommandBuilder.GetILGenerator();

            generator.Emit(OpCodes.Ldarg_1);
            generator.Emit(OpCodes.Ldstr, TableName);
            generator.Emit(OpCodes.Callvirt, typeof(ICommandBuilder).GetProperty("Tablename").GetSetMethod());

            foreach (Mapping mapping in _mappingInfos)
            {
                mapping.AddFillCommandBuilderCode(generator);
            }

            generator.Emit(OpCodes.Ret);
#endregion

            _dynamicType =
#if DNXCORE50
                typeBuilder.CreateTypeInfo().DeclaringType;
#else
                typeBuilder.CreateType();
#endif
            _constructor = Expression.Lambda<Func<object>>(Expression.New(_dynamicType.GetConstructor(Type.EmptyTypes))).Compile();
#endregion
        }

        public virtual object CreateObject()
        {
            return _constructor();
        }

        public virtual T FillCommand<T>(T commandBuilder) where T : ICommandBuilder
        {
            commandBuilder.Tablename = _tableName;
            foreach (Mapping mapping in _mappingInfos)
            {
                mapping.FillCommandBuilder(commandBuilder);
            }
            return commandBuilder;

        }

        public virtual MappedObjectKeys GetKeyValues(IDataReader reader)
        {
            return _getKeyObjectFromReader(reader); 
        }

        public virtual string ParseExpression(Expressions.ParameterExpression parameterExpression)
        {
            ReadOnlyCollection<Mapping> keyMappingInfos = this.KeyMappingInfos;
            if (keyMappingInfos.Count != 1)
                throw new Expressions.NotParsableException("ParameterExpressions of mapped types witch has no key or a segmented key cannot be parsed.");

            return keyMappingInfos[0].ParseExpression(parameterExpression);
        }
#endregion

#region Properties
        public virtual string TableName { 
            get { return _tableName; }
            set { _tableName = value; }
        }

        public virtual ReadOnlyCollection<Mapping> KeyMappingInfos
        {
            get
            {
                if (_keyMappingInfos == null)
                {
                    _keyMappingInfos = _mappingInfos.FindAll(x => x.IsPrimaryKey);
                }
                return _keyMappingInfos.AsReadOnly();
            }
        }

        protected Type Type
        {
            get
            {
                return _type;
            }
        }

        protected Type DynamicType
        {
            get
            {
                return _dynamicType;
            }
        }

        public virtual LoadBehavior LoadBehavior
        {
            get
            {
                return _loadBehavior;
            }
        }
#endregion
    }
}
