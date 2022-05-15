using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MadWolfTwitchBot.Models.Twitch
{
    public class Channel
    {
        public string Id { get; set; }
        public string Title { get; set; }

        public string Game_Id { get; set; }

        public bool Is_Live { get; set; }
        public string Started_At { get; set; }
    }
}
