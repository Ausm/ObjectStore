using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;

namespace ObjectStore
{
    public static class ExpressionHelper
    {
        public static List<Expression> GetFilteredExpression(Expression expression, Predicate<Expression> predicate)
        {
            if (expression == null)
                return new List<Expression>();

            if (predicate(expression)) return new List<Expression>() { expression };

            if (expression is BinaryExpression)
            {
                BinaryExpression binaryExpression = expression as BinaryExpression;
                List<Expression> returnValue = new List<Expression>();
                returnValue.AddRange(GetFilteredExpression(binaryExpression.Conversion, predicate));
                returnValue.AddRange(GetFilteredExpression(binaryExpression.Left, predicate));
                returnValue.AddRange(GetFilteredExpression(binaryExpression.Right, predicate));
                return returnValue;
            }
            else if (expression is ConditionalExpression)
            {
                ConditionalExpression conditionalExpression = expression as ConditionalExpression;
                List<Expression> returnValue = new List<Expression>();
                returnValue.AddRange(GetFilteredExpression(conditionalExpression.Test, predicate));
                returnValue.AddRange(GetFilteredExpression(conditionalExpression.IfTrue, predicate));
                returnValue.AddRange(GetFilteredExpression(conditionalExpression.IfFalse, predicate));
                return returnValue;
            }
            else if (expression is ConstantExpression)
            {
                return new List<Expression>();
            }
            else if (expression is InvocationExpression)
            {
                InvocationExpression invocationExpression = expression as InvocationExpression;
                List<Expression> returnvalue = GetFilteredExpression((expression as InvocationExpression).Expression, predicate);
                foreach (Expression exp in invocationExpression.Arguments)
                {
                    returnvalue.AddRange(GetFilteredExpression(exp, predicate));
                }
                return returnvalue;
            }
            else if (expression is LambdaExpression)
            {
                LambdaExpression lambdaExpression = expression as LambdaExpression;
                return GetFilteredExpression(lambdaExpression.Body, predicate);
            }
            else if (expression is ListInitExpression)
            {
                ListInitExpression listInitExpression = expression as ListInitExpression;
                List<Expression> returnvalue = GetFilteredExpression(listInitExpression.NewExpression, predicate);
                foreach (ElementInit exp in listInitExpression.Initializers)
                {
                    foreach (Expression arg in exp.Arguments)
                    {
                        returnvalue.AddRange(GetFilteredExpression(arg, predicate));
                    }
                }
                return returnvalue;
            }
            else if (expression is MemberExpression)
            {
                return GetFilteredExpression((expression as MemberExpression).Expression, predicate);
            }
            else if (expression is MemberInitExpression)
            {
                return GetFilteredExpression((expression as MemberInitExpression).NewExpression, predicate);
            }
            else if (expression is MethodCallExpression)
            {
                MethodCallExpression methodCallExpression = expression as MethodCallExpression;
                List<Expression> returnvalue = GetFilteredExpression(methodCallExpression.Object, predicate);
                foreach (Expression exp in methodCallExpression.Arguments)
                {
                    returnvalue.AddRange(GetFilteredExpression(exp, predicate));
                }
                return returnvalue;
            }
            else if (expression is NewArrayExpression)
            {
                NewArrayExpression newArrayExpression = expression as NewArrayExpression;
                List<Expression> returnValue = null;
                foreach (Expression exp in newArrayExpression.Expressions)
                {
                    if (returnValue == null)
                        returnValue = GetFilteredExpression(exp, predicate);
                    else
                        returnValue.AddRange(GetFilteredExpression(exp, predicate));
                }
                return returnValue == null ? new List<Expression>() : returnValue;
            }
            else if (expression is NewExpression)
            {
                NewExpression newExpression = expression as NewExpression;
                List<Expression> returnValue = null;
                foreach (Expression exp in newExpression.Arguments)
                {
                    if (returnValue == null)
                        returnValue = GetFilteredExpression(exp, predicate);
                    else
                        returnValue.AddRange(GetFilteredExpression(exp, predicate));
                }
                return returnValue == null ? new List<Expression>() : returnValue;
            }
            else if (expression is ParameterExpression)
            {
                return new List<Expression>();
            }
            else if (expression is TypeBinaryExpression)
            {
                TypeBinaryExpression typeBinaryExpression = expression as TypeBinaryExpression;
                return GetFilteredExpression(typeBinaryExpression.Expression, predicate);
            }
            else if (expression is UnaryExpression)
            {
                UnaryExpression unaryExpression = expression as UnaryExpression;
                return GetFilteredExpression(unaryExpression.Operand, predicate);
            }

            throw new InvalidOperationException("ExpressionType is unknown.");

        }

