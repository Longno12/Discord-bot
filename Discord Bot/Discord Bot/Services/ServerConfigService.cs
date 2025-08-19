using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using MyAwesomeBot.Models;

namespace MyAwesomeBot.Services
{
    public class ServerConfigService
    {
        private const string ConfigFilePath = "server_configs.json";
        public Dictionary<ulong, ServerConfig> ServerConfigs { get; private set; }

        public ServerConfigService()
        {
            ServerConfigs = new Dictionary<ulong, ServerConfig>();
        }

        public async Task LoadConfigsAsync()
        {
            if (!File.Exists(ConfigFilePath))
            {
                ServerConfigs = new Dictionary<ulong, ServerConfig>();
                return;
            }

            var json = await File.ReadAllTextAsync(ConfigFilePath);
            ServerConfigs = JsonSerializer.Deserialize<Dictionary<ulong, ServerConfig>>(json) ?? new Dictionary<ulong, ServerConfig>();
        }

        public async Task SaveConfigsAsync()
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(ServerConfigs, options);
            await File.WriteAllTextAsync(ConfigFilePath, json);
        }

        public ServerConfig GetServerConfig(ulong guildId)
        {
            if (!ServerConfigs.ContainsKey(guildId))
            {
                ServerConfigs[guildId] = new ServerConfig();
            }
            return ServerConfigs[guildId];
        }
    }
}