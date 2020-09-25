using System.Threading.Tasks;
using Xunit.Runners;
using Xunit.Runners.ResultChannels;

namespace DeviceTests
{
    public class CombinedResultChannel : IResultChannel
    {
        private readonly IResultChannel trx;
        private readonly IResultChannel text;

        public CombinedResultChannel(string filename)
        {
            trx = new TrxResultChannel(filename);
            text = new TextWriterResultChannel(null);
        }

        public async Task<bool> OpenChannel(string message = null)
        {
            await trx.OpenChannel();
            return await text.OpenChannel();
        }

        public void RecordResult(TestResultViewModel result)
        {
            trx.RecordResult(result);
            text.RecordResult(result);
        }

        public async Task CloseChannel()
        {
            await trx.CloseChannel();
            await text.CloseChannel();
        }
    }
}
