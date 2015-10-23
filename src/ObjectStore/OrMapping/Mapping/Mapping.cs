using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;

namespace ObjectStore.OrMapping
{
    internal abstract class Mapping
    {
        #region Erstellfunktion
        static Dictionary<PropertyInfo, Mapping> _mappings = new Dictionary<PropertyInfo, Mapping>();

        public static Mapping GetMapping(PropertyInfo propertyInfo)
        {
            if (_mappings.ContainsKey(propertyInfo))
            {
                return _mappings[propertyInfo];
            }

            if (propertyInfo.GetCustomAttributes(typeof(DisableMappingAttribute), true).Any())
            {
                return _mappings[propertyInfo] = null;
            }
            else if (propertyInfo.GetCustomAttributes(typeof(ForeignObjectMappingAttribute), true).Any())
            {
                return _mappings[propertyInfo] = new ForeignObjectPropertyMapping(propertyInfo);
            }
            else if (propertyInfo.GetCustomAttributes(typeof(ReferenceListMappingAttribute), true).Any())
            {
                return _mappings[propertyInfo] = new ReferenceListPropertyMapping(propertyInfo);
            }
            else
            {
                return _mappings[propertyInfo] = new PropertyMapping(propertyInfo);
            }
        }
        #endregion
        
        #region Membervariablen
        bool _isPrimaryKey;
        MemberInfo _memberInfo;
        #endregion

        #region Konstruktoren
        protected Mapping(MemberInfo memberInfo)
        {
            _isPrimaryKey = memberInfo.GetCustomAttributes(typeof(IsPrimaryKeyAttribute), true).Any();
            
            _memberInfo = memberInfo;
        }
        #endregion

        #region Abstrakte Funktionen
        #region Dynamische CodeGenerierung
        public abstract void AddDynamicGetKeyCode(System.Reflection.Emit.ILGenerator dynamicMethod, int index, LocalBuilder array);
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

        #region CommandFunktionen
        public abstract void FillCommandBuilder(ICommandBuilder updateCommandBuilder);

        public abstract string ParseExpression(Expressions.Expression expression);
        #endregion

        #region ExpressionParseFunktionen
        public virtual Expressions.ParsedExpression.Join GetJoinForProperty()
        {
            return null;
        }
        #endregion
        #endregion

        #region Properties

        public virtual bool IsPrimaryKey { get { return _isPrimaryKey; } }
        public virtual MemberInfo MemberInfo { get { return _memberInfo; } }

        public abstract string FieldName { get; }

        #region Abstrakte Properties
        public abstract Type FieldType { get; }
        #endregion
        #endregion
    }
}