using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
#if DNXCORE50
using System.Reflection;
#endif

namespace ObjectStore.Expressions
{
    public class ExpressionParser
    {
        class Execution
        {
            ExpressionParser _parent;
            Func<object, string> _getParamFunc;
            Stack<Expression> _expressionStack;
            IServiceProvider _services;


            public Execution(ExpressionParser parent, Func<object, string> getParamFunc, IServiceProvider services)
            {
                _parent = parent;
                _getParamFunc = getParamFunc;
                _expressionStack = new Stack<Expression>();
                _services = services;
            }

            public string ParseExpression(Expression expression)
            {
                ExpressionParserRule rule = _parent._rules.FirstOrDefault(x => x.IsApplicable(expression));
                if (rule != null)
                {
                    ExpressionParseEventArgs eventArgs = new ExpressionParseEventArgs(_getParamFunc, ParseExpression, _expressionStack.Peek(), _services);
                    _expressionStack.Push(expression);
                    string returnValue = rule.Parse(expression, eventArgs);
                    _expressionStack.Pop();
                    return returnValue;
                }
                else
                    throw new NotParsableException("There is no expression rule for this expression.", expression);
            }
        }

        class ServiceProvider : IServiceProvider
        {
            List<object> _services;

            public ServiceProvider(IEnumerable<object> services)
            {
                _services = services.ToList();
            }

            public object GetService(Type serviceType)
            {
                return _services.Where(x => serviceType.IsAssignableFrom(x.GetType())).FirstOrDefault();
            }
        }

        List<ExpressionParserRule> _rules;

        public ExpressionParser()
        {
            _rules = new List<ExpressionParserRule>();
        }

        public void AddRule(ExpressionParserRule rule)
        {
            _rules.Add(rule);
        }

        public string ParseExpression(LambdaExpression expression, Func<object, string> getParamFunction, IServiceProvider services)
        {
            return new Execution(this, getParamFunction, services).ParseExpression(expression);
        }

        public string ParseExpression(LambdaExpression expression, Func<object, string> getParamFunction, params object[] services)
        {
            return ParseExpression(expression, getParamFunction, new ServiceProvider(services));
        }
    }
    public class NotParsableException : Exception
    {
        Expression _expression;

        public NotParsableException(string message) : this(message, null) { }

        public NotParsableException(string message, Expression expression) : this(message, expression, null) { }

        public NotParsableException(string message, Expression expression, Exception innerException) : base(message, innerException) { _expression = expression; }

        public Expression Expression
        {
            get
            {
                return _expression;
            }
        }
    }
}
