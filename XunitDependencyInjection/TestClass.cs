using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using SimpleInjector;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace XunitDependencyInjection
{
    public class TestClass
    {
        private readonly IDepdencyA _depdencyA;
        private readonly IDepdencyB _depdencyB;
        private readonly ICalculator _calculator;

        public TestClass(IDepdencyA depdencyA, IDepdencyB depdencyB, ICalculator calculator)
        {
            _depdencyA = depdencyA;
            _depdencyB = depdencyB;
            _calculator = calculator;
        }

        [Fact]
        public void Should_()
        {
            Assert.True(true);
        }

        [Fact]
        public void Should()
        {
            Assert.Equal("Hello World!", _depdencyA.Bla() + _depdencyB.Bla());
        }

        [Theory]
        [InlineData(1, 2, 3)]
        [InlineData(3, 4, 7)]
        public void Should2(int a, int b, int expected)
        {
            Assert.Equal(expected, _calculator.Add(a, b));
        }
    }


    public interface IDepdencyA
    {
        string Bla();
    }

    public interface IDepdencyB
    {
        string Bla();
    }

    public class DepdencyA : IDepdencyA
    {
        public string Bla()
        {
            return "Hello World";
        }
    }

    public class DepdencyB : IDepdencyB
    {
        public string Bla()
        {
            return "!";
        }
    }

    public interface ICalculator
    {
        int Add(int a, int b);
    }

    public class Calculator : ICalculator
    {
        private readonly IDepdencyA _depdencyA;

        public Calculator(IDepdencyA depdencyA)
        {
            _depdencyA = depdencyA;
        }
        public int Add(int a, int b)
        {
            return a + b;
        }
    }



    [TestFrameworkDiscoverer("XunitDependencyInjection.CustomDependcyInjectionDiscoverer", "XunitDependencyInjection")]
    [AttributeUsage(AttributeTargets.Assembly)]
    public class XunitWithDependencyInjectionAttribute : Attribute, ITestFrameworkAttribute
    {
        public Type BootsTrapperType { get; set; }
    }

    public class CustomDependcyInjectionDiscoverer : ITestFrameworkTypeDiscoverer
    {
        public Type GetTestFrameworkType(IAttributeInfo attribute)
        {
            return typeof(CustomXunitTestFramework);
        }
    }

    public class CustomXunitTestFramework : XunitTestFramework
    {
        public CustomXunitTestFramework(IMessageSink messageSink) : base(messageSink)
        {
        }

        protected override ITestFrameworkExecutor CreateExecutor(AssemblyName assemblyName)
            => new CustomXunitTestFrameworkExecutor(assemblyName, SourceInformationProvider, DiagnosticMessageSink);
    }

    public class CustomXunitTestFrameworkExecutor : XunitTestFrameworkExecutor
    {
        public CustomXunitTestFrameworkExecutor(AssemblyName assemblyName,
            ISourceInformationProvider sourceInformationProvider, IMessageSink diagnosticMessageSink)
            : base(assemblyName, sourceInformationProvider, diagnosticMessageSink)
        {
        }

        protected override async void RunTestCases(
            IEnumerable<IXunitTestCase> testCases,
            IMessageSink executionMessageSink,
            ITestFrameworkExecutionOptions executionOptions)
        {
            using (var assemblyRunner = CreateTestAssemblyRunner(testCases, executionMessageSink, executionOptions))
                await assemblyRunner.RunAsync();
        }

        protected virtual TestAssemblyRunner<IXunitTestCase> CreateTestAssemblyRunner(
                IEnumerable<IXunitTestCase> testCases,
                IMessageSink executionMessageSink,
                ITestFrameworkExecutionOptions executionOptions)
            =>
            new CustomXunitTestAssemblyRunner(
                TestAssembly,
                testCases,
                DiagnosticMessageSink,
                executionMessageSink,
                executionOptions);
    }

    public class CustomXunitTestAssemblyRunner : XunitTestAssemblyRunner
    {
        public CustomXunitTestAssemblyRunner(ITestAssembly testAssembly, IEnumerable<IXunitTestCase> testCases,
            IMessageSink diagnosticMessageSink, IMessageSink executionMessageSink,
            ITestFrameworkExecutionOptions executionOptions)
            : base(testAssembly, testCases, diagnosticMessageSink, executionMessageSink, executionOptions)
        {
        }

        protected override Task<RunSummary> RunTestCollectionAsync(IMessageBus messageBus,
                ITestCollection testCollection, IEnumerable<IXunitTestCase> testCases,
                CancellationTokenSource cancellationTokenSource)
            =>
            new CustomXunitTestCollectionRunner(testCollection, testCases, DiagnosticMessageSink, messageBus,
                TestCaseOrderer, new ExceptionAggregator(Aggregator), cancellationTokenSource).RunAsync();
    }

    public class CustomXunitTestCollectionRunner : XunitTestCollectionRunner
    {
        private readonly IMessageSink _diagnosticMessageSink;

        public CustomXunitTestCollectionRunner(ITestCollection testCollection, IEnumerable<IXunitTestCase> testCases,
            IMessageSink diagnosticMessageSink, IMessageBus messageBus, ITestCaseOrderer testCaseOrderer,
            ExceptionAggregator aggregator, CancellationTokenSource cancellationTokenSource)
            : base(
                testCollection, testCases, diagnosticMessageSink, messageBus, testCaseOrderer, aggregator,
                cancellationTokenSource)
        {
            _diagnosticMessageSink = diagnosticMessageSink;
        }

        protected override Task<RunSummary> RunTestClassAsync(ITestClass testClass, IReflectionTypeInfo @class,
            IEnumerable<IXunitTestCase> testCases)
        {
            return new CustomXunitTestClassRunner(testClass, @class, testCases, _diagnosticMessageSink, MessageBus,
                TestCaseOrderer, new ExceptionAggregator(Aggregator), CancellationTokenSource, CollectionFixtureMappings).RunAsync();
        }
    }

    public class CustomXunitTestClassRunner : XunitTestClassRunner
    {
        public CustomXunitTestClassRunner(ITestClass testClass, IReflectionTypeInfo @class,
            IEnumerable<IXunitTestCase> testCases, IMessageSink diagnosticMessageSink, IMessageBus messageBus,
            ITestCaseOrderer testCaseOrderer, ExceptionAggregator aggregator,
            CancellationTokenSource cancellationTokenSource, IDictionary<Type, object> collectionFixtureMappings)
            : base(
                testClass, @class, testCases, diagnosticMessageSink, messageBus, testCaseOrderer, aggregator,
                cancellationTokenSource, collectionFixtureMappings)
        {
        }

        protected override Task<RunSummary> RunTestMethodAsync(ITestMethod testMethod, IReflectionMethodInfo method,
            IEnumerable<IXunitTestCase> testCases, object[] constructorArguments)
            => new CustomXunitTestMethodRunner(SelectTestClassConstructor(), testMethod, Class, method, testCases, DiagnosticMessageSink, MessageBus, new ExceptionAggregator(Aggregator), CancellationTokenSource, constructorArguments).RunAsync();


        protected override bool TryGetConstructorArgument(ConstructorInfo constructor, int index,
            ParameterInfo parameter, out object argumentValue)
        {
            // we just assign null, because we build the constructor parameters later
            // With this we don't support ClassFixtures, CollectionFixtures and TestOutputHelper anymore
            // It might be possible to support them in future
            argumentValue = null;
            return true;
        }
    }

    public class CustomXunitTestMethodRunner : XunitTestMethodRunner
    {
        private readonly ConstructorInfo _constructorInfo;
        private readonly IMessageSink _diagnosticMessageSink;
        private object[] _constructorParameterInstances;

        public CustomXunitTestMethodRunner(ConstructorInfo constructorInfo, ITestMethod testMethod, IReflectionTypeInfo @class,
            IReflectionMethodInfo method, IEnumerable<IXunitTestCase> testCases, IMessageSink diagnosticMessageSink, IMessageBus messageBus,
            ExceptionAggregator aggregator, CancellationTokenSource cancellationTokenSource, object[] constructorArguments) :
                base(testMethod, @class, method, testCases, diagnosticMessageSink, messageBus, aggregator, cancellationTokenSource, constructorArguments)
        {
            _constructorInfo = constructorInfo;
            _diagnosticMessageSink = diagnosticMessageSink;
        }

        protected override async Task<RunSummary> RunTestCasesAsync()
        {
            var summary = new RunSummary();

            foreach (var testCase in TestCases)
            {
                using (var container = GetContainer())
                {
                    _constructorParameterInstances = _constructorInfo.GetParameters()
                        .Select(x => x.ParameterType)
                        .Select(x => container.GetInstance(x))
                        .ToArray();

                    summary.Aggregate(await RunTestCaseAsync(testCase));
                }

                if (CancellationTokenSource.IsCancellationRequested)
                    break;
            }

            return summary;
        }

        protected override Task<RunSummary> RunTestCaseAsync(IXunitTestCase testCase)
            => testCase.RunAsync(_diagnosticMessageSink, MessageBus, _constructorParameterInstances, new ExceptionAggregator(Aggregator), CancellationTokenSource);

        private IDependencyResolver GetContainer()
        {
            var dependencyInjectionAttribute = GetType().Assembly
                .GetCustomAttributes(typeof(XunitWithDependencyInjectionAttribute), false)
                .Cast<XunitWithDependencyInjectionAttribute>().FirstOrDefault();

            if (dependencyInjectionAttribute == null)
                throw new InvalidOperationException($"Can not find an '{nameof(XunitWithDependencyInjectionAttribute)}' for the current assembly.");

            var dependencyResolver = (IDependencyResolver)Activator.CreateInstance(dependencyInjectionAttribute.BootsTrapperType);
            dependencyResolver.Boot();

            return dependencyResolver;
        }
    }

    public interface IDependencyResolver : IDisposable
    {
        object GetInstance(Type type);
        void Boot();
    }

    public class SimpleInjectorBootstrapper : IDependencyResolver
    {
        private readonly Container _container;

        public SimpleInjectorBootstrapper()
        {
            _container = new Container();
        }

        public void Boot()
        {
            _container.Register<IDepdencyA, DepdencyA>();
            _container.Register<IDepdencyB, DepdencyB>();
            _container.Register<ICalculator, Calculator>();
        }

        public void Dispose()
        {
            _container.Dispose();
        }

        public object GetInstance(Type type)
        {
            return _container.GetInstance(type);
        }
    }
}

