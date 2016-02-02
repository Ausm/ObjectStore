using System;
using System.Linq.Expressions;

namespace ObjectStore.Expressions
{
    public static class ExpressionParserExtensions
    {
        public static ExpressionParser AddRule<T>(this ExpressionParser expressionParser, Func<T, ExpressionParseEventArgs, string> parseFunction,
             Func<T, bool> isApplicable, params ExpressionType[] expressionTypes) where T : Expression
        {
            if (expressionTypes.Length == 0)
                expressionParser.AddRule(new ExpressionTypeParseRule<T>(default(ExpressionType?), isApplicable, parseFunction));
            else foreach(ExpressionType expressionType in expressionTypes)
                    expressionParser.AddRule(new ExpressionTypeParseRule<T>(expressionType, isApplicable, parseFunction));
            return expressionParser;
        }

        public static ExpressionParser AddRule<T>(this ExpressionParser expressionParser, Func<T, ExpressionParseEventArgs, string> parseFunction, params ExpressionType[] expressionTypes) where T : Expression
        {
            return expressionParser.AddRule(parseFunction, null, expressionTypes);
        }
    }
}
