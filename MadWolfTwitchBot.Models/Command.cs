using MadWolfTools.Common.AttributeExtension;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MadWolfTwitchBot.Models
{
    public class Command
    {
        public long Id { get; set; }

        public string Name { get; set; }
        [DbColumn("response_message")]
        public string ResponseMessage { get; set; }

        [DbColumn("bot_id")]
        public long? BotId { get; set; }
    }
}
