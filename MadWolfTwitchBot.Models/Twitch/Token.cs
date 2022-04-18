using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MadWolfTwitchBot.Models.Twitch
{
    public class Token
    {
        public string Access_Token { get; set; }
        public string Refresh_Token { get; set; }
        public int Expires_In { get; set; }

        public List<string> Scope { get; set; }
        public string Token_Type { get; set; }
    }
}
