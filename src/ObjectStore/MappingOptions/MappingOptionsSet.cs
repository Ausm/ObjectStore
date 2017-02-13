using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ObjectStore.MappingOptions
{
    public class MappingOptionsSet
    {
        #region Fields
        Dictionary<Type, TypeMappingOptions> _typeMappingOptions;
        List<Tuple<Func<Type, bool>, Action<TypeMappingOptions>>> _typeMappingRules = new List<Tuple<Func<Type, bool>, Action<TypeMappingOptions>>>();

        bool _isFrozen;
        #endregion

        #region Constructors
        public MappingOptionsSet()
        {
            _typeMappingOptions = new Dictionary<Type, TypeMappingOptions>();
            _isFrozen = false;
        }
        #endregion

        #region Methods
        #region Internal
        internal TypeMappingOptions GetTypeMappingOptions(Type type)
        {
            if (_typeMappingOptions.ContainsKey(type))
                return _typeMappingOptions[type];

            TypeMappingOptions returnValue = new TypeMappingOptions(type, this);

            foreach (Action<TypeMappingOptions> action in _typeMappingRules.Where(x => x.Item1(type)).Select(x => x.Item2))
                action(returnValue);

            _typeMappingOptions.Add(type, returnValue);
            return returnValue;
        }

        internal void Freeze()
        {
            _isFrozen = true;
        }
        #endregion
        #region Public
        public void AddTypeRule(Func<Type, bool> condition, Action<TypeMappingOptions> action)
        {
            if (_isFrozen)
                throw new InvalidOperationException("Object is frozen, and can't be changed in this state.");

            _typeMappingRules.Add(Tuple.Create(condition, action));

            foreach (TypeMappingOptions item in _typeMappingOptions.Where(x => condition(x.Key)).Select(x => x.Value))
                action(item);
        }
        #endregion
        #endregion
    }
}
