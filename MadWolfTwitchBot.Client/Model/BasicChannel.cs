using MadWolfTwitchBot.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace MadWolfTwitchBot.Client.Model
{
    public class BasicChannel
    {
        public long Id { get; set; }

        public string UserName { get; set; }
        public string DisplayName { get; set; }

        public BasicChannel() : this(new Channel()) { }
        public BasicChannel(Channel channel)
        {
            Id = channel.Id;

            UserName = channel.UserName;
            DisplayName = channel.DisplayName;
        }

        public override string ToString()
        {
            return DisplayName;
        }
    }
}
