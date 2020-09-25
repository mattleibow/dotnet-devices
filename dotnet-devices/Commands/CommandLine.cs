using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;

namespace DotNetDevices.Commands
{
    public static class CommandLine
    {
        public static RootCommand Create()
        {
            var command = new RootCommand
            {
                AppleCommand.Create(),
                TestCommand.Create(),
            };

            command.Name = "dotnet-device";

            return command;
        }

        public static Option CreateVerbosity() =>
            new Option(new[] { "--verbosity", "-v" }, "Set the verbosity level. Allowed values are: [q]uiet, [m]inimal, [n]ormal, [d]etailed, and [d]iagnostic.")
            {
                Argument = new Argument<string?>()
                { Arity = ArgumentArity.ExactlyOne }
                    .FromAmong("q", "quiet", "m", "minimal", "n", "normal", "d", "detailed", "diag", "diagnostic")
            };

        public static Command WithHandler(this Command command, ICommandHandler handler)
        {
            command.Handler = handler;
            return command;
        }

        public static string? ParseVersion(ArgumentResult result)
        {
            var version = result.Tokens[0].Value;

            if (Version.TryParse(version, out _))
                return version;

            if (int.TryParse(version, out _))
                return version;

            result.ErrorMessage = "The runtime version number must be in either <major> or <major>.<minor> version formats.";
            return null;
        }
    }
}
