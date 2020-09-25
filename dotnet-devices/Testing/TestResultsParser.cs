using System;
using System.Collections.Generic;
using DotNetDevices.Processes;

namespace DotNetDevices.Testing
{
    public class TestResultsParser
    {
        private readonly List<string> passedList = new List<string>();
        private readonly List<string> skippedList = new List<string>();
        private readonly List<string> failedList = new List<string>();

        public void ParseTestOutput(ProcessOutput output, Action<string>? onLog, Action? onComplete)
        {
            var line = output.Data;

            if (GetValue("[PASS]", out var fullPass, out var pass))
            {
                passedList.Add(pass);
                onLog?.Invoke(fullPass);
            }
            else if (GetValue("[SKIPPED]", out var fullSkipped, out var skipped))
            {
                skippedList.Add(skipped);
                onLog?.Invoke(fullSkipped);
            }
            else if (GetValue("[IGNORED]", out var fullIgnored, out var ignored))
            {
                skippedList.Add(ignored);
                onLog?.Invoke(fullIgnored);
            }
            else if (GetValue("[INCONCLUSIVE]", out var fullInconclusive, out var inconclusive))
            {
                skippedList.Add(inconclusive);
                onLog?.Invoke(fullInconclusive);
            }
            else if (GetValue("[FAIL]", out var fullFail, out var fail))
            {
                failedList.Add(fail);
                onLog?.Invoke(fullFail);
            }
            else if (GetValue("Tests run: ", out var fullTotals, out var totals))
            {
                onComplete?.Invoke();
                onLog?.Invoke(fullTotals);
            }

            bool GetValue(string type, out string full, out string name)
            {
                var idx = line.IndexOf(type, StringComparison.Ordinal);
                if (idx == -1)
                {
                    full = null;
                    name = null;
                    return false;
                }

                var subs = line.Substring(idx).Trim();
                if (string.IsNullOrEmpty(subs))
                {
                    full = null;
                    name = null;
                    return false;
                }

                full = subs;
                name = line.Substring(idx + type.Length).Trim();
                return true;
            }
        }
    }
}
