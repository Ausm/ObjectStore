using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ObjectStore.MappingOptions
{
    public class TypeMappingOptions
    {
        #region Fields
        MappingOptionsSet _parent;
        Type _forType;
        List<MemberMappingOptions> _memberMappingOptions;
        #endregion

        #region Constructor
        internal TypeMappingOptions(Type forType, MappingOptionsSet parent)
        {
            _parent = parent;
            _forType = forType;
            TableName = forType.Name;
            _memberMappingOptions = null;
        }
        #endregion

        #region Properties
        public Type Type => _forType;

        public string TableName { get; set; }

        public LoadBehavior LoadBehavior { get; set; } = LoadBehavior.OnDemandPartialLoad;

        public MethodInfo RaisePropertyChangeMethod { get; set; } = null;

        internal IEnumerable<MemberMappingOptions> MemberMappingOptions
        {
            get
            {
                if (_memberMappingOptions == null)
                {
                    _memberMappingOptions = _parent.GetMemberMappingOptions(_forType).ToList();
                }
                return _memberMappingOptions.AsReadOnly();
            }
        }
        #endregion

    }

}