        public static bool ContainsAny(Expression expression, Predicate<Expression> predicate)
        {
            if (expression == null)
                return false;

            if (predicate(expression)) return true;

            if (expression is BinaryExpression)
            {
                BinaryExpression binaryExpression = expression as BinaryExpression;
                return ContainsAny(binaryExpression.Conversion, predicate) ||
                        ContainsAny(binaryExpression.Left, predicate) ||
                        ContainsAny(binaryExpression.Right, predicate);
            }
            else if (expression is ConditionalExpression)
            {
                ConditionalExpression conditionalExpression = expression as ConditionalExpression;
                return ContainsAny(conditionalExpression.Test, predicate) ||
                        ContainsAny(conditionalExpression.IfTrue, predicate) ||
                        ContainsAny(conditionalExpression.IfFalse, predicate);
            }
            else if (expression is ConstantExpression)
            {
                return false;
            }
            else if (expression is InvocationExpression)
            {
                InvocationExpression invocationExpression = expression as InvocationExpression;
                if (ContainsAny(invocationExpression.Expression, predicate)) return true;
                foreach (Expression exp in invocationExpression.Arguments)
                {
                    if (ContainsAny(exp, predicate)) return true;
                }
                return false;
            }
            else if (expression is LambdaExpression)
            {
                LambdaExpression lambdaExpression = expression as LambdaExpression;
                return ContainsAny(lambdaExpression.Body, predicate);
            }
            else if (expression is ListInitExpression)
            {
                ListInitExpression listInitExpression = expression as ListInitExpression;
                if (ContainsAny(listInitExpression.NewExpression, predicate)) return true;
                foreach (ElementInit exp in listInitExpression.Initializers)
                {
                    foreach (Expression arg in exp.Arguments)
                    {
                        if (ContainsAny(arg, predicate)) return true;
                    }
                }
                return false;
            }
            else if (expression is MemberExpression)
            {
                return ContainsAny((expression as MemberExpression).Expression, predicate);
            }
            else if (expression is MemberInitExpression)
            {
                return ContainsAny((expression as MemberInitExpression).NewExpression, predicate);
            }
            else if (expression is MethodCallExpression)
            {
                MethodCallExpression methodCallExpression = expression as MethodCallExpression;
                if (ContainsAny(methodCallExpression.Object, predicate)) return true;
                foreach (Expression exp in methodCallExpression.Arguments)
                {
                    if (ContainsAny(exp, predicate)) return true;
                }
                return false;
            }
            else if (expression is NewArrayExpression)
            {
                NewArrayExpression newArrayExpression = expression as NewArrayExpression;
                foreach (Expression exp in newArrayExpression.Expressions)
                {
                    if (ContainsAny(exp, predicate)) return true;
                }
                return false;
            }
            else if (expression is NewExpression)
            {
                NewExpression newExpression = expression as NewExpression;
                foreach (Expression exp in newExpression.Arguments)
                {
                    if (ContainsAny(exp, predicate)) return true;
                }
                return false;
            }
            else if (expression is ParameterExpression)
            {
                return false;
            }
            else if (expression is TypeBinaryExpression)
            {
                TypeBinaryExpression typeBinaryExpression = expression as TypeBinaryExpression;
                return ContainsAny(typeBinaryExpression.Expression, predicate);
            }
            else if (expression is UnaryExpression)
            {
                UnaryExpression unaryExpression = expression as UnaryExpression;
                return ContainsAny(unaryExpression.Operand, predicate);
            }

            return false;
        }

