using System;

namespace DotNetDevices.Processes
{
    public class ProcessResultException : Exception
    {
        public ProcessResultException(ProcessResult processResult)
        {
            ProcessResult = processResult;
        }

        public ProcessResultException(ProcessResult processResult, string? message)
            : base(message)
        {
            ProcessResult = processResult;
        }

        public ProcessResultException(ProcessResult processResult, string? message, Exception? innerException)
            : base(message, innerException)
        {
            ProcessResult = processResult;
        }

        public ProcessResult ProcessResult { get; }
    }
}
