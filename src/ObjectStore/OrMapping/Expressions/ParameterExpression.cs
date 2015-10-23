using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ObjectStore.OrMapping.Expressions
{
    public class ParameterExpression : Expression
    {
        System.Linq.Expressions.ParameterExpression _parameterExpression;
        ParsedExpression _parsedExpressions;

        internal ParameterExpression(System.Linq.Expressions.ParameterExpression parameterExpression, Expression parentExpression, ParsedExpression parsedExpression) : base(parentExpression)
        {
            _parameterExpression = parameterExpression;
            _parsedExpressions = parsedExpression;
        }

        public override string SqlExpression
        {
            get 
            {
                MappingInfo mappingInfo = MappingInfo.GetMappingInfo(_parameterExpression.Type);
                if(mappingInfo == null) throw new NotParsableException("Type of parameter is not a mapped type.", _parameterExpression);

                return mappingInfo.ParseExpression(this);
            }
        }

        public string Alias
        {
            get
            {
                return _parsedExpressions[_parameterExpression];
            }
        }
    }

}