        public static Expression ReplaceExpressionParts(Expression expression, Func<Expression, Expression> replacementFunction)
        {
            if (expression == null)
                return null;

            Expression replaced = replacementFunction(expression);
            if (replaced != expression) return replaced;

            if (expression is BinaryExpression)
            {
                BinaryExpression binaryExpression = expression as BinaryExpression;
                Expression left = ReplaceExpressionParts(binaryExpression.Left, replacementFunction);
                Expression right = ReplaceExpressionParts(binaryExpression.Right, replacementFunction);
                Expression conversion = ReplaceExpressionParts(binaryExpression.Conversion, replacementFunction);

                if (left == binaryExpression.Left && right == binaryExpression.Right && conversion == binaryExpression.Conversion)
                    return expression;

                if (binaryExpression.NodeType == ExpressionType.Coalesce && binaryExpression.Conversion != null)
                {
                    return Expression.Coalesce(left, right, conversion as LambdaExpression);
                }
                else
                {
                    return Expression.MakeBinary(binaryExpression.NodeType, left, right, binaryExpression.IsLiftedToNull, binaryExpression.Method);
                }
            }
            else if (expression is ConditionalExpression)
            {
                ConditionalExpression conditionalExpression = expression as ConditionalExpression;
                Expression test = ReplaceExpressionParts(conditionalExpression.Test, replacementFunction);
                Expression ifTrue = ReplaceExpressionParts(conditionalExpression.IfTrue, replacementFunction);
                Expression ifFalse = ReplaceExpressionParts(conditionalExpression.IfFalse, replacementFunction);

                if (test == conditionalExpression.Test &&
                    ifTrue == conditionalExpression.IfTrue &&
                    ifFalse == conditionalExpression.IfFalse)
                    return conditionalExpression;

                return Expression.Condition(test, ifTrue, ifFalse);
            }
            else if (expression is ConstantExpression)
            {
                return expression;
            }
            else if (expression is InvocationExpression)
            {
                InvocationExpression invocationExpression = expression as InvocationExpression;
                Expression innerExpression = ReplaceExpressionParts(invocationExpression.Expression, replacementFunction);
                Expression[] arguments = new Expression[invocationExpression.Arguments.Count];
                bool isReplaced = innerExpression == invocationExpression;
                for (int i = 0; i < arguments.Length; i++)
                {
                    arguments[i] = ReplaceExpressionParts(invocationExpression.Arguments[i], replacementFunction);
                    if (arguments[i] != invocationExpression.Arguments[i])
                        isReplaced = true;
                }

                if (!isReplaced)
                    return invocationExpression;

                return Expression.Invoke(innerExpression, arguments);
            }
            else if (expression is LambdaExpression)
            {
                LambdaExpression lambdaExpression = expression as LambdaExpression;
                Expression body = ReplaceExpressionParts(lambdaExpression.Body, replacementFunction);
                if (body == lambdaExpression.Body)
                    return lambdaExpression;

                return Expression.Lambda(lambdaExpression.Type, body, lambdaExpression.Parameters);
            }
            else if (expression is ListInitExpression)
            {
                ListInitExpression listInitExpression = expression as ListInitExpression;
                Expression newExpression = ReplaceExpressionParts(listInitExpression.NewExpression, replacementFunction);
                bool isReplaced = newExpression == listInitExpression.NewExpression;
                ElementInit[] initializers = new ElementInit[listInitExpression.Initializers.Count];
                for (int i = 0; i < initializers.Length; i++)
                {
                    Expression[] expressionList = new Expression[listInitExpression.Initializers[i].Arguments.Count];
                    bool areArgutmentsReplaced = false;
                    for (int j = 0; j < expressionList.Length; j++)
                    {
                        expressionList[j] = ReplaceExpressionParts(listInitExpression.Initializers[i].Arguments[j], replacementFunction);
                        if (expressionList[j] != listInitExpression.Initializers[i].Arguments[j])
                            areArgutmentsReplaced = true;
                    }
                    if (areArgutmentsReplaced)
                    {
                        initializers[i] = Expression.ElementInit(listInitExpression.Initializers[i].AddMethod, expressionList);
                        isReplaced = true;
                    }
                    else
                    {
                        initializers[i] = listInitExpression.Initializers[i];
                    }
                }

                if (!isReplaced)
                    return listInitExpression;

                return Expression.ListInit(newExpression as NewExpression, initializers);
            }
            else if (expression is MemberExpression)
            {
                MemberExpression memberExpression = expression as MemberExpression;
                Expression innerExpression = ReplaceExpressionParts(memberExpression.Expression, replacementFunction);

                if (innerExpression == memberExpression.Expression)
                    return memberExpression;

                return Expression.MakeMemberAccess(innerExpression, memberExpression.Member);
            }
            else if (expression is MemberInitExpression)
            {
                MemberInitExpression memberInitExpression = expression as MemberInitExpression;
                Expression newExpression = ReplaceExpressionParts(memberInitExpression.NewExpression, replacementFunction);

                MemberBinding[] memberBindings = new MemberBinding[memberInitExpression.Bindings.Count];

                if (newExpression == memberInitExpression.NewExpression)
                    return memberInitExpression;

                return Expression.MemberInit(newExpression as NewExpression, memberInitExpression.Bindings);
            }
            else if (expression is MethodCallExpression)
            {
                MethodCallExpression methodCallExpression = expression as MethodCallExpression;
                Expression objectExpression = ReplaceExpressionParts(methodCallExpression.Object, replacementFunction);
                bool isReplaced = objectExpression == methodCallExpression.Object;
                Expression[] arguments = new Expression[methodCallExpression.Arguments.Count];
                for (int i = 0; i < arguments.Length; i++)
                {
                    arguments[i] = ReplaceExpressionParts(methodCallExpression.Arguments[i], replacementFunction);
                    if (arguments[i] != methodCallExpression.Arguments[i])
                        isReplaced = true;
                }

                if (!isReplaced)
                    return methodCallExpression;

                return Expression.Call(objectExpression, methodCallExpression.Method, arguments);
            }
            else if (expression is NewArrayExpression)
            {
                NewArrayExpression newArrayExpression = expression as NewArrayExpression;
                bool isReplaced = false;
                Expression[] expressions = new Expression[newArrayExpression.Expressions.Count];
                for (int i = 0; i < expressions.Length; i++)
                {
                    expressions[i] = ReplaceExpressionParts(newArrayExpression.Expressions[i], replacementFunction);
                    if (expressions[i] != newArrayExpression.Expressions[i])
                        isReplaced = true;
                }

                if (!isReplaced)
                    return newArrayExpression;

                return newArrayExpression.NodeType == ExpressionType.NewArrayInit ?
                    Expression.NewArrayInit(newArrayExpression.Type, expressions) :
                    Expression.NewArrayBounds(newArrayExpression.Type, expressions);
            }
            else if (expression is NewExpression)
            {
                NewExpression newExpression = expression as NewExpression;
                bool isReplaced = false;
                Expression[] arguments = new Expression[newExpression.Arguments.Count];
                for (int i = 0; i < arguments.Length; i++)
                {
                    arguments[i] = ReplaceExpressionParts(newExpression.Arguments[i], replacementFunction);
                    if (arguments[i] != newExpression.Arguments[i])
                        isReplaced = true;
                }

                if (!isReplaced)
                    return newExpression;

                return Expression.New(newExpression.Constructor, arguments, newExpression.Members);
            }
            else if (expression is ParameterExpression)
            {
                return expression;
            }
            else if (expression is TypeBinaryExpression)
            {
                TypeBinaryExpression typeBinaryExpression = expression as TypeBinaryExpression;
                Expression innerExpression = ReplaceExpressionParts(typeBinaryExpression.Expression, replacementFunction);
                return innerExpression == typeBinaryExpression.Expression ?
                            typeBinaryExpression :
                            Expression.TypeIs(innerExpression, typeBinaryExpression.TypeOperand);
            }
            else if (expression is UnaryExpression)
            {
                UnaryExpression unaryExpression = expression as UnaryExpression;
                Expression innerExpression = ReplaceExpressionParts(unaryExpression.Operand, replacementFunction);
                return innerExpression == unaryExpression.Operand ? unaryExpression :
                    Expression.MakeUnary(unaryExpression.NodeType, innerExpression, unaryExpression.Type, unaryExpression.Method);
            }

            return expression;
        }

