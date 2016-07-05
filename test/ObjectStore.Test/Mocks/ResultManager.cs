using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;

namespace ObjectStore.Test.Mocks
{
    public abstract class ResultManagerBase
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

            public void SetValues(IEnumerable<object[]> values)
            {
                _values.Insert(0, values);
            }

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
    public class ResultManager<T> : ResultManagerBase
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

        public void SetValues(T key, IEnumerable<object[]> values)
        {
            if (_items.ContainsKey(key))
                _items[key].SetValues(values);
        }

        public override DataReader GetReader(DbCommand command)
        {
            return GetReader(command, null);
        }

        public DataReader GetReader(DbCommand command, ICollection<T> key)
        {
            string fullCommandText = command.CommandText;
            try
            {
                string[] columnNames = null;
                IEnumerable<object[]> values = null;
                List<Tuple<string[], IEnumerable<object[]>>> resultSets = new List<Tuple<string[], IEnumerable<object[]>>>();

                KeyValuePair<T, Item> item = _items.FirstOrDefault(x => x.Value.IsApplicable(command));
                if (item.Value != null)
                {
                    key?.Add(item.Key);
                    columnNames = item.Value.ColumnNames;
                    values = item.Value.GetValues();
                    resultSets.Add(Tuple.Create(item.Value.ColumnNames, item.Value.GetValues()));
                }
                else
                {
                    foreach (string commandText in fullCommandText.Split(';'))
                    {
                        command.CommandText = commandText;

                        item = _items.FirstOrDefault(x => x.Value.IsApplicable(command));
                        if (item.Value == null)
                            return null;

                        key?.Add(item.Key);
                        if (columnNames == null)
                        {
                            columnNames = item.Value.ColumnNames;
                            values = item.Value.GetValues();
                        }
                        else
                        {
                            resultSets.Add(Tuple.Create(item.Value.ColumnNames, item.Value.GetValues()));
                        }
                    }
                }

                return new DataReader(columnNames, values, resultSets);
            }
            finally
            {
                command.CommandText = fullCommandText;
            }
        }
    }
}
