using System;
using System.IO;
using System.Reflection;
using Foundation;
using UIKit;
using Xunit.Runners.ResultChannels;

namespace DeviceTests.iOS
{
    [Register(nameof(AppDelegate))]
    public partial class AppDelegate : Xunit.Runner.RunnerAppDelegate
    {
        public static void Main(string[] args) =>
            UIApplication.Main(args, null, nameof(AppDelegate));

        public override bool FinishedLaunching(UIApplication app, NSDictionary options)
        {
            //// Invoke the headless test runner if a config was specified
            //var testCfg = System.IO.File.ReadAllText("tests.cfg")?.Split(':');
            //if (testCfg != null && testCfg.Length > 1)
            //{
            //    var ip = testCfg[0];
            //    if (int.TryParse(testCfg[1], out var port))
            //    {
            //        // Run the headless test runner for CI
            //        Task.Run(() =>
            //        {
            //            return Tests.RunAsync(new TestOptions
            //            {
            //                Assemblies = new List<Assembly> { typeof(Tests).Assembly },
            //                NetworkLogHost = ip,
            //                NetworkLogPort = port,
            //                Filters = Traits.GetCommonTraits(),
            //                Format = TestResultsFormat.XunitV2
            //            });
            //        });
            //    }
            //}

            var v = Environment.GetEnvironmentVariable("TEST_VAR");
            Console.WriteLine($"TEST_VAR = {v}");

            AddExecutionAssembly(Assembly.GetExecutingAssembly());

            AddTestAssembly(typeof(SharedTests).Assembly);

            var path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            Console.WriteLine(path);

            ResultChannel = new CombinedResultChannel(Path.Combine(path, "TestResults.trx"));

            AutoStart = true;
            TerminateAfterExecution = true;

            return base.FinishedLaunching(app, options);
        }
    }
}
