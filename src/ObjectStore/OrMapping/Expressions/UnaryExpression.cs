using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ObjectStore.OrMapping.Expressions
{
    public abstract class UnaryExpression : Expression
    {
        internal UnaryExpression(Expression parent)
            : base(parent)
        {
        }

        public abstract Expression Operand { get; }
    }

    public class NotExpression : UnaryExpression
    {
        System.Linq.Expressions.UnaryExpression _source;
        Expression _operand;

        internal NotExpression(System.Linq.Expressions.UnaryExpression source, Expression parentExpression, ParsedExpression parsedExpression)
            : base(parentExpression)
        {
            _source = source;
            _operand = Expression.ParseExpression(source.Operand, this, parsedExpression);

            if(_operand is ContainsExpression)
                ((ContainsExpression)_operand).Negate();
        }

        public override Expression Operand
        {
            get 
            { 
                return _operand; 
            }
        }

        public override string SqlExpression
        {
            get
            {
                if(_operand is QueryableExpression)
                    return string.Format("NOT {0}", _operand.SqlExpression);
                else if (_operand is ContainsExpression)
                    return _operand.SqlExpression;

                throw new NotParsableException(string.Format("Cannot parse Not-Expression with unsupported child expression type \"{0}\".", _operand.GetType().Name), _source);
            }
        }
    }
}
