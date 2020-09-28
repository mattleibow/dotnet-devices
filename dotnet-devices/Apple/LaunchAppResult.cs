using DotNetDevices.Processes;

namespace DotNetDevices.Apple
{
    public class LaunchAppResult
    {
        private ProcessResult result;

        public LaunchAppResult(ProcessResult result)
        {
            this.result = result;
        }

        public string Output => result.Output;
    }
}
