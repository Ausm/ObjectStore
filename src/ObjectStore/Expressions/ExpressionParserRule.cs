using System;
using System.Linq.Expressions;

namespace ObjectStore.Expressions
{
    public abstract class ExpressionParserRule
    {
        public abstract bool IsApplicable(Expression expression);

        public abstract string Parse(Expression expression, ExpressionParseEventArgs args);
    }

    public class ExpressionParseEventArgs : EventArgs
    {
        Func<Expression, string> _parseChild;
        Expression _parentExpression;
        IServiceProvider _services;

        public ExpressionParseEventArgs(Func<Expression, string> parseChild, Expression parentExpression, IServiceProvider services)
        {
            _parseChild = parseChild;
            _parentExpression = parentExpression;
            _services = services;
        }

        public string ParseChild(Expression expression)
        {
            return _parseChild(expression);
        }

        public T GetService<T>() where T : class
        {
            return _services.GetService(typeof(T)) as T;
        }

        public Expression ParentExpression
        {
            get
            {
                return _parentExpression;
            }
        }
    }
}
