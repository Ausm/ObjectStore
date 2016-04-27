using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace ObjectStore.Test
{

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    [XunitTestCaseDiscoverer("ObjectStore.Test.ExtTheoryDiscoverer", "ObjectStore.Test")]
    public class ExtTheoryAttribute : FactAttribute { }

    public class ExtTheoryDiscoverer : IXunitTestCaseDiscoverer
    {
        readonly IMessageSink diagnosticMessageSink;

        public ExtTheoryDiscoverer(IMessageSink diagnosticMessageSink)
        {
            this.diagnosticMessageSink = diagnosticMessageSink;
        }

        protected virtual IXunitTestCase CreateTestCaseForDataRow(ITestFrameworkDiscoveryOptions discoveryOptions, ITestMethod testMethod, IAttributeInfo theoryAttribute, int dataAttributeNumber, int dataRowNumber)
            => new NumberedDataRowTestCase(diagnosticMessageSink, discoveryOptions.MethodDisplayOrDefault(), testMethod, dataAttributeNumber, dataRowNumber);

        protected virtual IXunitTestCase CreateTestCaseForNamedDataRow(ITestFrameworkDiscoveryOptions discoveryOptions, ITestMethod testMethod, IAttributeInfo theoryAttribute, int dataAttributeNumber, string dataRowName)
            => new NamedDataRowTestCase(diagnosticMessageSink, discoveryOptions.MethodDisplayOrDefault(), testMethod, dataAttributeNumber, dataRowName);

        protected virtual IXunitTestCase CreateTestCaseForSkip(ITestFrameworkDiscoveryOptions discoveryOptions, ITestMethod testMethod, IAttributeInfo theoryAttribute, string skipReason)
            => new Xunit.Sdk.XunitTestCase(diagnosticMessageSink, discoveryOptions.MethodDisplayOrDefault(), testMethod);

        protected virtual IXunitTestCase CreateTestCaseForTheory(ITestFrameworkDiscoveryOptions discoveryOptions, ITestMethod testMethod, IAttributeInfo theoryAttribute)
            => new XunitTheoryTestCase(diagnosticMessageSink, discoveryOptions.MethodDisplayOrDefault(), testMethod);

        protected virtual IXunitTestCase CreateTestCaseForSkippedDataRow(ITestFrameworkDiscoveryOptions discoveryOptions, ITestMethod testMethod, IAttributeInfo theoryAttribute, object[] dataRow, string skipReason)
            => new XunitSkippedDataRowTestCase(diagnosticMessageSink, discoveryOptions.MethodDisplayOrDefault(), testMethod, skipReason, dataRow);

        public virtual IEnumerable<IXunitTestCase> Discover(ITestFrameworkDiscoveryOptions discoveryOptions, ITestMethod testMethod, IAttributeInfo theoryAttribute)
        {
            var skipReason = theoryAttribute.GetNamedArgument<string>("Skip");
            if (skipReason != null)
                return new[] { CreateTestCaseForSkip(discoveryOptions, testMethod, theoryAttribute, skipReason) };

            if (discoveryOptions.PreEnumerateTheoriesOrDefault())
            {
                try
                {
                    //System.Diagnostics.Debugger.Launch();

                    var dataAttributes = testMethod.Method.GetCustomAttributes(typeof(DataAttribute)).ToList();
                    var results = new List<IXunitTestCase>();

                    for(int dataAttribute = 0; dataAttribute < dataAttributes.Count; dataAttribute++)
                    {
                        var discovererAttribute = dataAttributes[dataAttribute].GetCustomAttributes(typeof(DataDiscovererAttribute)).First();
                        var discoverer = ExtensibilityPointFactory.GetDataDiscoverer(diagnosticMessageSink, discovererAttribute);
                        skipReason = dataAttributes[dataAttribute].GetNamedArgument<string>("Skip");

                        if (!discoverer.SupportsDiscoveryEnumeration(dataAttributes[dataAttribute], testMethod.Method))
                            return new[] { CreateTestCaseForTheory(discoveryOptions, testMethod, theoryAttribute) };

                        IEnumerable<object[]> data = discoverer.GetData(dataAttributes[dataAttribute], testMethod.Method).ToList();

                        if (data is IDictionary<string, object[]>)
                        {
                            foreach (KeyValuePair<string, object[]> dataRow in (IDictionary<string, object[]>)data)
                            {
                                var testCase =
                                    skipReason != null
                                        ? CreateTestCaseForSkippedDataRow(discoveryOptions, testMethod, theoryAttribute, dataRow.Value, skipReason)
                                        : CreateTestCaseForNamedDataRow(discoveryOptions, testMethod, theoryAttribute, dataAttribute, dataRow.Key);

                                results.Add(testCase);
                            }
                        }
                        else if (data.GroupBy(x => (x[0]?.ToString()) ?? string.Empty).All(x => x.Count() == 1))
                        {
                            foreach (object[] dataRow in data)
                            {
                                var testCase =
                                    skipReason != null
                                        ? CreateTestCaseForSkippedDataRow(discoveryOptions, testMethod, theoryAttribute, dataRow, skipReason)
                                        : CreateTestCaseForNamedDataRow(discoveryOptions, testMethod, theoryAttribute, dataAttribute, dataRow[0].ToString() ?? string.Empty);

                                results.Add(testCase);
                            }
                        }
                        else
                        {
                            results.AddRange(
                                data.Select((x, i) =>  
                                    skipReason != null ? 
                                        CreateTestCaseForSkippedDataRow(discoveryOptions, testMethod, theoryAttribute, x, skipReason) : 
                                        CreateTestCaseForDataRow(discoveryOptions, testMethod, theoryAttribute, dataAttribute, i)));

                        }
                    }

                    if (results.Count == 0)
                        results.Add(new ExecutionErrorTestCase(diagnosticMessageSink,
                                                               discoveryOptions.MethodDisplayOrDefault(),
                                                               testMethod,
                                                               $"No data found for {testMethod.TestClass.Class.Name}.{testMethod.Method.Name}"));

                    return results;
                }
                catch (Exception ex)    // If something goes wrong, fall through to return just the XunitTestCase
                {
                    diagnosticMessageSink.OnMessage(new Xunit.Sdk.DiagnosticMessage($"Exception thrown during theory discovery on '{testMethod.TestClass.Class.Name}.{testMethod.Method.Name}'; falling back to single test case.{Environment.NewLine}{ex}"));
                }
            }

            return new[] { CreateTestCaseForTheory(discoveryOptions, testMethod, theoryAttribute) };
        }
    }
}
