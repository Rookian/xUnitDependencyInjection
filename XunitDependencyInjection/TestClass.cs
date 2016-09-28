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
        public int Add(int a, int b)
        {
            return a + b;
        }
    }

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
        protected IMessageSink DiagnosticMessageSink { get; }

        public CustomXunitTestCollectionRunner(ITestCollection testCollection, IEnumerable<IXunitTestCase> testCases,
            IMessageSink diagnosticMessageSink, IMessageBus messageBus, ITestCaseOrderer testCaseOrderer,
            ExceptionAggregator aggregator, CancellationTokenSource cancellationTokenSource)
            : base(
                testCollection, testCases, diagnosticMessageSink, messageBus, testCaseOrderer, aggregator,
                cancellationTokenSource)
        {
            DiagnosticMessageSink = diagnosticMessageSink;
        }

        protected override async Task<RunSummary> RunTestClassAsync(ITestClass testClass, IReflectionTypeInfo @class,
            IEnumerable<IXunitTestCase> testCases)
        {
            // wird nur einmal aufgerufen für mehrere Facts
            var dependencyResolver = GetContainer();
            var summary = await new CustomXunitTestClassRunner(testClass, @class, testCases, DiagnosticMessageSink, MessageBus,
                    TestCaseOrderer, new ExceptionAggregator(Aggregator), CancellationTokenSource, CollectionFixtureMappings, dependencyResolver)
                .RunAsync();

            dependencyResolver.Dispose();
            return summary;
        }

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

    public class CustomXunitTestClassRunner : XunitTestClassRunner
    {
        private readonly IDependencyResolver _dependencyResolver;

        public CustomXunitTestClassRunner(ITestClass testClass, IReflectionTypeInfo @class,
            IEnumerable<IXunitTestCase> testCases, IMessageSink diagnosticMessageSink, IMessageBus messageBus,
            ITestCaseOrderer testCaseOrderer, ExceptionAggregator aggregator,
            CancellationTokenSource cancellationTokenSource, IDictionary<Type, object> collectionFixtureMappings, IDependencyResolver dependencyResolver)
            : base(
                testClass, @class, testCases, diagnosticMessageSink, messageBus, testCaseOrderer, aggregator,
                cancellationTokenSource, collectionFixtureMappings)
        {
            _dependencyResolver = dependencyResolver;
        }

        protected override bool TryGetConstructorArgument(ConstructorInfo constructor, int index,
            ParameterInfo parameter, out object argumentValue)
        {
            try
            {
                argumentValue = _dependencyResolver.GetInstance(parameter.ParameterType);
                return true;
            }
            catch (Exception)
            {
                argumentValue = null;
                return false;
            }
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