        public static Expression<F> CombineExpressions<F>(Expression<F> expression1, Expression<F> expression2)
        {
            ParameterExpression[] paramArray = new ParameterExpression[expression1.Parameters.Count];
            for (int i = 0; i < paramArray.Length; i++)
            {
                paramArray[i] = Expression.Parameter(expression1.Parameters[i].Type, expression1.Parameters[i].Name);
            }

            return Expression.Lambda<F>(Expression.AndAlso(
                Expression.Invoke(expression1, paramArray),
                Expression.Invoke(expression2, paramArray)),
                paramArray);
        }
    }

    public class ValueComparedExpression<T> where T : Expression
    {
        T _expression;
        int? _hashcode;

        private ValueComparedExpression(T expression)
        {
            _expression = expression;
            _hashcode = default(int?);
        }

        public override bool Equals(object obj)
        {
            if(base.Equals(obj))
                return true;
            if (_hashcode.HasValue &&
                obj is ValueComparedExpression<T> && ((ValueComparedExpression<T>)obj)._hashcode.HasValue &&
                _hashcode.Value != ((ValueComparedExpression<T>)obj)._hashcode.Value)
                return false;

            return Equals(this, obj is ValueComparedExpression<T> ? ((ValueComparedExpression<T>)obj)._expression : obj as T);
        }

