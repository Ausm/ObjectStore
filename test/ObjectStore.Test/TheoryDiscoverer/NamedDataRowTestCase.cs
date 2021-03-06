﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace ObjectStore.Test
{
    [DebuggerDisplay(@"\{ class = {TestMethod.TestClass.Class.Name}, method = {TestMethod.Method.Name}, display = {DisplayName}, skip = {SkipReason} \}")]
    public class NamedDataRowTestCase : TestMethodTestCase, IXunitTestCase
    {
        readonly IMessageSink diagnosticMessageSink;
        int _attributeNumber;
        string _rowName;

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Called by the de-serializer; should only be called by deriving classes for de-serialization purposes")]
        public NamedDataRowTestCase()
        {
            diagnosticMessageSink = new NullMessageSink();
        }

        public NamedDataRowTestCase(IMessageSink diagnosticMessageSink,
                             TestMethodDisplay defaultMethodDisplay,
                             TestMethodDisplayOptions defaultMethodDisplayOptions,
                             ITestMethod testMethod,
                             int attributeNumber,
                             string rowName)
            : base(defaultMethodDisplay, defaultMethodDisplayOptions, testMethod, GetTestMethodArguments(testMethod, attributeNumber, rowName, diagnosticMessageSink))
        {
            _attributeNumber = attributeNumber;
            _rowName = rowName;
            this.diagnosticMessageSink = diagnosticMessageSink;
        }

        protected virtual string GetDisplayName(IAttributeInfo factAttribute, string displayName)
            => TestMethod.Method.GetDisplayNameWithArguments(displayName, TestMethodArguments, MethodGenericTypes);

        protected virtual string GetSkipReason(IAttributeInfo factAttribute)
            => factAttribute.GetNamedArgument<string>("Skip");

        /// <inheritdoc/>
        protected override void Initialize()
        {
            base.Initialize();

            var factAttribute = TestMethod.Method.GetCustomAttributes(typeof(Xunit.FactAttribute)).First();
            var baseDisplayName = factAttribute.GetNamedArgument<string>("DisplayName") ?? BaseDisplayName;

            DisplayName = GetDisplayName(factAttribute, baseDisplayName);
            SkipReason = GetSkipReason(factAttribute);

            foreach (var traitAttribute in GetTraitAttributesData(TestMethod))
            {
                var discovererAttribute = traitAttribute.GetCustomAttributes(typeof(TraitDiscovererAttribute)).FirstOrDefault();
                if (discovererAttribute != null)
                {
                    var discoverer = ExtensibilityPointFactory.GetTraitDiscoverer(diagnosticMessageSink, discovererAttribute);
                    if (discoverer != null)
                        foreach (var keyValuePair in discoverer.GetTraits(traitAttribute))
                            Add(Traits, keyValuePair.Key, keyValuePair.Value);
                }
                else
                    diagnosticMessageSink.OnMessage(new DiagnosticMessage($"Trait attribute on '{DisplayName}' did not have [TraitDiscoverer]"));
            }
        }

        static void Add<TKey, TValue>(IDictionary<TKey, List<TValue>> dictionary, TKey key, TValue value)
        {
            (dictionary[key] ?? (dictionary[key] = new List<TValue>())).Add(value);
        }

        static IEnumerable<IAttributeInfo> GetTraitAttributesData(ITestMethod testMethod)
        {
            return testMethod.TestClass.Class.Assembly.GetCustomAttributes(typeof(ITraitAttribute))
                .Concat(testMethod.Method.GetCustomAttributes(typeof(ITraitAttribute)))
                .Concat(testMethod.TestClass.Class.GetCustomAttributes(typeof(ITraitAttribute)));
        }

        static object[] GetTestMethodArguments(ITestMethod testMethod, int attributeNumber, string rowName, IMessageSink diagnosticMessageSink)
        {
            try
            {
                IAttributeInfo dataAttribute = testMethod.Method.GetCustomAttributes(typeof(DataAttribute)).Where((x, i) => i == attributeNumber).FirstOrDefault();
                if (dataAttribute == null)
                    return null;

                IAttributeInfo discovererAttribute = dataAttribute.GetCustomAttributes(typeof(DataDiscovererAttribute)).First();
                IDataDiscoverer discoverer = ExtensibilityPointFactory.GetDataDiscoverer(diagnosticMessageSink, discovererAttribute);

                IEnumerable<object[]> data = discoverer.GetData(dataAttribute, testMethod.Method);

                if (data is IDictionary<string, object[]>)
                    return ((IDictionary<string, object[]>)data)[rowName];

                return data.Where(x => x[0].ToString() == rowName).FirstOrDefault();
            }
            catch
            {
                return null;
            }
        }

        public override void Serialize(IXunitSerializationInfo data)
        {
            data.AddValue("TestMethod", TestMethod);
            data.AddValue("AttributeNumber", _attributeNumber);
            data.AddValue("RowName", _rowName);
        }

        public override void Deserialize(IXunitSerializationInfo data)
        {
            TestMethod = data.GetValue<ITestMethod>("TestMethod");
            _attributeNumber = data.GetValue<int>("AttributeNumber");
            _rowName = data.GetValue<string>("RowName");
            TestMethodArguments = GetTestMethodArguments(TestMethod, _attributeNumber, _rowName, diagnosticMessageSink);
        }

        protected override string GetUniqueID()
        {
            return $"{TestMethod.TestClass.TestCollection.TestAssembly.Assembly.Name};{TestMethod.TestClass.Class.Name};{TestMethod.Method.Name};{_attributeNumber}/{_rowName}";
        }

        /// <inheritdoc/>
        public virtual Task<RunSummary> RunAsync(IMessageSink diagnosticMessageSink,
                                                 IMessageBus messageBus,
                                                 object[] constructorArguments,
                                                 ExceptionAggregator aggregator,
                                                 CancellationTokenSource cancellationTokenSource)
            => new XunitTestCaseRunner(this, DisplayName, SkipReason, constructorArguments, TestMethodArguments, messageBus, aggregator, cancellationTokenSource).RunAsync();

        public int Timeout => 0;
    }
}
