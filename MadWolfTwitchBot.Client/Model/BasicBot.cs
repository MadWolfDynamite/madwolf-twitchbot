using MadWolfTwitchBot.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace MadWolfTwitchBot.Client.Model
{
    public class BasicBot
    {
        public long Id { get; set; }

        public string Username { get; set; }
        public string DisplayName { get; set; }

        public string OAuthToken { get; set; }
        public string RefreshToken { get; set; }
        public DateTime? TokenTimestamp { get; set; }

        public long? ChannelId { get; set; }

        public BasicBot() : this(new Bot()) { }
        public BasicBot(Bot bot)
        {
            Id = bot.Id;

            Username = bot.Username;
            DisplayName = bot.DisplayName;

            OAuthToken = bot.OAuthToken;
            RefreshToken = bot.RefreshToken;
            TokenTimestamp = bot.TokenTimestamp;

            ChannelId = bot.ChannelId;
        }
    }
}
