using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace ObjectStore.MappingOptions
{
    public class MappingOptionsSet
    {
        #region Fields
        static Dictionary<Type, WeakReference<MappingOptionsSet>> _mappedInOptionSet = new Dictionary<Type, WeakReference<MappingOptionsSet>>();

        Dictionary<Type, TypeMappingOptions> _typeMappingOptions = new Dictionary<Type, TypeMappingOptions>();
        Dictionary<MemberInfo, MemberMappingOptions> _memberMappingOptions = new Dictionary<MemberInfo, MemberMappingOptions>();

        List<Tuple<Func<Type, bool>, Action<TypeMappingOptions>>> _typeMappingRules = new List<Tuple<Func<Type, bool>, Action<TypeMappingOptions>>>();
        List<Tuple<Func<MemberInfo, bool>, Action<MemberTypeOptions>>> _memberTypeRules = new List<Tuple<Func<MemberInfo, bool>, Action<MemberTypeOptions>>>();
        List<Tuple<Func<MemberInfo, bool>, Action<MemberMappingOptions>>> _memberMappingRules = new List<Tuple<Func<MemberInfo, bool>, Action<MemberMappingOptions>>>();

        bool _isFrozen = false;
        #endregion

        #region Constructors
        public MappingOptionsSet()
        {
        }
        #endregion

        #region Methods
        #region Internal
        internal static TypeMappingOptions GetExistingTypeMappingOptions(Type type)
        {
            MappingOptionsSet mappingOptionsSet = null;

            if (_mappedInOptionSet.ContainsKey(type))
                mappingOptionsSet = _mappedInOptionSet[type].Value;

            return mappingOptionsSet?.GetTypeMappingOptions(type);
        }

        internal static MemberMappingOptions GetExistingMemberMappingOptions(System.Linq.Expressions.Expression expression)
        {
            System.Linq.Expressions.MemberExpression memberExpression = expression as System.Linq.Expressions.MemberExpression;
            return GetExistingTypeMappingOptions(memberExpression.Expression.Type)?.MemberMappingOptions.Where(x => x.Member.MetadataToken == memberExpression.Member.MetadataToken && x.Member.Module == memberExpression.Member.Module).FirstOrDefault();
        }

        internal TypeMappingOptions GetTypeMappingOptions(Type type)
        {
            if (_mappedInOptionSet.ContainsKey(type))
            {
                WeakReference<MappingOptionsSet> optionSetReference = _mappedInOptionSet[type];
                if (!optionSetReference.IsAlive)
                    _mappedInOptionSet.Remove(type);
                else if (optionSetReference.Value != this)
                    throw new InvalidOperationException($"Type {type} is configured in more than one mapping options sets.");
            }

            _isFrozen = true;

            if (_typeMappingOptions.ContainsKey(type))
                return _typeMappingOptions[type];

            TypeMappingOptions returnValue = new TypeMappingOptions(type, this);

            foreach (Action<TypeMappingOptions> action in _typeMappingRules.Where(x => x.Item1(type)).Select(x => x.Item2))
                action(returnValue);

            _typeMappingOptions.Add(type, returnValue);
            _mappedInOptionSet.Add(type, this);
            return returnValue;
        }

        internal IEnumerable<MemberMappingOptions> GetMemberMappingOptions(Type type)
        {
            _isFrozen = true;

            foreach (PropertyInfo propertyInfo in type.GetProperties(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.GetProperty).Where(x => x.GetGetMethod()?.IsAbstract == true))
            {
                if (_memberMappingOptions.ContainsKey(propertyInfo))
                {
                    yield return _memberMappingOptions[propertyInfo];
                    continue;
                }

                MemberTypeOptions memberTypeOptions = new MemberTypeOptions(propertyInfo);

                foreach (Action<MemberTypeOptions> action in _memberTypeRules.Where(x => x.Item1(propertyInfo)).Select(x => x.Item2))
                    action(memberTypeOptions);

                MemberMappingOptions memberMappingOptions;

                switch (memberTypeOptions.MappingType)
                {
                    case MappingType.FieldMapping:
                        memberMappingOptions = new FieldMappingOptions(this, propertyInfo);
                        break;
                    case MappingType.ForeignObjectMapping:
                        memberMappingOptions = new ForeignObjectMappingOptions(this, propertyInfo);
                        break;
                    case MappingType.ReferenceListMapping:
                        memberMappingOptions = new ReferenceListMappingOptions(this, propertyInfo);
                        break;
                    default:
                        continue;
                }

                foreach (Action<MemberMappingOptions> action in _memberMappingRules.Where(x => x.Item1(propertyInfo)).Select(x => x.Item2))
                    action(memberMappingOptions);

                _memberMappingOptions.Add(propertyInfo, memberMappingOptions);

                yield return memberMappingOptions;
            }
        }

        private void CheckFrozen()
        {
            if (_isFrozen)
                throw new InvalidOperationException("Object is frozen, and can't be changed in this state.");
        }
        #endregion
        #region Public
        public void AddTypeRule(Func<Type, bool> condition, Action<TypeMappingOptions> action)
        {
            CheckFrozen();

            _typeMappingRules.Add(Tuple.Create(condition, action));
        }

        public void AddMemberTypeRule(Func<MemberInfo, bool> condition, Action<MemberTypeOptions> action)
        {
            CheckFrozen();

            _memberTypeRules.Add(Tuple.Create(condition, action));
        }

        public void AddMemberMappingRule(Func<MemberInfo, bool> condition, Action<MemberMappingOptions> action)
        {
            CheckFrozen();

            _memberMappingRules.Add(Tuple.Create(condition, action));
        }
        #endregion
        #endregion
    }
}
