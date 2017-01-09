using System;
using System.Xml.Linq;

namespace ObjectStore.Test.Entities
{
    [Table("dbo.DifferentTypesTable")]
    public abstract class DifferentTypes
    {
        [Mapping(FieldName = "Id"), IsPrimaryKey]
        public abstract int Id { get; }

        [Mapping(FieldName = "[Text]")]
        public abstract string Text { get; set; }

        [Mapping(FieldName = "[Boolean]")]
        public abstract bool Boolean { get; set; }

        [Mapping(FieldName = "[Int]")]
        public abstract int Int { get; set; }

        [Mapping(FieldName = "[Byte]")]
        public abstract byte Byte { get; set; }

        [Mapping(FieldName = "[Short]")]
        public abstract short Short { get; set; }

        [Mapping(FieldName = "[Long]")]
        public abstract long Long { get; set; }

        [Mapping(FieldName = "[DateTime]")]
        public abstract DateTime DateTime { get; set; }

        [Mapping(FieldName = "[Guid]")]
        public abstract Guid Guid { get; set; }

        [Mapping(FieldName = "[Binary]")]
        public abstract byte[] Binary { get; set; }

        [Mapping(FieldName = "[Decimal]")]
        public abstract decimal Decimal { get; set; }

        [Mapping(FieldName = "[Xml]")]
        public abstract XElement Xml { get; set; }
    }
}
