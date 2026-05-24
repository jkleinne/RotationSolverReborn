using RotationSolver.Tests;

var tests = new List<TestCase>();
tests.AddRange(PvPTestSuite.Tests);
tests.AddRange(PvETestSuite.Tests);

Environment.Exit(TestRunner.Run(tests));
