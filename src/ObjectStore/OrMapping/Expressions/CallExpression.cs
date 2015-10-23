#if DNXCORE50
using System.Reflection;
#endif

using System;
using System.Linq;

namespace ObjectStore.OrMapping.Expressions
{
    public abstract class CallExpression : Expression
    {
        internal CallExpression(Expression parentExpression) : base(parentExpression) { }

        internal static CallExpression Create(System.Linq.Expressions.MethodCallExpression linqExpression, Expression parentExpression, ParsedExpression parsedExpression)
        {
            SqlSubstituteAttribute substituteAttribute = linqExpression.Method.GetCustomAttributes(typeof(SqlSubstituteAttribute), true).FirstOrDefault() as SqlSubstituteAttribute;

            if (substituteAttribute != null)
            {
                return new CallSqlSubstitutedMethodExpression(linqExpression, parentExpression, parsedExpression, substituteAttribute);
            }

            if (linqExpression.Method.DeclaringType == typeof(System.Linq.Queryable) &&
                linqExpression.Arguments.Count > 0 &&
                linqExpression.Arguments[0].Type.GetInterfaces().Where(x => x == typeof(IQueryable)).Any())
            {
                return new QueryableExpression(linqExpression, parentExpression, parsedExpression);
            }

            if (linqExpression.Method.DeclaringType == typeof(Enumerable) &&
                linqExpression.Method.Name == "Contains" &&
                linqExpression.Arguments.Count == 2)
            {
                return new ContainsExpression(linqExpression, parentExpression, parsedExpression);
            }

            throw new NotParsableException(string.Format("Methodcall {0} can not be parsed.", linqExpression.Method.Name), linqExpression);
        }
    }

    public class CallSqlSubstitutedMethodExpression : CallExpression
    {
        Expression[] _arguments;
        SqlSubstituteAttribute _attribute;
        System.Linq.Expressions.MethodCallExpression _linqExpression;

        internal CallSqlSubstitutedMethodExpression(System.Linq.Expressions.MethodCallExpression linqExpression, Expression parendExpression, ParsedExpression parsedExpression, SqlSubstituteAttribute attribute)
            : base(parendExpression)
        {
            _linqExpression = linqExpression;
            _arguments = linqExpression.Arguments.Select(x => ParsedExpression.ParseExpression(x, this, parsedExpression)).ToArray();
            _attribute = attribute;
        }

        public override string SqlExpression
        {
            get
            {
                try
                {
                    return string.Format(_attribute.SqlSubstitution, _arguments.Select(x => x.SqlExpression).ToArray());
                }
                catch (ArgumentNullException ex)
                { 
                    throw new NotParsableException ("SqlSubstitute attribute value null is not valid.", _linqExpression, ex);
                }
                catch (FormatException ex)
                {
                    throw new NotParsableException("SqlSubstitute attribute value has the wrong format.", _linqExpression, ex);
                }
            }
        }
    }

    public class ContainsExpression : CallExpression
    {
        Expression _arrayExpression;
        Expression _expression;
        bool _isNegated;

        internal ContainsExpression(System.Linq.Expressions.MethodCallExpression linqExpression, Expression parentExpression, ParsedExpression parsedExpression) : base(parentExpression)
        {
            if (ExpressionHelper.ContainsAny(linqExpression.Arguments[0], x => x is System.Linq.Expressions.ParameterExpression))
            {
                throw new NotImplementedException();
            }
            else
            {
                _arrayExpression = ConstantExpression.Create(parsedExpression, parentExpression, System.Linq.Expressions.LambdaExpression.Lambda(linqExpression.Arguments[0]).Compile().DynamicInvoke());
            }

            _expression = Expression.ParseExpression(linqExpression.Arguments[1], parentExpression, parsedExpression);
            _isNegated = false;
        }

        public void Negate()
        {
            _isNegated = !_isNegated;
        }

        public override string SqlExpression
        {
            get
            {
                return string.Format(_isNegated ? "{0} NOT IN {1}" : "{0} IN {1}", _expression.SqlExpression, _arrayExpression.SqlExpression);
            }
        }
    }
}
