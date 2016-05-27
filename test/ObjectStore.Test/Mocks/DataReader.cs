using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;

namespace ObjectStore.Test.Mocks
{
    class DataReader : DbDataReader
    {
        #region Field
        string[] _columnNames;
        IEnumerable<object[]> _values;
        IEnumerator<object[]> _current;
        #endregion

        #region Constructors
        public DataReader(string[] columnNames, IEnumerable<object[]> values)
        {
            _columnNames = columnNames;
            _values = values;
            _current = _values.GetEnumerator();
        }
        #endregion

        #region Overrides
        public override object this[string name]
        {
            get
            {
                return GetValue(GetOrdinal(name));
            }
        }

        public override object this[int ordinal]
        {
            get
            {
                return GetValue(ordinal);
            }
        }

        public override int Depth
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override int FieldCount
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override bool HasRows
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override bool IsClosed
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override int RecordsAffected
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override bool GetBoolean(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override byte GetByte(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override long GetBytes(int ordinal, long dataOffset, byte[] buffer, int bufferOffset, int length)
        {
            throw new NotImplementedException();
        }

        public override char GetChar(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override long GetChars(int ordinal, long dataOffset, char[] buffer, int bufferOffset, int length)
        {
            throw new NotImplementedException();
        }

        public override string GetDataTypeName(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override DateTime GetDateTime(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override decimal GetDecimal(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override double GetDouble(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override IEnumerator GetEnumerator()
        {
            throw new NotImplementedException();
        }

        public override Type GetFieldType(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override float GetFloat(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override Guid GetGuid(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override short GetInt16(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override int GetInt32(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override long GetInt64(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override string GetName(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override int GetOrdinal(string name)
        {
            for (int i = 0; i < _columnNames.Length; i++)
            {
                if (_columnNames[i] == name)
                    return i;
            }

            throw new ArgumentOutOfRangeException(nameof(name));
        }

        public override string GetString(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override object GetValue(int ordinal)
        {
            return _current.Current[ordinal];
        }

        public override int GetValues(object[] values)
        {
            throw new NotImplementedException();
        }

        public override bool IsDBNull(int ordinal)
        {
            return this[ordinal] == DBNull.Value;
        }

        public override bool NextResult()
        {
            throw new NotImplementedException();
        }

        public override bool Read()
        {
            return _current.MoveNext();
        }
        #endregion
    }
}
