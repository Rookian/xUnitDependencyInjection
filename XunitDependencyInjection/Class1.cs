using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using SimpleInjector;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace XunitDependencyInjection
{
    public interface IDepdencyA
    {
        string Bla();
    }

    public class DepdencyA: IDepdencyA
    {
        public string Bla()
        {
            return "Hello World";
        }
    }

    public class Class1
    {
        private readonly IDepdencyA _depdencyA;

        public Class1(IDepdencyA depdencyA)
        {
            _depdencyA = depdencyA;
        }

        [DIFact]
        public void Should()
        {
            Assert.Equal("Hello World", _depdencyA.Bla());
        }
    }

    [XunitTestCaseDiscoverer("XunitDependencyInjection.CustomFactDiscoverer", "XunitDependencyInjection")]
    public class DIFactAttribute : FactAttribute
    {
        
    }

    public class CustomFactDiscoverer : IXunitTestCaseDiscoverer
    {
        private readonly IMessageSink _diagnosticMessageSink;

        public CustomFactDiscoverer(IMessageSink diagnosticMessageSink)
        {
            _diagnosticMessageSink = diagnosticMessageSink;
        }

        public IEnumerable<IXunitTestCase> Discover(ITestFrameworkDiscoveryOptions discoveryOptions, ITestMethod testMethod,
            IAttributeInfo factAttribute)
        {
            yield return new CustomXunitTestCase(_diagnosticMessageSink, discoveryOptions.MethodDisplayOrDefault(), testMethod);
        }
    }

    public class CustomXunitTestCase : XunitTestCase
    {
        private readonly ITestMethod _testMethod;

        public CustomXunitTestCase(IMessageSink diagnosticMessageSink, TestMethodDisplay testMethodDisplay, ITestMethod testMethod)
            : base(diagnosticMessageSink, testMethodDisplay, testMethod)
        {
            _testMethod = testMethod;
        }

        public override async Task<RunSummary> RunAsync(IMessageSink diagnosticMessageSink, IMessageBus messageBus,
            object[] constructorArguments, ExceptionAggregator aggregator, CancellationTokenSource cancellationTokenSource)
        {
            aggregator.Clear();
            var summary = await new CustomXunitTestCaseRunner(this, DisplayName, SkipReason, constructorArguments, TestMethodArguments, messageBus, aggregator, cancellationTokenSource).RunAsync();

            return summary;
        }
    }

    public class CustomXunitTestCaseRunner : XunitTestCaseRunner
    {
        public CustomXunitTestCaseRunner(IXunitTestCase testCase, string displayName, string skipReason, object[] constructorArguments, object[] testMethodArguments, IMessageBus messageBus, ExceptionAggregator aggregator, CancellationTokenSource cancellationTokenSource) : base(testCase, displayName, skipReason, constructorArguments, testMethodArguments, messageBus, aggregator, cancellationTokenSource)
        {
        }

        protected override async Task<RunSummary> RunTestAsync()
        {
            Aggregator.Clear();
            // TODO beforeAfterAttributes wird in Xunit private gesetzt und nicht im Property Bug?!
            return await new CustomXunitTestRunner(new XunitTest(TestCase, DisplayName), MessageBus, TestClass, ConstructorArguments, TestMethod, TestMethodArguments, SkipReason, BeforeAfterAttributes, Aggregator, CancellationTokenSource).RunAsync();
        }
    }

    public class CustomXunitTestRunner : XunitTestRunner
    {
        public CustomXunitTestRunner(ITest test,
                               IMessageBus messageBus,
                               Type testClass,
                               object[] constructorArguments,
                               MethodInfo testMethod,
                               object[] testMethodArguments,
                               string skipReason,
                               IReadOnlyList<BeforeAfterTestAttribute> beforeAfterAttributes,
                               ExceptionAggregator aggregator,
                               CancellationTokenSource cancellationTokenSource)
            : base(test, messageBus, testClass, constructorArguments, testMethod, testMethodArguments, skipReason, beforeAfterAttributes, aggregator, cancellationTokenSource)
        {
        }

        protected override async Task<decimal> InvokeTestMethodAsync(ExceptionAggregator aggregator)
        {
            Aggregator.Clear();
            return await new CustomXunitTestInvoker(Test, MessageBus, TestClass, ConstructorArguments, TestMethod, TestMethodArguments, BeforeAfterAttributes, aggregator, CancellationTokenSource).RunAsync();
        }
    }

    public class CustomXunitTestInvoker : XunitTestInvoker
    {
        public CustomXunitTestInvoker(ITest test, IMessageBus messageBus, Type testClass, object[] constructorArguments, MethodInfo testMethod, object[] testMethodArguments, IReadOnlyList<BeforeAfterTestAttribute> beforeAfterAttributes, ExceptionAggregator aggregator, CancellationTokenSource cancellationTokenSource) : base(test, messageBus, testClass, constructorArguments, testMethod, testMethodArguments, beforeAfterAttributes, aggregator, cancellationTokenSource)
        {
        }

        protected override object CreateTestClass()
        {
            var container = ContainerBootstrapper.Boot();
            var testClass = container.GetInstance(TestClass);
            return testClass;
        }
    }

    public static class ContainerBootstrapper
    {
        public static Container Boot()
        {
            var container = new Container();
            container.Register<IDepdencyA, DepdencyA>();
            return container;
        }
    }
}

