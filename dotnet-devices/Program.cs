using System.CommandLine;
using System.CommandLine.Parsing;
using System.Threading.Tasks;
using DotNetDevices.Commands;

namespace DotNetDevices
{
    public class Program
    {
        public static async Task<int> Main(string[] args) =>
            await CommandLine.Create().InvokeAsync(args);
    }
}
