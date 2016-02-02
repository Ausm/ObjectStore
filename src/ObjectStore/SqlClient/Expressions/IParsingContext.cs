using System.Linq.Expressions;

namespace ObjectStore.SqlClient
{
    public interface IParsingContext
    {
        string GetAlias(ParameterExpression expression);

        string GetJoin(MemberExpression expression);

        string GetParameter(object value);
    }
}
