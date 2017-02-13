using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ObjectStore.MappingOptions
{
    public class TypeMappingOptions
    {
        #region Fields
        MappingOptionsSet _parent;
        Type _forType;
        #endregion

        #region Constructor
        internal TypeMappingOptions(Type forType, MappingOptionsSet parent)
        {
            _parent = parent;
            _forType = forType;
            TableName = forType.Name;
        }
        #endregion

        #region Properties
        public Type Type => _forType;

        public string TableName { get; set; }

        public LoadBehavior LoadBehavior { get; set; } = LoadBehavior.OnDemandPartialLoad;
        #endregion

    }

}
