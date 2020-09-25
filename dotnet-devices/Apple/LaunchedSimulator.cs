using DotNetDevices.Processes;

namespace DotNetDevices.Apple
{
    public class LaunchedSimulator
    {
        private ProcessResult result;

        public LaunchedSimulator(ProcessResult result)
        {
            this.result = result;
        }

        public string Output => result.Output;
    }
}
