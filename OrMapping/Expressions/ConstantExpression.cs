using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ausm.ObjectStore.OrMapping.Expressions
{
    public abstract class ConstantExpression : Expression
    {
        #region Membervariablen
        ParsedExpression _parsedExpression;
        object _value;
        #endregion

        #region Konstruktoren
        internal ConstantExpression(ParsedExpression parsedExpression, Expression parentExpression, object value)
            : base(parentExpression)
        {
            _parsedExpression = parsedExpression;
            _value = value;
        }
        #endregion

        #region Properties
        protected ParsedExpression ParsedExpression
        {
            get
            {
                return _parsedExpression;
            }
        }

        protected object Value
        {
            get
            {
                return _value;
            }
        }
        #endregion

        #region Statische CreateFunktion
        public static ConstantExpression Create(ParsedExpression parsedExpression, Expression parentExpression, object value)
        {
            if (value == null)
                return new NullExpression(parsedExpression, parentExpression);

            if (value is IFillAbleObject)
                return new MappedObjectExpression(parsedExpression, parentExpression, value as IFillAbleObject);
            if(value is string)
                return new ValueExpression(parsedExpression, parentExpression, value);
            if (value is System.Collections.IEnumerable)
                return new ArrayObjectExpression(parsedExpression, parentExpression, (System.Collections.IEnumerable)value);
            return new ValueExpression(parsedExpression, parentExpression, value);
        }
        #endregion
    }

    public class ValueExpression : ConstantExpression
    {
        System.Data.SqlClient.SqlParameter _sqlParam = null;

        internal ValueExpression(ParsedExpression parsedExpression, Expression parentExpression, object value)
            : base(parsedExpression, parentExpression, value)
        {}

        public override string SqlExpression
        {
            get { return ((System.Data.SqlClient.SqlParameter)_sqlParam == null ? _sqlParam = ParsedExpression.AquireSqlParameter(Value): _sqlParam).ParameterName; }
        }
    }

    public class MappedObjectExpression : ConstantExpression
    {
        System.Data.SqlClient.SqlParameter _sqlParameter = null;

        internal MappedObjectExpression(ParsedExpression parsedExpression, Expression parentExpression, IFillAbleObject value)
            : base(parsedExpression, parentExpression, value)
        {
        }

        public override string SqlExpression
        {
            get 
            {
                System.Collections.IEnumerator enumerator = ((IFillAbleObject)Value).Keys.GetEnumerator();
                if (enumerator.MoveNext())
                {
                    object value = enumerator.Current;
                    if (!enumerator.MoveNext())
                        return ((System.Data.SqlClient.SqlParameter)_sqlParameter == null ? _sqlParameter = ParsedExpression.AquireSqlParameter(value) : _sqlParameter).ParameterName;

                    throw new NotParsableException("Objects with segmented keys can not be parsed.");
                }
                throw new NotParsableException("Objects with no keys can not be parsed.");
            }
        }
    }

    public class ArrayObjectExpression : ConstantExpression
    {
        List<ConstantExpression> _constantExpressions = null;
        
        internal ArrayObjectExpression(ParsedExpression parsedExpression, Expression parentExpression, System.Collections.IEnumerable enumerable) : base(parsedExpression, parentExpression, enumerable)
        {
            _constantExpressions = new List<ConstantExpression>();
            foreach (object obj in enumerable)
            {
                _constantExpressions.Add(ConstantExpression.Create(parsedExpression, this, obj));
            }
        }

        public override string SqlExpression
        {
            get 
            {
                if (_constantExpressions.Count == 0)
                    return string.Format("(SELECT NULL)");
                else
                    return string.Format("({0})", string.Join(", ", _constantExpressions.Select(x => x.SqlExpression).ToArray()));
            }
        }
    }

    public class NullExpression : ConstantExpression
    {
        internal NullExpression(ParsedExpression parsedExpression, Expression parentExpression)
            : base(parsedExpression, parentExpression, null)
        {
        }

        public override string SqlExpression
        {
            get 
            {
                throw new NotParsableException("This use of Null is not implemented");
            }
        }
    }
}
