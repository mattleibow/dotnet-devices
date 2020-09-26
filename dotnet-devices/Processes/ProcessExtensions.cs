using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace DotNetDevices.Processes
{
    public static class ProcessExtensions
    {
        private const int ThreadStillRunningExitCode = 259;
        private const int ThreadStillRunningRetry = 3;

        public static async Task<ProcessResult> RunAsync(this ProcessStartInfo processStartInfo, string? input = null, Action<ProcessOutput>? handleOutput = null, CancellationToken cancellationToken = default)
        {
            // override some info in order to capture the output
            processStartInfo.UseShellExecute = false;
            processStartInfo.RedirectStandardOutput = true;
            processStartInfo.RedirectStandardError = true;

            if (input != null)
                processStartInfo.RedirectStandardInput = true;

            var process = new Process
            {
                StartInfo = processStartInfo,
                EnableRaisingEvents = true
            };

            var tcs = new TaskCompletionSource<ProcessResult>();

            var stopwatch = new Stopwatch();
            var startTime = DateTime.Now;
            var output = new ConcurrentQueue<ProcessOutput>();

            // attach outputs
            var outputTcs = new TaskCompletionSource<bool>();
            var errorsTcs = new TaskCompletionSource<bool>();
            process.OutputDataReceived += HandleOutputData;
            process.ErrorDataReceived += HandleErrorData;

            // attach exit
            process.Exited += HandleExited;

            // support user cancellation
            using var reg = cancellationToken.Register(() => Terminate(true));

            // start!
            cancellationToken.ThrowIfCancellationRequested();
            stopwatch.Start();
            startTime = DateTime.Now;

            if (process.Start())
            {
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
            }
            else
            {
                tcs.TrySetException(new InvalidOperationException("Failed to start process."));
            }

            if (input != null)
            {
                var write = process.StandardInput.WriteLineAsync(input);
                await Task.WhenAll(tcs.Task, write);
            }

            return await tcs.Task;

            async void HandleExited(object? sender, EventArgs e)
            {
                // if the process is still exiting, give it a little more time
                for (var retries = 0; retries < ThreadStillRunningRetry && process.ExitCode == ThreadStillRunningExitCode; retries++)
                {
                    await Task.Delay(200);
                }

                // if it takes too long, just pretend it exited completely
                var exitCode = process.ExitCode;
                if (exitCode == ThreadStillRunningExitCode)
                    exitCode = 0;

                try
                {
                    startTime = process.StartTime;
                }
                catch (Exception)
                {
                }

                try
                {
                    // wait for all the outputs to finish
                    await Task.WhenAll(outputTcs.Task, errorsTcs.Task).ConfigureAwait(false);

                    var result = new ProcessResult(output.ToArray(), exitCode, startTime, stopwatch.ElapsedMilliseconds);

                    tcs.TrySetResult(result);
                }
                catch (Exception ex)
                {
                    var result = new ProcessResult(output.ToArray(), exitCode, startTime, stopwatch.ElapsedMilliseconds);

                    tcs.TrySetException(new ProcessResultException($"The process threw an exception: {ex.Message}", ex, result));
                }
            }

            void Terminate(bool cancel = false)
            {
                if (cancel)
                    tcs?.TrySetCanceled();

                if (process != null)
                {
                    try
                    {
                        if (!process.HasExited)
                            process.Kill();
                    }
                    catch (InvalidOperationException)
                    {
                    }
                }
            }

            void HandleOutputData(object? sender, DataReceivedEventArgs e)
            {
                if (e.Data == null)
                {
                    outputTcs.TrySetResult(true);
                    return;
                }

                var o = new ProcessOutput(e.Data, stopwatch.ElapsedMilliseconds);

                if (handleOutput != null)
                {
                    try
                    {
                        handleOutput.Invoke(o);
                    }
                    catch (OperationCanceledException)
                    {
                        outputTcs.TrySetCanceled();
                        Terminate();
                        return;
                    }
                    catch (Exception ex)
                    {
                        outputTcs.TrySetException(ex);
                        Terminate();
                        return;
                    }
                }

                output.Enqueue(o);
            }

            void HandleErrorData(object? sender, DataReceivedEventArgs e)
            {
                if (e.Data == null)
                {
                    errorsTcs.TrySetResult(true);
                    return;
                }

                var o = new ProcessOutput(e.Data, stopwatch.ElapsedMilliseconds, true);

                if (handleOutput != null)
                {
                    try
                    {
                        handleOutput.Invoke(o);
                    }
                    catch (OperationCanceledException)
                    {
                        errorsTcs.TrySetCanceled();
                        Terminate();
                        return;
                    }
                    catch (Exception ex)
                    {
                        errorsTcs.TrySetException(ex);
                        Terminate();
                        return;
                    }
                }

                output.Enqueue(o);
            }
        }
    }
}
