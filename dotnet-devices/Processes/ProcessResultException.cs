using System;

namespace DotNetDevices.Processes
{
    public class ProcessResultException : Exception
    {
        public ProcessResultException(ProcessResult? processResult = null)
        {
            ProcessResult = processResult;
        }

        public ProcessResultException(string? message, ProcessResult? processResult = null)
            : base(message)
        {
            ProcessResult = processResult;
        }

        public ProcessResultException(string? message, Exception? innerException, ProcessResult? processResult = null)
            : base(message, innerException)
        {
            ProcessResult = processResult;
        }

        public ProcessResult? ProcessResult { get; }
    }
}
