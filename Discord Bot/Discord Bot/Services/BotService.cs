using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Threading.Tasks;

namespace MyAwesomeBot.Services
{
    public class BotService : IHostedService
    {
        private Bot _bot;

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _bot = new Bot();
            await _bot.RunAsync();
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_bot != null && _bot.Client != null)
            {
                await _bot.Client.DisconnectAsync();
            }
        }
    }
}