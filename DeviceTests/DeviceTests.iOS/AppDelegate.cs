using System;
using System.IO;
using System.Reflection;
using Foundation;
using UIKit;

namespace DeviceTests.iOS
{
    [Register(nameof(AppDelegate))]
    public partial class AppDelegate : Xunit.Runner.RunnerAppDelegate
    {
        public static void Main(string[] args) =>
            UIApplication.Main(args, null, nameof(AppDelegate));

        public override bool FinishedLaunching(UIApplication app, NSDictionary options)
        {
            AddExecutionAssembly(Assembly.GetExecutingAssembly());

            AddTestAssembly(typeof(SharedTests).Assembly);

            var path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            ResultChannel = new CombinedResultChannel(Path.Combine(path, "TestResults.trx"));

            AutoStart = true;
            TerminateAfterExecution = true;

            return base.FinishedLaunching(app, options);
        }
    }
}
