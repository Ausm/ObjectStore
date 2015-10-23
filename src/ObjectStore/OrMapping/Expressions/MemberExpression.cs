using System;
using System.Linq;
#if DNXCORE50
using System.Reflection;
#endif

namespace ObjectStore.OrMapping.Expressions
{
    public abstract class MemberExpression : Expression
    {
        internal MemberExpression(Expression parentExpression) : base(parentExpression) { }

        internal static MemberExpression Create(System.Linq.Expressions.MemberExpression linqExpression, Expression parentExpression, ParsedExpression parsedExpression)
        {
#if DNXCORE50
            if (linqExpression.Member is PropertyInfo)
#else
            if (linqExpression.Member.MemberType == System.Reflection.MemberTypes.Property)
#endif
            {
                if (linqExpression.Expression.NodeType == System.Linq.Expressions.ExpressionType.Parameter)
                {
                    if (parentExpression is JoinedObjectExpression || parentExpression is AccessJoinedObjectExpression)
                        return new JoinedObjectExpression(linqExpression, parentExpression, parsedExpression);


                    Mapping mapping = Mapping.GetMapping((System.Reflection.PropertyInfo)linqExpression.Member);
                    if (mapping == null) throw new NotParsableException("Parametermember is not mapped.", linqExpression);
                    return new MappedPropertyExpression(mapping, (System.Linq.Expressions.ParameterExpression)linqExpression.Expression, parentExpression, parsedExpression);
                }
                else if (linqExpression.Expression.NodeType == System.Linq.Expressions.ExpressionType.MemberAccess &&
                    (linqExpression.Expression as System.Linq.Expressions.MemberExpression).Member.GetCustomAttributes(typeof(ForeignObjectMappingAttribute), true).Any())
                {
                    if (parentExpression is AccessJoinedObjectExpression)
                        return new JoinedObjectExpression(linqExpression, parentExpression, parsedExpression);
                    else
                        return new AccessJoinedObjectExpression(linqExpression, parentExpression, parsedExpression);
                }
            }
            return null;
        }

        public abstract string Alias { get; }
    }

    public class MappedPropertyExpression : MemberExpression
    {
        Mapping _mapping;
        ParsedExpression _parsedExpression;
        System.Linq.Expressions.ParameterExpression _paramExpression;

        internal MappedPropertyExpression(Mapping mapping, System.Linq.Expressions.ParameterExpression paramExpression, Expression parentExpression, ParsedExpression parsedExpression)
            : base(parentExpression)
        {
            _mapping = mapping;
            _paramExpression = paramExpression;
            _parsedExpression = parsedExpression;
        }

        public override string SqlExpression
        {
            get { return _mapping.ParseExpression(this); }
        }

        public override string Alias
        {
            get
            {
                return _parsedExpression[_paramExpression];
            }
        }
    }

    public class JoinedObjectExpression : MemberExpression
    {
        Mapping _mapping;
        ParsedExpression _parsedExpression;
        ParsedExpression.Join _join;
        Expression _innerexpression;
        string _alias;

        internal JoinedObjectExpression(System.Linq.Expressions.MemberExpression expression, Expression parentExpression, ParsedExpression parsedExpression)
            : base(parentExpression)
        {
            _mapping = Mapping.GetMapping(expression.Member as System.Reflection.PropertyInfo);
            _parsedExpression = parsedExpression;
            _innerexpression = ParsedExpression.ParseExpression(expression.Expression, this, parsedExpression);

            if (_innerexpression is ParameterExpression)
                _alias = (_innerexpression as ParameterExpression).Alias;
            else if (_innerexpression is JoinedObjectExpression)
                _alias = (_innerexpression as JoinedObjectExpression).SqlExpression;
            else
                throw new NotSupportedException("Unsupported inner expression in a JoinedObjectExpression");

            System.Linq.Expressions.ParameterExpression parameterExpression = expression.Expression is System.Linq.Expressions.ParameterExpression ?
                (System.Linq.Expressions.ParameterExpression)expression.Expression : System.Linq.Expressions.Expression.Parameter(expression.Type, string.Format("param{0}", ObjectStoreManager.CurrentUniqe()));

            _join = parsedExpression.AquireJoin(expression.Member as System.Reflection.PropertyInfo, parameterExpression,
                _innerexpression is JoinedObjectExpression ? ((JoinedObjectExpression)_innerexpression)._join : null);
        }

        public override string SqlExpression
        {
            get { return _join.Alias; }
        }

        public override string Alias
        {
            get { return _alias; }
        }
    }

    public class AccessJoinedObjectExpression : MemberExpression
    {
        Mapping _mapping;
        Expression _innerExpression;
        ParsedExpression _parsedExpression;

        internal AccessJoinedObjectExpression(System.Linq.Expressions.MemberExpression expression, Expression parentExpression, ParsedExpression parsedExpression)
            : base(parentExpression)
        {
            _mapping = Mapping.GetMapping(expression.Member as System.Reflection.PropertyInfo);
            _innerExpression = ParsedExpression.ParseExpression(expression.Expression, this, parsedExpression);
            _parsedExpression = parsedExpression;
        }

        public override string SqlExpression
        {
            get { return _mapping.ParseExpression(this); }
        }

        public override string Alias
        {
            get
            {
                return _innerExpression.SqlExpression;
            }
        }

    }


}
