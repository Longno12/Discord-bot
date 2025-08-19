using DSharpPlus.Entities;
using System.Collections.Generic;

namespace MyAwesomeBot.Models
{
    public class ServerConfig
    {
        public ulong? LogChannelId { get; set; } = null;
        public ulong? WelcomeChannelId { get; set; } = null;
        public string WelcomeMessage { get; set; } = "Welcome to the server, {user}!";
    }
}