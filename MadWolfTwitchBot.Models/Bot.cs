using MadWolfTools.Common.AttributeExtension;
using System;

namespace MadWolfTwitchBot.Models
{
    public class Bot
    {
        public long Id { get; set; }

        public string Username { get; set; }
        [DbColumn("display_name")]
        public string DisplayName { get; set; }

        [DbColumn("oauth_token")]
        public string OAuthToken { get; set; }
        [DbColumn("refresh_token")]
        public string RefreshToken { get; set; }
        [DbColumn("token_timestamp", Convert = true)]
        public DateTime? TokenTimestamp { get; set; }

        [DbColumn("channel_id")]
        public long? ChannelId { get; set; }
    }
}
