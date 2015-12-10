using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ObjectStore;

namespace ObjectStore.Test.Entities
{
    [Table("dbo.TestTable")]
    public abstract class Test
    {
        [Mapping(FieldName = "Id"), IsPrimaryKey]
        public abstract int Id { get; }

        [Mapping(FieldName ="[Name]")]
        public abstract string Name { get; set; }

        [Mapping(FieldName ="[Description]")]
        public abstract string Description { get; set; }
    }
}