        public static bool Equals(Expression a, Expression b)
        {
            if (object.Equals(a, b))
                return true;

            if (a == null || b == null ||
                a.GetType() != b.GetType() ||
                a.NodeType != b.NodeType ||
                a.Type != b.Type)
                return false;

            if (a is BinaryExpression)
            {
                BinaryExpression binaryExpressionA = (BinaryExpression)a;
                BinaryExpression binaryExpressionB = (BinaryExpression)b;
                return binaryExpressionA.IsLifted == binaryExpressionB.IsLifted &&
                        binaryExpressionA.IsLiftedToNull == binaryExpressionB.IsLiftedToNull &&
                        binaryExpressionA.Method == binaryExpressionB.Method &&
                        Equals(binaryExpressionA.Conversion, binaryExpressionB.Conversion) &&
                        Equals(binaryExpressionA.Left, binaryExpressionB.Left) &&
                        Equals(binaryExpressionA.Right, binaryExpressionB.Right);
            }
            else if (a is ConditionalExpression)
            {
                ConditionalExpression conditionalExpressionA = (ConditionalExpression)a;
                ConditionalExpression conditionalExpressionB = (ConditionalExpression)b;
                return Equals(conditionalExpressionA.Test, conditionalExpressionB.Test) &&
                        Equals(conditionalExpressionA.IfTrue, conditionalExpressionB.IfTrue) &&
                        Equals(conditionalExpressionA.IfFalse, conditionalExpressionB.IfFalse);
            }
            else if (a is ConstantExpression)
            {
                return object.Equals(((ConstantExpression)a).Value, ((ConstantExpression)b).Value);
            }
            else if (a is InvocationExpression)
            {
                InvocationExpression invocationExpressionA = (InvocationExpression)a;
                InvocationExpression invocationExpressionB = (InvocationExpression)b;
                if (invocationExpressionA.Arguments.Count != invocationExpressionB.Arguments.Count ||
                    !Equals(invocationExpressionA.Expression, invocationExpressionB.Expression)) return false;
                for (int i = 0; i < invocationExpressionA.Arguments.Count; i++)
                {
                    if (!Equals(invocationExpressionA.Arguments[i], invocationExpressionB.Arguments[i])) return false;
                }
                return true;
            }
            else if (a is LambdaExpression)
            {
                LambdaExpression lambdaExpressionA = (LambdaExpression)a;
                LambdaExpression lambdaExpressionB = (LambdaExpression)b;
                if (lambdaExpressionA.Parameters.Count != lambdaExpressionB.Parameters.Count ||
                    !Equals(lambdaExpressionA.Body, lambdaExpressionB.Body)) return false;
                for (int i = 0; i < lambdaExpressionA.Parameters.Count; i++)
                    if (!Equals(lambdaExpressionA.Parameters[i], lambdaExpressionB.Parameters[i])) return false;

                return true;
            }
            else if (a is ListInitExpression)
            {
                ListInitExpression listInitExpressionA = (ListInitExpression)a;
                ListInitExpression listInitExpressionB = (ListInitExpression)b;
                if (listInitExpressionA.Initializers.Count != listInitExpressionB.Initializers.Count ||
                    !Equals(listInitExpressionA.NewExpression, listInitExpressionA.NewExpression)) return false;
                for (int i = 0; i < listInitExpressionA.Initializers.Count; i++)
                {
                    if (listInitExpressionA.Initializers[i].Arguments.Count != listInitExpressionB.Initializers[i].Arguments.Count ||
                        listInitExpressionA.Initializers[i].AddMethod != listInitExpressionB.Initializers[i].AddMethod)
                        return false;

                    for (int j = 0; j < listInitExpressionA.Initializers[i].Arguments.Count; i++)
                        if (!Equals(listInitExpressionA.Initializers[i].Arguments[j], listInitExpressionB.Initializers[i].Arguments[j]))
                            return false;

                }
                return true;
            }
            else if (a is MemberExpression)
            {
                MemberExpression memberExpressionA = (MemberExpression)a;
                MemberExpression memberExpressionB = (MemberExpression)b;
                return memberExpressionA.Member == memberExpressionB.Member &&
                    Equals(memberExpressionA.Expression, memberExpressionB.Expression);
            }
            else if (a is MemberInitExpression)
            {
                MemberInitExpression memberInitExpressionA = (MemberInitExpression)a;
                MemberInitExpression memberInitExpressionB = (MemberInitExpression)b;

                if (memberInitExpressionA.Bindings.Count != memberInitExpressionB.Bindings.Count ||
                    !Equals(memberInitExpressionA.NewExpression, memberInitExpressionB.NewExpression))
                    return false;

                for (int i = 0; i < memberInitExpressionA.Bindings.Count; i++)
                    if (memberInitExpressionA.Bindings[i].BindingType != memberInitExpressionB.Bindings[i].BindingType ||
                        memberInitExpressionA.Bindings[i].Member != memberInitExpressionB.Bindings[i].Member)
                        return false;

                return true;
            }
            else if (a is MethodCallExpression)
            {
                MethodCallExpression methodCallExpressionA = a as MethodCallExpression;
                MethodCallExpression methodCallExpressionB = b as MethodCallExpression;

                if (methodCallExpressionA.Method != methodCallExpressionB.Method ||
                    methodCallExpressionA.Arguments.Count != methodCallExpressionA.Arguments.Count ||
                    !Equals(methodCallExpressionA.Object, methodCallExpressionA.Object))
                    return false;

                for (int i = 0; i < methodCallExpressionA.Arguments.Count; i++)
                    if (!Equals(methodCallExpressionA.Arguments[i], methodCallExpressionB.Arguments[i]))
                        return false;

                return true;
            }
            else if (a is NewArrayExpression)
            {
                NewArrayExpression newArrayExpressionA = (NewArrayExpression)a;
                NewArrayExpression newArrayExpressionB = (NewArrayExpression)b;
                if (newArrayExpressionA.Expressions.Count != newArrayExpressionB.Expressions.Count)
                    return false;

                for (int i = 0; i < newArrayExpressionA.Expressions.Count; i++)
                    if (!Equals(newArrayExpressionA.Expressions[i], newArrayExpressionB.Expressions[i]))
                        return false;

                return true;
            }
            else if (a is NewExpression)
            {
                NewExpression newExpressionA = (NewExpression)a;
                NewExpression newExpressionB = (NewExpression)b;

                if (newExpressionA.Constructor != newExpressionB.Constructor ||
                    newExpressionA.Arguments.Count != newExpressionB.Arguments.Count ||
                    ((newExpressionA.Members == null) != (newExpressionB.Members == null)) ||
                    (newExpressionA.Members != null && newExpressionA.Members.Count != newExpressionB.Members.Count))
                    return false;

                if (newExpressionA.Members != null)
                    for (int i = 0; i < newExpressionA.Members.Count; i++)
                        if (newExpressionA.Members[i] != newExpressionB.Members[i])
                            return false;

                for (int i = 0; i < newExpressionA.Arguments.Count; i++)
                    if (!Equals(newExpressionA.Arguments[i], newExpressionB.Arguments[i]))
                        return false;

                return true;
            }
            else if (a is ParameterExpression)
            {
                return true;
            }
            else if (a is TypeBinaryExpression)
            {
                TypeBinaryExpression typeBinaryExpressionA = (TypeBinaryExpression)a;
                TypeBinaryExpression typeBinaryExpressionB = (TypeBinaryExpression)b;
                return typeBinaryExpressionA.TypeOperand == typeBinaryExpressionB.TypeOperand &&
                    Equals(typeBinaryExpressionA.Expression, typeBinaryExpressionA.Expression);
            }
            else if (a is UnaryExpression)
            {
                UnaryExpression unaryExpressionA = (UnaryExpression)a;
                UnaryExpression unaryExpressionB = (UnaryExpression)b;
                return unaryExpressionA.IsLifted == unaryExpressionB.IsLifted &&
                                                    unaryExpressionA.IsLiftedToNull == unaryExpressionB.IsLiftedToNull &&
                                                    unaryExpressionA.Method == unaryExpressionB.Method &&
                                                    Equals(unaryExpressionA.Operand, unaryExpressionB.Operand);
            }
            else
            {
                throw new NotSupportedException("Expression is not supported.");
            }
        }

