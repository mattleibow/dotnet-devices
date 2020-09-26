using System.IO;
using System.Reflection;
using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Xunit.Runners.UI;
using Environment = System.Environment;

namespace DeviceTests.Droid
{
    [Activity(MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    public class MainActivity : RunnerActivity
    {
        protected override void OnCreate(Bundle bundle)
        {
            AddExecutionAssembly(Assembly.GetExecutingAssembly());

            AddTestAssembly(typeof(SharedTests).Assembly);

            var path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            ResultChannel = new CombinedResultChannel(Path.Combine(path, "TestResults.trx"));

            AutoStart = true;
            TerminateAfterExecution = true;

            base.OnCreate(bundle);
        }
    }
}
