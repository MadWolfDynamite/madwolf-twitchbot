using MadWolfTwitchBot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MadWolfTwitchBot.Client.Model
{
    public class BasicCommand
    {
        public long Id { get; set; }

        public string Name { get; set; }
        public string ResponseMessage { get; set; }

        public long? BotId { get; set; }

        public BasicCommand() : this(new Command()) { }

        public BasicCommand(Command command)
        {
            Id = command.Id;

            Name = command.Name;
            ResponseMessage = command.ResponseMessage;

            BotId = command.BotId;
        }
    }
}
