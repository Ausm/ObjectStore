using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using System.Data.SqlClient;

namespace ObjectStore.OrMapping.Expressions
{
    public abstract class Expression
    {
        #region Membervariablen
        Expression _parent;
        #endregion

        #region Konstruktoren
        protected Expression(Expression parent)
        {
            _parent = parent;
        }
        #endregion

        #region Properties
        public Expression Parent { get { return _parent; } }

        public abstract string SqlExpression { get; }
        #endregion

        #region Statische Create-Funktion
        public static ParsedExpression ParseExpression(LambdaExpression expression, Func<object, SqlParameter> getParamFunction)
        {
            return new ParsedExpression(expression, getParamFunction);
        }

        internal static Expression ParseExpression(System.Linq.Expressions.Expression expression, Expression parentExpression, ParsedExpression parsedExpression)
        {
            if (!ExpressionHelper.ContainsAny(expression, x => x is System.Linq.Expressions.ParameterExpression))
            {
               return ConstantExpression.Create(parsedExpression, parentExpression, LambdaExpression.Lambda(expression).Compile().DynamicInvoke());
            }

            switch (expression.NodeType)
            {
                case ExpressionType.Add:
                case ExpressionType.AddChecked:
                    return new AddExpression((System.Linq.Expressions.BinaryExpression)expression, parentExpression, parsedExpression);
                case ExpressionType.And:
                case ExpressionType.AndAlso:
                    return new AndExpression((System.Linq.Expressions.BinaryExpression)expression, parentExpression, parsedExpression);
                case ExpressionType.ArrayIndex:
                    break;
                case ExpressionType.ArrayLength:
                    break;
                case ExpressionType.Call:
                    return CallExpression.Create((System.Linq.Expressions.MethodCallExpression)expression, parentExpression, parsedExpression);
                case ExpressionType.Coalesce:
                    break;
                case ExpressionType.Conditional:
                    break;
                case ExpressionType.Constant:
                    return ConstantExpression.Create(parsedExpression, parentExpression, ((System.Linq.Expressions.ConstantExpression)expression).Value);
                case ExpressionType.Convert:
                case ExpressionType.ConvertChecked:
                    return ParseExpression(((System.Linq.Expressions.UnaryExpression)expression).Operand, parentExpression, parsedExpression);
                case ExpressionType.Divide:
                    break;
                case ExpressionType.Equal:
                    return new EqualExpression((System.Linq.Expressions.BinaryExpression)expression, parentExpression, parsedExpression);
                case ExpressionType.ExclusiveOr:
                    break;
                case ExpressionType.GreaterThan:
                    return new GraterThanExpression((System.Linq.Expressions.BinaryExpression)expression, parentExpression, parsedExpression);
                case ExpressionType.GreaterThanOrEqual:
                    return new GraterThanOrEqualExpression((System.Linq.Expressions.BinaryExpression)expression, parentExpression, parsedExpression);
                case ExpressionType.Invoke:
                    break;
                case ExpressionType.Lambda:
                    return new ParsedExpression((LambdaExpression)expression, parentExpression, parsedExpression);
                case ExpressionType.LeftShift:
                    break;
                case ExpressionType.LessThan:
                    return new LessThanExpression((System.Linq.Expressions.BinaryExpression)expression, parentExpression, parsedExpression);
                case ExpressionType.LessThanOrEqual:
                    return new LessThanOrEqualExpression((System.Linq.Expressions.BinaryExpression)expression, parentExpression, parsedExpression);
                case ExpressionType.ListInit:
                    break;
                case ExpressionType.MemberAccess:
                    return MemberExpression.Create((System.Linq.Expressions.MemberExpression)expression, parentExpression, parsedExpression);
                case ExpressionType.MemberInit:
                    break;
                case ExpressionType.Modulo:
                    break;
                case ExpressionType.Multiply:
                    break;
                case ExpressionType.MultiplyChecked:
                    break;
                case ExpressionType.Negate:
                    break;
                case ExpressionType.NegateChecked:
                    break;
                case ExpressionType.New:
                    break;
                case ExpressionType.NewArrayBounds:
                    break;
                case ExpressionType.NewArrayInit:
                    break;
                case ExpressionType.Not:
                    return new NotExpression((System.Linq.Expressions.UnaryExpression)expression, parentExpression, parsedExpression);
                case ExpressionType.NotEqual:
                    return new NotEqualExpression((System.Linq.Expressions.BinaryExpression)expression, parentExpression, parsedExpression);
                case ExpressionType.Or:
                case ExpressionType.OrElse:
                    return new OrExpression((System.Linq.Expressions.BinaryExpression)expression, parentExpression, parsedExpression);
                case ExpressionType.Parameter:
                    return new ParameterExpression((System.Linq.Expressions.ParameterExpression)expression, parentExpression, parsedExpression);
                case ExpressionType.Power:
                    break;
                case ExpressionType.Quote:
                    break;
                case ExpressionType.RightShift:
                    break;
                case ExpressionType.Subtract:
                case ExpressionType.SubtractChecked:
                    return new SubtractExpression((System.Linq.Expressions.BinaryExpression)expression, parentExpression, parsedExpression);
                case ExpressionType.TypeAs:
                    break;
                case ExpressionType.TypeIs:
                    break;
                case ExpressionType.UnaryPlus:
                    break;
                default:
                    break;
            }

            throw new NotParsableException("Expressiontype is not parseable.", expression);
        }
        #endregion
    }

    public class NotParsableException : Exception
    {
        System.Linq.Expressions.Expression _expression;

        public NotParsableException(string message) : this(message, null) { }

        public NotParsableException(string message, System.Linq.Expressions.Expression expression) : this(message, expression, null) { }
        
        public NotParsableException(string message, System.Linq.Expressions.Expression expression, Exception innerException) : base(message, innerException) { _expression = expression; }

        public System.Linq.Expressions.Expression Expression
        {
            get
            {
                return _expression;
            }
        }
    }
}
