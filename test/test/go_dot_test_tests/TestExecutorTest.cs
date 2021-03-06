using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using GoDotTest;
using Moq;
using Shouldly;

// Use the test system to test itself :O
public class TestExecutorTest : TestClass {
  public TestExecutorTest(Node testScene) : base(testScene) { }

  // Test data shared between TestTest and ourselves.
  public static readonly List<string> Called = new();


  [Test]
  public async Task RunsASingleTestSuiteForReal() {
    var methodExecutor = new TestMethodExecutor();
    var testExecutor = new TestExecutor(
      methodExecutor: methodExecutor,
      stopOnError: false,
      sequential: false,
      timeoutMilliseconds: 0
    );

    testExecutor.StopOnError.ShouldBe(false);
    testExecutor.Sequential.ShouldBe(false);
    testExecutor.TimeoutMilliseconds.ShouldBe(0);

    // Typically, you should mock all your dependencies. Testing the test
    // provider would be a real pain, and we know it works or this test
    // wouldn't even be running, so we'll just use one of its static methods.
    // Otherwise, this test would be much more involved.
    var suite = TestProvider.GetTestSuite(typeof(TestTest));
    suite.ShouldBeAssignableTo<ITestSuite>();

    var reporter = new Mock<ITestReporter>();

    await testExecutor.Run(
      TestScene, new List<ITestSuite>() { suite }, reporter.Object
    );

    TestExecutorTest.Called.ShouldBe(new List<string>() {
      "SetupAll",
      "Setup",
      "Test",
      "Cleanup",
      "CleanupAll"
    });
  }

  [Test]
  public async Task RunStopsOnError() {
    var methodExecutor = new Mock<ITestMethodExecutor>(MockBehavior.Strict);

    methodExecutor.Setup(
      exe => exe.Run(
        It.Is<ITestMethod>(
          method => method.Name == nameof(TestTest2.Test1)
        ),
        It.IsAny<TestClass>(),
        It.IsAny<int>()
      )
    ).ThrowsAsync(new InvalidOperationException("Ahem"));

    var testExecutor = new TestExecutor(
      methodExecutor: methodExecutor.Object,
      stopOnError: true,
      sequential: false
    );

    var reporter = new Mock<ITestReporter>();

    var suite = TestProvider.GetTestSuite(typeof(TestTest2));

    await testExecutor.Run(
      sceneRoot: TestScene,
      suites: new List<ITestSuite>() {
        suite
      },
      reporter: reporter.Object
    );

    methodExecutor.VerifyAll();
  }

  [Test]
  public async Task RunSkipsSubsequentOnSequentialWhenErrorOccurs() {
    var methodExecutor = new Mock<ITestMethodExecutor>(MockBehavior.Strict);

    methodExecutor.Setup(
      exe => exe.Run(
        It.Is<ITestMethod>(
          method => method.Name == nameof(TestTest3.Test1)
        ),
        It.IsAny<TestClass>(),
        It.IsAny<int>()
      )
    ).ThrowsAsync(new InvalidOperationException("Ahem"));

    methodExecutor.Setup(
      exe => exe.Run(
        It.Is<ITestMethod>(
          method => method.Name == nameof(TestTest3.CleanupAll)
        ),
        It.IsAny<TestClass>(),
        It.IsAny<int>()
      )
    );

    var testExecutor = new TestExecutor(
      methodExecutor: methodExecutor.Object,
      stopOnError: false,
      sequential: true
    );

    var reporter = new Mock<ITestReporter>();

    var suite = TestProvider.GetTestSuite(typeof(TestTest3));

    await testExecutor.Run(
      sceneRoot: TestScene,
      suites: new List<ITestSuite>() {
        suite
      },
      reporter: reporter.Object
    );
  }
}
