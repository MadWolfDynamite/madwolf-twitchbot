using MadWolfTools.Common.AttributeExtension;
using System;

namespace MadWolfTwitchBot.Models
{
    public class BotHistory
    {
        public long Id { get; set; }

        [DbColumn("bot_id")]
        public long BotId { get; set; }
        [DbColumn("bot_name")]
        public string BotName { get; set; }

        [DbColumn("channel_id")]
        public long ChannelId { get; set; }
        [DbColumn("channel_username")]
        public string ChannelUsername { get; set; }
        [DbColumn("channel_name")]
        public string ChannelName { get; set; }

        [DbColumn("connect_timestamp", Convert = true)]
        public DateTime LastConnection { get; set; }
    }
}
