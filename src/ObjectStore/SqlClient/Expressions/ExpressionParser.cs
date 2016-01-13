using ObjectStore.Expressions;
using ObjectStore.OrMapping;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace ObjectStore.SqlClient
{
    public partial class DataBaseProvider 
    {
        class ParserContext
        {
            Dictionary<ParameterExpression, string> _aliases;

            public ParserContext()
            {
                _aliases = new Dictionary<ParameterExpression, string>();
            }

            public string GetAlias(ParameterExpression expression)
            {
                if (_aliases.ContainsKey(expression))
                    return _aliases[expression];

                string returnValue = $"T{Instance.GetUniqe()}";
                _aliases.Add(expression, returnValue);
                return returnValue;
            }
        }

        partial void InitExpressionParser()
        {
            _expressionParser = new ExpressionParser()
                        .AddRule<BinaryExpression>((exp, args) => $"{args.ParseChild(exp.Left)} + {args.ParseChild(exp.Right)}", ExpressionType.Add, ExpressionType.AddChecked)
                        .AddRule<BinaryExpression>((exp, args) => $"({args.ParseChild(exp.Left)}) AND ({args.ParseChild(exp.Right)})", ExpressionType.And, ExpressionType.AndAlso)
                        .AddRule<ConstantExpression>((exp, args) => args.GetParameter(((OrMapping.IFillAbleObject)exp.Value).Keys.Single()),
                            e => e.Value is OrMapping.IFillAbleObject, ExpressionType.Constant)
                        .AddRule<ConstantExpression>((exp, args) =>
                        {
                            string[] values = ((IEnumerable)exp.Value).OfType<object>().Select(x => args.ParseChild(Expression.Constant(x))).ToArray();
                            return values.Length == 0 ? "(SELECT NULL)" : $"({string.Join(", ", values)})";
                        }, e => e.Value is IEnumerable, ExpressionType.Constant)
                        .AddRule<ConstantExpression>((exp, args) => args.GetParameter(exp.Value))
                        .AddRule<BinaryExpression>((exp, args) => $"{args.ParseChild(exp.Left)} IS NULL", x => x.Right is ConstantExpression && ((ConstantExpression)x.Right).Value == null, ExpressionType.Equal)
                        .AddRule<BinaryExpression>((exp, args) => $"{args.ParseChild(exp.Right)} IS NULL", x => x.Left is ConstantExpression && ((ConstantExpression)x.Left).Value == null, ExpressionType.Equal)
                        .AddRule<BinaryExpression>((exp, args) => $"{args.ParseChild(exp.Left)} = {args.ParseChild(exp.Right)}", ExpressionType.Equal)
                        .AddRule<BinaryExpression>((exp, args) => $"{args.ParseChild(exp.Left)} != {args.ParseChild(exp.Right)}", ExpressionType.NotEqual)
                        .AddRule<BinaryExpression>((exp, args) => $"{args.ParseChild(exp.Left)} > {args.ParseChild(exp.Right)}", ExpressionType.GreaterThan)
                        .AddRule<BinaryExpression>((exp, args) => $"{args.ParseChild(exp.Left)} >= {args.ParseChild(exp.Right)}", ExpressionType.GreaterThanOrEqual)
                        .AddRule<BinaryExpression>((exp, args) => $"{args.ParseChild(exp.Left)} < {args.ParseChild(exp.Right)}", ExpressionType.LessThan)
                        .AddRule<BinaryExpression>((exp, args) => $"{args.ParseChild(exp.Left)} <= {args.ParseChild(exp.Right)}", ExpressionType.LessThanOrEqual)
                        .AddRule<BinaryExpression>((exp, args) => $"{args.ParseChild(exp.Left)} - {args.ParseChild(exp.Right)}", ExpressionType.Subtract, ExpressionType.SubtractChecked)
                        .AddRule<MemberExpression>((exp, args) => $"{Mapping.GetMapping((PropertyInfo)exp.Member).FieldName}.{args.ParseChild(exp.Expression)}",
#if DNXCORE50
                            e => e.Member is PropertyInfo
#else
                            e => e.Member.MemberType == MemberTypes.Property
#endif                            
                            && e.Expression.NodeType == ExpressionType.Parameter,
                            ExpressionType.MemberAccess)
                        .AddRule<ParameterExpression>((exp, args) => args.GetService<ParserContext>().GetAlias(exp))
                        .AddRule<LambdaExpression>((exp, args) => args.ParseChild(exp.Body))
                        .AddRule<UnaryExpression>((exp, args) => args.ParseChild(exp.Operand), ExpressionType.Convert, ExpressionType.ConvertChecked);
        }
    }
}