        public override int GetHashCode()
        {
            return _hashcode.HasValue ? _hashcode.Value : (_hashcode = GetHashCode(_expression)).Value;
        }

        private static int GetHashCode(Object obj)
        {
            if (obj == null)
                return 0;
            else if (obj is Expression)
                return GetHashCode((Expression)obj);
            else
                return obj.GetHashCode();
        }

        private static int GetHashCode(Expression expression)
        {
            if (expression == null)
                return 0;

            int returnValue = expression.NodeType.GetHashCode() ^
                expression.Type.GetHashCode();

            if (expression is BinaryExpression)
            {
                BinaryExpression binaryExpression = expression as BinaryExpression;
                returnValue ^=
                        GetHashCode(binaryExpression.Method) ^
                        GetHashCode(binaryExpression.Conversion) ^
                        GetHashCode(binaryExpression.Left) ^
                        GetHashCode(binaryExpression.Right);
                returnValue = binaryExpression.IsLifted ^ binaryExpression.IsLiftedToNull ? returnValue : ~returnValue;
            }
            else if (expression is ConditionalExpression)
            {
                ConditionalExpression conditionalExpression = expression as ConditionalExpression;
                returnValue ^= GetHashCode(conditionalExpression.Test) ^
                                GetHashCode(conditionalExpression.IfTrue) ^
                                GetHashCode(conditionalExpression.IfFalse);
            }
            else if (expression is ConstantExpression)
            {
                returnValue ^= GetHashCode(((ConstantExpression)expression).Value);
            }
            else if (expression is InvocationExpression)
            {
                InvocationExpression invocationExpression = (InvocationExpression)expression;
                returnValue ^= GetHashCode(invocationExpression.Expression);
                foreach (Expression exp in invocationExpression.Arguments)
                    returnValue ^= GetHashCode(exp);
            }
            else if (expression is LambdaExpression)
            {
                LambdaExpression lambdaExpression = expression as LambdaExpression;
                return GetHashCode(lambdaExpression.Body);
            }
            else if (expression is ListInitExpression)
            {
                ListInitExpression listInitExpression = expression as ListInitExpression;
                returnValue ^= GetHashCode(listInitExpression.NewExpression);
                foreach (ElementInit exp in listInitExpression.Initializers)
                    foreach (Expression arg in exp.Arguments)
                        returnValue ^= GetHashCode(arg);
            }
            else if (expression is MemberExpression)
            {
                MemberExpression memberExpression = (MemberExpression)expression;
                returnValue ^= GetHashCode(memberExpression.Member) ^ GetHashCode(memberExpression.Expression);
            }
            else if (expression is MemberInitExpression)
            {
                MemberInitExpression memberInitExpression = (MemberInitExpression)expression;
                foreach (MemberBinding binding in memberInitExpression.Bindings)
                    returnValue ^= binding.BindingType.GetHashCode() ^ binding.Member.GetHashCode();
                returnValue ^= GetHashCode(memberInitExpression.NewExpression);
            }
            else if (expression is MethodCallExpression)
            {
                MethodCallExpression methodCallExpression = (MethodCallExpression)expression;
                returnValue ^=
                    GetHashCode(methodCallExpression.Object) ^
                    GetHashCode(methodCallExpression.Method);
                foreach (Expression exp in methodCallExpression.Arguments)
                    returnValue ^= GetHashCode(exp);
            }
            else if (expression is NewArrayExpression)
            {
                NewArrayExpression newArrayExpression = (NewArrayExpression)expression;
                foreach (Expression exp in newArrayExpression.Expressions)
                    returnValue ^= GetHashCode(exp);
            }
            else if (expression is NewExpression)
            {
                NewExpression newExpression = (NewExpression)expression;
                returnValue ^= GetHashCode(newExpression.Constructor);
                if (newExpression.Members != null)
                    foreach (System.Reflection.MemberInfo memberInfo in newExpression.Members)
                        returnValue ^= GetHashCode(memberInfo);
                foreach (Expression exp in newExpression.Arguments)
                    returnValue ^= GetHashCode(exp);
            }
            else if (expression is TypeBinaryExpression)
            {
                TypeBinaryExpression typeBinaryExpression = (TypeBinaryExpression)expression;
                returnValue ^= GetHashCode(typeBinaryExpression.TypeOperand) ^ GetHashCode(typeBinaryExpression.Expression);
            }
            else if (expression is UnaryExpression)
            {
                UnaryExpression unaryExpression = (UnaryExpression)expression;
                if (unaryExpression.IsLifted ^ unaryExpression.IsLiftedToNull)
                    returnValue = ~returnValue;
                returnValue ^= GetHashCode(unaryExpression.Method) ^ GetHashCode(unaryExpression.Operand);
            }
            return returnValue;
        }

        public static implicit operator ValueComparedExpression<T>(T expression)
        {
            return new ValueComparedExpression<T>(expression);
        }

        public static implicit operator T(ValueComparedExpression<T> expression)
        {
            return expression._expression;
        }

        public static bool operator ==(ValueComparedExpression<T> a, object b)
        {
            return a.Equals(b);
        }
        
        public static bool operator !=(ValueComparedExpression<T> a, object b)
        {
            return !a.Equals(b);
        }
    }
}
