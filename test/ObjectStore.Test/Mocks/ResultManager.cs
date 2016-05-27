using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;

namespace ObjectStore.Test.Mocks
{
    abstract class ResultManagerBase
    {
        protected class Item
        {
            Func<DbCommand, bool> _condition;
            string[] _columnNames;
            List<IEnumerable<object[]>> _values;

            public Item(Func<DbCommand, bool> condition, string[] columnNames, params object[][] values) : this (condition, columnNames, (IEnumerable<object[]>) values) {}

            public Item(Func<DbCommand, bool> condition, string[] columnNames, IEnumerable<object[]> values)
            {
                _condition = condition;
                _columnNames = columnNames;
                _values = new List<IEnumerable<object[]>>(new[] { values });
            }

            public bool IsApplicable(DbCommand command) => _condition(command);

            public string[] ColumnNames => _columnNames;

            public IEnumerable<object[]> GetValues()
            {
                if (_values.Count == 0)
                    return Enumerable.Empty<object[]>();

                IEnumerable<object[]> returnValue = _values[0];
                if (_values.Count > 1)
                    _values.RemoveAt(0);

                return returnValue;
            }
        }

        public abstract DataReader GetReader(DbCommand command);
    }
    class ResultManager<T> : ResultManagerBase
    {

        Dictionary<T, Item> _items;

        public ResultManager()
        {
            _items = new Dictionary<T, Item>();
        }

        public void AddItem(T key, Func<DbCommand, bool> condition, string[] columnNames, IEnumerable<object[]> values)
        {
            if (_items.ContainsKey(key))
                _items[key] = new Item(condition, columnNames, values);
            else
                _items.Add(key, new Item(condition, columnNames, values));
        }

        public override DataReader GetReader(DbCommand command)
        {
            T key;
            return GetReader(command, out key);
        }

        public DataReader GetReader(DbCommand command, out T key)
        {
            KeyValuePair<T, Item> item = _items.FirstOrDefault(x => x.Value.IsApplicable(command));
            key = item.Key;
            if (item.Value == null)
                return null;

            return new DataReader(item.Value.ColumnNames, item.Value.GetValues());
        }
    }
}
