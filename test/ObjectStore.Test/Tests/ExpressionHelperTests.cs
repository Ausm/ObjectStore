using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace ObjectStore.Test.Tests
{
    public class ExpressionHelperTests
    {
        #region Subclasses
        class TestClass
        {
            public TestClass(string first, string second)
            {
            }

            public string Third { get; set; }
        }
        #endregion

        public ExpressionHelperTests()
        {
        }

        [ExtTheory, MemberData(nameof(ContainsAnyData))]
        public void ContainsAny(Expression<Func<string, string>> expression, Predicate<Expression> predicate, bool expectedValue)
        {
            bool result = ExpressionHelper.ContainsAny(expression, predicate);
            Assert.Equal(expectedValue, result);
        }

        [ExtTheory, MemberData(nameof(GetFilteredExpressionData))]
        public void GetFilteredExpression(Expression<Func<string, string>> expression, Predicate<Expression> predicate, Action<Expression>[] checks)
        {
            List<Expression> result = ExpressionHelper.GetFilteredExpression(expression, predicate);
            Assert.Collection(result, checks);
        }

        public static TheoryData<Expression<Func<string, string>>, Predicate<Expression>, bool> ContainsAnyData
        {
            get
            {
                TheoryData<Expression<Func<string, string>>, Predicate<Expression>, bool> returnValue = new TheoryData<Expression<Func<string, string>>, Predicate<Expression>, bool>();
                Expression<Func<string, string>> expression;

                #region ConditionalExpression 
                expression = x => string.IsNullOrEmpty(x) ? string.Empty : x;
                returnValue.Add(expression,
                    x => x.NodeType == ExpressionType.MemberAccess && 
                        ((MemberExpression)x).Member.Name == nameof(string.Empty), true);
                returnValue.Add(expression,
                    x => x.NodeType == ExpressionType.MemberAccess &&
                        ((MemberExpression)x).Member.Name == nameof(string.Empty), true);
                returnValue.Add(expression,
                    x => false, false);
                #endregion

                #region InvocationExpression
                expression = x => new Func<string, string>(y => y.Length.ToString())(x);
                returnValue.Add(expression, x => x.NodeType == ExpressionType.Parameter && ((ParameterExpression)x).Name == "x", true);
                returnValue.Add(expression, x => x.NodeType == ExpressionType.Parameter && ((ParameterExpression)x).Name == "y", true);
                returnValue.Add(expression, x => false, false);
                #endregion

                #region ListInitExpression
                expression = Expression.Lambda<Func<string, string>>(
                        Expression.Call(
                            Expression.ListInit(
                                Expression.New(typeof(List<string>).GetTypeInfo().GetConstructor(Type.EmptyTypes)),
                                typeof(List<string>).GetTypeInfo().GetMethod("Add"),
                                Expression.Constant("One"),
                                Expression.Constant("Two"),
                                Expression.Constant("Three")),
                            "ToString", Type.EmptyTypes), Expression.Parameter(typeof(string)));

                returnValue.Add(expression, x => x.NodeType == ExpressionType.New, true);
                returnValue.Add(expression, x => x.NodeType == ExpressionType.Constant && ((ConstantExpression)x).Value as string == "Three", true);
                returnValue.Add(expression, x => false, false);
                #endregion

                #region MemberInitExpression
                expression = x => new TestClass("first", "second") { Third = "third" }.ToString();
                returnValue.Add(expression, x => x.NodeType == ExpressionType.Constant && ((ConstantExpression)x).Value as string == "second", true);
                returnValue.Add(expression, x => x.NodeType == ExpressionType.Constant && ((ConstantExpression)x).Value as string == "third", true);
                returnValue.Add(expression, x => false, false);
                #endregion

                #region NewArrayExpression
                expression = x => (new string[] { "first", "second", "third" })[1];
                returnValue.Add(expression, x => x.NodeType == ExpressionType.Constant && ((ConstantExpression)x).Value as string == "second", true);
                returnValue.Add(expression, x => x.NodeType == ExpressionType.Constant && ((ConstantExpression)x).Value as string == "third", true);
                returnValue.Add(expression, x => false, false);
                #endregion

                #region TypeBinaryExpression
                expression = x => (new object[] { 1, "second", 2L })[2] is string ? "true" : "false";
                returnValue.Add(expression, x => x.NodeType == ExpressionType.Constant && ((ConstantExpression)x).Value as string == "second", true);
                returnValue.Add(expression, x => x.NodeType == ExpressionType.Constant && ((ConstantExpression)x).Value is long, true);
                returnValue.Add(expression, x => false, false);
                #endregion

                return returnValue;
            }
        }

        public static TheoryData<Expression<Func<string, string>>, Predicate<Expression>, Action<Expression>[]> GetFilteredExpressionData
        {
            get
            {
                TheoryData<Expression<Func<string, string>>, Predicate<Expression>, Action<Expression>[]> returnValue = new TheoryData<Expression<Func<string, string>>, Predicate<Expression>, Action<Expression>[]>();
                Expression<Func<string, string>> expression;

                #region ConditionalExpression 
                expression = x => string.IsNullOrEmpty(x) ? string.Empty : x;
                returnValue.Add(expression,
                    x => x.NodeType == ExpressionType.MemberAccess &&
                        ((MemberExpression)x).Member.Name == nameof(string.Empty), new Action<Expression>[] {
                            x => Assert.Equal(typeof(string), ((MemberExpression)x).Member.DeclaringType)
                        });

                returnValue.Add(expression,
                    x => x.NodeType == ExpressionType.MemberAccess &&
                        ((MemberExpression)x).Member.Name == nameof(string.Empty), new Action<Expression>[] {
                            x => Assert.Equal(typeof(string), ((MemberExpression)x).Member.DeclaringType)
                        });

                returnValue.Add(expression,
                    x => false, new Action<Expression>[] { });
                #endregion

                #region InvocationExpression
                expression = x => new Func<string, string>(y => y.Length.ToString())(x);
                returnValue.Add(expression, x => x.NodeType == ExpressionType.Parameter && ((ParameterExpression)x).Name == "x", new Action<Expression>[] {
                            x => Assert.Equal("x", ((ParameterExpression)x).Name)
                        });
                returnValue.Add(expression, x => x.NodeType == ExpressionType.Parameter && ((ParameterExpression)x).Name == "y", new Action<Expression>[] {
                            x => Assert.Equal("y", ((ParameterExpression)x).Name)
                        });
                returnValue.Add(expression, x => false, new Action<Expression>[] { });
                #endregion

                #region ListInitExpression
                expression = Expression.Lambda<Func<string, string>>(
                        Expression.Call(
                            Expression.ListInit(
                                Expression.New(typeof(List<string>).GetTypeInfo().GetConstructor(Type.EmptyTypes)),
                                typeof(List<string>).GetTypeInfo().GetMethod("Add"),
                                Expression.Constant("One"),
                                Expression.Constant("Two"),
                                Expression.Constant("Three")),
                            "ToString", Type.EmptyTypes), Expression.Parameter(typeof(string)));

                returnValue.Add(expression, x => x.NodeType == ExpressionType.New, new Action<Expression>[] {
                    x => Assert.Equal(typeof(List<string>), (x as NewExpression)?.Type)
                });
                returnValue.Add(expression, x => x.NodeType == ExpressionType.Constant && ((ConstantExpression)x).Value as string == "Three", 
                    new Action<Expression>[] {
                        x => Assert.Equal("Three", (x as ConstantExpression)?.Value as string)
                    });
                returnValue.Add(expression, x => false, new Action<Expression>[] { });
                #endregion

                #region MemberInitExpression
                expression = x => new TestClass("first", "second") { Third = "third" }.ToString();
                returnValue.Add(expression, x => x.NodeType == ExpressionType.Constant && ((ConstantExpression)x).Value as string == "second", new Action<Expression>[] {
                        x => Assert.Equal("second", (x as ConstantExpression)?.Value as string)
                    });
                returnValue.Add(expression, x => x.NodeType == ExpressionType.Constant && ((ConstantExpression)x).Value as string == "third", new Action<Expression>[] {
                        x => Assert.Equal("third", (x as ConstantExpression)?.Value as string)
                    });
                returnValue.Add(expression, x => false, new Action<Expression>[] { });
                #endregion

                #region NewArrayExpression
                expression = x => (new string[] { "first", "second", "third" })[1];
                returnValue.Add(expression, x => x.NodeType == ExpressionType.Constant && ((ConstantExpression)x).Value as string == "second", new Action<Expression>[] {
                        x => Assert.Equal("second", (x as ConstantExpression)?.Value as string)
                    });
                returnValue.Add(expression, x => x.NodeType == ExpressionType.Constant && ((ConstantExpression)x).Value as string == "third", new Action<Expression>[] {
                        x => Assert.Equal("third", (x as ConstantExpression)?.Value as string)
                    });
                returnValue.Add(expression, x => false, new Action<Expression>[] { });
                #endregion

                #region TypeBinaryExpression
                expression = x => (new object[] { 1, "second", 2L })[2] is string ? "true" : "false";
                returnValue.Add(expression, x => x.NodeType == ExpressionType.Constant && ((ConstantExpression)x).Value as string == "second", new Action<Expression>[] {
                    x => Assert.Equal("second", (x as ConstantExpression)?.Value as string)
                });
                returnValue.Add(expression, x => x.NodeType == ExpressionType.Constant && ((ConstantExpression)x).Value is long, new Action<Expression>[] {
                    x => Assert.Equal(2L, (long)((ConstantExpression)x).Value)
                });
                returnValue.Add(expression, x => false, new Action<Expression>[] { });
                returnValue.Add(x => (new object[] { }).Length == 0 ? "true" : "false", x => false, new Action<Expression>[] { });
                #endregion

                return returnValue;
            }
        }
    }
}
