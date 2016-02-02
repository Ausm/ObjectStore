using System;
using System.Linq.Expressions;

namespace ObjectStore.Expressions
{
    public class ExpressionTypeParseRule<Texpression> : ExpressionParserRule where Texpression : Expression
    {
        Func<Texpression, ExpressionParseEventArgs, string> _parseFunction;
        Func<Texpression, bool> _isApplicable;
        ExpressionType? _expressionType;

        public ExpressionTypeParseRule(ExpressionType? expressionType, Func<Texpression, bool> isApplicable, Func<Texpression, ExpressionParseEventArgs, string> parseFunction)
        {
            _expressionType = expressionType;
            _isApplicable = isApplicable;
            _parseFunction = parseFunction;
        }

        public override bool IsApplicable(Expression expression)
        {
            return expression is Texpression &&
                (!_expressionType.HasValue || _expressionType.Value == expression.NodeType) &&
                (_isApplicable == null || _isApplicable((Texpression)expression));
        }

        public override string Parse(Expression expression, ExpressionParseEventArgs args)
        {
            return _parseFunction((Texpression)expression, args);
        }
    }
}
