using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MadWolfTwitchBot.Client.Constants
{
    public static class SystemBotVariables
    {
        public static readonly List<string> SystemVariables = new() 
        {
            "{bot}",
            "{name}",
            "{channel}",
            "{game}",
            "{uptime}",
        };
    }
}
