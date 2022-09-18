using MadWolfTools.Common.AttributeExtension;

namespace MadWolfTwitchBot.Models
{
    public class BotPromo
    {
        public long Id { get; set; }

        [DbColumn("bot_id")]
        public long BotId { get; set; }

        [DbColumn("response_message")]
        public string ResponseMessage { get; set; }
    }
}
