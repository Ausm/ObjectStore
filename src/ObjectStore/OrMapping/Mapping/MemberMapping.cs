using System;
using System.Reflection;
using System.Collections.Generic;
using System.Reflection.Emit;
using ObjectStore.MappingOptions;
using ObjectStore.Database;

namespace ObjectStore.OrMapping
{
    internal abstract class MemberMapping
    {
        #region Create functions
        static Dictionary<MemberInfo, MemberMapping> _mappings = new Dictionary<MemberInfo, MemberMapping>();

        public static MemberMapping GetMapping(MemberMappingOptions memberMappingOptions)
        {
            if (_mappings.ContainsKey(memberMappingOptions.Member))
                return _mappings[memberMappingOptions.Member];

            if (memberMappingOptions is ForeignObjectMappingOptions)
                return _mappings[memberMappingOptions.Member] = new ForeignObjectPropertyMapping((ForeignObjectMappingOptions)memberMappingOptions);
            else if (memberMappingOptions is ReferenceListMappingOptions)
                return _mappings[memberMappingOptions.Member] = new ReferenceListPropertyMapping((ReferenceListMappingOptions)memberMappingOptions);
            else
                return _mappings[memberMappingOptions.Member] = new PropertyMapping((FieldMappingOptions)memberMappingOptions);
        }
        #endregion

        #region Membervariablen
        MemberMappingOptions _memberMappingOptions;
        #endregion

        #region Konstruktoren
        protected MemberMapping(MemberMappingOptions memberMappingOptions)
        {
            _memberMappingOptions = memberMappingOptions;
        }
        #endregion

        #region Abstract Methods
        #region Dynamic code generation
        public abstract void AddDynamicGetKeyCode(ILGenerator dynamicMethod, int index, LocalBuilder array);
        public abstract void AddDynamicGetKeyFromReaderCode(ILGenerator dynamicMethod, int index, LocalBuilder array);
        public virtual void AddInhertiedProperty(TypeBuilder typeBuilder, MethodInfo notifyChangeMethode){}
        public virtual void AddConstructorCode(ILGenerator generator) { }
        public virtual void AddFillMethodeCode(ILGenerator generator) { }
        public virtual void AddFillCommandBuilderCode(ILGenerator generator) { }
        public virtual void AddDeleteChildObjectsCode(ILGenerator generator) { }
        public virtual void AddSaveChildObjectsCode(ILGenerator generator) { }
        public virtual void AddCheckChildObjectsChangedCode(ILGenerator generator) { }
        public virtual void AddDropChangesCodeChildObjects(ILGenerator generator) { }
        public virtual void AddCommitCode(ILGenerator generator, MethodInfo raisePropertyChanged) { }
        public virtual void AddRollbackCode(ILGenerator generator) { }
        public virtual void AddModifiedCode(ILGenerator generator, Label returnTrueLabel) { }
        public virtual void AddDropChangesCode(ILGenerator generator) { }
        #endregion

        #region Command methods
        public abstract void FillCommandBuilder(ICommandBuilder updateCommandBuilder);
        #endregion
        #endregion

        #region Properties

        public virtual bool IsPrimaryKey => _memberMappingOptions.IsPrimaryKey;
        public virtual MemberInfo MemberInfo => _memberMappingOptions.Member;

        public abstract string FieldName { get; }

        #region Abstract properties
        public abstract Type FieldType { get; }
        #endregion
        #endregion
    }
}