using System.Linq.Expressions;

namespace ObjectStore.Sqlite
{
    internal interface IParsingContext
    {
        string GetAlias(ParameterExpression expression);

        string GetJoin(MemberExpression expression);

        string GetParameter(object value);
    }
}
